namespace WasmNet.Core;

public class WasmRuntime
{
    private readonly Store _store = new();
    private readonly IDictionary<string, IDictionary<string, object?>> _importables = new Dictionary<string, IDictionary<string, object?>>();

    public void RegisterImportable(string module, string name, object? value)
    {
        if (!_importables.TryGetValue(module, out var importables))
        {
            importables = new Dictionary<string, object?>();
            _importables.Add(module, importables);
        }

        importables.Add(name, value);
    }
    
    public async Task<ModuleInstance> InstantiateModuleAsync(string path)
    {
        var bytes = await File.ReadAllBytesAsync(path);

        await using var ms = new MemoryStream(bytes);

        var reader = new WasmReader(ms);

        var module = reader.ReadModule();

        var moduleInstance = CompileModule(module);
        
        moduleInstance.Importables = _importables;

        return moduleInstance;
    }

    private ModuleInstance CompileModule(WasmModule module)
    {
        var moduleInstance = new ModuleInstance(module, _store);

        CompileModuleImports(module, moduleInstance);
        
        CompileModuleFunctions(module, moduleInstance);

        return moduleInstance;
    }

    private void CompileModuleImports(WasmModule module, ModuleInstance moduleInstance)
    {
        if (module.ImportSection is not { } importSection
            || module.TypeSection is not { } typeSection)
        {
            return;
        }

        foreach (var import in importSection.Imports)
        {
            if (!_importables.TryGetValue(import.ModuleName, out var importModule))
            {
                throw new InvalidOperationException($"Could not find module {import.ModuleName} to import");
            }

            if (!importModule.TryGetValue(import.Name, out var importValue))
            {
                throw new InvalidOperationException($"Could not find {import.Name} in module {import.ModuleName} to import");
            }

            if (import.Descriptor is WasmFunctionImportDescriptor funcImport)
            {
                if (importValue is null)
                {
                    throw new InvalidOperationException($"Cannot import a null function for {import.ModuleName}.{import.Name}");
                }

                if (importValue is not Delegate del)
                {
                    throw new InvalidOperationException("Cannot import anything but a Delegate as a function");
                }

                var funcType = typeSection.Types[funcImport.TypeIndex];

                var hostFunc = new HostFunctionInstance(funcType, del);
                var funcAddr = _store.AddFunction(hostFunc);
                moduleInstance.AddFunctionAddress(funcAddr);
            }
            else
            {
                throw new NotImplementedException($"Support for {import.Descriptor.GetType()} not yet implemented");
            }
        }
    }

    private void CompileModuleFunctions(WasmModule module, ModuleInstance moduleInstance)
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
            var type = typeSection.Types[(int)func.FunctionSignatureIndex];
            var code = codeSection.Codes[i];

            var funcInstance = new WasmFunctionInstance(type, moduleInstance, code);

            var funcAddr = _store.AddFunction(funcInstance);

            moduleInstance.AddFunctionAddress(funcAddr);

            moduleInstance.EmitAssembly.Value.CreateFunctionBuilder(
                funcInstance.EmitName,
                funcInstance.ReturnType,
                funcInstance.ParameterTypes
            );
        }

        CompileFunctionBodies(moduleInstance);
    }

    private void CompileFunctionBodies(ModuleInstance moduleInstance)
    {
        foreach (var functionAddress in moduleInstance.FunctionAddresses)
        {
            var func = _store.Functions[functionAddress];

            if (func is WasmFunctionInstance wasmFunc)
            {
                var builder = moduleInstance.EmitAssembly.Value.GetFunctionBuilder(wasmFunc.EmitName)
                              ?? throw new InvalidOperationException(
                                  $"Unable to find function builder {wasmFunc.EmitName}");

                WasmCompiler.CompileFunction(moduleInstance, builder, wasmFunc.Type, wasmFunc.Code);
            }
        }
    }

    public object? Invoke(ModuleInstance module, string function, params object?[] args)
    {
        var export = module.Module.ExportSection?.Exports.FirstOrDefault(f => f.Name == function);

        if (export == null)
        {
            throw new InvalidOperationException($"Function {function} not found.");
        }

        if (export.Kind != WasmExportKind.Function)
        {
            throw new InvalidOperationException($"Export {function} is not a function.");
        }

        //var funcIndex = (int)export.Index - (module.Module.ImportSection?.FunctionImports.Count ?? 0);
        var funcAddr = module.FunctionAddresses[(int)export.Index];
        var funcInstance = _store.Functions[funcAddr];

        return funcInstance.Invoke(args);
    }
}