namespace WasmNet.Core;

public class ModuleInstance(WasmModule module, Store store)
{
    private readonly List<WasmType> _types = new();
    private readonly List<int> _functionAddresses = new();
    private readonly List<int> _memoryAddresses = new();
    private readonly List<(int Address, bool Mutable)> _globalAddresses = new();
    private readonly List<int> _tableAddresses = new();

    public WasmModule Module { get; } = module;

    public Store Store { get; } = store;

    public IReadOnlyList<WasmType> Types => _types;

    public IReadOnlyList<int> FunctionAddresses => _functionAddresses;
    
    public IReadOnlyList<(int Address, bool Mutable)> GlobalAddresses => _globalAddresses;
    
    public IReadOnlyList<int> MemoryAddresses => _memoryAddresses;
    
    public IReadOnlyList<int> TableAddresses => _tableAddresses;
    
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
    
    public int AddTableAddress(int address)
    {
        var index = _tableAddresses.Count;
        _tableAddresses.Add(address);
        return index;
    }
    
    /// <summary>
    /// Performs an indirect call to a function.
    ///
    /// The opcode call_indirect is in the docs with two arguments:
    /// - x: the index of the table address
    /// - y: the index of the function type
    ///
    /// It then expects the next value on the stack to be the index
    /// of the element in the table. In this case, we're going to
    /// take it as an argument and evaluate this in .NET code.
    /// </summary>
    /// <param name="tableIndex">The index of the table address</param>
    /// <param name="typeIndex">The index of the function type</param>
    /// <param name="elementIndex">The index of the element in the table</param>
    /// <param name="arguments">The arguments to pass to the function</param>
    /// <returns>The result of the function</returns>
    public object? CallIndirect(int tableIndex, 
        int typeIndex, 
        int elementIndex, 
        params object?[]? arguments)
    {
        var tableAddress = _tableAddresses[tableIndex];
        var table = Store.Tables[tableAddress];
        var functionType = _types[typeIndex];
        var element = table.Elements[elementIndex];
        
        if (element is NullReference)
        {
            throw new InvalidOperationException("Cannot call null reference");
        }

        if (element is not FunctionReference functionReference)
        {
            throw new InvalidOperationException("Cannot call non-function reference");
        }
        
        var function = Store.Functions[functionReference.Address];

        if (function.ParameterTypes.Length != functionType.Parameters.Count)
        {
            // TODO: also check the parameter types
            throw new InvalidOperationException("Function parameter count mismatch");
        }

        if (function.ReturnType != functionType.Results[0].DotNetType)
        {
            throw new InvalidOperationException("Function return type mismatch");
        }

        var functionArguments = arguments?.ToArray() ?? Array.Empty<object?>();
        var functionResult = function.Invoke(functionArguments);
        return functionResult;
    }
    
    public FunctionReference GetFunctionReference(int index)
    {
        var address = _functionAddresses[index];

        return new FunctionReference(address);
    }
}