using System.Reflection;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace WasmNet.Core.ILGeneration;

public class MonoCecilILGenerator(MethodDefinition methodDefinition) 
    : IILGenerator
{
    private readonly ILProcessor _il = methodDefinition.Body.GetILProcessor();
    
    public ILLocal DeclareLocal(Type localType)
    {
        var variableDefinition = new VariableDefinition(ImportType(localType));
        methodDefinition.Body.Variables.Add(variableDefinition);
        return new ILLocal(variableDefinition.Index);
    }

    public ILLabel DefineLabel()
    {
        var instruction = Instruction.Create(OpCodes.Nop);
        return new MonoCecilLabel(instruction);
    }

    public void MarkLabel(ILLabel label)
    {
        if (label is not MonoCecilLabel monoCecilLabel)
        {
            throw new ArgumentException("Label is not a MonoCecilLabel", nameof(label));
        }
        
        _il.Append(monoCecilLabel.Instruction);
    }

    public void Emit(ILOpcode opcode)
    {
        _il.Append(Instruction.Create(opcode switch
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
        }));
    }

    public void EmitBox(Type type) 
        => _il.Append(Instruction.Create(OpCodes.Box, ImportType(type)));

    public void EmitBr(ILLabel label)
    {
        if (label is not MonoCecilLabel monoCecilLabel)
        {
            throw new ArgumentException("Label is not a MonoCecilLabel", nameof(label));
        }
        
        _il.Append(Instruction.Create(OpCodes.Br, monoCecilLabel.Instruction));
    }

    public void EmitBrTrue(ILLabel label)
    {
        if (label is not MonoCecilLabel monoCecilLabel)
        {
            throw new ArgumentException("Label is not a MonoCecilLabel", nameof(label));
        }
        
        _il.Append(Instruction.Create(OpCodes.Brtrue, monoCecilLabel.Instruction));
    }

    public void EmitLdarg(int i)
    {
        if (i < 0 || i >= methodDefinition.Parameters.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(i), i, "Parameter index is out of range");
        }
        
        _il.Append(Instruction.Create(OpCodes.Ldarg, methodDefinition.Parameters[i]));
    }

    public void EmitLdcI4(int i) => _il.Append(Instruction.Create(OpCodes.Ldc_I4, i));

    public void EmitLdcI8(long l) => _il.Append(Instruction.Create(OpCodes.Ldc_I8, l));

    public void EmitLdcR4(float f) => _il.Append(Instruction.Create(OpCodes.Ldc_R4, f));

    public void EmitLdcR8(double d) => _il.Append(Instruction.Create(OpCodes.Ldc_R8, d));

    public void EmitLdloc(int i)
    {
        if (i < 0 || i >= methodDefinition.Body.Variables.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(i), i, "Variable index is out of range");
        }
        
        _il.Append(Instruction.Create(OpCodes.Ldloc, methodDefinition.Body.Variables[i]));
    }

    public void EmitCall(MethodInfo method) 
        => _il.Append(Instruction.Create(OpCodes.Call, ImportMethod(method)));

    public void EmitCallVirt(MethodInfo method) 
        => _il.Append(Instruction.Create(OpCodes.Callvirt, ImportMethod(method)));

    public void EmitNewarr(Type elementType) 
        => _il.Append(Instruction.Create(OpCodes.Newarr, ImportType(elementType)));

    public void EmitNewobj(ConstructorInfo ctor) 
        => _il.Append(Instruction.Create(OpCodes.Newobj, ImportMethod(ctor)));

    public void EmitStarg(int i)
    {
        if (i < 0 || i >= methodDefinition.Parameters.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(i), i, "Parameter index is out of range");
        }
        
        _il.Append(Instruction.Create(OpCodes.Starg, methodDefinition.Parameters[i]));
    }

    public void EmitStloc(int i)
    {
        if (i < 0 || i >= methodDefinition.Body.Variables.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(i), i, "Variable index is out of range");
        }
        
        _il.Append(Instruction.Create(OpCodes.Stloc, methodDefinition.Body.Variables[i]));
    }

    public void EmitSwitch(IEnumerable<ILLabel> labels)
    {
        var instructions = labels
            .OfType<MonoCecilLabel>()
            .Select(i => i.Instruction)
            .ToArray();
        
        _il.Append(Instruction.Create(OpCodes.Switch, instructions));
    }

    public void EmitUnboxAny(Type type) 
        => _il.Append(Instruction.Create(OpCodes.Unbox_Any, ImportType(type)));
    
    private TypeReference ImportType(Type type) => methodDefinition.Module.ImportReference(type);

    private MethodReference ImportMethod(MethodBase method) => methodDefinition.Module.ImportReference(method);
}