namespace WasmNet.Core;

public class Table(int min, int max)
{
    private readonly List<Reference> _elements = new(min);

    public IReadOnlyList<Reference> Elements => _elements;
    
    public int Min { get; } = min;
    
    public int Max { get; } = max;
    
    public int Count => _elements.Count;
    
    public void Add(Reference reference)
    {
        if (_elements.Count >= Max)
        {
            throw new InvalidOperationException("Table is full");
        }
        
        _elements.Add(reference);
    }
    
    public Reference this[int index]
    {
        get => _elements[index];
        set => _elements[index] = value;
    }
}