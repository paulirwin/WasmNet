namespace WasmNet.Core;

public class WasmFunctionImportDescriptor : WasmImportDescriptor
{
    public required int TypeIndex { get; init; }
}