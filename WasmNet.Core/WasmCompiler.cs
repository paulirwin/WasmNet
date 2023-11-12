using System.Diagnostics;
using System.Reflection;
using WasmNet.Core.ILGeneration;

namespace WasmNet.Core;

public class WasmCompiler(IILGenerator il, ModuleInstance module, Type returnType, WasmType type, WasmCode code)
{
    private readonly Stack<Type> _stack = new();
    private readonly Stack<ILLabel> _labels = new();
    
    private int _callArgsLocalIndex = -1, 
        _callTempLocalIndex = -1,
        _globalTempLocalIndex = -1,
        _callIndirectElementLocalIndex = -1,
        _memoryStoreLoadOffsetLocalIndex = -1,
        _memoryStoreIntLocalIndex = -1,
        _memoryStoreLongLocalIndex = -1,
        _memoryStoreSingleLocalIndex = -1,
        _memoryStoreDoubleLocalIndex = -1,
        _memoryInitDestLocalIndex = -1,
        _memoryInitSrcLocalIndex = -1,
        _memoryInitCountLocalIndex = -1;
    
    public void CompileFunction()
    {
        DeclareLocals();

        if (code.Body.FlattenedInstructions().Any(i => i.Opcode is WasmOpcode.Call or WasmOpcode.CallIndirect))
        {
            var argsLocal = il.DeclareLocal(typeof(object[])); // args
            _callArgsLocalIndex = argsLocal.LocalIndex;
            
            var tempLocal = il.DeclareLocal(typeof(object));   // temp array value for arg
            _callTempLocalIndex = tempLocal.LocalIndex;
        }
        
        if (code.Body.FlattenedInstructions().Any(i => i.Opcode == WasmOpcode.CallIndirect))
        {
            var elementLocal = il.DeclareLocal(typeof(int));   // temp array value for element index
            _callIndirectElementLocalIndex = elementLocal.LocalIndex;
        }

        if (code.Body.FlattenedInstructions().Any(i => i.Opcode == WasmOpcode.GlobalSet))
        {
            var tempLocal = il.DeclareLocal(typeof(object));   // temp global value
            _globalTempLocalIndex = tempLocal.LocalIndex;
        }

        if (code.Body.FlattenedInstructions().Any(i => i.Opcode is WasmOpcode.I32Store 
                or WasmOpcode.I32Load 
                or WasmOpcode.I64Store 
                or WasmOpcode.I64Load
                or WasmOpcode.I32Store8
                or WasmOpcode.I32Store16
                or WasmOpcode.I32Load8S
                or WasmOpcode.I64Load8S
                or WasmOpcode.I32Load8U
                or WasmOpcode.I64Load8U
                or WasmOpcode.I32Load16S
                or WasmOpcode.I64Load16S
                or WasmOpcode.I32Load16U
                or WasmOpcode.I64Load16U
                or WasmOpcode.I64Load32S
                or WasmOpcode.I64Load32U
                or WasmOpcode.I64Store8
                or WasmOpcode.I64Store16
                or WasmOpcode.I64Store32
                or WasmOpcode.F32Load
                or WasmOpcode.F64Load))
        {
            var offsetLocal = il.DeclareLocal(typeof(int)); // temp offset value
            _memoryStoreLoadOffsetLocalIndex = offsetLocal.LocalIndex;
        }

        if (code.Body.FlattenedInstructions().Any(i => i.Opcode is WasmOpcode.I32Store 
                or WasmOpcode.I32Store8
                or WasmOpcode.I32Store16))
        {
            var intLocal = il.DeclareLocal(typeof(int));   // temp int value
            _memoryStoreIntLocalIndex = intLocal.LocalIndex;
        }
        
        if (code.Body.FlattenedInstructions().Any(i => i.Opcode is WasmOpcode.F32Store))
        {
            var floatLocal = il.DeclareLocal(typeof(float));   // temp float value
            _memoryStoreSingleLocalIndex = floatLocal.LocalIndex;
        }
        
        if (code.Body.FlattenedInstructions().Any(i => i.Opcode is WasmOpcode.F64Store))
        {
            var doubleLocal = il.DeclareLocal(typeof(double));   // temp double value
            _memoryStoreDoubleLocalIndex = doubleLocal.LocalIndex;
        }
        
        if (code.Body.FlattenedInstructions().Any(i => i.Opcode is WasmOpcode.I64Store
                or WasmOpcode.I64Store8
                or WasmOpcode.I64Store16
                or WasmOpcode.I64Store32))
        {
            var intLocal = il.DeclareLocal(typeof(long));   // temp long value
            _memoryStoreLongLocalIndex = intLocal.LocalIndex;
        }
        
        if (code.Body.FlattenedInstructions().Any(i => i.Opcode == WasmOpcode.MemoryInit))
        {
            var destLocal = il.DeclareLocal(typeof(int));   // temp dest value
            _memoryInitDestLocalIndex = destLocal.LocalIndex;
            
            var srcLocal = il.DeclareLocal(typeof(int));   // temp src value
            _memoryInitSrcLocalIndex = srcLocal.LocalIndex;
            
            var countLocal = il.DeclareLocal(typeof(int));   // temp count value
            _memoryInitCountLocalIndex = countLocal.LocalIndex;
        }
        
        foreach (var instruction in code.Body.Instructions)
        {
            CompileInstruction(instruction);
        }

        // WASM 0x0b is the end opcode, which is equivalent to a return
        if (code.Body.Instructions.Count == 0 || code.Body.Instructions[^1].Opcode != WasmOpcode.Return)
        {
            Ret();
        }
    }

