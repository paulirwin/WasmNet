namespace WasmNet.Core;

public class WasmCode : WasmModuleSection
{
    public required uint LocalDeclarationCount { get; init; }
    
    public IList<WasmInstruction> Body { get; init; } = new List<WasmInstruction>();

    public Delegate? MethodDelegate { get; set; }
}