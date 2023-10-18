namespace WasmNet.Core;

public class HostFunctionInstance : IFunctionInstance
{
    public HostFunctionInstance(WasmType type, Delegate hostCode)
    {
        Type = type;
        HostCode = hostCode;
    }

    public WasmType Type { get; }
    
    public Delegate HostCode { get; }

    public Type ReturnType => HostCode.Method.ReturnType;

    public Type[] ParameterTypes => HostCode.Method.GetParameters()
        .Select(i => i.ParameterType)
        .ToArray();
    
    // TODO: validate arguments and return type?
    public object? Invoke(params object?[]? args) => HostCode.DynamicInvoke(args);
}