namespace WasmNet.Tests;

public class IntegrationTests
{
    [InlineData("0001-BasicExample.wat")]
    [InlineData("0002-BasicParameters.wat")]
    [InlineData("0003-BasicParametersInt64.wat")]
    [InlineData("0004-BasicParametersFloat32.wat")]
    [InlineData("0005-BasicParametersFloat64.wat")]
    [InlineData("0006-I32Sub.wat")]
    [InlineData("0007-I64Sub.wat")]
    [InlineData("0008-F32Sub.wat")]
    [InlineData("0009-F64Sub.wat")]
    [InlineData("0010-I64Const.wat")]
    [InlineData("0011-F32Const.wat")]
    [InlineData("0012-F64Const.wat")]
    [InlineData("0013-I32Mul.wat")]
    [InlineData("0014-I64Mul.wat")]
    [InlineData("0015-F32Mul.wat")]
    [InlineData("0016-F64Mul.wat")]
    [InlineData("0017-F32Div.wat")]
    [InlineData("0018-F64Div.wat")]
    [InlineData("0019-I32DivU.wat")]
    [InlineData("0020-I64DivU.wat")]
    [InlineData("0021-I32DivS.wat")]
    [InlineData("0022-I64DivS.wat")]
    [InlineData("0023-I32RemU.wat")]
    [InlineData("0024-I64RemU.wat")]
    [InlineData("0025-I32RemS.wat")]
    [InlineData("0026-I64RemS.wat")]
    [InlineData("0027-I32And.wat")]
    [InlineData("0028-I64And.wat")]
    [InlineData("0029-I32Or.wat")]
    [InlineData("0030-I64Or.wat")]
    [InlineData("0031-I32Xor.wat")]
    [InlineData("0032-I64Xor.wat")]
    [InlineData("0033-I32Shl.wat")]
    [InlineData("0034-I64Shl.wat")]
    [InlineData("0035-I32ShrU.wat")]
    [InlineData("0036-I32ShrS.wat")]
    [InlineData("0037-I64ShrU.wat")]
    [InlineData("0038-I64ShrS.wat")]
    [InlineData("0039-I32Eq.wat")]
    [InlineData("0040-I64Eq.wat")]
    [InlineData("0041-LocalSet.wat")]
    [InlineData("0042-LocalSetWithParameters.wat")]
    [InlineData("0043-ExportWasmFunction.wat")]
    [InlineData("0044-BasicCall.wat")]
    [InlineData("0045-CallWithParameters.wat")]
    [InlineData("0046-BasicImport.wat")]
    [InlineData("0047-ImportGlobal.wat")]
    [Theory]
    public async Task IntegrationTest(string file)
    {
        var filePath = Path.Combine("IntegrationTests", file);
        var fileText = await File.ReadAllTextAsync(filePath);
        
        var header = Header.Parse(fileText);
        
        var wasmFile = Path.Combine("IntegrationTests", file.Replace(".wat", ".wasm"));
        
        if (!File.Exists(wasmFile))
        {
            throw new Exception($"wat2wasm failed to produce {wasmFile}.");
        }
        
        WasmRuntime runtime = new();
        
        runtime.RegisterImportable("console", "log", (object? param) => Console.WriteLine(param));
        
        foreach (var global in header.Operations.OfType<GlobalOperation>())
        {
            if (global.Value.Type is not { } type)
            {
                throw new InvalidOperationException("Globals require a type");
            }

            runtime.RegisterImportable(global.Namespace, global.Name, new Global(type, global.Mutable, global.Value.Value));
        }
        
        var module = await runtime.InstantiateModuleAsync(wasmFile);
        
        object? result = null;

        foreach (var op in header.Operations)
        {
            if (op is InvokeOperation invoke)
            {
                result = runtime.Invoke(module, invoke.Function, invoke.Args);
            }
            else if (op is GlobalOperation)
            {
                // NOTE: globals are registered above out-of-order
            }
            else if (op is ExpectOperation expect)
            {
                if (expect.Value.Type is null)
                {
                    Assert.Null(result);
                }
                else if (expect.Value.Value is float f)
                {
                    var resultF = Assert.IsType<float>(result);
                    Assert.Equal(f, resultF, 0.000001);
                }
                else if (expect.Value.Value is double d)
                {
                    var resultD = Assert.IsType<double>(result);
                    Assert.Equal(d, resultD, 0.000001);
                }
                else
                {
                    Assert.Equal(expect.Value.Value, result);
                }       
            }
        }
    }

