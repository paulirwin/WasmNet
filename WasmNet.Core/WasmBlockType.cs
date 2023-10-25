namespace WasmNet.Core;

public abstract class WasmBlockType : WasmValue
{
    public class EmptyBlockType : WasmBlockType
    {
        public override bool Equals(object? other) => other is EmptyBlockType;

        public override int GetHashCode() => 0;

        public override string ToString() => "empty";
    }
    
    public class ValueTypeBlockType(WasmValueType valueType) : WasmBlockType
    {
        public WasmValueType ValueType { get; } = valueType;
        
        public override bool Equals(object? other) 
            => other is ValueTypeBlockType type && type.ValueType == ValueType;

        public override int GetHashCode() => ValueType.GetHashCode();
        
        public override string ToString() => ValueType.ToString() ?? "null";
    }
    
    public class FunctionTypeBlockType(WasmType functionType) : WasmBlockType
    {
        public WasmType FunctionType { get; } = functionType;
        
        public override bool Equals(object? other) 
            => other is FunctionTypeBlockType type && type.FunctionType == FunctionType;

        public override int GetHashCode() => FunctionType.GetHashCode();

        public override string ToString() => FunctionType.ToString() ?? "null";
    }
}