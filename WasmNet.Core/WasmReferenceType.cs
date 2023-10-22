namespace WasmNet.Core;

public class WasmReferenceType : WasmValueType
{
    public override bool Equals(object? other) => other is WasmReferenceType;

    public override int GetHashCode() => 0;
}