namespace WasmNet.Core;

public class WasmMemorySection : WasmModuleSection
{
    public IList<WasmMemory> Memories { get; set; } = new List<WasmMemory>();
}