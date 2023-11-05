using System.Reflection;
using System.Reflection.Emit;

namespace WasmNet.Core;

public class EmitAssembly
{
    private const TypeAttributes StaticClass = TypeAttributes.Class | TypeAttributes.AutoLayout |
                                               TypeAttributes.AnsiClass | TypeAttributes.Abstract |
                                               TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit;

    public const MethodAttributes PublicStaticMethod =
        MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Static;

    private readonly IDictionary<CompilationType, Compilation> _compilations =
        new Dictionary<CompilationType, Compilation>();

    private readonly IDictionary<string, MethodBuilder> _methodBuilders = new Dictionary<string, MethodBuilder>();

    public EmitAssembly()
    {
        Assembly = AssemblyBuilder.DefineDynamicAssembly(
            new AssemblyName($"WasmAssembly_{Id:N}"),
            AssemblyBuilderAccess.Run
        );

        Module = Assembly.DefineDynamicModule(
            $"WasmModule_{Id:N}"
        );

        _compilations[CompilationType.Function] = new(Module.DefineType($"WasmFunctionHolder_{Id:N}", StaticClass));
        _compilations[CompilationType.Global] = new(Module.DefineType($"WasmGlobalHolder_{Id:N}", StaticClass));
        _compilations[CompilationType.Data] = new(Module.DefineType($"WasmDataHolder_{Id:N}", StaticClass));
        _compilations[CompilationType.Element] = new(Module.DefineType($"WasmElementHolder_{Id:N}", StaticClass));
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

    public MethodBuilder CreateFunctionBuilder(string name, Type? returnType, Type[] parameters)
    {
        if (_methodBuilders.TryGetValue(name, out var builder))
        {
            throw new InvalidOperationException($"Function {name} already created");
        }

        var paramsWithModule = new[] { typeof(ModuleInstance) }
            .Concat(parameters)
            .ToArray();

        var typeBuilder = GetTypeBuilder(CompilationType.Function);
        
        builder = typeBuilder.DefineMethod(name, PublicStaticMethod, returnType, paramsWithModule);
        
        _methodBuilders.Add(name, builder);

        return builder;
    }
    
    public MethodBuilder CreateGlobalBuilder(CompilationType type, string name, Type? returnType)
    {
        if (_methodBuilders.TryGetValue(name, out var builder))
        {
            throw new InvalidOperationException($"Function {name} already created");
        }

        var typeBuilder = GetTypeBuilder(type);

        builder = typeBuilder.DefineMethod(name, PublicStaticMethod, returnType, new[] { typeof(ModuleInstance) });
        
        _methodBuilders.Add(name, builder);

        return builder;
    }

    public MethodBuilder? GetFunctionBuilder(string name) =>
        _methodBuilders.TryGetValue(name, out var builder)
            ? builder
            : null;

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
}