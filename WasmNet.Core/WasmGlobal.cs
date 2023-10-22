namespace WasmNet.Core;

public class WasmGlobal(WasmValueType type, bool mutable, Expression init)
{
    public WasmValueType Type { get; } = type;

    public bool Mutable { get; } = mutable;

    public Expression Init { get; } = init;
}