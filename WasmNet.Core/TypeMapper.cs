namespace WasmNet.Core;

public static class TypeMapper
{
    public static Type MapWasmTypeToDotNetType(this WasmValueType type)
    {
        return type switch
        {
            WasmNumberType numberType => numberType.Kind switch
            {
                WasmNumberTypeKind.I32 => typeof(int),
                WasmNumberTypeKind.I64 => typeof(long),
                WasmNumberTypeKind.F32 => typeof(float),
                WasmNumberTypeKind.F64 => typeof(double),
                _ => throw new ArgumentOutOfRangeException()
            },
            WasmReferenceType _ => typeof(Reference),
            _ => throw new ArgumentOutOfRangeException()
        };
    }
}