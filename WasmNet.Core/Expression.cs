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
}