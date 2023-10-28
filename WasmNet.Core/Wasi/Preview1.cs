namespace WasmNet.Core.Wasi;

public static partial class Preview1
{
    public const string Namespace = "wasi_snapshot_preview1";
    
    public static void RegisterWasiPreview1(this WasmRuntime runtime)
    {
        var actionIntType = new WasmType
        {
            Kind = WasmTypeKind.Function,
            Parameters = new List<WasmValueType>
            {
                WasmNumberType.I32
            }
        };
        
        runtime.RegisterImportable(Namespace, "proc_exit", (int exitCode) => ProcExit(runtime, exitCode));
    }
}