    private abstract class Operation
    {
    }

    private class GlobalOperation : Operation
    {
        public required string Namespace { get; init; }
        
        public required string Name { get; init; }
        
        public required bool Mutable { get; init; }
        
        public required TypedValue Value { get; init; }
        
        public static GlobalOperation Parse(string text)
        {
            var parts = text.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            
            var nameParts = parts[0].Split('.', StringSplitOptions.TrimEntries);
            
            if (nameParts.Length != 2)
            {
                throw new Exception($"Invalid global name: {parts[1]}");
            }
            
            var ns = nameParts[0];
            var name = nameParts[1];
            
            var mutable = parts is [_, "mut", _];
            var value = TypedValue.Parse(parts[^1]);
            
            return new GlobalOperation
            {
                Namespace = ns,
                Name = name,
                Mutable = mutable,
                Value = value,
            };
        }
    }

    private class InvokeOperation : Operation
    {
        public required string Function { get; init; }
        
        public required object?[] Args { get; init; }
        
        public static InvokeOperation Parse(string text)
        {
            var parts = text.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            
            var function = parts[0];
            
            var args = parts[1..]
                .Select(TypedValue.Parse)
                .Select(i => i.Value)
                .ToArray();
            
            return new InvokeOperation
            {
                Function = function,
                Args = args,
            };
        }
    }
    
    private class ExpectOperation : Operation
    {
        public required TypedValue Value { get; init; }
        
        public static ExpectOperation Parse(string text)
        {
            var value = TypedValue.Parse(text);
            
            return new ExpectOperation
            {
                Value = value,
            };
        }
    }

    private class Header
    {
        public required IReadOnlyList<Operation> Operations { get; init; }
        
        public static Header Parse(string text)
        {
            var lines = text.TrimStart(' ', '\t', '\n', '\r')
                .Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(i => i.StartsWith(";; "))
                .Select(i => i[3..])
                .ToList();
            
            var ops = new List<Operation>();
            
            foreach (var line in lines)
            {
                if (line.StartsWith("invoke: "))
                {
                    ops.Add(InvokeOperation.Parse(line[8..])); 
                }
                else if (line.StartsWith("expect: "))
                {
                    ops.Add(ExpectOperation.Parse(line[8..]));
                }
                else if (line.StartsWith("global: "))
                {
                    ops.Add(GlobalOperation.Parse(line[8..]));
                }
                else if (line.StartsWith("source: ") || line.StartsWith("TODO: "))
                {
                    // ignore
                }
                else
                {
                    throw new Exception($"Invalid header line: {line}");
                }
            }
            
            return new Header
            {
                Operations = ops,
            };
        }
    }

    private class TypedValue
    {
        public required WasmValueType? Type { get; init; }
        
        public required object? Value { get; init; }

        public static TypedValue Parse(string input)
        {
            var parts = input.Trim('(', ')')
                .Split(':', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

            if (parts[0] == "void")
            {
                return new TypedValue
                {
                    Type = null,
                    Value = null,
                };
            }
            
            if (parts.Length != 2)
            {
                throw new InvalidOperationException("Cannot parse typed value");
            }
            
            var type = TypeNameToValueType(parts[0]);
            
            return new TypedValue
            {
                Type = type,
                Value = ConvertValue(type, parts[1]),
            };
        }

        private static object? ConvertValue(WasmValueType type, string part)
        {
            return type switch
            {
                WasmNumberType { Kind: WasmNumberTypeKind.I32 } => (object)int.Parse(part),
                WasmNumberType { Kind: WasmNumberTypeKind.I64 } => long.Parse(part),
                WasmNumberType { Kind: WasmNumberTypeKind.F32 } => float.Parse(part),
                WasmNumberType { Kind: WasmNumberTypeKind.F64 } => double.Parse(part),
                _ => throw new NotImplementedException("Need to implement ConvertValue for type")
            };
        }

        private static WasmValueType TypeNameToValueType(string name) =>
            name switch
            {
                "i32" => new WasmNumberType(WasmNumberTypeKind.I32),
                "i64" => new WasmNumberType(WasmNumberTypeKind.I64),
                "f32" => new WasmNumberType(WasmNumberTypeKind.F32),
                "f64" => new WasmNumberType(WasmNumberTypeKind.F64),
                _ => throw new NotImplementedException($"Unknown type name: {name}")
            };
    }
}