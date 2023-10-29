using System.Reflection;
using System.Text;
using Xunit.Abstractions;

namespace WasmNet.Tests;

public class IntegrationTests(ITestOutputHelper testOutputHelper)
{
    [MemberData(nameof(GetWatFiles))]
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

        var output = new StringBuilder();

        await using var outputWriter = new StringWriter(output);
        var oldOut = Console.Out;
        Console.SetOut(outputWriter);
        
        WasmRuntime runtime = new()
        {
            ExitHandler = code => throw new ExitCodeException(code),
        };

        var externalCalls = new Dictionary<string, int>();
        
        runtime.RegisterImportable("console", "log", (object? param) =>
        {
            externalCalls["console.log"] = externalCalls.TryGetValue("console.log", out var count) ? count + 1 : 1;
            
            testOutputHelper.WriteLine(param?.ToString() ?? "null");
        });
        
        runtime.RegisterImportable("console", "logmem", (int offset, int size) =>
        {
            externalCalls["console.logmem"] = externalCalls.TryGetValue("console.logmem", out var count) ? count + 1 : 1;

            var memory = runtime.Store.Memory[0];
            var bytes = memory.Read(offset, size);
            var str = Encoding.UTF8.GetString(bytes);
            
            testOutputHelper.WriteLine(str);
        });
        
        foreach (var global in header.Operations.OfType<GlobalOperation>())
        {
            if (global.Value.Type is not { } type)
            {
                throw new InvalidOperationException("Globals require a type");
            }

            runtime.RegisterImportable(global.Namespace, global.Name, new Global(type, global.Mutable, global.Value.Value));
        }
        
        foreach (var memory in header.Operations.OfType<MemoryOperation>())
        {
            runtime.RegisterImportable(memory.Namespace, memory.Name, new Memory(memory.Min, memory.Max ?? int.MaxValue));
        }
        
        var module = await runtime.InstantiateModuleAsync(wasmFile);
        
        object? result = null;
        Exception? exception = null;

        foreach (var op in header.Operations)
        {
            if (op is InvokeOperation invoke)
            {
                exception = null;
                result = null;
                
                try
                {
                    testOutputHelper.WriteLine($"Invoking {invoke.Function}({string.Join(", ", invoke.Args)})");
                    result = runtime.Invoke(module, invoke.Function, invoke.Args);
                }
                catch (Exception ex)
                {
                    exception = ex;
                    
                    while (exception is TargetInvocationException tie)
                    {
                        exception = tie.InnerException;
                    }
                }
            }
            else if (op is GlobalOperation or MemoryOperation)
            {
                // NOTE: globals and memories are registered above out-of-order
            }
            else if (op is ExpectCallOperation expectCall)
            {
                if (exception is not null)
                {
                    throw new Exception($"Expected call to {expectCall.Namespace}.{expectCall.Name} but it threw an exception: {exception}");
                }
                
                Assert.True(externalCalls.TryGetValue($"{expectCall.Namespace}.{expectCall.Name}", out var count), $"Expected call to {expectCall.Namespace}.{expectCall.Name} but it was not called");
                Assert.True(count > 0);
                testOutputHelper.WriteLine($"Expected call to {expectCall.Namespace}.{expectCall.Name} and it was called {count} times");
            }
            else if (op is ExpectOperation expect)
            {
                if (exception is not null)
                {
                    throw new Exception($"Expected {expect.Value} but it threw an exception: {exception}");
                }
                
                if (expect.Value.Type is null)
                {
                    Assert.Null(result);
                    testOutputHelper.WriteLine($"Expected null and got null");
                }
                else if (expect.Value.Value is float f)
                {
                    var resultF = Assert.IsType<float>(result);
                    Assert.Equal(f, resultF, 0.000001);
                    testOutputHelper.WriteLine($"Expected {f} and got {resultF}");
                }
                else if (expect.Value.Value is double d)
                {
                    var resultD = Assert.IsType<double>(result);
                    Assert.Equal(d, resultD, 0.000001);
                    testOutputHelper.WriteLine($"Expected {d} and got {resultD}");
                }
                else
                {
                    Assert.Equal(expect.Value.Value, result);
                    testOutputHelper.WriteLine($"Expected {expect.Value.Value} and got {result}");
                }       
            }
            else if (op is ExpectTrapOperation expectTrap)
            {
                Assert.NotNull(exception);
                Assert.Equal(expectTrap.ExceptionType, exception.GetType().Name);
                testOutputHelper.WriteLine($"Exception is of type {exception.GetType().Name}: {exception.Message}");
            }
            else if (op is ExitCodeOperation exitCode)
            {
                if (exception is not ExitCodeException ece)
                {
                    throw new Exception($"Expected exit code {exitCode.ExitCode} but the program did not exit");
                }
                
                Assert.Equal(exitCode.ExitCode, ece.ExitCode);
                testOutputHelper.WriteLine($"Expected exit code {exitCode.ExitCode} and got {ece.ExitCode}");
            }
            else if (op is OutputOperation outputOp)
            {
                if (exception is not null)
                {
                    throw new Exception($"Expected output \"{outputOp.Text.Replace("\n", "\\n")}\" but it threw an exception: {exception}");
                }
                
                Assert.Equal(outputOp.Text, output.ToString());
                testOutputHelper.WriteLine($"Expected output: {outputOp.Text.Replace("\n", "\\n")}");
                testOutputHelper.WriteLine($"Actual output: {output.ToString().Replace("\n", "\\n")}");
            }
            else
            {
                throw new NotImplementedException($"Unknown operation: {op.GetType().Name}");
            }
        }
        
