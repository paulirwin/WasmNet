using System.Reflection.Emit;

namespace WasmNet.Core.ILGeneration;

public record ReflectionEmitLabel(Label Label) : ILLabel;