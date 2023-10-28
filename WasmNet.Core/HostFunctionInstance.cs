namespace WasmNet.Core;

public class HostFunctionInstance(WasmType type, Delegate hostCode) : IFunctionInstance
{
    public WasmType Type { get; } = type;

    public Delegate HostCode { get; } = hostCode;

    public Type ReturnType => HostCode.Method.ReturnType;

    public Type[] ParameterTypes => HostCode.Method.GetParameters()
        .Select(i => i.ParameterType)
        .ToArray();
    
    // TODO: validate arguments and return type?
    public object? Invoke(params object?[]? args) => HostCode.DynamicInvoke(args);
}