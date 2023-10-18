namespace WasmNet.Core;

public class ModuleInstance
{
    private readonly List<WasmType> _types = new();
    private readonly List<int> _functionAddresses = new();

    public ModuleInstance(WasmModule module, Store store)
    {
        Module = module;
        Store = store;
    }
    
    public WasmModule Module { get; }
    
    public Store Store { get; }

    public IReadOnlyList<WasmType> Types => _types;

    public IReadOnlyList<int> FunctionAddresses => _functionAddresses;
    
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
}