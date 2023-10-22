namespace WasmNet.Core;

public class Table
{
    public IList<Reference> Elements { get; } = new List<Reference>();
}