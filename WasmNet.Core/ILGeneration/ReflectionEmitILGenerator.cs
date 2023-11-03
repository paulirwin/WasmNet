using System.Reflection;
using System.Reflection.Emit;

namespace WasmNet.Core.ILGeneration;

public class ReflectionEmitILGenerator(MethodBuilder method) 
    : IILGenerator
{
    private readonly ILGenerator _il = method.GetILGenerator();
    
    public ILLocal DeclareLocal(Type localType)
    {
        var local = _il.DeclareLocal(localType);
        return new ILLocal(local.LocalIndex);
    }

    public ILLabel DefineLabel()
    {
        var label = _il.DefineLabel();
        return new ReflectionEmitLabel(label);
    }

    public void MarkLabel(ILLabel label)
    {
        if (label is not ReflectionEmitLabel reLabel)
            throw new ArgumentException("Invalid label type", nameof(label));
        
        _il.MarkLabel(reLabel.Label);
    }

    public void Emit(ILOpcode opcode) =>
        _il.Emit(opcode switch
        {
            ILOpcode.Add => OpCodes.Add,
            ILOpcode.And => OpCodes.And,
            ILOpcode.Ceq => OpCodes.Ceq,
            ILOpcode.Cgt => OpCodes.Cgt,
            ILOpcode.CgtUn => OpCodes.Cgt_Un,
            ILOpcode.Clt => OpCodes.Clt,
            ILOpcode.CltUn => OpCodes.Clt_Un,
            ILOpcode.ConvI4 => OpCodes.Conv_I4,
            ILOpcode.ConvI8 => OpCodes.Conv_I8,
            ILOpcode.ConvU8 => OpCodes.Conv_U8,
            ILOpcode.ConvOvfI4 => OpCodes.Conv_Ovf_I4,
            ILOpcode.ConvOvfI8 => OpCodes.Conv_Ovf_I8,
            ILOpcode.ConvOvfI4Un => OpCodes.Conv_Ovf_I4_Un,
            ILOpcode.ConvOvfI8Un => OpCodes.Conv_Ovf_I8_Un,
            ILOpcode.ConvR4 => OpCodes.Conv_R4,
            ILOpcode.ConvR8 => OpCodes.Conv_R8,
            ILOpcode.ConvRUn => OpCodes.Conv_R_Un,
            ILOpcode.Div => OpCodes.Div,
            ILOpcode.DivUn => OpCodes.Div_Un,
            ILOpcode.Dup => OpCodes.Dup,
            ILOpcode.Mul => OpCodes.Mul,
            ILOpcode.Neg => OpCodes.Neg,
            ILOpcode.Nop => OpCodes.Nop,
            ILOpcode.Or => OpCodes.Or,
            ILOpcode.Pop => OpCodes.Pop,
            ILOpcode.Rem => OpCodes.Rem,
            ILOpcode.RemUn => OpCodes.Rem_Un,
            ILOpcode.Ret => OpCodes.Ret,
            ILOpcode.Shl => OpCodes.Shl,
            ILOpcode.Shr => OpCodes.Shr,
            ILOpcode.ShrUn => OpCodes.Shr_Un,
            ILOpcode.StelemRef => OpCodes.Stelem_Ref,
            ILOpcode.Sub => OpCodes.Sub,
            ILOpcode.Throw => OpCodes.Throw,
            ILOpcode.Xor => OpCodes.Xor,
            _ => throw new ArgumentOutOfRangeException(nameof(opcode), opcode, null)
        });

    public void EmitBox(Type type) => _il.Emit(OpCodes.Box, type);

    public void EmitBr(ILLabel label)
    {
        if (label is not ReflectionEmitLabel reLabel)
            throw new ArgumentException("Invalid label type", nameof(label));
        
        _il.Emit(OpCodes.Br, reLabel.Label);
    }

    public void EmitBrTrue(ILLabel label)
    {
        if (label is not ReflectionEmitLabel reLabel)
            throw new ArgumentException("Invalid label type", nameof(label));
        
        _il.Emit(OpCodes.Brtrue, reLabel.Label);
    }

    public void EmitLdarg(int i) => _il.Emit(OpCodes.Ldarg, i);

    public void EmitLdcI4(int i) => _il.Emit(OpCodes.Ldc_I4, i);

    public void EmitLdcI8(long l) => _il.Emit(OpCodes.Ldc_I8, l);

    public void EmitLdcR4(float f) => _il.Emit(OpCodes.Ldc_R4, f);

    public void EmitLdcR8(double d) => _il.Emit(OpCodes.Ldc_R8, d);

    public void EmitLdloc(int i) => _il.Emit(OpCodes.Ldloc, i);

    public void EmitCall(MethodInfo method) => _il.Emit(OpCodes.Call, method);

    public void EmitCallVirt(MethodInfo method) => _il.Emit(OpCodes.Callvirt, method);

    public void EmitNewarr(Type elementType) => _il.Emit(OpCodes.Newarr, elementType);

    public void EmitNewobj(ConstructorInfo ctor) => _il.Emit(OpCodes.Newobj, ctor);

    public void EmitStarg(int i) => _il.Emit(OpCodes.Starg, i);

    public void EmitStloc(int i) => _il.Emit(OpCodes.Stloc, i);

    public void EmitSwitch(IEnumerable<ILLabel> labels) 
        => _il.Emit(OpCodes.Switch, labels.Cast<ReflectionEmitLabel>().Select(l => l.Label).ToArray());

    public void EmitUnboxAny(Type type) => _il.Emit(OpCodes.Unbox_Any, type);
}