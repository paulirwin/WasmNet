namespace WasmNet.Core;

public class WasmLocal
{
    public required int Count { get; init; }
    
    public required WasmValueType Type { get; init; }
}