    private void CompileInstruction(WasmInstruction instruction)
    {
        Debug.WriteLine($"Compiling {instruction} (stack: [{StackAsString}])");
        
        switch (instruction.Opcode)
        {
            case WasmOpcode.Unreachable:
                Unreachable();
                break;
            case WasmOpcode.Nop:
                Nop();
                break;
            case WasmOpcode.Return:
                Ret();
                break;
            case WasmOpcode.Drop:
                Pop();
                break;
            case WasmOpcode.I32Const:
                LdcI4(instruction);
                break;
            case WasmOpcode.I64Const:
                LdcI8(instruction);
                break;
            case WasmOpcode.F32Const:
                LdcR4(instruction);
                break;
            case WasmOpcode.F64Const:
                LdcR8(instruction);
                break;
            case WasmOpcode.I32Add or WasmOpcode.I64Add or WasmOpcode.F32Add or WasmOpcode.F64Add:
                Add(instruction);
                break;
            case WasmOpcode.I32Sub or WasmOpcode.I64Sub or WasmOpcode.F32Sub or WasmOpcode.F64Sub:
                Sub(instruction);
                break;
            case WasmOpcode.I32Mul or WasmOpcode.I64Mul or WasmOpcode.F32Mul or WasmOpcode.F64Mul:
                Mul(instruction);
                break;
            case WasmOpcode.F32Div or WasmOpcode.F64Div or WasmOpcode.I32DivS or WasmOpcode.I64DivS:
                Div(instruction);
                break;
            case WasmOpcode.I32DivU or WasmOpcode.I64DivU:
                DivUn(instruction);
                break;
            case WasmOpcode.I32RemS or WasmOpcode.I64RemS:
                Rem(instruction);
                break;
            case WasmOpcode.I32RemU or WasmOpcode.I64RemU:
                RemUn(instruction);
                break;
            case WasmOpcode.I32And or WasmOpcode.I64And:
                And(instruction);
                break;
            case WasmOpcode.I32Or or WasmOpcode.I64Or:
                Or(instruction);
                break;
            case WasmOpcode.I32Xor or WasmOpcode.I64Xor:
                Xor(instruction);
                break;
            case WasmOpcode.I32Shl or WasmOpcode.I64Shl:
                Shl(instruction);
                break;
            case WasmOpcode.I32ShrS or WasmOpcode.I64ShrS:
                Shr(instruction);
                break;
            case WasmOpcode.I32ShrU or WasmOpcode.I64ShrU:
                ShrUn(instruction);
                break;
            case WasmOpcode.I32Eqz:
                I32Eqz();
                break;
            case WasmOpcode.I64Eqz:
                I64Eqz();
                break;
            case WasmOpcode.I32Eq or WasmOpcode.I64Eq or WasmOpcode.F32Eq or WasmOpcode.F64Eq:
                Ceq();
                break;
            case WasmOpcode.I32Ne or WasmOpcode.I64Ne or WasmOpcode.F32Ne or WasmOpcode.F64Ne:
                NotEqual();
                break;
            case WasmOpcode.I32LtS or WasmOpcode.I64LtS or WasmOpcode.F32Lt or WasmOpcode.F64Lt:
                Clt();
                break;
            case WasmOpcode.I32LtU or WasmOpcode.I64LtU:
                Clt_Un();
                break;
            case WasmOpcode.I32GtS or WasmOpcode.I64GtS or WasmOpcode.F32Gt or WasmOpcode.F64Gt:
                Cgt();
                break;
            case WasmOpcode.I32GtU or WasmOpcode.I64GtU:
                Cgt_Un();
                break;
            case WasmOpcode.I32GeS or WasmOpcode.I64GeS or WasmOpcode.F32Ge or WasmOpcode.F64Ge:
                // >= is the same as !(<)
                Clt();
                I32Eqz();
                break;
            case WasmOpcode.I32GeU or WasmOpcode.I64GeU:
                // >= is the same as !(<)
                Clt_Un();
                I32Eqz();
                break;
            case WasmOpcode.I32LeS or WasmOpcode.I64LeS or WasmOpcode.F32Le or WasmOpcode.F64Le:
                // <= is the same as !(>)
                Cgt();
                I32Eqz();
                break;
            case WasmOpcode.I32LeU or WasmOpcode.I64LeU:
                // <= is the same as !(>)
                Cgt_Un();
                I32Eqz();
                break;
            case WasmOpcode.I64ExtendI32S:
                ConvI8();
                break;
            case WasmOpcode.I64ExtendI32U:
                ConvU8();
                break;
            case WasmOpcode.I32TruncF32S:
                Truncate(typeof(float), typeof(int), signed: true);
                break;
            case WasmOpcode.I32TruncF32U:
                Truncate(typeof(float), typeof(int), signed: false);
                break;
            case WasmOpcode.I32TruncF64S:
                Truncate(typeof(double), typeof(int), signed: true);
                break;
            case WasmOpcode.I32TruncF64U:
                Truncate(typeof(double), typeof(int), signed: false);
                break;
            case WasmOpcode.I64TruncF32S:
                Truncate(typeof(float), typeof(long), signed: true);
                break;
            case WasmOpcode.I64TruncF32U:
                Truncate(typeof(float), typeof(long), signed: false);
                break;
            case WasmOpcode.I64TruncF64S:
                Truncate(typeof(double), typeof(long), signed: true);
                break;
            case WasmOpcode.I64TruncF64U:
                Truncate(typeof(double), typeof(long), signed: false);
                break;
            case WasmOpcode.LocalGet:
                LocalGet(instruction);
                break;
            case WasmOpcode.LocalSet:
                LocalSet(instruction);
                break;
            case WasmOpcode.LocalTee:
                LocalTee(instruction);
                break;
            case WasmOpcode.Call:
                Call(instruction);
                break;
            case WasmOpcode.CallIndirect:
                CallIndirect(instruction);
                break;
            case WasmOpcode.GlobalGet:
                GlobalGet(instruction);
                break;
            case WasmOpcode.GlobalSet:
                GlobalSet(instruction);
                break;
            case WasmOpcode.RefFunc:
                RefFunc(instruction);
                break;
            case WasmOpcode.I32Store:
                MemoryStore(instruction, typeof(int));
                break;
            case WasmOpcode.I32Store8:
                MemoryStore(instruction, typeof(int), 8);
                break;
            case WasmOpcode.I32Store16:
                MemoryStore(instruction, typeof(int), 16);
                break;
            case WasmOpcode.I64Store:
                MemoryStore(instruction, typeof(long));
                break;
            case WasmOpcode.I64Store8:
                MemoryStore(instruction, typeof(long), 8);
                break;
            case WasmOpcode.I64Store16:
                MemoryStore(instruction, typeof(long), 16);
                break;
            case WasmOpcode.I64Store32:
                MemoryStore(instruction, typeof(long), 32);
                break;
            case WasmOpcode.F32Store:
                MemoryStore(instruction, typeof(float));
                break;
            case WasmOpcode.F64Store:
                MemoryStore(instruction, typeof(double));
                break;
            case WasmOpcode.I32Load:
                MemoryLoad(instruction, typeof(int));
                break;
            case WasmOpcode.I32Load8S:
                MemoryLoad(instruction, typeof(int), 8, signExtend: true);
                break;
            case WasmOpcode.I32Load8U:
                MemoryLoad(instruction, typeof(int), 8, signExtend: false);
                break;
            case WasmOpcode.I64Load8S:
                MemoryLoad(instruction, typeof(long), 8, signExtend: true);
                break;
            case WasmOpcode.I64Load8U:
                MemoryLoad(instruction, typeof(long), 8, signExtend: false);
                break;
            case WasmOpcode.I32Load16S:
                MemoryLoad(instruction, typeof(int), 16, signExtend: true);
                break;
            case WasmOpcode.I32Load16U:
                MemoryLoad(instruction, typeof(int), 16, signExtend: false);
                break;
            case WasmOpcode.I64Load16S:
                MemoryLoad(instruction, typeof(long), 16, signExtend: true);
                break;
            case WasmOpcode.I64Load16U:
                MemoryLoad(instruction, typeof(long), 16, signExtend: false);
                break;
            case WasmOpcode.I64Load32S:
                MemoryLoad(instruction, typeof(long), 32, signExtend: true);
                break;
            case WasmOpcode.I64Load32U:
                MemoryLoad(instruction, typeof(long), 32, signExtend: false);
                break;
            case WasmOpcode.I64Load:
                MemoryLoad(instruction, typeof(long));
                break;
            case WasmOpcode.F32Load:
                MemoryLoad(instruction, typeof(float));
                break;
            case WasmOpcode.F64Load:
                MemoryLoad(instruction, typeof(double));
                break;
            case WasmOpcode.F32ConvertI32S or WasmOpcode.F32ConvertI64S:
                ConvR4();
                break;
            case WasmOpcode.F32ConvertI32U or WasmOpcode.F32ConvertI64U:
                ConvR4Un();
                break;
            case WasmOpcode.F64ConvertI32S or WasmOpcode.F64ConvertI64S:
                ConvR8();
                break;
            case WasmOpcode.F64ConvertI32U or WasmOpcode.F64ConvertI64U:
                ConvR8Un();
                break;
            case WasmOpcode.I32ReinterpretF32:
                I32ReinterpretF32();
                break;
            case WasmOpcode.I64ReinterpretF64:
                I64ReinterpretF64();
                break;
            case WasmOpcode.F32ReinterpretI32:
                F32ReinterpretI32();
                break;
            case WasmOpcode.F64ReinterpretI64:
                F64ReinterpretI64();
                break;
            case WasmOpcode.I32WrapI64:
                ConvI4();
                break;
            case WasmOpcode.F32Abs:
                F32Abs();
                break;
            case WasmOpcode.F32Neg:
                Neg(typeof(float));
                break;
            case WasmOpcode.F64Abs:
                F64Abs();
                break;
            case WasmOpcode.F64Neg:
                Neg(typeof(double));
                break;
            case WasmOpcode.Block:
                Block(instruction);
                break;
            case WasmOpcode.Loop:
                Loop(instruction);
                break;
            case WasmOpcode.Br:
                Br(instruction);
                break;
            case WasmOpcode.BrIf:
                BrIf(instruction);
                break;
            case WasmOpcode.BrTable:
                BrTable(instruction);
                break;
            case WasmOpcode.MemoryInit:
                MemoryInit(instruction);
                break;
            case WasmOpcode.DataDrop:
                DataDrop(instruction);
                break;
            case WasmOpcode.Select:
                Select();
                break;
            case WasmOpcode.If:
                If(instruction);
                break;
            default:
                throw new NotImplementedException($"Opcode {instruction.Opcode} not implemented in compiler.");
        }
    }

