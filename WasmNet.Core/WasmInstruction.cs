namespace WasmNet.Core;

public class WasmInstruction(WasmOpcode opcode, params WasmValue[] operands)
{
    public WasmOpcode Opcode { get; } = opcode;

    public IList<WasmValue> Operands { get; } = operands;
    
    public override bool Equals(object? other) 
        => other is WasmInstruction instruction 
           && instruction.Opcode == Opcode 
           && instruction.Operands.SequenceEqual(Operands);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Opcode);
        
        foreach (var operand in Operands)
        {
            hash.Add(operand);
        }
        
        return hash.ToHashCode();
    }
}