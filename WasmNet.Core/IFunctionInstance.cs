namespace WasmNet.Core;

public interface IFunctionInstance
{
    object? Invoke(params object?[]? args);
    Type ReturnType { get; }
    Type[] ParameterTypes { get; }
}