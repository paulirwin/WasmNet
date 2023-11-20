namespace WasmNet.Core;

public class WasmFunctionInstance(int index, WasmType type, ModuleInstance module, WasmCode code) 
    : IFunctionInstance
{
    public WasmType Type { get; } = type;

    public ModuleInstance Module { get; } = module;

    public WasmCode Code { get; } = code;

    public string EmitName { get; } = $"WasmFunction_{index}";
    
    public string Name => module.Module.ExportSection?.Exports
        .FirstOrDefault(x => x.Index == index)
        ?.Name ?? EmitName;

    public Type ReturnType =>
        Type.Results.Count == 0
            ? typeof(void)
            : Type.Results[0].DotNetType;

    public Type[] ParameterTypes => Type.Parameters.Select(x => x.DotNetType).ToArray();

    public object? Invoke(params object?[]? args)
    {
        var method = Module.CompilationAssembly.GetCompiledMethod(CompilationType.Function, Name);
        
        var argsWithModule = new[] { Module }
            .Concat(args ?? Array.Empty<object?>())
            .ToArray();

        return method.Invoke(null, argsWithModule);
    }
}