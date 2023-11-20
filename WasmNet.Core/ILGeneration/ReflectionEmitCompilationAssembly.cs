using System.Reflection;
using System.Reflection.Emit;

namespace WasmNet.Core.ILGeneration;

public class ReflectionEmitCompilationAssembly : ICompilationAssembly
{
    private const TypeAttributes StaticClass = TypeAttributes.Class | TypeAttributes.AutoLayout |
                                               TypeAttributes.AnsiClass | TypeAttributes.Abstract |
                                               TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit;

    public const MethodAttributes PublicStaticMethod =
        MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Static;

    private readonly IDictionary<CompilationType, Compilation> _compilations =
        new Dictionary<CompilationType, Compilation>();

    private readonly IDictionary<string, MethodBuilder> _methodBuilders = new Dictionary<string, MethodBuilder>();

    public ReflectionEmitCompilationAssembly()
    {
        Assembly = AssemblyBuilder.DefineDynamicAssembly(
            new AssemblyName($"WasmAssembly_{Id:N}"),
            AssemblyBuilderAccess.Run
        );

        Module = Assembly.DefineDynamicModule(
            $"WasmModule_{Id:N}"
        );

        foreach (var compilationType in Enum.GetValues<CompilationType>())
        {
            _compilations[compilationType] = new(Module.DefineType(compilationType == CompilationType.Data ? "Data" : $"{compilationType}s", StaticClass));    
        }
    }

    public Guid Id { get; } = Guid.NewGuid();

    public AssemblyBuilder Assembly { get; }

    public ModuleBuilder Module { get; }

    public TypeBuilder GetTypeBuilder(CompilationType type) =>
        _compilations.TryGetValue(type, out var compilation)
            ? compilation.TypeBuilder
            : throw new ArgumentException($"Compilation type {type} does not exist");
    
    public Type GetCompiledType(CompilationType type) =>
        _compilations.TryGetValue(type, out var compilation)
            ? compilation.Type
            : throw new ArgumentException($"Compilation type {type} does not exist");

    private class Compilation(TypeBuilder typeBuilder)
    {
        private Type? _type;

        public TypeBuilder TypeBuilder { get; } = typeBuilder;

        public Type Type
        {
            get
            {
                _type ??= TypeBuilder.CreateType();

                return _type;
            }
        }
    }

    public MethodInfo GetCompiledMethod(CompilationType compilationType, string name)
    {
        var type = GetCompiledType(compilationType);
        
        return type.GetMethod(name, BindingFlags.Public | BindingFlags.Static)
            ?? throw new InvalidOperationException(
                $"Unable to find method {name} in generated {compilationType} holder type");
    }

    public void DeclareMethod(CompilationType compilationType, 
        string name, 
        Type? returnType, 
        IEnumerable<Type> parameterTypes)
    {
        if (_methodBuilders.TryGetValue(name, out var builder))
        {
            throw new InvalidOperationException($"Function {name} already created");
        }

        var paramsWithModule = new[] { typeof(ModuleInstance) }
            .Concat(parameterTypes)
            .ToArray();

        var typeBuilder = GetTypeBuilder(compilationType);
        
        builder = typeBuilder.DefineMethod(name, PublicStaticMethod, returnType, paramsWithModule);
        
        _methodBuilders.Add(name, builder);
    }

    public void CompileDeclaredMethod(ModuleInstance moduleInstance, string name, WasmType type, WasmCode code)
    {
        if (!_methodBuilders.TryGetValue(name, out var builder))
        {
            throw new InvalidOperationException($"Unable to find function builder {name}");
        }

        var compiler = new WasmCompiler(
            new ReflectionEmitILGenerator(builder), 
            moduleInstance, 
            builder.ReturnType, 
            type, 
            code);
        
        compiler.CompileFunction();
    }
}