namespace WasmNet.Core;

public class WasmCodeSection : WasmModuleSection
{
    public IList<WasmCode> Codes { get; init; } = new List<WasmCode>();
}