    private string StackAsString => string.Join(", ", _stack.Reverse()
        .Select(i => i.FullName switch
        {
            "System.Int32" => "i32",
            "System.Int64" => "i64",
            "System.Single" => "f32",
            "System.Double" => "f64",
            _ => i.FullName
        }));

    private void ConvR8()
    {
        il.Emit(ILOpcode.ConvR8);
        _stack.Pop();
        _stack.Push(typeof(double));
    }

    private void ConvR4Un()
    {
        il.Emit(ILOpcode.ConvRUn);
        il.Emit(ILOpcode.ConvR4); // cast to float in case of double
        _stack.Pop();
        _stack.Push(typeof(float));
    }
    
    private void ConvR8Un()
    {
        il.Emit(ILOpcode.ConvRUn);
        il.Emit(ILOpcode.ConvR8); // cast to double in case of float
        _stack.Pop();
        _stack.Push(typeof(double));
    }

    private void ConvR4()
    {
        il.Emit(ILOpcode.ConvR4);
        _stack.Pop();
        _stack.Push(typeof(float));
    }

    private void Truncate(Type source, Type dest, bool signed)
    {
        var type = _stack.Pop();
        
        if (type != source)
        {
            throw new InvalidOperationException($"Truncate expects {source} but stack contains {type}");
        }
        
        if (dest == typeof(int))
        {
            if (signed)
            {
                il.Emit(ILOpcode.ConvOvfI4);
            }
            else
            {
                il.Emit(ILOpcode.ConvOvfI4Un);
            }
        }
        else if (dest == typeof(long))
        {
            if (signed)
            {
                il.Emit(ILOpcode.ConvOvfI8);
            }
            else
            {
                il.Emit(ILOpcode.ConvOvfI8Un);
            }
        }
        else
        {
            throw new NotImplementedException($"Truncate from {source} to {dest} not implemented");
        }
        
        _stack.Push(dest);
    }

    private void F64Abs()
    {
        il.EmitCall(typeof(Math).GetMethod(nameof(Math.Abs), new[] { typeof(double) })!);
        _stack.Pop();
        _stack.Push(typeof(double));
    }

    private void Neg(Type t)
    {
        il.Emit(ILOpcode.Neg);
        _stack.Pop();
        _stack.Push(t);
    }

    private void F32Abs()
    {
        il.EmitCall(typeof(MathF).GetMethod(nameof(MathF.Abs))!);
        _stack.Pop();
        _stack.Push(typeof(float));
    }

