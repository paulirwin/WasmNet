namespace WasmNet.Core;

public class Store
{
    private readonly List<IFunctionInstance> _functions = new();
    private readonly List<Global> _globals = new();

    public IReadOnlyList<IFunctionInstance> Functions => _functions;
    
    public IReadOnlyList<Global> Globals => _globals;
    
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
}