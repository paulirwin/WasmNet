namespace WasmNet.Core;

public class WasmMemoryImportDescriptor : WasmImportDescriptor
{
    public required WasmLimits Limits { get; init; }
}