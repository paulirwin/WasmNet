namespace WasmNet.Core;

public class UnreachableException : Exception
{
    public UnreachableException()
        : base("Unreachable opcode reached.")
    {
    }
}