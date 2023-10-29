using System.Numerics;

namespace WasmNet.Core;

public abstract class WasmNumberValue<T>(WasmNumberType type, T value) : WasmValue
    where T : INumber<T>
{
    public WasmNumberType Type { get; } = type;
    
    public T Value { get; } = value;

    public override bool Equals(object? obj) => obj is WasmNumberValue<T> value && value.Value.Equals(Value);
    
    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value.ToString() ?? throw new InvalidOperationException("Value.ToString() returned null");
}

public class WasmI32Value(int value) 
    : WasmNumberValue<int>(WasmNumberType.I32, value);
    
public class WasmI64Value(long value)
    : WasmNumberValue<long>(WasmNumberType.I64, value);

public class WasmF32Value(float value)
    : WasmNumberValue<float>(WasmNumberType.F32, value);
    
public class WasmF64Value(double value)
    : WasmNumberValue<double>(WasmNumberType.F64, value);