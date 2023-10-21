namespace WasmNet.Core;

public class WasmTableSection : WasmModuleSection
{
    public IList<WasmTable> Tables { get; init; } = new List<WasmTable>();
}