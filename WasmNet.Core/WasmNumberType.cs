namespace WasmNet.Core;

public class WasmNumberType : WasmValueType
{
    public static readonly WasmNumberType I32 = new(typeof(int));
    public static readonly WasmNumberType I64 = new(typeof(long));
    public static readonly WasmNumberType F32 = new(typeof(float));
    public static readonly WasmNumberType F64 = new(typeof(double));

    private WasmNumberType(Type dotNetType)
    {
        DotNetType = dotNetType;
    }
    
    public override bool Equals(object? other)
    {
        if (other is not WasmNumberType type) return false;
        return this == type;
    }

    public override int GetHashCode() => DotNetType.GetHashCode();

    public override Type DotNetType { get; }
}