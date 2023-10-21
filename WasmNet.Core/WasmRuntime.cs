using System.Reflection;

namespace WasmNet.Core;

public class WasmRuntime
{
    public Store Store { get; } = new();
    
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
        var moduleInstance = new ModuleInstance(module, Store);

        EvaluateModuleImports(module, moduleInstance);
        
        EvaluateModuleMemory(module, moduleInstance);
        
        CompileAndEvaluateModuleData(module, moduleInstance);
        
        CompileAndEvaluateModuleGlobals(module, moduleInstance);
        
        CompileModuleFunctions(module, moduleInstance);

        return moduleInstance;
    }

    private void EvaluateModuleMemory(WasmModule module, ModuleInstance moduleInstance)
    {
        if (module.MemorySection is not { } memorySection)
        {
            return;
        }

        foreach (var memory in memorySection.Memories)
        {
            var memoryInstance = new Memory(memory.Limits.Min, memory.Limits.Max);

            var memoryAddr = Store.AddMemory(memoryInstance);
            moduleInstance.AddMemoryAddress(memoryAddr);
        }
    }

    private void CompileAndEvaluateModuleData(WasmModule module, ModuleInstance moduleInstance)
    {
        if (module.DataSection is not { } dataSection)
        {
            return;
        }

        foreach (var data in dataSection.Data)
        {
            if (data.DataKind == WasmDataKind.Passive)
            {
                continue;
            }

            CompileAndEvaluateDataRecord(moduleInstance, data);
        }
    }

    private void CompileAndEvaluateDataRecord(ModuleInstance moduleInstance, WasmData data)
    {
        int memoryIndex = data.MemoryIndex ?? 0;
        var memoryAddress = moduleInstance.MemoryAddresses[memoryIndex];
        var memory = Store.Memory[memoryAddress];

        var offsetExpr = data.OffsetExpr ?? throw new InvalidOperationException("Data offset expression is null");
        var offset = CompileAndEvaluateValue(moduleInstance,
            $"WasmDataOffset_{Guid.NewGuid():N}",
            new WasmNumberType(WasmNumberTypeKind.I32),
            offsetExpr);

        if (offset is not int offsetInt)
        {
            throw new InvalidOperationException("Data offset expression did not evaluate to an int");
        }

        var dataBytes = data.Data;

        memory.Write(offsetInt, dataBytes);
    }

    private void CompileAndEvaluateModuleGlobals(WasmModule module, ModuleInstance moduleInstance)
    {
        if (module.GlobalSection is not { } globalSection)
        {
            return;
        }

        foreach (var global in globalSection.Globals)
        {
            CompileAndEvaluateGlobal(moduleInstance, global);
        }
    }

    private void CompileAndEvaluateGlobal(ModuleInstance moduleInstance, WasmGlobal global)
    {
        var emitName = $"WasmGlobal_{Guid.NewGuid():N}";
        var valueType = global.Type;

        var globalValue = CompileAndEvaluateValue(moduleInstance, emitName, valueType, global.Init);

        var globalInstance = new Global(valueType, global.Mutable, globalValue);

        var globalAddr = Store.AddGlobal(globalInstance);
        moduleInstance.AddGlobalAddress(globalAddr, global.Mutable);
    }

    private static object? CompileAndEvaluateValue(ModuleInstance moduleInstance, 
        string emitName, 
        WasmValueType valueType,
        IEnumerable<WasmInstruction> expr)
    {
        var builder = moduleInstance.EmitAssembly.Value.CreateGlobalBuilder(
            emitName,
            valueType.MapWasmTypeToDotNetType()
        );

        var funcType = new WasmType
        {
            Kind = WasmTypeKind.Function,
            Parameters = new List<WasmValueType>(),
            Results = new List<WasmValueType>
            {
                valueType,
            }
        };

        var funcCode = new WasmCode
        {
            Locals = new List<WasmLocal>(),
            Body = expr.ToList(),
        };

        WasmCompiler.CompileFunction(moduleInstance, builder, funcType, funcCode);

        var method = moduleInstance.EmitAssembly.Value.GlobalHolderFuncType.GetMethod(emitName,
                         BindingFlags.Public | BindingFlags.Static)
                     ?? throw new InvalidOperationException(
                         $"Unable to find method {emitName} in generated function holder type");

        return method.Invoke(null, new object?[] { moduleInstance });
    }

    private void EvaluateModuleImports(WasmModule module, ModuleInstance moduleInstance)
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
                var funcAddr = Store.AddFunction(hostFunc);
                moduleInstance.AddFunctionAddress(funcAddr);
            }
            else if (import.Descriptor is WasmGlobalImportDescriptor globalImport)
            {
                if (importValue is null)
                {
                    throw new InvalidOperationException($"Cannot import a null global for {import.ModuleName}.{import.Name}");
                }

                if (importValue is not Global global)
                {
                    throw new InvalidOperationException("Cannot import anything but a Global as a global");
                }

                if (!globalImport.Type.Equals(global.Type))
                {
                    throw new InvalidOperationException($"Global type mismatch for {import.ModuleName}.{import.Name}");
                }
                
                if (globalImport.Mutable && !global.Mutable)
                {
                    throw new InvalidOperationException($"Global mutability mismatch for {import.ModuleName}.{import.Name}");
                }

                var globalAddr = Store.AddGlobal(global);
                moduleInstance.AddGlobalAddress(globalAddr, globalImport.Mutable);
            }
            else if (import.Descriptor is WasmMemoryImportDescriptor memoryImport)
            {
                if (importValue is null)
                {
                    throw new InvalidOperationException($"Cannot import a null memory for {import.ModuleName}.{import.Name}");
                }

                if (importValue is not Memory memory)
                {
                    throw new InvalidOperationException("Cannot import anything but a Memory as a memory");
                }
                
                if (memoryImport.Limits.Min > memory.MinPages)
                {
                    throw new InvalidOperationException($"Memory min size mismatch for {import.ModuleName}.{import.Name}");
                }
                
                if (memoryImport.Limits.Max > memory.MaxPages)
                {
                    throw new InvalidOperationException($"Memory max size mismatch for {import.ModuleName}.{import.Name}");
                }

                var memoryAddr = Store.AddMemory(memory);
                moduleInstance.AddMemoryAddress(memoryAddr);
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

            var funcAddr = Store.AddFunction(funcInstance);

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
            var func = Store.Functions[functionAddress];

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
        var funcInstance = Store.Functions[funcAddr];

        return funcInstance.Invoke(args);
    }
}