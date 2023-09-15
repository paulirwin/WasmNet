using System.Diagnostics;
using System.Text.Json;

namespace WasmNet.Tests;

public class IntegrationTests
{
    [InlineData("0001-BasicExample.wat")]
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
        
        await process.WaitForExitAsync();
        
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
        
        var resultText = JsonSerializer.Serialize(result);
        
        Assert.Equal(header.Expect, resultText);
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
                .Select(i => i.Trim())
                .Select(i => JsonSerializer.Deserialize<object?>(i))
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
}