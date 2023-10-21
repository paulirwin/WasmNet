namespace WasmNet.Core;

public class WasmData
{
    public required WasmDataKind DataKind { get; set; }
    
    public required int? MemoryIndex { get; init; }
    
    public required List<WasmInstruction>? OffsetExpr { get; init; }
    
    public required byte[] Data { get; init; }
}