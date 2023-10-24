using System.Reflection;

namespace WasmNet.Core;

public class WasmFunctionInstance(WasmType type, ModuleInstance module, WasmCode code) 
    : IFunctionInstance
{
    public WasmType Type { get; } = type;

    public ModuleInstance Module { get; } = module;

    public WasmCode Code { get; } = code;

    public string EmitName { get; } = $"WasmFunction_{Guid.NewGuid():N}";

    public Type ReturnType =>
        Type.Results.Count == 0
            ? typeof(void)
            : Type.Results[0].DotNetType;

    public Type[] ParameterTypes => Type.Parameters.Select(x => x.DotNetType).ToArray();

    public object? Invoke(params object?[]? args)
    {
        var method = Module.EmitAssembly.Value.FunctionHolderType.GetMethod(EmitName,
            BindingFlags.Public | BindingFlags.Static);

        if (method == null)
        {
            throw new InvalidOperationException(
                $"Unable to find method {EmitName} in generated function holder type");
        }
        
        var argsWithModule = new[] { Module }
            .Concat(args ?? Array.Empty<object?>())
            .ToArray();

        return method.Invoke(null, argsWithModule);
    }
}