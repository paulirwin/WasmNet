namespace WasmNet.Core;

public class WasmModule
{
    public WasmTypeSection? TypeSection { get; set; }
    
    public WasmImportSection? ImportSection { get; set; }

    public WasmFunctionSection? FunctionSection { get; set; }

    public WasmExportSection? ExportSection { get; set; }

    public WasmCodeSection? CodeSection { get; set; }
    
    public WasmGlobalSection? GlobalSection { get; set; }
    
    public WasmDataSection? DataSection { get; set; }
    
    public WasmMemorySection? MemorySection { get; set; }
    
    public WasmTableSection? TableSection { get; set; }
    
    public WasmElementSection? ElementsSection { get; set; }
    
    public WasmDataCountSection? DataCountSection { get; set; }
    
    public WasmStartSection? StartSection { get; set; }
}