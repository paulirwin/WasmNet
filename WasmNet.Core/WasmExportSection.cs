namespace WasmNet.Core;

public class WasmExportSection : WasmModuleSection
{
    public IList<WasmExport> Exports { get; init; } = new List<WasmExport>();
}