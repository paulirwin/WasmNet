namespace WasmNet.Core;

public class Global(WasmValueType type, bool mutable, object? value)
{
    private object? _value = value;
    
    public WasmValueType Type { get; } = type;

    public bool Mutable { get; } = mutable;

    public object? Value
    {
        get => _value;
        set
        {
            if (!Mutable)
            {
                throw new InvalidOperationException("Cannot set value of immutable global");
            }
            
            _value = value;
        }
    }
}