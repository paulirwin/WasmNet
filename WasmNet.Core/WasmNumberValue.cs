using System.Numerics;

namespace WasmNet.Core;

public class WasmNumberValue<T> : WasmNumberType
    where T : INumber<T>
{
    public WasmNumberValue(WasmNumberTypeKind kind, T value)
        : base(kind)
    {
        Value = value;
    }
    
    public T Value { get; }
}