namespace WasmNet.Core;

public class WasmNumberType : WasmValueType
{
    public WasmNumberType(WasmNumberTypeKind kind)
    {
        Kind = kind;
    }

    public WasmNumberTypeKind Kind { get; }
}