namespace WasmNet.Core;

public class WasmImportSection : WasmModuleSection
{
    public IList<WasmImport> Imports { get; } = new List<WasmImport>();
    
    public IList<WasmImport> FunctionImports => Imports.Where(x => x.Kind == WasmImportKind.Function).ToList();
}