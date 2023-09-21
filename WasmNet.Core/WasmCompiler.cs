using System.Reflection.Emit;

namespace WasmNet.Core;

public static class WasmCompiler
{
    public static void CompileFunction(WasmModule module, MethodBuilder method, WasmType type, WasmCode code)
    {
        var il = method.GetILGenerator();
        
        foreach (var local in code.Locals)
        {
            for (var i = 0; i < local.Count; i++)
            {
                il.DeclareLocal(local.Type.MapWasmTypeToDotNetType());
            }
        }
        
        foreach (var instruction in code.Body)
        {
            if (instruction.Opcode == WasmOpcode.End)
            {
                il.Emit(OpCodes.Ret);
                continue;
            }
            
            if (instruction.Opcode == WasmOpcode.I32Const)
            {
                if (instruction.Arguments.Count != 1)
                    throw new InvalidOperationException();
                
                if (instruction.Arguments[0] is not WasmNumberValue<int> numberValue)
                    throw new InvalidOperationException();
                
                il.Emit(OpCodes.Ldc_I4, numberValue.Value);
                continue;
            }
            
            if (instruction.Opcode == WasmOpcode.I64Const)
            {
                if (instruction.Arguments.Count != 1)
                    throw new InvalidOperationException();
                
                if (instruction.Arguments[0] is not WasmNumberValue<long> numberValue)
                    throw new InvalidOperationException();
                
                il.Emit(OpCodes.Ldc_I8, numberValue.Value);
                continue;
            }
            
            if (instruction.Opcode == WasmOpcode.F32Const)
            {
                if (instruction.Arguments.Count != 1)
                    throw new InvalidOperationException();
                
                if (instruction.Arguments[0] is not WasmNumberValue<float> numberValue)
                    throw new InvalidOperationException();
                
                il.Emit(OpCodes.Ldc_R4, numberValue.Value);
                continue;
            }
            
            if (instruction.Opcode == WasmOpcode.F64Const)
            {
                if (instruction.Arguments.Count != 1)
                    throw new InvalidOperationException();
                
                if (instruction.Arguments[0] is not WasmNumberValue<double> numberValue)
                    throw new InvalidOperationException();
                
                il.Emit(OpCodes.Ldc_R8, numberValue.Value);
                continue;
            }
            
            if (instruction.Opcode is WasmOpcode.I32Add or WasmOpcode.I64Add or WasmOpcode.F32Add or WasmOpcode.F64Add)
            {
                il.Emit(OpCodes.Add);
                continue;
            }

            if (instruction.Opcode is WasmOpcode.I32Sub or WasmOpcode.I64Sub or WasmOpcode.F32Sub or WasmOpcode.F64Sub)
            {
                il.Emit(OpCodes.Sub);
                continue;
            }
            
            if (instruction.Opcode is WasmOpcode.I32Mul or WasmOpcode.I64Mul or WasmOpcode.F32Mul or WasmOpcode.F64Mul)
            {
                il.Emit(OpCodes.Mul);
                continue;
            }
            
            if (instruction.Opcode is WasmOpcode.F32Div or WasmOpcode.F64Div or WasmOpcode.I32DivS or WasmOpcode.I64DivS)
            {
                il.Emit(OpCodes.Div);
                continue;
            }
            
            if (instruction.Opcode is WasmOpcode.I32DivU or WasmOpcode.I64DivU)
            {
                il.Emit(OpCodes.Div_Un);
                continue;
            }
            
            if (instruction.Opcode is WasmOpcode.I32RemS or WasmOpcode.I64RemS)
            {
                il.Emit(OpCodes.Rem);
                continue;
            }
            
            if (instruction.Opcode is WasmOpcode.I32RemU or WasmOpcode.I64RemU)
            {
                il.Emit(OpCodes.Rem_Un);
                continue;
            }
            
            if (instruction.Opcode is WasmOpcode.I32And or WasmOpcode.I64And)
            {
                il.Emit(OpCodes.And);
                continue;
            }
            
            if (instruction.Opcode is WasmOpcode.I32Or or WasmOpcode.I64Or)
            {
                il.Emit(OpCodes.Or);
                continue;
            }
            
            if (instruction.Opcode is WasmOpcode.I32Xor or WasmOpcode.I64Xor)
            {
                il.Emit(OpCodes.Xor);
                continue;
            }
            
            if (instruction.Opcode is WasmOpcode.I32Shl or WasmOpcode.I64Shl)
            {
                il.Emit(OpCodes.Shl);
                continue;
            }
            
            if (instruction.Opcode is WasmOpcode.I32ShrS or WasmOpcode.I64ShrS)
            {
                il.Emit(OpCodes.Shr);
                continue;
            }
            
            if (instruction.Opcode is WasmOpcode.I32ShrU or WasmOpcode.I64ShrU)
            {
                il.Emit(OpCodes.Shr_Un);
                continue;
            }

            if (instruction.Opcode is WasmOpcode.I32Eq or WasmOpcode.I64Eq)
            {
                il.Emit(OpCodes.Ceq);
                continue;
            }
            
            if (instruction.Opcode == WasmOpcode.LocalGet)
            {
                if (instruction.Arguments.Count != 1)
                    throw new InvalidOperationException();
                
                if (instruction.Arguments[0] is not WasmNumberValue<int> numberValue)
                    throw new InvalidOperationException();

                if (numberValue.Value < type.Parameters.Count)
                {
                    il.Emit(OpCodes.Ldarg, numberValue.Value);
                }
                else
                {
                    il.Emit(OpCodes.Ldloc, numberValue.Value - type.Parameters.Count);
                }
                
                continue;
            }

            if (instruction.Opcode == WasmOpcode.LocalSet)
            {
                if (instruction.Arguments.Count != 1)
                    throw new InvalidOperationException();
                
                if (instruction.Arguments[0] is not WasmNumberValue<int> numberValue)
                    throw new InvalidOperationException();

                if (numberValue.Value < type.Parameters.Count)
                {
                    il.Emit(OpCodes.Starg, numberValue.Value);
                }
                else
                {
                    il.Emit(OpCodes.Stloc, numberValue.Value - type.Parameters.Count);
                }
                
                continue;
            }

            if (instruction.Opcode == WasmOpcode.Call)
            {
                if (instruction.Arguments.Count != 1)
                    throw new InvalidOperationException();

                if (instruction.Arguments[0] is not WasmNumberValue<int> numberValue)
                    throw new InvalidOperationException();

                var callFunc = module.FunctionSection?.Functions[numberValue.Value]
                               ?? throw new InvalidOperationException("Invalid function index.");

                var callMethod = module.EmitAssembly.Value.GetFunctionBuilder(callFunc.EmitName)
                    ?? throw new InvalidOperationException($"Unable to find method {callFunc.EmitName} in generated type");
                
                il.Emit(OpCodes.Call, callMethod);

                continue;
            }
            
            throw new NotImplementedException($"Opcode {instruction.Opcode} not implemented in compiler.");
        }
    }
}