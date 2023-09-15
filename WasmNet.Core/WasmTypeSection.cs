namespace WasmNet.Core;

public class WasmTypeSection : WasmModuleSection
{
    public IList<WasmType> Types { get; init; } = new List<WasmType>();
}