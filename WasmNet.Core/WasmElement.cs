namespace WasmNet.Core;

public class WasmElement
{
    public required WasmReferenceKind Kind { get; init; }
    
    public required IList<Expression> Init { get; init; }
    
    public required WasmElementMode Mode { get; init; }
    
    public int? TableIndex { get; init; }
    
    public Expression? Offset { get; init; }
}