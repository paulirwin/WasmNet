namespace WasmNet.Core;

public class Store
{
    private readonly List<IFunctionInstance> _functions = new();

    public IReadOnlyList<IFunctionInstance> Functions => _functions;
    
    public int AddFunction(IFunctionInstance function)
    {
        var index = _functions.Count;
        _functions.Add(function);
        return index;
    }
}