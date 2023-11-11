using System.Text.Json;
using WasmNet.Core;
using WasmNet.Core.ILGeneration;

if (args.Length < 1)
{
    Console.WriteLine("WasmNet: a simple WebAssembly runtime for .NET.");
    Console.WriteLine();
    Console.WriteLine("Usage: WasmNet <path>");
    Console.WriteLine();
    Console.WriteLine("Options:");
    Console.WriteLine("  <path>  Path to the WebAssembly file to run.");
    Console.WriteLine("  --invoke <function> <args...>  Invoke the specified function with the specified arguments.");
    return;
}

var runtime = new WasmRuntime(new ReflectionEmitCompilationAssembly());

var module = await runtime.InstantiateModuleAsync(args[0]);

if (args.Length > 1 && args[1] == "--invoke")
{
    var function = args[2];
    var invokeArgs = args[3..].Select(arg => JsonSerializer.Deserialize<object>(arg)).ToArray();

    var result = runtime.Invoke(module, function, invokeArgs);

    if (result != null)
    {
        Console.WriteLine(JsonSerializer.Serialize(result));
    }
}