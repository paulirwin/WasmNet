using System.Reflection;

namespace WasmNet.Core.ILGeneration;

public interface IILGenerator
{
    ILLocal DeclareLocal(Type localType);
    
    ILLabel DefineLabel();
    
    void MarkLabel(ILLabel label);

    void Emit(ILOpcode opcode);
    
    void EmitBox(Type type);
    
    void EmitBr(ILLabel label);
    
    void EmitBrTrue(ILLabel label);
    
    void EmitBrFalse(ILLabel label);
    
    void EmitLdarg(int i);
    
    void EmitLdcI4(int i);
    
    void EmitLdcI8(long l);
    
    void EmitLdcR4(float f);
    
    void EmitLdcR8(double d);
    
    void EmitLdloc(int i);
    
    void EmitCall(MethodInfo method);
    
    void EmitCallVirt(MethodInfo method);
    
    void EmitNewarr(Type elementType);
    
    void EmitNewobj(ConstructorInfo ctor);
    
    void EmitStarg(int i);
    
    void EmitStloc(int i);
    
    void EmitSwitch(IEnumerable<ILLabel> labels);
    
    void EmitUnboxAny(Type type);
}