    private void BrTable(WasmInstruction instruction)
    {
        if (instruction.Operands.Count != 2)
        {
            throw new InvalidOperationException("br_table expects two arguments");
        }
        
        if (instruction.Operands[0] is not WasmI32VectorValue { Values: var labels })
        {
            throw new InvalidOperationException("br_table expects first argument to be the break depths");
        }
        
        if (instruction.Operands[1] is not WasmNumberValue<int> { Value: var defaultLabelIndex })
        {
            throw new InvalidOperationException("br_table expects last argument to be the default label index");
        }

        var type = _stack.Pop();
        
        if (type != typeof(int))
        {
            throw new InvalidOperationException($"br_table expects i32 but stack contains {type}");
        }
        
        il.EmitSwitch(labels.Select(i => _labels.ElementAt(i)).ToArray());

        if (defaultLabelIndex == 0 && _labels.Count == 0)
        {
            il.Emit(ILOpcode.Ret);
        }
        else
        {
            var defaultLabel = _labels.ElementAt(defaultLabelIndex);
            il.EmitBr(defaultLabel);
        }
    }

    private void ConvI4()
    {
        il.Emit(ILOpcode.ConvI4);
        _stack.Pop();
        _stack.Push(typeof(int));
    }

    private void F64ReinterpretI64()
    {
        il.EmitCall(typeof(BitConverter).GetMethod(nameof(BitConverter.Int64BitsToDouble))!);
        _stack.Pop();
        _stack.Push(typeof(double));
    }

    private void F32ReinterpretI32()
    {
        il.EmitCall(typeof(BitConverter).GetMethod(nameof(BitConverter.Int32BitsToSingle))!);
        _stack.Pop();
        _stack.Push(typeof(float));
    }

    private void I64ReinterpretF64()
    {
        il.EmitCall(typeof(BitConverter).GetMethod(nameof(BitConverter.DoubleToInt64Bits))!);
        _stack.Pop();
        _stack.Push(typeof(long));
    }

    private void I32ReinterpretF32()
    {
        il.EmitCall(typeof(BitConverter).GetMethod(nameof(BitConverter.SingleToInt32Bits))!);
        _stack.Pop();
        _stack.Push(typeof(int));
    }

    private void NotEqual()
    {
        il.Emit(ILOpcode.Ceq);
        il.EmitLdcI4(0);
        il.Emit(ILOpcode.Ceq);
        _stack.Pop();
        _stack.Pop();
        _stack.Push(typeof(int));
    }

    private void Select()
    {
        var type = _stack.Pop();
        var type2 = _stack.Pop();
        var type3 = _stack.Pop();
        
        if (type != typeof(int))
        {
            throw new InvalidOperationException("Select expects its first value to be i32");
        }
        
        if (type2 != type3)
        {
            throw new InvalidOperationException("Select expects its two values to be the same type");
        }
        
        il.EmitCall(typeof(SelectFunctions).GetMethod(nameof(SelectFunctions.Select))!.MakeGenericMethod(type2));
        _stack.Push(type2);
    }

    private void Cgt_Un()
    {
        il.Emit(ILOpcode.CgtUn);
        _stack.Pop();
        _stack.Pop();
        _stack.Push(typeof(int));
    }

    private void Clt_Un()
    {
        il.Emit(ILOpcode.CltUn);
        _stack.Pop();
        _stack.Pop();
        _stack.Push(typeof(int));
    }

    private void Unreachable()
    {
        il.EmitNewobj(typeof(UnreachableException).GetConstructor(Type.EmptyTypes)!);
        il.Emit(ILOpcode.Throw);
    }

    private void DataDrop(WasmInstruction instruction)
    {
        if (instruction.Operands.Count != 1)
        {
            throw new InvalidOperationException("data.drop expects one argument");
        }
        
        if (instruction.Operands[0] is not WasmNumberValue<int> { Value: var x })
        {
            throw new InvalidOperationException("data.drop expects first argument to be the data index");
        }

        il.EmitLdarg(0); // load module instance
        il.EmitLdcI4(x); // load data index
        
        il.EmitCallVirt(typeof(ModuleInstance).GetMethod(nameof(ModuleInstance.DataDrop))!);
    }

    private void MemoryInit(WasmInstruction instruction)
    {
        if (instruction.Operands.Count != 1)
        {
            throw new InvalidOperationException("memory.init expects one argument");
        }
        
        if (instruction.Operands[0] is not WasmNumberValue<int> { Value: var x })
        {
            throw new InvalidOperationException("memory.init expects first argument to be the data index");
        }

        if (_stack.Pop() != typeof(int))
        {
            throw new InvalidOperationException("memory.init expects i32 count but stack contains {argType}");
        }
        
        il.EmitStloc(_memoryInitCountLocalIndex);
        
        if (_stack.Pop() != typeof(int))
        {
            throw new InvalidOperationException("memory.init expects i32 src but stack contains {argType}");
        }
        
        il.EmitStloc(_memoryInitSrcLocalIndex);
        
        if (_stack.Pop() != typeof(int))
        {
            throw new InvalidOperationException("memory.init expects i32 dest but stack contains {argType}");
        }
        
        il.EmitStloc(_memoryInitDestLocalIndex);
        
        il.EmitLdarg(0); // load module instance
        il.EmitLdcI4(x); // load data index
        il.EmitLdloc(_memoryInitDestLocalIndex); // load dest
        il.EmitLdloc(_memoryInitSrcLocalIndex); // load src
        il.EmitLdloc(_memoryInitCountLocalIndex); // load count
        
        il.EmitCallVirt(typeof(ModuleInstance).GetMethod(nameof(ModuleInstance.MemoryInit))!);
    }

    private void ConvU8()
    {
        il.Emit(ILOpcode.ConvU8);
        _stack.Pop();
        _stack.Push(typeof(long));
    }
    
    private void ConvI8()
    {
        il.Emit(ILOpcode.ConvI8);
        _stack.Pop();
        _stack.Push(typeof(long));
    }

    private void Pop()
    {
        il.Emit(ILOpcode.Pop);
        _stack.Pop();
    }

    private void LocalTee(WasmInstruction instruction)
    {
        var type = _stack.Peek();
        il.Emit(ILOpcode.Dup); // duplicate value on stack
        _stack.Push(type); // push duplicated value type onto stack
        LocalSet(instruction); // set local
    }

