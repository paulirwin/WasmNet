using System.Diagnostics;

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
    [Theory]
    public async Task IntegrationTest(string file)
    {
        var filePath = Path.Combine("IntegrationTests", file);
        var fileText = await File.ReadAllTextAsync(filePath);
        
        var header = Header.Parse(fileText);
        var (function, args) = header.ParseInvoke();
        var expected = TypedValue.Parse(header.Expect);
        
        var wasmFile = Path.Combine("IntegrationTests", file.Replace(".wat", ".wasm"));
        
        // must have wat2wasm on your PATH
        var process = new Process();
        process.StartInfo.FileName = "wat2wasm";
        process.StartInfo.WorkingDirectory = Path.Combine(Environment.CurrentDirectory, "IntegrationTests");
        process.StartInfo.Arguments = file;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.UseShellExecute = false;
        process.Start();
        
        process.WaitForExit();
        
        if (process.ExitCode != 0)
        {
            throw new Exception($"wat2wasm failed with exit code {process.ExitCode}.");
        }
        
        if (!File.Exists(wasmFile))
        {
            throw new Exception($"wat2wasm failed to produce {wasmFile}.");
        }
        
        WasmRuntime runtime = new();
        
        await runtime.LoadModuleAsync(wasmFile);
        
        var result = await runtime.InvokeAsync(function, args);

        if (expected.Value is float f)
        {
            var resultF = Assert.IsType<float>(result);
            Assert.Equal(f, resultF, 0.000001);
        }
        else if (expected.Value is double d)
        {
            var resultD = Assert.IsType<double>(result);
            Assert.Equal(d, resultD, 0.000001);
        }
        else
        {
            Assert.Equal(expected.Value, result);
        }
    }

    private class Header
    {
        public required string Invoke { get; init; }
        
        public required string Expect { get; init; }
        
        public (string Function, object?[] Args) ParseInvoke()
        {
            var parts = Invoke.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            
            var function = parts[0];
            
            var args = parts[1..]
                .Select(TypedValue.Parse)
                .Select(i => i.Value)
                .ToArray();
            
            return (function, args);
        }
        
        public static Header Parse(string text)
        {
            var lines = text.TrimStart(' ', '\t', '\n', '\r')
                .Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(i => i.StartsWith(";; "))
                .Select(i => i[3..])
                .ToList();
            
            string? invoke = null;
            string? expect = null;
            
            foreach (var line in lines)
            {
                if (line.StartsWith("invoke: "))
                {
                    invoke = line[8..]; 
                }
                else if (line.StartsWith("expect: "))
                {
                    expect = line[8..];
                }
                else
                {
                    throw new Exception($"Invalid header line: {line}");
                }
            }
            
            if (invoke is null)
            {
                throw new Exception("Missing invoke header.");
            }
            
            if (expect is null)
            {
                throw new Exception("Missing expect header.");
            }
            
            return new Header
            {
                Invoke = invoke,
                Expect = expect,
            };
        }
    }

    private class TypedValue
    {
        public required WasmValueType Type { get; init; }
        
        public required object? Value { get; init; }

        public static TypedValue Parse(string input)
        {
            var parts = input.Trim('(', ')')
                .Split(':', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

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