namespace WasmNet.Core;

public class WasmExport : WasmModuleSection
{
    public required string Name { get; init; }
    
    public required WasmExportKind Kind { get; init; }
    
    public required int Index { get; init; }
}