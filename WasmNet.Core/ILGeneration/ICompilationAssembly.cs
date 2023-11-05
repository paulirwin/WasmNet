using System.Reflection;

namespace WasmNet.Core.ILGeneration;

public interface ICompilationAssembly
{
    MethodInfo GetCompiledMethod(CompilationType compilationType, string name);

    void DeclareMethod(CompilationType compilationType, 
        string name, 
        Type? returnType, 
        IEnumerable<Type> parameterTypes);

    void CompileDeclaredMethod(ModuleInstance moduleInstance,
        string name,
        WasmType type,
        WasmCode code);

    public void DeclareAndCompileMethod(
        ModuleInstance moduleInstance,
        CompilationType compilationType,
        string name,
        Type? returnType,
        IEnumerable<Type> parameterTypes,
        WasmType wasmType,
        WasmCode code)
    {
        DeclareMethod(compilationType, name, returnType, parameterTypes);
        CompileDeclaredMethod(moduleInstance, name, wasmType, code);
    }
}