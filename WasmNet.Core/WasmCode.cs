namespace WasmNet.Core;

public class WasmCode : WasmModuleSection
{
    public IList<WasmLocal> Locals { get; init; } = new List<WasmLocal>();
    
    public IList<WasmInstruction> Body { get; init; } = new List<WasmInstruction>();

    public Delegate? MethodDelegate { get; set; }
}