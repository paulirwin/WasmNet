namespace WasmNet.Core;

public class WasmFunction
{
    public required uint FunctionSignatureIndex { get; init; }

    public string EmitName { get; } = $"WasmFunction_{Guid.NewGuid():N}";
}