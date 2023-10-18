namespace WasmNet.Core;

public class WasmImport
{
    public required string ModuleName { get; init; }

    public required string Name { get; init; }

    public required WasmImportKind Kind { get; init; }
    
    public required WasmImportDescriptor Descriptor { get; init; }
}