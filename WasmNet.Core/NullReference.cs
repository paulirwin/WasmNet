namespace WasmNet.Core;

public class NullReference : Reference
{
    public override int GetHashCode() => 0;
    
    public override bool Equals(object? obj) => obj is NullReference;
}