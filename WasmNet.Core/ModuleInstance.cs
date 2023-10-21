namespace WasmNet.Core;

public class ModuleInstance(WasmModule module, Store store)
{
    private readonly List<WasmType> _types = new();
    private readonly List<int> _functionAddresses = new();
    private readonly List<int> _memoryAddresses = new();
    private readonly List<(int Address, bool Mutable)> _globalAddresses = new();

    public WasmModule Module { get; } = module;

    public Store Store { get; } = store;

    public IReadOnlyList<WasmType> Types => _types;

    public IReadOnlyList<int> FunctionAddresses => _functionAddresses;
    
    public IReadOnlyList<(int Address, bool Mutable)> GlobalAddresses => _globalAddresses;
    
    public IReadOnlyList<int> MemoryAddresses => _memoryAddresses;
    
    public Lazy<EmitAssembly> EmitAssembly { get; } = new(LazyThreadSafetyMode.ExecutionAndPublication);
    
    public IDictionary<string, IDictionary<string, object?>>? Importables { get; set; }

    public int AddType(WasmType type)
    {
        var index = _types.Count;
        _types.Add(type);
        return index;
    }
    
    public int AddFunctionAddress(int address)
    {
        var index = _functionAddresses.Count;
        _functionAddresses.Add(address);
        return index;
    }
    
    public IFunctionInstance GetFunction(int index)
    {
        var address = _functionAddresses[index];

        return Store.Functions[address];
    }

    public int AddGlobalAddress(int address, bool mutable)
    {
        var index = _globalAddresses.Count;
        _globalAddresses.Add((address, mutable));
        return index;
    }
    
    public object? GetGlobalValue(int index)
    {
        var (address, _) = _globalAddresses[index];

        return Store.Globals[address].Value;
    }
    
    public void SetGlobalValue(int index, object? value)
    {
        var (address, mutable) = _globalAddresses[index];

        if (!mutable)
        {
            throw new InvalidOperationException("Cannot set value of immutable global");
        }
        
        Store.Globals[address].Value = value;
    }

    public (Global Global, bool Mutable) GetGlobal(int index)
    {
        var (address, mutable) = _globalAddresses[index];

        return (Store.Globals[address], mutable);
    }
    
    public int AddMemoryAddress(int address)
    {
        var index = _memoryAddresses.Count;
        _memoryAddresses.Add(address);
        return index;
    }
}