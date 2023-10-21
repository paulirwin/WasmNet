namespace WasmNet.Core;

public class WasmMemoryImportDescriptor : WasmImportDescriptor
{
    public required WasmMemoryLimits Limits { get; init; }
}