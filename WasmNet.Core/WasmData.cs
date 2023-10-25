namespace WasmNet.Core;

public class WasmData
{
    private byte[]? _data;
    
    public required WasmDataKind DataKind { get; set; }
    
    public required int? MemoryIndex { get; init; }
    
    public required Expression? OffsetExpr { get; init; }

    public required byte[] Data
    {
        get => _data ?? throw new InvalidOperationException("Data has been dropped");
        init => _data = value;
    }
    
    public bool HasData => _data != null;
    
    public void Drop() => _data = null;
}