    private void I32Eqz()
    {
        il.EmitLdcI4(0);
        il.Emit(ILOpcode.Ceq);
        _stack.Pop();
        _stack.Push(typeof(int));
    }
    
    private void I64Eqz()
    {
        il.EmitLdcI8(0L);
        il.Emit(ILOpcode.Ceq);
        _stack.Pop();
        _stack.Push(typeof(int));
    }

    private void Br(WasmInstruction instruction)
    {
        if (instruction.Operands.Count != 1)
            throw new InvalidOperationException();

        if (instruction.Operands[0] is not WasmNumberValue<int> { Value: var labelIndex })
            throw new InvalidOperationException();

        var label = _labels.ElementAt(labelIndex);
        il.EmitBr(label);
    }
    
    private void BrIf(WasmInstruction instruction)
    {
        if (instruction.Operands.Count != 1)
            throw new InvalidOperationException();

        if (instruction.Operands[0] is not WasmNumberValue<int> { Value: var labelIndex })
            throw new InvalidOperationException();

        var label = _labels.ElementAt(labelIndex);
        var type = _stack.Pop();
        
        if (type != typeof(int))
        {
            throw new InvalidOperationException($"BrIf expects i32 but stack contains {type}");
        }
        
        il.EmitBrTrue(label);
    }
    
    private ILLabel CreateWasmVisibleLabel()
    {
        var label = il.DefineLabel();
        _labels.Push(label);
        return label;
    }

    private void If(WasmInstruction instruction)
    {
        bool hasElse = instruction.Operands.Count switch
        {
            2 => false,
            3 => true,
            _ => throw new ArgumentException("Invalid number of operands for if opcode")
        };
        
        if (instruction.Operands[0] is not WasmBlockType blockType)
            throw new InvalidOperationException("The first operand to if must be a block type");
        
        if (instruction.Operands[1] is not WasmExpressionValue ifBody)
            throw new InvalidOperationException("If expression operand is not an expression");

        WasmExpressionValue? elseBody = null;

        if (hasElse)
        {
            if (instruction.Operands[2] is not WasmExpressionValue elseExpr)
            {
                throw new InvalidOperationException("Else expression operand is not an expression");
            }

            elseBody = elseExpr;
        }

        // Assert: due to validation, a value of value type i32 is on the top of the stack.
        var type = _stack.Pop();

        if (type != typeof(int))
        {
            throw new InvalidOperationException("Expected an i32 on the stack");
        }

        var elseLabel = il.DefineLabel();
        il.EmitBrFalse(elseLabel);
        
        Block(new WasmInstruction(WasmOpcode.Block, blockType, ifBody));

        ILLabel? ifEndLabel = null;
        
        if (elseBody is not null)
        {
            ifEndLabel = il.DefineLabel();
            il.EmitBr(ifEndLabel);
        }

        il.MarkLabel(elseLabel);
        Nop();

        if (elseBody is not null)
        {
            Block(new WasmInstruction(WasmOpcode.Block, blockType, elseBody));
            
            il.MarkLabel(ifEndLabel ?? throw new InvalidOperationException("ifEndLabel is null"));
            Nop();
        }
    }

    private void Block(WasmInstruction instruction)
    {
        if (instruction.Operands.Count != 2)
            throw new InvalidOperationException();

        if (instruction.Operands[0] is not WasmBlockType blockType)
            throw new InvalidOperationException();
        
        if (instruction.Operands[1] is not WasmExpressionValue { Expression: var expression })
            throw new InvalidOperationException();

        // TODO.PI: validate block type
        // if (blockType is not WasmBlockType.EmptyBlockType)
        //     throw new NotImplementedException("Only empty block types are supported");

        var label = CreateWasmVisibleLabel();
        
        foreach (var exprInstruction in expression.Instructions)
        {
            CompileInstruction(exprInstruction);
        }
        
        il.MarkLabel(label);
        Nop();
        _labels.Pop();
    }

    private void Nop()
    {
        il.Emit(ILOpcode.Nop);
    }

    private void Loop(WasmInstruction instruction)
    {
        if (instruction.Operands.Count != 2)
            throw new InvalidOperationException();

        if (instruction.Operands[0] is not WasmBlockType blockType)
            throw new InvalidOperationException();
        
        if (instruction.Operands[1] is not WasmExpressionValue { Expression: var expression })
            throw new InvalidOperationException();

        if (blockType is not WasmBlockType.EmptyBlockType)
            throw new NotImplementedException("Only empty block types are supported");

        var label = CreateWasmVisibleLabel();
        il.MarkLabel(label);
        Nop();
        
        foreach (var exprInstruction in expression.Instructions)
        {
            CompileInstruction(exprInstruction);
        }

        _labels.Pop();
        Nop();
    }

    private void MemoryLoad(WasmInstruction instruction, Type t, int? bits = null, bool signExtend = false)
    {
        // stack should contain [i32]
        // i32 is the offset
        
        if (instruction.Operands.Count != 2)
            throw new InvalidOperationException();
        
        if (instruction.Operands[0] is not WasmNumberValue<int> { Value: var offset })
            throw new InvalidOperationException();
        
        // ignore align for now
        var offsetType = _stack.Pop();
        
        if (offsetType != typeof(int))
        {
            throw new InvalidOperationException($"Memory load expects i32 offset but stack contains {offsetType}");
        }
        
        il.EmitStloc(_memoryStoreLoadOffsetLocalIndex); // store offset in temp local
        
        il.EmitLdarg(0); // load module instance
        il.EmitLdloc(_memoryStoreLoadOffsetLocalIndex); // load dynamic offset from temp local
        il.EmitLdcI4(offset); // load static offset

        if (t == typeof(int) || t == typeof(long))
        {
            il.EmitLdcI4(bits ?? 0); // storage size i.e. i32.load8_s
            il.EmitLdcI4(signExtend ? 1 : 0); // sign extend i.e. i32.load8_s
        }

        MethodInfo method;
        
        if (t == typeof(int))
            method = typeof(ModuleInstance).GetMethod(nameof(ModuleInstance.MemoryLoadI32))!;
        else if (t == typeof(long))
            method = typeof(ModuleInstance).GetMethod(nameof(ModuleInstance.MemoryLoadI64))!;
        else if (t == typeof(float))
            method = typeof(ModuleInstance).GetMethod(nameof(ModuleInstance.MemoryLoadF32))!;
        else if (t == typeof(double))
            method = typeof(ModuleInstance).GetMethod(nameof(ModuleInstance.MemoryLoadF64))!;
        else
            throw new InvalidOperationException($"Memory load expects int, long, float or double but got {t}");

        il.EmitCallVirt(method); // load from memory
        _stack.Push(t);
    }
    
