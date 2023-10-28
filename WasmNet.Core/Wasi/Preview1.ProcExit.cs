namespace WasmNet.Core.Wasi;

public static partial class Preview1
{
    public static void ProcExit(WasmRuntime runtime, int exitCode) => runtime.ExitHandler(exitCode);
}