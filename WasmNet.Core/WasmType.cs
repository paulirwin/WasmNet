namespace WasmNet.Core;

public class WasmType
{
    public required WasmTypeKind Kind { get; init; }
    
    public IList<WasmValueType> Parameters { get; init; } = new List<WasmValueType>();
    
    public IList<WasmValueType> Results { get; init; } = new List<WasmValueType>();
}