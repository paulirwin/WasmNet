namespace WasmNet.Core;

public enum WasmOpcode
{
    End = 0x0B,
    LocalGet = 0x20,
    I32Const = 0x41,
    I64Const = 0x42,
    F32Const = 0x43,
    F64Const = 0x44,
    I32Add = 0x6A,
    I32Sub = 0x6B,
    I32Mul = 0x6C,
    I64Add = 0x7C,
    I64Sub = 0x7D,
    I64Mul = 0x7E,
    F32Add = 0x92,
    F32Sub = 0x93,
    F32Mul = 0x94,
    F64Add = 0xA0,
    F64Sub = 0xA1,
    F64Mul = 0xA2,
}