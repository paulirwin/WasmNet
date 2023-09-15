namespace WasmNet.Core;

public class WasmInstruction
{
    public WasmInstruction(WasmOpcode opcode, params WasmValueType[] args)
    {
        Opcode = opcode;
        Arguments = args;
    }

    public WasmOpcode Opcode { get; }
    
    public IList<WasmValueType> Arguments { get; }
}