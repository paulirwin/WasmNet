namespace WasmNet.Core.Wasi;

public static partial class Preview1
{
    public const string Namespace = "wasi_snapshot_preview1";
    
    public static void RegisterWasiPreview1(this WasmRuntime runtime)
    {
        runtime.RegisterImportable(Namespace, "proc_exit", (int exitCode) => ProcExit(runtime, exitCode));
        runtime.RegisterImportable(Namespace, "fd_write", (int fd, int iovs, int iovsLen, int nWritten) => FdWrite(runtime, fd, iovs, iovsLen, nWritten));
    }
}