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
            
            if (instruction.Opcode is WasmOpcode.I32Add or WasmOpcode.I64Add or WasmOpcode.F32Add or WasmOpcode.F64Add)
            {
                il.Emit(OpCodes.Add);
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
            
            throw new NotImplementedException($"Opcode {instruction.Opcode:X} not implemented.");
        }
        
        var delegateType = Expression.GetDelegateType(parameters.Concat(new[] { returnType }).ToArray());
        
        code.MethodDelegate = method.CreateDelegate(delegateType);
        
        return code.MethodDelegate;
    }
}