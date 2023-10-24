namespace WasmNet.Core;

public class WasmReferenceType : WasmValueType
{
    public static readonly WasmReferenceType FuncRef = new(typeof(FunctionReference));
    // TODO: ExternRef

    private WasmReferenceType(Type dotNetType)
    {
        DotNetType = dotNetType;
    }
    
    public override bool Equals(object? other)
    {
        if (other is not WasmReferenceType type) return false;
        return DotNetType == type.DotNetType;
    }

    public override int GetHashCode() => DotNetType.GetHashCode();

    public override Type DotNetType { get; }
}