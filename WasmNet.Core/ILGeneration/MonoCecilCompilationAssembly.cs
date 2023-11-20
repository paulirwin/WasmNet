using System.Reflection;
using Mono.Cecil;
using MethodAttributes = Mono.Cecil.MethodAttributes;
using TypeAttributes = Mono.Cecil.TypeAttributes;

namespace WasmNet.Core.ILGeneration;

public class MonoCecilCompilationAssembly : ICompilationAssembly, ISavableAssembly
{
    private const TypeAttributes StaticClass = TypeAttributes.Class | TypeAttributes.AutoLayout |
                                               TypeAttributes.AnsiClass | TypeAttributes.Abstract |
                                               TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit;
    
    private readonly IDictionary<CompilationType, TypeDefinition> _typeDefinitions =
        new Dictionary<CompilationType, TypeDefinition>();
    
    private readonly IDictionary<string, MethodDefinition> _methods = new Dictionary<string, MethodDefinition>();

    public MonoCecilCompilationAssembly()
    {
        var assemblyName = $"WasmAssembly_{Id:N}";
        
        Assembly = AssemblyDefinition.CreateAssembly(
            new AssemblyNameDefinition(assemblyName, new Version(1, 0)), 
            assemblyName, 
            ModuleKind.Dll);

        Module = Assembly.MainModule;
        
        var objectType = Module.ImportReference(typeof(object));

        foreach (var compilationType in Enum.GetValues<CompilationType>())
        {
            var typeDefinition = new TypeDefinition(
                @namespace: assemblyName, 
                name: $"Wasm{compilationType}Holder_{Id:N}", 
                attributes: StaticClass,
                baseType: objectType);
            
            _typeDefinitions[compilationType] = typeDefinition;
            Module.Types.Add(typeDefinition);
        }
    }

    public Guid Id { get; } = Guid.NewGuid();
    
    public AssemblyDefinition Assembly { get; }
    
    public ModuleDefinition Module { get; }
    
    public void DeclareMethod(CompilationType compilationType,
        string name,
        Type? returnType,
        IEnumerable<Type> parameterTypes)
    {
        if (!_typeDefinitions.TryGetValue(compilationType, out var compilation))
        {
            throw new ArgumentException($"Compilation type {compilationType} does not exist");
        }

        var methodDefinition = new MethodDefinition(name, 
            MethodAttributes.Public | MethodAttributes.Static, 
            ImportType(returnType ?? typeof(void)));
        
        methodDefinition.Parameters.Add(new ParameterDefinition(ImportType(typeof(ModuleInstance))));
        
        foreach (var parameterType in parameterTypes)
        {
            methodDefinition.Parameters.Add(new ParameterDefinition(ImportType(parameterType)));
        }
        
        compilation.Methods.Add(methodDefinition);
        _methods[name] = methodDefinition;
    }

    public void CompileDeclaredMethod(ModuleInstance moduleInstance, 
        string name, 
        WasmType type, 
        WasmCode code)
    {
        if (!_methods.TryGetValue(name, out var builder))
        {
            throw new InvalidOperationException($"Unable to find method definition {name}");
        }

        var compiler = new WasmCompiler(
            new MonoCecilILGenerator(builder), 
            moduleInstance, 
            type.Results.Count == 0 ? typeof(void) : type.Results[0].DotNetType, 
            type, 
            code);
        
        compiler.CompileFunction();
    }

    public void SaveAssembly(string path) => Assembly.Write(path);

    public MethodInfo GetCompiledMethod(CompilationType compilationType, string name)
    {
        var type = BuildType(compilationType);
        
        return type.GetMethod(name, BindingFlags.Public | BindingFlags.Static)
            ?? throw new InvalidOperationException(
                $"Unable to find method {name} in generated {compilationType} holder type");
    }
    
    private TypeReference ImportType(Type type) => Module.ImportReference(type);
    
    private Type BuildType(CompilationType compilationType)
    {
        if (!_typeDefinitions.TryGetValue(compilationType, out var typeDefinition))
        {
            throw new ArgumentException($"Compilation type {compilationType} does not exist");
        }
        
        // TODO.PI: cache generated types
        using var ms = new MemoryStream();
        
        Assembly.Write(ms);
        
        var assembly = System.Reflection.Assembly.Load(ms.ToArray());
        
        return assembly.GetType(typeDefinition.FullName) 
               ?? throw new InvalidOperationException($"Unable to find generated type {typeDefinition.FullName}");
    }
}