namespace WasmNet.Core;

public class WasmElementSection : WasmModuleSection
{
    public IList<WasmElement> Elements { get; init; } = new List<WasmElement>();
}