        Console.SetOut(oldOut);
    }
    
    public static IEnumerable<object[]> GetWatFiles()
    {
        var files = Directory.GetFiles("IntegrationTests", "*.wat", SearchOption.AllDirectories);
        
        return files.Select(i => new object[] { Path.GetFileName(i) });
    }
    
    private class ExitCodeException : Exception
    {
        public ExitCodeException(int exitCode)
        {
            ExitCode = exitCode;
        }
        
        public int ExitCode { get; }
    }

    private abstract class Operation;
    
    private class OutputOperation : Operation
    {
        public required string Text { get; init; }
        
        public static OutputOperation Parse(string text)
        {
            return new OutputOperation
            {
                Text = text.Replace("\\n", "\n"),
            };
        }
    }
    
    private class ExitCodeOperation : Operation
    {
        public required int ExitCode { get; init; }
        
        public static ExitCodeOperation Parse(string text)
        {
            var code = int.Parse(text);
            
            return new ExitCodeOperation
            {
                ExitCode = code,
            };
        }
    }
    
    private class ExpectTrapOperation : Operation
    {
        public required string ExceptionType { get; init; }
        
        public static ExpectTrapOperation Parse(string text)
        {
            return new ExpectTrapOperation
            {
                ExceptionType = text,
            };
        }
    }

    private class MemoryOperation : Operation
    {
        public required string Namespace { get; init; }
        
        public required string Name { get; init; }
        
        public required int Min { get; init; }
        
        public int? Max { get; init; }
        
        public static MemoryOperation Parse(string text)
        {
            var parts = text.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            
            var nameParts = parts[0].Split('.', StringSplitOptions.TrimEntries);
            
            if (nameParts.Length != 2)
            {
                throw new Exception($"Invalid global name: {parts[1]}");
            }
            
            var ns = nameParts[0];
            var name = nameParts[1];
            
            var min = int.Parse(parts[1]);
            int? max = parts.Length > 2 ? int.Parse(parts[2]) : null;
            
            return new MemoryOperation
            {
                Namespace = ns,
                Name = name,
                Min = min,
                Max = max,
            };
        }
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

    private class ExpectCallOperation : Operation
    {
        public required string Namespace { get; init; }
        
        public required string Name { get; init; }
        
        public static ExpectCallOperation Parse(string text)
        {
            var parts = text.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            
            var nameParts = parts[0].Split('.', StringSplitOptions.TrimEntries);
            
            if (nameParts.Length != 2)
            {
                throw new Exception($"Invalid global name: {parts[1]}");
            }
            
            var ns = nameParts[0];
            var name = nameParts[1];
            
            return new ExpectCallOperation
            {
                Namespace = ns,
                Name = name,
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
                else if (line.StartsWith("expect_call: "))
                {
                    ops.Add(ExpectCallOperation.Parse(line[13..]));
                }
                else if (line.StartsWith("global: "))
                {
                    ops.Add(GlobalOperation.Parse(line[8..]));
                }
                else if (line.StartsWith("memory: "))
                {
                    ops.Add(MemoryOperation.Parse(line[8..]));
                }
                else if (line.StartsWith("expect_trap: "))
                {
                    ops.Add(ExpectTrapOperation.Parse(line[13..]));
                }
                else if (line.StartsWith("exit_code: "))
                {
                    ops.Add(ExitCodeOperation.Parse(line[11..]));
                }
                else if (line.StartsWith("output: "))
                {
                    ops.Add(OutputOperation.Parse(line[8..]));
                }
                else if (line.StartsWith("source: ") || line.StartsWith("TODO: ") || line.StartsWith("NOTE:"))
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

        private static object ConvertValue(WasmValueType type, string part)
        {
            if (type.Equals(WasmNumberType.I32))
            {
                return int.Parse(part);
            }

            if (type.Equals(WasmNumberType.I64))
            {
                return long.Parse(part);
            }

            if (type.Equals(WasmNumberType.F32))
            {
                return float.Parse(part);
            }

            if (type.Equals(WasmNumberType.F64))
            {
                return double.Parse(part);
            }

            throw new NotImplementedException($"Unknown type: {type}");
        }

        private static WasmValueType TypeNameToValueType(string name) =>
            name switch
            {
                "i32" => WasmNumberType.I32,
                "i64" => WasmNumberType.I64,
                "f32" => WasmNumberType.F32,
                "f64" => WasmNumberType.F64,
                _ => throw new NotImplementedException($"Unknown type name: {name}")
            };
    }
}