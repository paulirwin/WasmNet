namespace WasmNet.Core;

public class WasmInstruction(WasmOpcode opcode, params WasmValue[] args)
{
    public WasmOpcode Opcode { get; } = opcode;

    public IList<WasmValue> Arguments { get; } = args;
}