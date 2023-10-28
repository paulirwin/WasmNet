namespace WasmNet.Core;

public class WasmI32VectorValue(int[] values) : WasmValue
{
    public int[] Values { get; } = values;
    
    public override bool Equals(object? other)
    {
        if (other is not WasmI32VectorValue otherVector)
        {
            return false;
        }

        if (Values.Length != otherVector.Values.Length)
        {
            return false;
        }

        for (int i = 0; i < Values.Length; i++)
        {
            if (Values[i] != otherVector.Values[i])
            {
                return false;
            }
        }

        return true;
    }
    
    public override int GetHashCode()
    {
        unchecked
        {
            var hash = 17;

            foreach (var value in Values)
            {
                hash = hash * 23 + value.GetHashCode();
            }

            return hash;
        }
    }
}