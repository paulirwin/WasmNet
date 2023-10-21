namespace WasmNet.Core;

public class WasmDataSection : WasmModuleSection
{
    public IList<WasmData> Data { get; set; } = new List<WasmData>();
}