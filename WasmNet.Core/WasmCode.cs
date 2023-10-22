namespace WasmNet.Core;

public class WasmCode : WasmModuleSection
{
    public IList<WasmLocal> Locals { get; init; } = new List<WasmLocal>();
    
    public required Expression Body { get; init; }
}