    private void MemoryStore(WasmInstruction instruction, Type t, int? bits = null)
    {
        // stack should contain [i32, t]
        // i32 is the offset
        // c is the value to store of type t
        
        if (instruction.Operands.Count != 2)
            throw new InvalidOperationException();
        
        if (instruction.Operands[0] is not WasmNumberValue<int> { Value: var offset })
            throw new InvalidOperationException();
        
        // ignore align for now
        
        var argType = _stack.Pop();
        
        if (argType != t)
        {
            throw new InvalidOperationException(
                $"Memory store expects type {t} but stack contains {argType}");
        }
        
        if (_stack.Pop() != typeof(int))
        {
            throw new InvalidOperationException(
                $"Memory store expects i32 offset but stack contains {argType}");
        }
        
        int localIndex;
        
        if (t == typeof(int))
            localIndex = _memoryStoreIntLocalIndex;
        else if (t == typeof(long))
            localIndex = _memoryStoreLongLocalIndex;
        else if (t == typeof(float))
            localIndex = _memoryStoreSingleLocalIndex;
        else if (t == typeof(double))
            localIndex = _memoryStoreDoubleLocalIndex;
        else
            throw new InvalidOperationException($"Memory store expects int, long, float or double but got {t}");

        il.EmitStloc(localIndex); // store arg in temp local
        il.EmitStloc(_memoryStoreLoadOffsetLocalIndex); // store offset in temp local
        
        il.EmitLdarg(0); // load module instance
        il.EmitLdloc(_memoryStoreLoadOffsetLocalIndex); // load dynamic offset
        il.EmitLdloc(localIndex); // load value from temp local
        il.EmitLdcI4(offset); // load static offset
        
        if (t == typeof(int) || t == typeof(long))
            il.EmitLdcI4(bits ?? 0); // to support i.e. i32.store8
        
        MethodInfo method;

        if (t == typeof(int))
            method = typeof(ModuleInstance).GetMethod(nameof(ModuleInstance.MemoryStoreI32))!;
        else if (t == typeof(long))
            method = typeof(ModuleInstance).GetMethod(nameof(ModuleInstance.MemoryStoreI64))!;
        else if (t == typeof(float))
            method = typeof(ModuleInstance).GetMethod(nameof(ModuleInstance.MemoryStoreF32))!;
        else if (t == typeof(double))
            method = typeof(ModuleInstance).GetMethod(nameof(ModuleInstance.MemoryStoreF64))!;
        else
            throw new InvalidOperationException($"Memory store expects int, long, float or double but got {t}");

        il.EmitCallVirt(method); // store in memory
    }

    private void RefFunc(WasmInstruction instruction)
    {
        if (instruction.Operands.Count != 1)
            throw new InvalidOperationException();

        if (instruction.Operands[0] is not WasmNumberValue<int> { Value: var numberValue })
            throw new InvalidOperationException();

        il.EmitLdarg(0); // load module instance
        il.EmitLdcI4(numberValue); // load function index
        il.EmitCallVirt(typeof(ModuleInstance).GetMethod(nameof(ModuleInstance.GetFunctionReference))!); // get function reference
        // stack now contains function reference

        _stack.Push(typeof(FunctionReference));
    }

    private void DeclareLocals()
    {
        foreach (var local in code.Locals)
        {
            for (var i = 0; i < local.Count; i++)
            {
                il.DeclareLocal(local.Type.DotNetType);
            }
        }
    }

    private void GlobalSet(WasmInstruction instruction)
    {
        if (instruction.Operands.Count != 1)
            throw new InvalidOperationException();

        if (instruction.Operands[0] is not WasmNumberValue<int> numberValue)
            throw new InvalidOperationException();

        var globalIndex = numberValue.Value;
        var globalRef = module.GetGlobal(globalIndex);
        var globalType = globalRef.Global.Type.DotNetType;

        var argType = _stack.Pop();

        if (argType != globalType)
        {
            throw new InvalidOperationException(
                $"Global {globalIndex} is of type {globalType} but stack contains {argType}");
        }

        if (!globalRef.Mutable || !globalRef.Global.Mutable)
        {
            throw new InvalidOperationException($"Global {globalIndex} is not mutable");
        }

        if (argType.IsValueType)
        {
            il.EmitBox(argType); // box value type
        }

        il.EmitStloc(_globalTempLocalIndex); // store arg in temp local

        il.EmitLdarg(0); // load module instance
        il.EmitLdcI4(globalIndex); // load global index
        il.EmitLdloc(_globalTempLocalIndex); // load arg from temp local
        il.EmitCallVirt(typeof(ModuleInstance).GetMethod(nameof(ModuleInstance.SetGlobalValue))!); // set global instance
    }

    private void GlobalGet(WasmInstruction instruction)
    {
        if (instruction.Operands.Count != 1)
            throw new InvalidOperationException();

        if (instruction.Operands[0] is not WasmNumberValue<int> numberValue)
            throw new InvalidOperationException();

        var globalIndex = numberValue.Value;

        var globalRef = module.GetGlobal(globalIndex);
        var globalType = globalRef.Global.Type.DotNetType;

        il.EmitLdarg(0); // load module instance
        il.EmitLdcI4(globalIndex); // load global index
        il.EmitCallVirt(typeof(ModuleInstance).GetMethod(nameof(ModuleInstance.GetGlobalValue))!); // get global instance
        // stack now contains global value

        if (globalType.IsValueType)
        {
            il.EmitUnboxAny(globalRef.Global.Type.DotNetType); // unbox global value
        }

        _stack.Push(globalType);
    }

