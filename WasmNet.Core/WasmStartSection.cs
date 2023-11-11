namespace WasmNet.Core;

public class WasmStartSection : WasmModuleSection
{
    public required int FuncIndex { get; init; }
}