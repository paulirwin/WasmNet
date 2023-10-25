namespace WasmNet.Core;

public class Data
{
    private byte[]? _value;

    public required byte[] Value
    {
        get => _value ?? throw new InvalidOperationException("Data has been dropped");
        init => _value = value;
    }
    
    public required WasmData ModuleData { get; init; }
    
    public bool HasValue => _value != null;
    
    public void Drop()
    {
        _value = null;
        ModuleData.Drop();
    }

    public static Data FromWasmData(WasmData data)
    {
        return new Data
        {
            ModuleData = data,
            Value = data.Data
        };
    }
}