    private void CallIndirect(WasmInstruction instruction)
    {
        if (instruction.Operands.Count != 2)
            throw new InvalidOperationException("call_indirect expects two arguments");
        
        if (instruction.Operands[0] is not WasmNumberValue<int> { Value: var tableIndexValue })
            throw new InvalidOperationException("call_indirect expects first argument to be table index");
        
        if (instruction.Operands[1] is not WasmNumberValue<int> { Value: var typeIndexValue })
            throw new InvalidOperationException("call_indirect expects second argument to be type index");
        
        var type = module.Types[typeIndexValue];
        var returnType = type.Results.Count == 0 ? typeof(void) : type.Results[0].DotNetType;

        // stack now contains the element index int, store to local
        il.EmitStloc(_callIndirectElementLocalIndex);
        _stack.Pop();
        
        PrepareCallArgsArray(type.Parameters.Count);
        
        il.EmitLdarg(0); // load module instance
        il.EmitLdcI4(tableIndexValue); // load table index
        il.EmitLdcI4(typeIndexValue); // load type index
        il.EmitLdloc(_callIndirectElementLocalIndex); // load element index
        il.EmitLdloc(_callArgsLocalIndex); // load args array
        il.EmitCallVirt(typeof(ModuleInstance).GetMethod(nameof(ModuleInstance.CallIndirect))!); // get function instance
        // stack now contains return value
        _stack.Push(returnType);

        CleanUpCallReturnStack(returnType);
    }

    private void PrepareCallArgsArray(int paramCount)
    {
        il.EmitLdcI4(paramCount); // num_args = # of args
        il.EmitNewarr(typeof(object)); // new object[num_args]
        il.EmitStloc(_callArgsLocalIndex); // store array in local

        var stackValues = _stack.ToList();

        if (stackValues.Count < paramCount)
            throw new InvalidOperationException("Stack would underflow");

        // Push args onto array in reverse order.
        // The stack at this point for a call of the form func(1, 2, 3) is 1, 2, 3
        // Since we can only pop the args off the stack in reverse order,
        // we need to store them in a temp local one by one and then set the array element.
        for (int paramIndex = paramCount - 1, stackIndex = 0;
             paramIndex >= 0;
             paramIndex--, stackIndex++)
        {
            var stackType = stackValues[stackIndex];

            if (stackType.IsValueType)
            {
                il.EmitBox(stackType); // box value type
            }

            il.EmitStloc(_callTempLocalIndex); // store arg in temp local
            il.EmitLdloc(_callArgsLocalIndex); // load array
            il.EmitLdcI4(paramIndex); // array index
            il.EmitLdloc(_callTempLocalIndex); // load arg from temp local
            il.Emit(ILOpcode.StelemRef); // args[index] = arg
            _stack.Pop();
        }
    }

    private void Call(WasmInstruction instruction)
    {
        var (funcInstance, funcIndex) = GetFunctionInstanceForCall(module, instruction);

        PrepareCallArgsArray(funcInstance.ParameterTypes.Length);

        il.EmitLdarg(0); // load module instance
        il.EmitLdcI4(funcIndex); // load function index
        il.EmitCallVirt(typeof(ModuleInstance).GetMethod(nameof(ModuleInstance.GetFunction))!); // get function instance
        // stack now contains function instance

        il.EmitLdloc(_callArgsLocalIndex); // load args array
        il.EmitCallVirt(typeof(IFunctionInstance).GetMethod(nameof(IFunctionInstance.Invoke))!); // invoke function
        // stack now contains return value
        _stack.Push(funcInstance.ReturnType);

        CleanUpCallReturnStack(funcInstance.ReturnType);
    }

    private void CleanUpCallReturnStack(Type returnType)
    {
        if (returnType == typeof(void))
        {
            Pop(); // pop return value
        }
        else if (returnType.IsValueType)
        {
            il.EmitUnboxAny(returnType); // unbox return value
        }
    }

    private void LocalSet(WasmInstruction instruction)
    {
        if (instruction.Operands.Count != 1)
            throw new InvalidOperationException();

        if (instruction.Operands[0] is not WasmNumberValue<int> numberValue)
            throw new InvalidOperationException();

        if (numberValue.Value < type.Parameters.Count)
        {
            il.EmitStarg(numberValue.Value + 1);
            _stack.Pop();
        }
        else
        {
            il.EmitStloc(numberValue.Value - type.Parameters.Count);
            _stack.Pop();
        }
    }

    private void LocalGet(WasmInstruction instruction)
    {
        if (instruction.Operands.Count != 1)
            throw new InvalidOperationException();

        if (instruction.Operands[0] is not WasmNumberValue<int> numberValue)
            throw new InvalidOperationException();

        if (numberValue.Value < type.Parameters.Count)
        {
            il.EmitLdarg(numberValue.Value + 1);
            _stack.Push(type.Parameters[numberValue.Value].DotNetType);
        }
        else
        {
            il.EmitLdloc(numberValue.Value - type.Parameters.Count);
            _stack.Push(code.Locals[numberValue.Value - type.Parameters.Count].Type.DotNetType);
        }
    }

    private void Ceq()
    {
        il.Emit(ILOpcode.Ceq);
        _stack.Pop();
        _stack.Pop();
        _stack.Push(typeof(int));
    }
    
    private void Clt()
    {
        il.Emit(ILOpcode.Clt);
        _stack.Pop();
        _stack.Pop();
        _stack.Push(typeof(int));
    }
    
    private void Cgt()
    {
        il.Emit(ILOpcode.Cgt);
        _stack.Pop();
        _stack.Pop();
        _stack.Push(typeof(int));
    }

    private void ShrUn(WasmInstruction instruction)
    {
        il.Emit(ILOpcode.ShrUn);
        _stack.Pop();
        _stack.Pop();
        _stack.Push(instruction.Opcode switch
        {
            WasmOpcode.I32ShrU => typeof(int),
            WasmOpcode.I64ShrU => typeof(long),
            _ => throw new InvalidOperationException()
        });
    }

    private void Shr(WasmInstruction instruction)
    {
        il.Emit(ILOpcode.Shr);
        _stack.Pop();
        _stack.Pop();
        _stack.Push(instruction.Opcode switch
        {
            WasmOpcode.I32ShrS => typeof(int),
            WasmOpcode.I64ShrS => typeof(long),
            _ => throw new InvalidOperationException()
        });
    }

    private void Shl(WasmInstruction instruction)
    {
        il.Emit(ILOpcode.Shl);
        _stack.Pop();
        _stack.Pop();
        _stack.Push(instruction.Opcode switch
        {
            WasmOpcode.I32Shl => typeof(int),
            WasmOpcode.I64Shl => typeof(long),
            _ => throw new InvalidOperationException()
        });
    }

