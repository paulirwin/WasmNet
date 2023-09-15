using System.Diagnostics;
using System.Text.Json;

namespace WasmNet.Tests;

public class IntegrationTests
{
    [InlineData("0001-BasicExample.wat")]
    [InlineData("0002-BasicParameters.wat")]
    [Theory]
    public async Task IntegrationTest(string file)
    {
        var filePath = Path.Combine("IntegrationTests", file);
        var fileText = await File.ReadAllTextAsync(filePath);
        
        var header = Header.Parse(fileText);
        
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
        
        var (function, args) = header.ParseInvoke();
        
        var result = await runtime.InvokeAsync(function, args);
        
        var expected = TypedValue.Parse(header.Expect);
        
        Assert.Equal(expected.Value, result);
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
                WasmNumberType { Kind: WasmNumberTypeKind.I32 } => int.Parse(part),
                _ => throw new NotImplementedException("Need to implement ConvertValue for type")
            };
        }

        private static WasmValueType TypeNameToValueType(string name) =>
            name switch
            {
                "i32" => new WasmNumberType(WasmNumberTypeKind.I32),
                _ => throw new NotImplementedException($"Unknown type name: {name}")
            };
    }
}