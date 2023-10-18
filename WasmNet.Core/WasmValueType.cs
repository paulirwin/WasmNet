namespace WasmNet.Core;

public abstract class WasmValueType
{
    public abstract override bool Equals(object? other);
    
    public abstract override int GetHashCode();
}