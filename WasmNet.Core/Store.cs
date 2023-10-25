namespace WasmNet.Core;

public class Store
{
    private readonly List<IFunctionInstance> _functions = new();
    private readonly List<Global> _globals = new();
    private readonly List<Memory> _memory = new();
    private readonly List<Table> _tables = new();
    private readonly List<Data> _data = new();  

    public IReadOnlyList<IFunctionInstance> Functions => _functions;
    
    public IReadOnlyList<Global> Globals => _globals;
    
    public IReadOnlyList<Memory> Memory => _memory;
    
    public IReadOnlyList<Table> Tables => _tables;
    
    public IReadOnlyList<Data> Data => _data;

    public int AddFunction(IFunctionInstance function)
    {
        var index = _functions.Count;
        _functions.Add(function);
        return index;
    }

    public int AddGlobal(Global global)
    {
        var index = _globals.Count;
        _globals.Add(global);
        return index;
    }
    
    public int AddMemory(Memory memory)
    {
        var index = _memory.Count;
        _memory.Add(memory);
        return index;
    }
    
    public int AddTable(Table table)
    {
        var index = _tables.Count;
        _tables.Add(table);
        return index;
    }
    
    public int AddData(Data data)
    {
        var index = _data.Count;
        _data.Add(data);
        return index;
    }
}