    private void Xor(WasmInstruction instruction)
    {
        il.Emit(ILOpcode.Xor);
        _stack.Pop();
        _stack.Pop();
        _stack.Push(instruction.Opcode switch
        {
            WasmOpcode.I32Xor => typeof(int),
            WasmOpcode.I64Xor => typeof(long),
            _ => throw new InvalidOperationException()
        });
    }

    private void Or(WasmInstruction instruction)
    {
        il.Emit(ILOpcode.Or);
        _stack.Pop();
        _stack.Pop();
        _stack.Push(instruction.Opcode switch
        {
            WasmOpcode.I32Or => typeof(int),
            WasmOpcode.I64Or => typeof(long),
            _ => throw new InvalidOperationException()
        });
    }

    private void And(WasmInstruction instruction)
    {
        il.Emit(ILOpcode.And);
        _stack.Pop();
        _stack.Pop();
        _stack.Push(instruction.Opcode switch
        {
            WasmOpcode.I32And => typeof(int),
            WasmOpcode.I64And => typeof(long),
            _ => throw new InvalidOperationException()
        });
    }

    private void RemUn(WasmInstruction instruction)
    {
        il.Emit(ILOpcode.RemUn);
        _stack.Pop();
        _stack.Pop();
        _stack.Push(instruction.Opcode switch
        {
            WasmOpcode.I32RemU => typeof(int),
            WasmOpcode.I64RemU => typeof(long),
            _ => throw new InvalidOperationException()
        });
    }

    private void Rem(WasmInstruction instruction)
    {
        il.Emit(ILOpcode.Rem);
        _stack.Pop();
        _stack.Pop();
        _stack.Push(instruction.Opcode switch
        {
            WasmOpcode.I32RemS => typeof(int),
            WasmOpcode.I64RemS => typeof(long),
            _ => throw new InvalidOperationException()
        });
    }

    private void DivUn(WasmInstruction instruction)
    {
        il.Emit(ILOpcode.DivUn);
        _stack.Pop();
        _stack.Pop();
        _stack.Push(instruction.Opcode switch
        {
            WasmOpcode.I32DivU => typeof(int),
            WasmOpcode.I64DivU => typeof(long),
            _ => throw new InvalidOperationException()
        });
    }

    private void Div(WasmInstruction instruction)
    {
        il.Emit(ILOpcode.Div);
        _stack.Pop();
        _stack.Pop();
        _stack.Push(instruction.Opcode switch
        {
            WasmOpcode.I32DivS => typeof(int),
            WasmOpcode.I64DivS => typeof(long),
            WasmOpcode.F32Div => typeof(float),
            WasmOpcode.F64Div => typeof(double),
            _ => throw new InvalidOperationException()
        });
    }

    private void Mul(WasmInstruction instruction)
    {
        il.Emit(ILOpcode.Mul);
        _stack.Pop();
        _stack.Pop();
        _stack.Push(instruction.Opcode switch
        {
            WasmOpcode.I32Mul => typeof(int),
            WasmOpcode.I64Mul => typeof(long),
            WasmOpcode.F32Mul => typeof(float),
            WasmOpcode.F64Mul => typeof(double),
            _ => throw new InvalidOperationException()
        });
    }

    private void Sub(WasmInstruction instruction)
    {
        il.Emit(ILOpcode.Sub);
        _stack.Pop();
        _stack.Pop();
        _stack.Push(instruction.Opcode switch
        {
            WasmOpcode.I32Sub => typeof(int),
            WasmOpcode.I64Sub => typeof(long),
            WasmOpcode.F32Sub => typeof(float),
            WasmOpcode.F64Sub => typeof(double),
            _ => throw new InvalidOperationException()
        });
    }

    private void Add(WasmInstruction instruction)
    {
        il.Emit(ILOpcode.Add);
        _stack.Pop();
        _stack.Pop();
        _stack.Push(instruction.Opcode switch
        {
            WasmOpcode.I32Add => typeof(int),
            WasmOpcode.I64Add => typeof(long),
            WasmOpcode.F32Add => typeof(float),
            WasmOpcode.F64Add => typeof(double),
            _ => throw new InvalidOperationException()
        });
    }

    private void LdcR8(WasmInstruction instruction)
    {
        if (instruction.Operands.Count != 1)
            throw new InvalidOperationException();

        if (instruction.Operands[0] is not WasmNumberValue<double> numberValue)
            throw new InvalidOperationException();

        il.EmitLdcR8(numberValue.Value);
        _stack.Push(typeof(double));
    }

    private void LdcR4(WasmInstruction instruction)
    {
        if (instruction.Operands.Count != 1)
            throw new InvalidOperationException();

        if (instruction.Operands[0] is not WasmNumberValue<float> numberValue)
            throw new InvalidOperationException();

        il.EmitLdcR4(numberValue.Value);
        _stack.Push(typeof(float));
    }

    private void LdcI8(WasmInstruction instruction)
    {
        if (instruction.Operands.Count != 1)
            throw new InvalidOperationException();

        if (instruction.Operands[0] is not WasmNumberValue<long> numberValue)
            throw new InvalidOperationException();

        il.EmitLdcI8(numberValue.Value);
        _stack.Push(typeof(long));
    }

    private void LdcI4(WasmInstruction instruction)
    {
        if (instruction.Operands.Count != 1)
            throw new InvalidOperationException();

        if (instruction.Operands[0] is not WasmNumberValue<int> numberValue)
            throw new InvalidOperationException();

        il.EmitLdcI4(numberValue.Value);
        _stack.Push(typeof(int));
    }

    private void Ret()
    {
        il.Emit(ILOpcode.Ret);

        if (returnType != typeof(void))
        {
            _stack.Pop();
        }
    }

    private static (IFunctionInstance Function, int Index) GetFunctionInstanceForCall(
        ModuleInstance module,
        WasmInstruction instruction)
    {
        if (instruction.Operands.Count != 1)
            throw new InvalidOperationException();

        if (instruction.Operands[0] is not WasmNumberValue<int> numberValue)
            throw new InvalidOperationException();

        return (module.GetFunction(numberValue.Value), numberValue.Value);
    }
}