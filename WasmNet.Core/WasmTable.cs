namespace WasmNet.Core;

public class WasmTable 
{
    public required WasmReferenceType TableReferenceType { get; init; }

    public required WasmLimits Limits { get; init; }
}