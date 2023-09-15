namespace WasmNet.Core;

public class WasmFunctionSection : WasmModuleSection
{
    public IList<WasmFunction> Functions { get; init; } = new List<WasmFunction>();
}