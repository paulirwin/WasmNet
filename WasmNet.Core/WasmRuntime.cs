using System.Reflection;

namespace WasmNet.Core;

public class WasmRuntime
{
    private readonly IList<WasmModule> _modules = new List<WasmModule>();
    
    public async Task LoadModuleAsync(string path)
    {
        var bytes = await File.ReadAllBytesAsync(path);
        
        await using var ms = new MemoryStream(bytes);
        
        var reader = new WasmReader(ms);
        
        var module = reader.ReadModule();
        
        CompileModule(module);
        
        _modules.Add(module);
    }

    private static void CompileModule(WasmModule module)
    {
        ForEachFunction(module, (func, type, _) =>
        {
            var returnType = type.Results.Count == 0
                ? typeof(void)
                : type.Results[0].MapWasmTypeToDotNetType();

            var parameters = type.Parameters.Select(x => x.MapWasmTypeToDotNetType()).ToArray();

            module.EmitAssembly.Value.CreateFunctionBuilder(
                func.EmitName,
                returnType,
                parameters
            );
        });

        ForEachFunction(module, (func, type, code) =>
        {
            var builder = module.EmitAssembly.Value.GetFunctionBuilder(func.EmitName)
                          ?? throw new InvalidOperationException($"Unable to find function builder {func.EmitName}");
            
            WasmCompiler.CompileFunction(module, builder, type, code);
        });
    }

    private static void ForEachFunction(WasmModule module, Action<WasmFunction, WasmType, WasmCode> callback)
    {
        if (module.FunctionSection is not { } functionSection
            || module.TypeSection is not { } typeSection
            || module.CodeSection is not { } codeSection)
        {
            return;
        }

        for (int i = 0; i < functionSection.Functions.Count; i++)
        {
            var func = functionSection.Functions[i];
            var type = typeSection.Types[(int) func.FunctionSignatureIndex];
            // HACK? Assuming the indexes are the same
            var code = codeSection.Codes[i];
            
            callback(func, type, code);
        }
    }

    public object? Invoke(string function, params object?[] args)
    {
        WasmExport? export = null;
        WasmModule? foundInModule = null;
        
        foreach (var module in _modules)
        {
            export = module.ExportSection?.Exports.FirstOrDefault(f => f.Name == function);
            
            if (export != null)
            {
                foundInModule = module;
                break;
            }
        }
        
        if (export == null || foundInModule == null)
        {
            throw new InvalidOperationException($"Function {function} not found.");
        }
        
        if (export.Kind != WasmExportKind.Function)
        {
            throw new InvalidOperationException($"Export {function} is not a function.");
        }
        
        var func = foundInModule.FunctionSection?.Functions[(int) export.Index];

        if (func == null)
        {
            throw new InvalidOperationException($"Function {function} not found.");
        }

        var method = foundInModule.EmitAssembly.Value.FunctionHolderType.GetMethod(func.EmitName, 
            BindingFlags.Public | BindingFlags.Static);

        if (method == null)
        {
            throw new InvalidOperationException(
                $"Unable to find method {func.EmitName} in generated function holder type");
        }
        
        return method.Invoke(null, args);
    }
}