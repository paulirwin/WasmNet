using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;

namespace WasmNet.Core;

public static class WasmCompiler
{
    public static Delegate CompileFunction(WasmModule module, WasmType type, WasmCode code)
    {
        var returnType = type.Results.Count == 0 
            ? typeof(void) 
            : type.Results[0].MapWasmTypeToDotNetType();
        
        var parameters = type.Parameters.Select(x => x.MapWasmTypeToDotNetType()).ToArray();

        if (module.DynamicModule is not { } dynamicModule)
        {
            dynamicModule = module.DynamicModule = AssemblyBuilder.DefineDynamicAssembly(
                new AssemblyName($"WasmModule_{Guid.NewGuid():N}"),
                AssemblyBuilderAccess.Run
            ).DefineDynamicModule(
                $"WasmModule_{Guid.NewGuid():N}"
            );
        }
        
        var method = new DynamicMethod(
            $"WasmFunction_{Guid.NewGuid():N}",
            returnType,
            parameters,
            dynamicModule,
            true
        );
        
        var il = method.GetILGenerator();
        
        // TODO: locals
        
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
                    throw new NotImplementedException("local.get for locals not implemented.");
                }
                
                continue;
            }
            
            throw new NotImplementedException($"Opcode {instruction.Opcode} not implemented in compiler.");
        }
        
        var delegateType = Expression.GetDelegateType(parameters.Concat(new[] { returnType }).ToArray());
        
        code.MethodDelegate = method.CreateDelegate(delegateType);
        
        return code.MethodDelegate;
    }
}