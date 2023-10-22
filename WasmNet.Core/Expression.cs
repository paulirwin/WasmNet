namespace WasmNet.Core;

public class Expression
{
    public required IReadOnlyList<WasmInstruction> Instructions { get; init; }
    
    public string EmitName { get; } = $"Expression_{Guid.NewGuid():N}";
}