using System.Reflection.Emit;

namespace WasmNet.Core;

public class WasmCompiler(ModuleInstance module, MethodBuilder method, WasmType type, WasmCode code)
{
    private readonly ILGenerator _il = method.GetILGenerator();
    private readonly Stack<Type> _stack = new();
    
    private int _callArgsLocalIndex = -1, 
        _callTempLocalIndex = -1,
        _globalTempLocalIndex = -1,
        _callIndirectElementLocalIndex = -1,
        _memoryStoreLoadOffsetLocalIndex = -1,
        _memoryStoreIntLocalIndex = -1;

    public static void CompileFunction(ModuleInstance module, MethodBuilder method, WasmType type, WasmCode code)
    {
        var compiler = new WasmCompiler(module, method, type, code);
        compiler.CompileFunction();
    }
    
    public void CompileFunction()
    {
        DeclareLocals();

        if (code.Body.Instructions.Any(i => i.Opcode is WasmOpcode.Call or WasmOpcode.CallIndirect))
        {
            var argsLocal = _il.DeclareLocal(typeof(object[])); // args
            _callArgsLocalIndex = argsLocal.LocalIndex;
            
            var tempLocal = _il.DeclareLocal(typeof(object));   // temp array value for arg
            _callTempLocalIndex = tempLocal.LocalIndex;
        }
        
        if (code.Body.Instructions.Any(i => i.Opcode == WasmOpcode.CallIndirect))
        {
            var elementLocal = _il.DeclareLocal(typeof(int));   // temp array value for element index
            _callIndirectElementLocalIndex = elementLocal.LocalIndex;
        }

        if (code.Body.Instructions.Any(i => i.Opcode == WasmOpcode.GlobalSet))
        {
            var tempLocal = _il.DeclareLocal(typeof(object));   // temp global value
            _globalTempLocalIndex = tempLocal.LocalIndex;
        }

        if (code.Body.Instructions.Any(i => i.Opcode is WasmOpcode.I32Store or WasmOpcode.I32Load))
        {
            var offsetLocal = _il.DeclareLocal(typeof(int)); // temp offset value
            _memoryStoreLoadOffsetLocalIndex = offsetLocal.LocalIndex;
        }

        if (code.Body.Instructions.Any(i => i.Opcode == WasmOpcode.I32Store))
        {
            var intLocal = _il.DeclareLocal(typeof(int));   // temp int value
            _memoryStoreIntLocalIndex = intLocal.LocalIndex;
        }
        
        foreach (var instruction in code.Body.Instructions)
        {
            CompileInstruction(instruction);
        }
    }

    private void CompileInstruction(WasmInstruction instruction)
    {
        switch (instruction.Opcode)
        {
            case WasmOpcode.End or WasmOpcode.Return:
                Ret();
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
            case WasmOpcode.I32Eq or WasmOpcode.I64Eq:
                Ceq();
                break;
            case WasmOpcode.LocalGet:
                LocalGet(instruction);
                break;
            case WasmOpcode.LocalSet:
                LocalSet(instruction);
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
            case WasmOpcode.I32Load:
                MemoryLoad(instruction, typeof(int));
                break;
            default:
                throw new NotImplementedException($"Opcode {instruction.Opcode} not implemented in compiler.");
        }
    }

    private void MemoryLoad(WasmInstruction instruction, Type t)
    {
        // stack should contain [i32]
        // i32 is the offset
        
        if (instruction.Arguments.Count != 2)
            throw new InvalidOperationException();
        
        if (instruction.Arguments[0] is not WasmNumberValue<int> { Value: var offset })
            throw new InvalidOperationException();
        
        // ignore align for now
        var offsetType = _stack.Pop();
        
        if (offsetType != typeof(int))
        {
            throw new InvalidOperationException(
                $"Memory load expects i32 offset but stack contains {offsetType}");
        }
        
        _il.Emit(OpCodes.Stloc, _memoryStoreLoadOffsetLocalIndex); // store offset in temp local
        
        _il.Emit(OpCodes.Ldarg_0); // load module instance
        _il.Emit(OpCodes.Ldloc, _memoryStoreLoadOffsetLocalIndex); // load static offset from temp local
        _il.Emit(OpCodes.Ldc_I4, offset); // load dynamic offset
        _il.Emit(OpCodes.Ldc_I4_0); // TODO: support storage size opcodes i.e. i32.store8
        _il.Emit(OpCodes.Ldc_I4_0); // TODO: support signExtend, for now assume false
        _il.Emit(OpCodes.Callvirt,
            typeof(ModuleInstance).GetMethod(nameof(ModuleInstance.MemoryLoad))!); // store in memory
        _stack.Push(t);
    }
    
