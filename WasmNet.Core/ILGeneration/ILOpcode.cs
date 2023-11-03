namespace WasmNet.Core.ILGeneration;

/// <summary>
/// An enumeration of all the basic, parameterless IL opcodes.
/// </summary>
public enum ILOpcode
{
    Add,
    And,
    Ceq,
    Cgt,
    CgtUn,
    Clt,
    CltUn,
    ConvI4,
    ConvI8,
    ConvU8,
    ConvOvfI4,
    ConvOvfI8,
    ConvOvfI4Un,
    ConvOvfI8Un,
    ConvR4,
    ConvR8,
    ConvRUn,
    Div,
    DivUn,
    Dup,
    Mul,
    Neg,
    Nop,
    Or,
    Pop,
    Rem,
    RemUn,
    Ret,
    Shl,
    Shr,
    ShrUn,
    StelemRef,
    Sub,
    Throw,
    Xor,
}