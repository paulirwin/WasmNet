namespace WasmNet.Core;

public class WasmTable 
{
    public required WasmReferenceKind TableReferenceKind { get; init; }

    public required WasmLimits Limits { get; init; }
}