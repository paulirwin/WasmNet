namespace WasmNet.Core;

public class Expression
{
    public required IReadOnlyList<WasmInstruction> Instructions { get; init; }
    
    public string EmitName { get; } = $"Expression_{Guid.NewGuid():N}";

    public override bool Equals(object? obj)
    {
        if (obj is not Expression other) return false;
        if (Instructions.Count != other.Instructions.Count) return false;
        for (var i = 0; i < Instructions.Count; i++)
        {
            if (!Instructions[i].Equals(other.Instructions[i])) return false;
        }
        return true;
    }
    
    public override int GetHashCode()
    {
        var hash = new HashCode();
        
        foreach (var instruction in Instructions)
        {
            hash.Add(instruction);
        }
        
        return hash.ToHashCode();
    }
    
    public IEnumerable<WasmInstruction> FlattenedInstructions()
    {
        foreach (var instruction in Instructions)
        {
            yield return instruction;
            
            foreach (var operand in instruction.Operands)
            {
                if (operand is WasmExpressionValue { Expression: { } expr })
                {
                    foreach (var nestedInstruction in expr.FlattenedInstructions())
                    {
                        yield return nestedInstruction;
                    }
                }
            }
        }
    }
}