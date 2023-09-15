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
        
        _modules.Add(module);
    }

    public async Task<object?> InvokeAsync(string function, params object?[] args)
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
            throw new Exception($"Function {function} not found.");
        }
        
        if (export.Kind != WasmExportKind.Function)
        {
            throw new Exception($"Export {function} is not a function.");
        }
        
        var func = foundInModule.FunctionSection?.Functions[(int) export.Index];
        
        if (func == null)
        {
            throw new Exception($"Function {function} not found.");
        }
        
        var type = foundInModule.TypeSection?.Types[(int) func.FunctionSignatureIndex];
        
        if (type == null)
        {
            throw new Exception($"Function {function} type signature not found.");
        }
        
        // HACK? Assuming the indexes are the same
        var code = foundInModule.CodeSection?.Codes[(int) export.Index];
        
        if (code == null)
        {
            throw new Exception($"Function {function} code section not found.");
        }

        code.MethodDelegate ??= WasmCompiler.CompileFunction(foundInModule, type, code);
        
        return code.MethodDelegate.DynamicInvoke(args);
    }
}