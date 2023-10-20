namespace WasmNet.Core;

public class WasmGlobalSection : WasmModuleSection
{
    public IList<WasmGlobal> Globals { get; set; } = new List<WasmGlobal>();
}