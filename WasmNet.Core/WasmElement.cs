namespace WasmNet.Core;

public class WasmElement
{
    public required WasmReferenceType Type { get; init; }
    
    public required IList<IList<WasmInstruction>> Init { get; init; }
    
    public required WasmElementMode Mode { get; init; }
    
    public int? TableIndex { get; init; }
    
    public IList<WasmInstruction>? Offset { get; init; }
}