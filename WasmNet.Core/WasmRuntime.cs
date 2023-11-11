using WasmNet.Core.ILGeneration;
using WasmNet.Core.Wasi;

namespace WasmNet.Core;

public class WasmRuntime
{
    private readonly ICompilationAssembly _compilationAssembly;
    private readonly WasmRuntimeOptions _options;
    private readonly IDictionary<string, IDictionary<string, object?>> _importables = new Dictionary<string, IDictionary<string, object?>>();

    public WasmRuntime(ICompilationAssembly compilationAssembly,
        WasmRuntimeOptions? options = null)
    {
        _compilationAssembly = compilationAssembly;
        _options = options ?? new WasmRuntimeOptions();
        this.RegisterWasiPreview1();
    }
    
    public Action<int> ExitHandler { get; set; } = Environment.Exit;
    
    public Store Store { get; } = new();

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

        if (_options.InvokeStartOnInstantiation)
        {
            TryInvokeStartFunc(module, moduleInstance);
        }

        return moduleInstance;
    }

    private static void TryInvokeStartFunc(WasmModule module, ModuleInstance moduleInstance)
    {
        IFunctionInstance? startFunc = null;

        if (module.StartSection is { FuncIndex: var startFuncIndex })
        {
            startFunc = moduleInstance.GetFunction(startFuncIndex);
        }
        else if (module.ExportSection is { Exports: var exports })
        {
            var startExport = exports.FirstOrDefault(i => i.Name == "_start");

            if (startExport != null)
            {
                startFunc = moduleInstance.GetFunction(startExport.Index);
            }
        }

        // TODO.PI: pass args?
        startFunc?.Invoke();
    }

    private ModuleInstance CompileModule(WasmModule module)
    {
        var moduleInstance = new ModuleInstance(module, Store, _compilationAssembly);
        
        EvaluateModuleTypes(module, moduleInstance);

        EvaluateModuleTables(module, moduleInstance);
        
        EvaluateModuleImports(module, moduleInstance);
        
        EvaluateModuleMemory(module, moduleInstance);
        
        DeclareModuleFunctions(module, moduleInstance);
        
        CompileAndEvaluateModuleGlobals(module, moduleInstance);
        
        CompileAndEvaluateModuleElements(module, moduleInstance);
        
        CompileAndEvaluateModuleData(module, moduleInstance);
        
        CompileFunctionBodies(moduleInstance);

        return moduleInstance;
    }

    private void CompileAndEvaluateModuleElements(WasmModule module, ModuleInstance moduleInstance)
    {
        if (module.ElementsSection is not { } elementSection)
        {
            return;
        }
        
        // pass 1: compile elements
        foreach (var element in elementSection.Elements)
        {
            CompileElement(moduleInstance, element);
        }

        foreach (var element in elementSection.Elements)
        {
            EvaluateElement(moduleInstance, element);
        }
    }

    private void EvaluateElement(ModuleInstance moduleInstance, WasmElement element)
    {
        if (element.Offset is not { } offsetExpr)
        {
            throw new InvalidOperationException("Element offset expression is null");
        }
        
        var tableIndex = element.TableIndex ?? 0;
        var tableAddress = moduleInstance.TableAddresses[tableIndex];
        var table = Store.Tables[tableAddress];
        
        var offset = GetExpressionValue(moduleInstance, CompilationType.Element, offsetExpr.EmitName);

        if (offset is not int offsetInt)
        {
            throw new InvalidOperationException("Element offset expression did not evaluate to an int");
        }

        var funcRefs = element.Init
            .Select(i => GetExpressionValue(moduleInstance, CompilationType.Element, i.EmitName))
            .Cast<FunctionReference>()
            .ToList();

        foreach (var funcRef in funcRefs)
        {
            table[offsetInt] = funcRef;
            offsetInt++;
        }
    }

    private void CompileElement(ModuleInstance moduleInstance, WasmElement element)
    {
        // TODO: deal with passive/declarative elements
        if (element.Mode != WasmElementMode.Active)
        {
            throw new NotImplementedException("Only active elements are supported");
        }
        
        var offsetExpr = element.Offset ?? throw new InvalidOperationException("Element offset expression is null");
        CompileExpression(moduleInstance, CompilationType.Element, WasmNumberType.I32, offsetExpr);

        foreach (var initExpr in element.Init)
        {
            CompileExpression(moduleInstance, CompilationType.Element, WasmReferenceType.FuncRef, initExpr);
        }
    }

    private void EvaluateModuleTables(WasmModule module, ModuleInstance moduleInstance)
    {
        if (module.TableSection is not { } tableSection)
        {
            return;
        }

        foreach (var table in tableSection.Tables)
        {
            var tableInstance = new Table(table.Limits.Min, table.Limits.Max);

            var tableAddr = Store.AddTable(tableInstance);
            moduleInstance.AddTableAddress(tableAddr);
        }
    }

    private void EvaluateModuleTypes(WasmModule module, ModuleInstance moduleInstance)
    {
        if (module.TypeSection is not { } typeSection)
        {
            return;
        }

        foreach (var type in typeSection.Types)
        {
            moduleInstance.AddType(type);
        }
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
        
        // pass 1: compile data
        foreach (var data in dataSection.Data)
        {
            if (data.DataKind != WasmDataKind.Passive)
            {
                CompileActiveDataRecord(moduleInstance, data);
            }
        }

        // pass 2: evaluate data
        foreach (var moduleData in dataSection.Data)
        {
            var data = Data.FromWasmData(moduleData);
            var da = Store.AddData(data);
            moduleInstance.AddDataAddress(da);

            if (moduleData.DataKind != WasmDataKind.Passive)
            {
                EvaluateActiveDataRecord(moduleInstance, data);
            }
        }
    }

    private void EvaluateActiveDataRecord(ModuleInstance moduleInstance, Data data)
    {
        if (data.ModuleData.OffsetExpr is not { } offsetExpr)
        {
            throw new InvalidOperationException("Data offset expression is null");
        }
        
        int memoryIndex = data.ModuleData.MemoryIndex ?? 0;
        var memoryAddress = moduleInstance.MemoryAddresses[memoryIndex];
        var memory = Store.Memory[memoryAddress];

        var offset = GetExpressionValue(moduleInstance, CompilationType.Data, offsetExpr.EmitName);

        if (offset is not int offsetInt)
        {
            throw new InvalidOperationException("Data offset expression did not evaluate to an int");
        }

        var dataBytes = data.Value;

        memory.Write(offsetInt, dataBytes);
        
        data.Drop();
    }

    private void CompileActiveDataRecord(ModuleInstance moduleInstance, WasmData data)
    {
        var offsetExpr = data.OffsetExpr ?? throw new InvalidOperationException("Data offset expression is null");
        CompileExpression(moduleInstance,
            CompilationType.Data,
            WasmNumberType.I32,
            offsetExpr);
    }

    private void CompileAndEvaluateModuleGlobals(WasmModule module, ModuleInstance moduleInstance)
    {
        if (module.GlobalSection is not { } globalSection)
        {
            return;
        }
        
        // pass 1: compile globals
        foreach (var global in globalSection.Globals)
        {
            CompileGlobal(moduleInstance, global);
        }

        // pass 2: evaluate globals
        foreach (var global in globalSection.Globals)
        {
            EvaluateGlobal(moduleInstance, global, global.Type);
        }
    }

    private void CompileGlobal(ModuleInstance moduleInstance, WasmGlobal global)
    {
        var valueType = global.Type;

        CompileExpression(moduleInstance, CompilationType.Global, valueType, global.Init);
    }

    private void EvaluateGlobal(ModuleInstance moduleInstance, WasmGlobal global, WasmValueType valueType)
    {
        var globalValue = GetExpressionValue(moduleInstance, CompilationType.Global, global.Init.EmitName);

        var globalInstance = new Global(valueType, global.Mutable, globalValue);

        var globalAddr = Store.AddGlobal(globalInstance);
        moduleInstance.AddGlobalAddress(globalAddr, global.Mutable);
    }

    private static void CompileExpression(
        ModuleInstance moduleInstance,
        CompilationType compilationType,
        WasmValueType valueType,
        Expression expr)
    {
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
            Body = expr,
        };
        
        moduleInstance.CompilationAssembly.DeclareAndCompileMethod(
            moduleInstance,
            compilationType,
            name: expr.EmitName,
            returnType: valueType.DotNetType,
            parameterTypes: Enumerable.Empty<Type>(),
            wasmType: funcType, 
            code: funcCode
        );
    }

    private static object? GetExpressionValue(ModuleInstance moduleInstance, 
        CompilationType compilationType, 
        string functionName)
    {
        var method = moduleInstance.CompilationAssembly.GetCompiledMethod(compilationType, functionName);

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

    private void DeclareModuleFunctions(WasmModule module, ModuleInstance moduleInstance)
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

            moduleInstance.CompilationAssembly.DeclareMethod(CompilationType.Function, 
                funcInstance.EmitName, 
                funcInstance.ReturnType, 
                funcInstance.ParameterTypes);
        }
    }

    private void CompileFunctionBodies(ModuleInstance moduleInstance)
    {
        foreach (var functionAddress in moduleInstance.FunctionAddresses)
        {
            var func = Store.Functions[functionAddress];

            if (func is WasmFunctionInstance wasmFunc)
            {
                moduleInstance.CompilationAssembly.CompileDeclaredMethod(moduleInstance, 
                    wasmFunc.EmitName, 
                    wasmFunc.Type, 
                    wasmFunc.Code);
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

        var funcInstance = module.GetFunction(export.Index);

        return funcInstance.Invoke(args);
    }
}