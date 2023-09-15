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
    I64Add = 0x7C,
    I64Sub = 0x7D,
    F32Add = 0x92,
    F32Sub = 0x93,
    F64Add = 0xA0,
    F64Sub = 0xA1,
}