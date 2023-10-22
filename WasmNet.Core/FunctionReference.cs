namespace WasmNet.Core;

public class FunctionReference(int address) : Reference
{
    public int Address { get; } = address;

    public override int GetHashCode() => Address.GetHashCode();
    
    public override bool Equals(object? obj) 
        => obj is FunctionReference reference && reference.Address == Address;
}