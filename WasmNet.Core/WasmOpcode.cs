namespace WasmNet.Core;

public enum WasmOpcode
{
    I32Const = 0x41,
    I32Add = 0x6A,
    End = 0x0B,
    LocalGet = 0x20,
    I64Add = 0x7C,
    F32Add = 0x92,
    F64Add = 0xA0,
}