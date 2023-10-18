namespace WasmNet.Core;

public class WasmGlobalImportDescriptor : WasmImportDescriptor
{
    public required WasmValueType Type { get; init; }
    
    public required bool Mutable { get; init; }
}