    private void MemoryStore(WasmInstruction instruction, Type t)
    {
        // stack should contain [i32, c]
        // i32 is the offset
        // c is the value to store of type t
        
        if (instruction.Arguments.Count != 2)
            throw new InvalidOperationException();
        
        if (instruction.Arguments[0] is not WasmNumberValue<int> { Value: var offset })
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
        
        _il.Emit(OpCodes.Stloc, _memoryStoreIntLocalIndex); // store arg in temp local
        _il.Emit(OpCodes.Stloc, _memoryStoreLoadOffsetLocalIndex); // store offset in temp local
        
        _il.Emit(OpCodes.Ldarg_0); // load module instance
        _il.Emit(OpCodes.Ldloc, _memoryStoreLoadOffsetLocalIndex); // load offset
        _il.Emit(OpCodes.Ldloc, _memoryStoreIntLocalIndex); // load arg from temp local
        _il.Emit(OpCodes.Ldc_I4, offset); // load offset
        _il.Emit(OpCodes.Ldc_I4_0); // TODO: support storage size opcodes i.e. i32.store8
        _il.Emit(OpCodes.Callvirt,
            typeof(ModuleInstance).GetMethod(nameof(ModuleInstance.MemoryStore))!); // store in memory
    }

    private void RefFunc(WasmInstruction instruction)
    {
        if (instruction.Arguments.Count != 1)
            throw new InvalidOperationException();

        if (instruction.Arguments[0] is not WasmNumberValue<int> { Value: var numberValue })
            throw new InvalidOperationException();

        _il.Emit(OpCodes.Ldarg_0); // load module instance
        _il.Emit(OpCodes.Ldc_I4, numberValue); // load function index
        _il.Emit(OpCodes.Callvirt, typeof(ModuleInstance).GetMethod(nameof(ModuleInstance.GetFunctionReference))!); // get function reference
        // stack now contains function reference

        _stack.Push(typeof(FunctionReference));
    }

    private void DeclareLocals()
    {
        foreach (var local in code.Locals)
        {
            for (var i = 0; i < local.Count; i++)
            {
                _il.DeclareLocal(local.Type.DotNetType);
            }
        }
    }

    private void GlobalSet(WasmInstruction instruction)
    {
        if (instruction.Arguments.Count != 1)
            throw new InvalidOperationException();

        if (instruction.Arguments[0] is not WasmNumberValue<int> numberValue)
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
            _il.Emit(OpCodes.Box, argType); // box value type
        }

        _il.Emit(OpCodes.Stloc, _globalTempLocalIndex); // store arg in temp local

