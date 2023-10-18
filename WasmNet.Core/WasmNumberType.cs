namespace WasmNet.Core;

public class WasmNumberType(WasmNumberTypeKind kind) : WasmValueType
{
    public WasmNumberTypeKind Kind { get; } = kind;

    public override bool Equals(object? other) => other is WasmNumberType type && type.Kind == Kind;

    public override int GetHashCode() => Kind.GetHashCode();
}