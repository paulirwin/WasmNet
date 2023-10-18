using System.Reflection.Emit;

namespace WasmNet.Core;

public static class WasmCompiler
{
    public static void CompileFunction(ModuleInstance module, MethodBuilder method, WasmType type, WasmCode code)
    {
        var il = method.GetILGenerator();
        var stack = new Stack<Type>();
        
        foreach (var local in code.Locals)
        {
            for (var i = 0; i < local.Count; i++)
            {
                il.DeclareLocal(local.Type.MapWasmTypeToDotNetType());
            }
        }
        
        int callArgsLocalIndex = -1, 
            callTempLocalIndex = -1,
            globalTempLocalIndex = -1;

        if (code.Body.Any(i => i.Opcode == WasmOpcode.Call))
        {
            var argsLocal = il.DeclareLocal(typeof(object[])); // args
            callArgsLocalIndex = argsLocal.LocalIndex;
            
            var tempLocal = il.DeclareLocal(typeof(object));   // temp array value for arg
            callTempLocalIndex = tempLocal.LocalIndex;
        }

        if (code.Body.Any(i => i.Opcode == WasmOpcode.GlobalSet))
        {
            var tempLocal = il.DeclareLocal(typeof(object));   // temp global value
            globalTempLocalIndex = tempLocal.LocalIndex;
        }
        
        foreach (var instruction in code.Body)
        {
            if (instruction.Opcode == WasmOpcode.End)
            {
                il.Emit(OpCodes.Ret);

                if (method.ReturnType != typeof(void))
                {
                    stack.Pop();
                }

                continue;
            }
            
            if (instruction.Opcode == WasmOpcode.I32Const)
            {
                if (instruction.Arguments.Count != 1)
                    throw new InvalidOperationException();
                
                if (instruction.Arguments[0] is not WasmNumberValue<int> numberValue)
                    throw new InvalidOperationException();
                
                il.Emit(OpCodes.Ldc_I4, numberValue.Value);
                stack.Push(typeof(int));
                continue;
            }
            
            if (instruction.Opcode == WasmOpcode.I64Const)
            {
                if (instruction.Arguments.Count != 1)
                    throw new InvalidOperationException();
                
                if (instruction.Arguments[0] is not WasmNumberValue<long> numberValue)
                    throw new InvalidOperationException();
                
                il.Emit(OpCodes.Ldc_I8, numberValue.Value);
                stack.Push(typeof(long));
                continue;
            }
            
            if (instruction.Opcode == WasmOpcode.F32Const)
            {
                if (instruction.Arguments.Count != 1)
                    throw new InvalidOperationException();
                
                if (instruction.Arguments[0] is not WasmNumberValue<float> numberValue)
                    throw new InvalidOperationException();
                
                il.Emit(OpCodes.Ldc_R4, numberValue.Value);
                stack.Push(typeof(float));
                continue;
            }
            
            if (instruction.Opcode == WasmOpcode.F64Const)
            {
                if (instruction.Arguments.Count != 1)
                    throw new InvalidOperationException();
                
                if (instruction.Arguments[0] is not WasmNumberValue<double> numberValue)
                    throw new InvalidOperationException();
                
                il.Emit(OpCodes.Ldc_R8, numberValue.Value);
                stack.Push(typeof(double));
                continue;
            }
            
            if (instruction.Opcode is WasmOpcode.I32Add or WasmOpcode.I64Add or WasmOpcode.F32Add or WasmOpcode.F64Add)
            {
                il.Emit(OpCodes.Add);
                stack.Pop();
                stack.Pop();
                stack.Push(instruction.Opcode switch {
                    WasmOpcode.I32Add => typeof(int),
                    WasmOpcode.I64Add => typeof(long),
                    WasmOpcode.F32Add => typeof(float),
                    WasmOpcode.F64Add => typeof(double),
                    _ => throw new InvalidOperationException()
                });
                continue;
            }

            if (instruction.Opcode is WasmOpcode.I32Sub or WasmOpcode.I64Sub or WasmOpcode.F32Sub or WasmOpcode.F64Sub)
            {
                il.Emit(OpCodes.Sub);
                stack.Pop();
                stack.Pop();
                stack.Push(instruction.Opcode switch {
                    WasmOpcode.I32Sub => typeof(int),
                    WasmOpcode.I64Sub => typeof(long),
                    WasmOpcode.F32Sub => typeof(float),
                    WasmOpcode.F64Sub => typeof(double),
                    _ => throw new InvalidOperationException()
                });
                continue;
            }
            
            if (instruction.Opcode is WasmOpcode.I32Mul or WasmOpcode.I64Mul or WasmOpcode.F32Mul or WasmOpcode.F64Mul)
            {
                il.Emit(OpCodes.Mul);
                stack.Pop();
                stack.Pop();
                stack.Push(instruction.Opcode switch {
                    WasmOpcode.I32Mul => typeof(int),
                    WasmOpcode.I64Mul => typeof(long),
                    WasmOpcode.F32Mul => typeof(float),
                    WasmOpcode.F64Mul => typeof(double),
                    _ => throw new InvalidOperationException()
                });
                continue;
            }
            
            if (instruction.Opcode is WasmOpcode.F32Div or WasmOpcode.F64Div or WasmOpcode.I32DivS or WasmOpcode.I64DivS)
            {
                il.Emit(OpCodes.Div);
                stack.Pop();
                stack.Pop();
                stack.Push(instruction.Opcode switch {
                    WasmOpcode.I32DivS => typeof(int),
                    WasmOpcode.I64DivS => typeof(long),
                    WasmOpcode.F32Div => typeof(float),
                    WasmOpcode.F64Div => typeof(double),
                    _ => throw new InvalidOperationException()
                });
                continue;
            }
            
            if (instruction.Opcode is WasmOpcode.I32DivU or WasmOpcode.I64DivU)
            {
                il.Emit(OpCodes.Div_Un);
                stack.Pop();
                stack.Pop();
                stack.Push(instruction.Opcode switch {
                    WasmOpcode.I32DivU => typeof(int),
                    WasmOpcode.I64DivU => typeof(long),
                    _ => throw new InvalidOperationException()
                });
                continue;
            }
            
            if (instruction.Opcode is WasmOpcode.I32RemS or WasmOpcode.I64RemS)
            {
                il.Emit(OpCodes.Rem);
                stack.Pop();
                stack.Pop();
                stack.Push(instruction.Opcode switch {
                    WasmOpcode.I32RemS => typeof(int),
                    WasmOpcode.I64RemS => typeof(long),
                    _ => throw new InvalidOperationException()
                });
                continue;
            }
            
            if (instruction.Opcode is WasmOpcode.I32RemU or WasmOpcode.I64RemU)
            {
                il.Emit(OpCodes.Rem_Un);
                stack.Pop();
                stack.Pop();
                stack.Push(instruction.Opcode switch {
                    WasmOpcode.I32RemU => typeof(int),
                    WasmOpcode.I64RemU => typeof(long),
                    _ => throw new InvalidOperationException()
                });
                continue;
            }
            
            if (instruction.Opcode is WasmOpcode.I32And or WasmOpcode.I64And)
            {
                il.Emit(OpCodes.And);
                stack.Pop();
                stack.Pop();
                stack.Push(instruction.Opcode switch {
                    WasmOpcode.I32And => typeof(int),
                    WasmOpcode.I64And => typeof(long),
                    _ => throw new InvalidOperationException()
                });
                continue;
            }
            
            if (instruction.Opcode is WasmOpcode.I32Or or WasmOpcode.I64Or)
            {
                il.Emit(OpCodes.Or);
                stack.Pop();
                stack.Pop();
                stack.Push(instruction.Opcode switch {
                    WasmOpcode.I32Or => typeof(int),
                    WasmOpcode.I64Or => typeof(long),
                    _ => throw new InvalidOperationException()
                });
                continue;
            }
            
            if (instruction.Opcode is WasmOpcode.I32Xor or WasmOpcode.I64Xor)
            {
                il.Emit(OpCodes.Xor);
                stack.Pop();
                stack.Pop();
                stack.Push(instruction.Opcode switch {
                    WasmOpcode.I32Xor => typeof(int),
                    WasmOpcode.I64Xor => typeof(long),
                    _ => throw new InvalidOperationException()
                });
                continue;
            }
            
            if (instruction.Opcode is WasmOpcode.I32Shl or WasmOpcode.I64Shl)
            {
                il.Emit(OpCodes.Shl);
                stack.Pop();
                stack.Pop();
                stack.Push(instruction.Opcode switch {
                    WasmOpcode.I32Shl => typeof(int),
                    WasmOpcode.I64Shl => typeof(long),
                    _ => throw new InvalidOperationException()
                });
                continue;
            }
            
            if (instruction.Opcode is WasmOpcode.I32ShrS or WasmOpcode.I64ShrS)
            {
                il.Emit(OpCodes.Shr);
                stack.Pop();
                stack.Pop();
                stack.Push(instruction.Opcode switch {
                    WasmOpcode.I32ShrS => typeof(int),
                    WasmOpcode.I64ShrS => typeof(long),
                    _ => throw new InvalidOperationException()
                });
                continue;
            }
            
            if (instruction.Opcode is WasmOpcode.I32ShrU or WasmOpcode.I64ShrU)
            {
                il.Emit(OpCodes.Shr_Un);
                stack.Pop();
                stack.Pop();
                stack.Push(instruction.Opcode switch {
                    WasmOpcode.I32ShrU => typeof(int),
                    WasmOpcode.I64ShrU => typeof(long),
                    _ => throw new InvalidOperationException()
                });
                continue;
            }

            if (instruction.Opcode is WasmOpcode.I32Eq or WasmOpcode.I64Eq)
            {
                il.Emit(OpCodes.Ceq);
                stack.Pop();
                stack.Pop();
                stack.Push(typeof(bool));
                continue;
            }
            
            if (instruction.Opcode == WasmOpcode.LocalGet)
            {
                if (instruction.Arguments.Count != 1)
                    throw new InvalidOperationException();
                
                if (instruction.Arguments[0] is not WasmNumberValue<int> numberValue)
                    throw new InvalidOperationException();

                if (numberValue.Value < type.Parameters.Count)
                {
                    il.Emit(OpCodes.Ldarg, numberValue.Value + 1);
                    stack.Push(type.Parameters[numberValue.Value].MapWasmTypeToDotNetType());
                }
                else
                {
                    il.Emit(OpCodes.Ldloc, numberValue.Value - type.Parameters.Count);
                    stack.Push(code.Locals[numberValue.Value - type.Parameters.Count].Type.MapWasmTypeToDotNetType());
                }
                
                continue;
            }

            if (instruction.Opcode == WasmOpcode.LocalSet)
            {
                if (instruction.Arguments.Count != 1)
                    throw new InvalidOperationException();
                
                if (instruction.Arguments[0] is not WasmNumberValue<int> numberValue)
                    throw new InvalidOperationException();

                if (numberValue.Value < type.Parameters.Count)
                {
                    il.Emit(OpCodes.Starg, numberValue.Value + 1);
                    stack.Pop();
                }
                else
                {
                    il.Emit(OpCodes.Stloc, numberValue.Value - type.Parameters.Count);
                    stack.Pop();
                }
                
                continue;
            }

            if (instruction.Opcode == WasmOpcode.Call)
            {
                var (funcInstance, funcIndex) = GetFunctionInstanceForCall(module, instruction);

                il.Emit(OpCodes.Ldc_I4, funcInstance.ParameterTypes.Length); // num_args = # of args
                il.Emit(OpCodes.Newarr, typeof(object));                     // new object[num_args]
                il.Emit(OpCodes.Stloc, callArgsLocalIndex);              // store array in local

                var stackValues = stack.ToList();
                
                if (stackValues.Count < funcInstance.ParameterTypes.Length)
                    throw new InvalidOperationException("Stack would underflow");
                
                // Push args onto array in reverse order.
                // The stack at this point for a call of the form func(1, 2, 3) is 1, 2, 3
                // Since we can only pop the args off the stack in reverse order,
                // we need to store them in a temp local one by one and then set the array element.
                for (int paramIndex = funcInstance.ParameterTypes.Length - 1, stackIndex = 0; 
                     paramIndex >= 0; 
                     paramIndex--, stackIndex++)
                {
                    var stackType = stackValues[stackIndex];

                    if (stackType.IsValueType)
                    {
                        il.Emit(OpCodes.Box, stackType); // box value type
                    }

                    il.Emit(OpCodes.Stloc, callTempLocalIndex);      // store arg in temp local
                    il.Emit(OpCodes.Ldloc, callArgsLocalIndex);      // load array
                    il.Emit(OpCodes.Ldc_I4, paramIndex);                 // array index
                    il.Emit(OpCodes.Ldloc, callTempLocalIndex);      // load arg from temp local
                    il.Emit(OpCodes.Stelem_Ref);                         // args[index] = arg
                    stack.Pop();
                }
                    
                il.Emit(OpCodes.Ldarg_0);                           // load module instance
                il.Emit(OpCodes.Ldc_I4, funcIndex);                 // load function index
                il.Emit(OpCodes.Callvirt, typeof(ModuleInstance).GetMethod(nameof(ModuleInstance.GetFunction))!); // get function instance
                // stack now contains function instance
                
                il.Emit(OpCodes.Ldloc, callArgsLocalIndex);     // load args array
                il.Emit(OpCodes.Callvirt, typeof(IFunctionInstance).GetMethod(nameof(IFunctionInstance.Invoke))!); // invoke function
                // stack now contains return value
                stack.Push(funcInstance.ReturnType);
                
                if (funcInstance.ReturnType == typeof(void))
                {
                    il.Emit(OpCodes.Pop); // pop return value
                    stack.Pop();
                }
                else if (funcInstance.ReturnType.IsValueType)
                {
                    il.Emit(OpCodes.Unbox_Any, funcInstance.ReturnType); // unbox return value
                }
                
                continue;
            }

            if (instruction.Opcode == WasmOpcode.GlobalGet)
            {
                if (instruction.Arguments.Count != 1)
                    throw new InvalidOperationException();
                
                if (instruction.Arguments[0] is not WasmNumberValue<int> numberValue)
                    throw new InvalidOperationException();
                
                var globalIndex = numberValue.Value;

                var globalRef = module.GetGlobal(globalIndex);
                var globalType = globalRef.Global.Type.MapWasmTypeToDotNetType();
                
                il.Emit(OpCodes.Ldarg_0);                           // load module instance
                il.Emit(OpCodes.Ldc_I4, globalIndex);               // load global index
                il.Emit(OpCodes.Callvirt, typeof(ModuleInstance).GetMethod(nameof(ModuleInstance.GetGlobalValue))!); // get global instance
                // stack now contains global value
                
                if (globalType.IsValueType)
                {
                    il.Emit(OpCodes.Unbox_Any, globalRef.Global.Type.MapWasmTypeToDotNetType()); // unbox global value
                }
                
                stack.Push(globalType);
                
                continue;
            }
            
            if (instruction.Opcode == WasmOpcode.GlobalSet)
            {
                if (instruction.Arguments.Count != 1)
                    throw new InvalidOperationException();
                
                if (instruction.Arguments[0] is not WasmNumberValue<int> numberValue)
                    throw new InvalidOperationException();
                
                var globalIndex = numberValue.Value;
                var globalRef = module.GetGlobal(globalIndex);
                var globalType = globalRef.Global.Type.MapWasmTypeToDotNetType();

                var argType = stack.Pop();
                
                if (argType != globalType) 
                {
                    throw new InvalidOperationException($"Global {globalIndex} is of type {globalType} but stack contains {argType}");
                }

                if (!globalRef.Mutable || !globalRef.Global.Mutable)
                {
                    throw new InvalidOperationException($"Global {globalIndex} is not mutable");
                }
                
                if (argType.IsValueType)
                {
                    il.Emit(OpCodes.Box, argType); // box value type
                }
                
                il.Emit(OpCodes.Stloc, globalTempLocalIndex);      // store arg in temp local
                
                il.Emit(OpCodes.Ldarg_0);                           // load module instance
                il.Emit(OpCodes.Ldc_I4, globalIndex);               // load global index
                il.Emit(OpCodes.Ldloc, globalTempLocalIndex);       // load arg from temp local
                il.Emit(OpCodes.Callvirt, typeof(ModuleInstance).GetMethod(nameof(ModuleInstance.SetGlobalValue))!); // set global instance
                
                continue;
            }
            
            throw new NotImplementedException($"Opcode {instruction.Opcode} not implemented in compiler.");
        }
    }

    private static (IFunctionInstance Function, int Index) GetFunctionInstanceForCall(
        ModuleInstance module,
        WasmInstruction instruction)
    {
        if (instruction.Arguments.Count != 1)
            throw new InvalidOperationException();

        if (instruction.Arguments[0] is not WasmNumberValue<int> numberValue)
            throw new InvalidOperationException();

        return (module.GetFunction(numberValue.Value), numberValue.Value);
    }
}