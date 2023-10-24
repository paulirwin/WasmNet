namespace WasmNet.Core;

public abstract class WasmValue
{
    public abstract override bool Equals(object? other);
    
    public abstract override int GetHashCode();
}