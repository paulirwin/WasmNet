namespace WasmNet.Core;

public class WasmExpressionValue(Expression expr) : WasmValue
{
    public Expression Expression { get; } = expr;
    
    public override bool Equals(object? other) 
        => other is WasmExpressionValue value && value.Expression == Expression;

    public override int GetHashCode() => Expression.GetHashCode();
}