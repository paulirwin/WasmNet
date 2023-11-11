using Mono.Cecil.Cil;

namespace WasmNet.Core.ILGeneration;

public record MonoCecilLabel(Instruction Instruction) : ILLabel;