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
        
        // TODO: parameters

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
            Array.Empty<Type>(),
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
                
                if (instruction.Arguments[0] is not WasmNumberValue<uint> numberValue)
                    throw new InvalidOperationException();
                
                il.Emit(OpCodes.Ldc_I4, numberValue.Value);
                continue;
            }
            
            throw new NotImplementedException($"Opcode {instruction.Opcode:X} not implemented.");
        }
        
        var delegateType = typeof(Func<>).MakeGenericType(returnType);
        
        code.MethodDelegate = method.CreateDelegate(delegateType);
        
        return code.MethodDelegate;
    }
}