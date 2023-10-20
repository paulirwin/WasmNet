namespace WasmNet.Core;

public class WasmGlobal(WasmValueType type, bool mutable, IReadOnlyList<WasmInstruction> init)
{
    public WasmValueType Type { get; } = type;

    public bool Mutable { get; } = mutable;

    public IReadOnlyList<WasmInstruction> Init { get; } = init;
}