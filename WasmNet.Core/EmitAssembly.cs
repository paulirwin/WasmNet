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

    private Type? _funcTypeInfo;
    private Type? _globalTypeInfo;
    private Type? _dataTypeInfo;
    private Type? _elementTypeInfo;

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

        FunctionHolder = Module.DefineType($"WasmFunctionHolder_{Id:N}", StaticClass);
        GlobalHolder = Module.DefineType($"WasmGlobalHolder_{Id:N}", StaticClass);
        DataHolder = Module.DefineType($"WasmDataHolder_{Id:N}", StaticClass);
        ElementHolder = Module.DefineType($"WasmElementHolder_{Id:N}", StaticClass);
    }

    public Guid Id { get; } = Guid.NewGuid();

    public AssemblyBuilder Assembly { get; }

    public ModuleBuilder Module { get; }

    public TypeBuilder FunctionHolder { get; }
    
    public TypeBuilder GlobalHolder { get; }
    
    public TypeBuilder DataHolder { get; }
    
    public TypeBuilder ElementHolder { get; }

    public Type FunctionHolderType
    {
        get
        {
            _funcTypeInfo ??= FunctionHolder.CreateType();

            return _funcTypeInfo;
        }
    }
    
    public Type GlobalHolderType
    {
        get
        {
            _globalTypeInfo ??= GlobalHolder.CreateType();

            return _globalTypeInfo;
        }
    }
    
    public Type DataHolderType
    {
        get
        {
            _dataTypeInfo ??= DataHolder.CreateType();

            return _dataTypeInfo;
        }
    }
    
    public Type ElementHolderType
    {
        get
        {
            _elementTypeInfo ??= ElementHolder.CreateType();

            return _elementTypeInfo;
        }
    }

    public MethodBuilder CreateFunctionBuilder(string name, Type? returnType, Type[] parameters)
    {
        if (_methodBuilders.TryGetValue(name, out var builder))
        {
            throw new InvalidOperationException($"Function {name} already created");
        }

        var paramsWithModule = new[] { typeof(ModuleInstance) }
            .Concat(parameters)
            .ToArray();
        
        builder = FunctionHolder.DefineMethod(name, PublicStaticMethod, returnType, paramsWithModule);
        
        _methodBuilders.Add(name, builder);

        return builder;
    }
    
    public MethodBuilder CreateGlobalBuilder(TypeBuilder typeBuilder, string name, Type? returnType)
    {
        if (_methodBuilders.TryGetValue(name, out var builder))
        {
            throw new InvalidOperationException($"Function {name} already created");
        }

        builder = typeBuilder.DefineMethod(name, PublicStaticMethod, returnType, new[] { typeof(ModuleInstance) });
        
        _methodBuilders.Add(name, builder);

        return builder;
    }

    public MethodBuilder? GetFunctionBuilder(string name) =>
        _methodBuilders.TryGetValue(name, out var builder)
            ? builder
            : null;
}