        _il.Emit(OpCodes.Ldarg_0); // load module instance
        _il.Emit(OpCodes.Ldc_I4, globalIndex); // load global index
        _il.Emit(OpCodes.Ldloc, _globalTempLocalIndex); // load arg from temp local
        _il.Emit(OpCodes.Callvirt,
            typeof(ModuleInstance).GetMethod(nameof(ModuleInstance.SetGlobalValue))!); // set global instance
    }

    private void GlobalGet(WasmInstruction instruction)
    {
        if (instruction.Arguments.Count != 1)
            throw new InvalidOperationException();

        if (instruction.Arguments[0] is not WasmNumberValue<int> numberValue)
            throw new InvalidOperationException();

        var globalIndex = numberValue.Value;

        var globalRef = module.GetGlobal(globalIndex);
        var globalType = globalRef.Global.Type.DotNetType;

        _il.Emit(OpCodes.Ldarg_0); // load module instance
        _il.Emit(OpCodes.Ldc_I4, globalIndex); // load global index
        _il.Emit(OpCodes.Callvirt,
            typeof(ModuleInstance).GetMethod(nameof(ModuleInstance.GetGlobalValue))!); // get global instance
        // stack now contains global value

        if (globalType.IsValueType)
        {
            _il.Emit(OpCodes.Unbox_Any, globalRef.Global.Type.DotNetType); // unbox global value
        }

        _stack.Push(globalType);
    }

    private void CallIndirect(WasmInstruction instruction)
    {
        if (instruction.Arguments.Count != 2)
            throw new InvalidOperationException("call_indirect expects two arguments");
        
        if (instruction.Arguments[0] is not WasmNumberValue<int> { Value: var tableIndexValue })
            throw new InvalidOperationException("call_indirect expects first argument to be table index");
        
        if (instruction.Arguments[1] is not WasmNumberValue<int> { Value: var typeIndexValue })
            throw new InvalidOperationException("call_indirect expects second argument to be type index");
        
        var type = module.Types[typeIndexValue];
        var returnType = type.Results.Count == 0 ? typeof(void) : type.Results[0].DotNetType;
        
        PrepareCallArgsArray(type.Parameters.Count);

        // stack now contains the element index int, store to local
        _il.Emit(OpCodes.Stloc, _callIndirectElementLocalIndex);
        _stack.Pop();
        
        _il.Emit(OpCodes.Ldarg_0); // load module instance
        _il.Emit(OpCodes.Ldc_I4, tableIndexValue); // load table index
        _il.Emit(OpCodes.Ldc_I4, typeIndexValue); // load type index
        _il.Emit(OpCodes.Ldloc, _callIndirectElementLocalIndex); // load element index
        _il.Emit(OpCodes.Ldloc, _callArgsLocalIndex); // load args array
        _il.Emit(OpCodes.Callvirt,
            typeof(ModuleInstance).GetMethod(nameof(ModuleInstance.CallIndirect))!); // get function instance
        // stack now contains return value
        _stack.Push(returnType);

        CleanUpCallReturnStack(returnType);
    }

    private void PrepareCallArgsArray(int paramCount)
    {
        _il.Emit(OpCodes.Ldc_I4, paramCount); // num_args = # of args
        _il.Emit(OpCodes.Newarr, typeof(object)); // new object[num_args]
        _il.Emit(OpCodes.Stloc, _callArgsLocalIndex); // store array in local

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
                _il.Emit(OpCodes.Box, stackType); // box value type
            }

            _il.Emit(OpCodes.Stloc, _callTempLocalIndex); // store arg in temp local
            _il.Emit(OpCodes.Ldloc, _callArgsLocalIndex); // load array
            _il.Emit(OpCodes.Ldc_I4, paramIndex); // array index
            _il.Emit(OpCodes.Ldloc, _callTempLocalIndex); // load arg from temp local
            _il.Emit(OpCodes.Stelem_Ref); // args[index] = arg
            _stack.Pop();
        }
    }

    private void Call(WasmInstruction instruction)
    {
        var (funcInstance, funcIndex) = GetFunctionInstanceForCall(module, instruction);

        PrepareCallArgsArray(funcInstance.ParameterTypes.Length);

        _il.Emit(OpCodes.Ldarg_0); // load module instance
        _il.Emit(OpCodes.Ldc_I4, funcIndex); // load function index
        _il.Emit(OpCodes.Callvirt,
            typeof(ModuleInstance).GetMethod(nameof(ModuleInstance.GetFunction))!); // get function instance
        // stack now contains function instance

        _il.Emit(OpCodes.Ldloc, _callArgsLocalIndex); // load args array
        _il.Emit(OpCodes.Callvirt,
            typeof(IFunctionInstance).GetMethod(nameof(IFunctionInstance.Invoke))!); // invoke function
        // stack now contains return value
        _stack.Push(funcInstance.ReturnType);

        CleanUpCallReturnStack(funcInstance.ReturnType);
    }

    private void CleanUpCallReturnStack(Type returnType)
    {
        if (returnType == typeof(void))
        {
            _il.Emit(OpCodes.Pop); // pop return value
            _stack.Pop();
        }
        else if (returnType.IsValueType)
        {
            _il.Emit(OpCodes.Unbox_Any, returnType); // unbox return value
        }
    }

    private void LocalSet(WasmInstruction instruction)
    {
        if (instruction.Arguments.Count != 1)
            throw new InvalidOperationException();

        if (instruction.Arguments[0] is not WasmNumberValue<int> numberValue)
            throw new InvalidOperationException();

        if (numberValue.Value < type.Parameters.Count)
        {
            _il.Emit(OpCodes.Starg, numberValue.Value + 1);
            _stack.Pop();
        }
        else
        {
            _il.Emit(OpCodes.Stloc, numberValue.Value - type.Parameters.Count);
            _stack.Pop();
        }
    }

    private void LocalGet(WasmInstruction instruction)
    {
        if (instruction.Arguments.Count != 1)
            throw new InvalidOperationException();

        if (instruction.Arguments[0] is not WasmNumberValue<int> numberValue)
            throw new InvalidOperationException();

        if (numberValue.Value < type.Parameters.Count)
        {
            _il.Emit(OpCodes.Ldarg, numberValue.Value + 1);
            _stack.Push(type.Parameters[numberValue.Value].DotNetType);
        }
        else
        {
            _il.Emit(OpCodes.Ldloc, numberValue.Value - type.Parameters.Count);
            _stack.Push(code.Locals[numberValue.Value - type.Parameters.Count].Type.DotNetType);
        }
    }

    private void Ceq()
    {
        _il.Emit(OpCodes.Ceq);
        _stack.Pop();
        _stack.Pop();
        _stack.Push(typeof(bool));
    }

    private void ShrUn(WasmInstruction instruction)
    {
        _il.Emit(OpCodes.Shr_Un);
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
        _il.Emit(OpCodes.Shr);
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
        _il.Emit(OpCodes.Shl);
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
        _il.Emit(OpCodes.Xor);
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
        _il.Emit(OpCodes.Or);
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
        _il.Emit(OpCodes.And);
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
        _il.Emit(OpCodes.Rem_Un);
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
        _il.Emit(OpCodes.Rem);
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
        _il.Emit(OpCodes.Div_Un);
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
        _il.Emit(OpCodes.Div);
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
        _il.Emit(OpCodes.Mul);
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
        _il.Emit(OpCodes.Sub);
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
        _il.Emit(OpCodes.Add);
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
        if (instruction.Arguments.Count != 1)
            throw new InvalidOperationException();

        if (instruction.Arguments[0] is not WasmNumberValue<double> numberValue)
            throw new InvalidOperationException();

        _il.Emit(OpCodes.Ldc_R8, numberValue.Value);
        _stack.Push(typeof(double));
    }

    private void LdcR4(WasmInstruction instruction)
    {
        if (instruction.Arguments.Count != 1)
            throw new InvalidOperationException();

        if (instruction.Arguments[0] is not WasmNumberValue<float> numberValue)
            throw new InvalidOperationException();

        _il.Emit(OpCodes.Ldc_R4, numberValue.Value);
        _stack.Push(typeof(float));
    }

    private void LdcI8(WasmInstruction instruction)
    {
        if (instruction.Arguments.Count != 1)
            throw new InvalidOperationException();

        if (instruction.Arguments[0] is not WasmNumberValue<long> numberValue)
            throw new InvalidOperationException();

        _il.Emit(OpCodes.Ldc_I8, numberValue.Value);
        _stack.Push(typeof(long));
    }

    private void LdcI4(WasmInstruction instruction)
    {
        if (instruction.Arguments.Count != 1)
            throw new InvalidOperationException();

        if (instruction.Arguments[0] is not WasmNumberValue<int> numberValue)
            throw new InvalidOperationException();

        _il.Emit(OpCodes.Ldc_I4, numberValue.Value);
        _stack.Push(typeof(int));
    }

    private void Ret()
    {
        _il.Emit(OpCodes.Ret);

        if (method.ReturnType != typeof(void))
        {
            _stack.Pop();
        }
    }

    private static (IFunctionInstance Function, int Index) GetFunctionInstanceForCall(
        ModuleInstance module,
        WasmInstruction instruction)
    {
        if (instruction.Arguments.Count != 1)
            throw new InvalidOperationException();

        if (instruction.Arguments[0] is not WasmNumberValue<int> numberValue)
            throw new InvalidOperationException();

        return (module.GetFunction(numberValue.Value), numberValue.Value);
    }
}