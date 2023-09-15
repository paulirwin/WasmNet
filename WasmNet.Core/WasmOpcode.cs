namespace WasmNet.Core;

public enum WasmOpcode
{
    End = 0x0B,
    LocalGet = 0x20,
    I32Const = 0x41,
    I32Add = 0x6A,
    I32Sub = 0x6B,
    I64Add = 0x7C,
    I64Sub = 0x7D,
    F32Add = 0x92,
    F32Sub = 0x93,
    F64Add = 0xA0,
    F64Sub = 0xA1,
}