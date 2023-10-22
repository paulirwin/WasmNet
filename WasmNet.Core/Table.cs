namespace WasmNet.Core;

public class Table
{
    private readonly List<Reference> _elements;

    public Table(int min, int max)
    {
        _elements = Enumerable.Repeat(new NullReference(), min).OfType<Reference>().ToList();
        Min = min;
        Max = max;
    }

    public IReadOnlyList<Reference> Elements => _elements;
    
    public int Min { get; }
    
    public int Max { get; }
    
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