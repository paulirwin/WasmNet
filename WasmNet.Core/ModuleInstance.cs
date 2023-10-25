namespace WasmNet.Core;

public class ModuleInstance(WasmModule module, Store store)
{
    private readonly List<WasmType> _types = new();
    private readonly List<int> _functionAddresses = new();
    private readonly List<int> _memoryAddresses = new();
    private readonly List<(int Address, bool Mutable)> _globalAddresses = new();
    private readonly List<int> _tableAddresses = new();

    public WasmModule Module { get; } = module;

    public Store Store { get; } = store;

    public IReadOnlyList<WasmType> Types => _types;

    public IReadOnlyList<int> FunctionAddresses => _functionAddresses;
    
    public IReadOnlyList<(int Address, bool Mutable)> GlobalAddresses => _globalAddresses;
    
    public IReadOnlyList<int> MemoryAddresses => _memoryAddresses;
    
    public IReadOnlyList<int> TableAddresses => _tableAddresses;
    
    public Lazy<EmitAssembly> EmitAssembly { get; } = new(LazyThreadSafetyMode.ExecutionAndPublication);
    
    public IDictionary<string, IDictionary<string, object?>>? Importables { get; set; }

    public int AddType(WasmType type)
    {
        var index = _types.Count;
        _types.Add(type);
        return index;
    }
    
    public int AddFunctionAddress(int address)
    {
        var index = _functionAddresses.Count;
        _functionAddresses.Add(address);
        return index;
    }
    
    public IFunctionInstance GetFunction(int index)
    {
        var address = _functionAddresses[index];

        return Store.Functions[address];
    }

    public int AddGlobalAddress(int address, bool mutable)
    {
        var index = _globalAddresses.Count;
        _globalAddresses.Add((address, mutable));
        return index;
    }
    
    public object? GetGlobalValue(int index)
    {
        var (address, _) = _globalAddresses[index];

        return Store.Globals[address].Value;
    }
    
    public void SetGlobalValue(int index, object? value)
    {
        var (address, mutable) = _globalAddresses[index];

        if (!mutable)
        {
            throw new InvalidOperationException("Cannot set value of immutable global");
        }
        
        Store.Globals[address].Value = value;
    }

    public (Global Global, bool Mutable) GetGlobal(int index)
    {
        var (address, mutable) = _globalAddresses[index];

        return (Store.Globals[address], mutable);
    }
    
    public int AddMemoryAddress(int address)
    {
        var index = _memoryAddresses.Count;
        _memoryAddresses.Add(address);
        return index;
    }
    
    public int AddTableAddress(int address)
    {
        var index = _tableAddresses.Count;
        _tableAddresses.Add(address);
        return index;
    }
    
    /// <summary>
    /// Performs an indirect call to a function.
    ///
    /// The opcode call_indirect is in the docs with two arguments:
    /// - x: the index of the table address
    /// - y: the index of the function type
    ///
    /// It then expects the next value on the stack to be the index
    /// of the element in the table. In this case, we're going to
    /// take it as an argument and evaluate this in .NET code.
    /// </summary>
    /// <param name="tableIndex">The index of the table address</param>
    /// <param name="typeIndex">The index of the function type</param>
    /// <param name="elementIndex">The index of the element in the table</param>
    /// <param name="arguments">The arguments to pass to the function</param>
    /// <returns>The result of the function</returns>
    public object? CallIndirect(int tableIndex, 
        int typeIndex, 
        int elementIndex, 
        params object?[]? arguments)
    {
        var tableAddress = _tableAddresses[tableIndex];
        var table = Store.Tables[tableAddress];
        var functionType = _types[typeIndex];
        var element = table.Elements[elementIndex];
        
        if (element is NullReference)
        {
            throw new InvalidOperationException("Cannot call null reference");
        }

        if (element is not FunctionReference functionReference)
        {
            throw new InvalidOperationException("Cannot call non-function reference");
        }
        
        var function = Store.Functions[functionReference.Address];

        if (function.ParameterTypes.Length != functionType.Parameters.Count)
        {
            // TODO: also check the parameter types
            throw new InvalidOperationException("Function parameter count mismatch");
        }

        if (function.ReturnType != functionType.Results[0].DotNetType)
        {
            throw new InvalidOperationException("Function return type mismatch");
        }

        var functionArguments = arguments?.ToArray() ?? Array.Empty<object?>();
        var functionResult = function.Invoke(functionArguments);
        return functionResult;
    }
    
    public FunctionReference GetFunctionReference(int index)
    {
        var address = _functionAddresses[index];

        return new FunctionReference(address);
    }
    
    public void MemoryStoreI32(int dynamicOffset, int value, int staticOffset, int storageSize)
    {
        // WASM spec section 4.4.7
        
        // 1. Let 𝐹 be the current frame.
        // 2. Assert: due to validation, 𝐹.module.memaddrs[0] exists.
        // 3. Let 𝑎 be the memory address 𝐹.module.memaddrs[0].
        var a = _memoryAddresses[0];
        
        // 4. Assert: due to validation, 𝑆.mems[𝑎] exists.
        // 5. Let mem be the memory instance 𝑆.mems[𝑎].
        var mem = Store.Memory[a];
        
        // 6. Assert: due to validation, a value of value type 𝑡 is on the top of the stack.
        // 7. Pop the value 𝑡.const 𝑐 from the stack.
        //    (this is the value parameter)
        var c = value;
        
        // 8. Assert: due to validation, a value of value type i32 is on the top of the stack.
        // 9. Pop the value i32.const 𝑖 from the stack.
        //    (this is the dynamicOffset parameter)
        var i = dynamicOffset;
        
        // 10. Let ea be the integer 𝑖 + memarg.offset.
        var ea = i + staticOffset;
        
        // 11. If 𝑁 is not part of the instruction, then:
        //      a. Let 𝑁 be the bit width |𝑡| of number type 𝑡.
        //     (this is the storageSize parameter)
        var N = storageSize;

        if (N == 0)
        {
            N = 32;
        }
        
        // 12. If ea + 𝑁/8 is larger than the length of mem.data, then:
        //      a. Trap.
        if (ea + N / 8 > mem.Size)
        {
            throw new InvalidOperationException("Memory store out of bounds");
        }
        
        // 13. If 𝑁 is part of the instruction, then:
        //      a. Let 𝑛 be the result of computing wrap_|𝑡|,𝑁 (𝑐).
        //      b. Let 𝑏* be the byte sequence bytes_i𝑁(𝑛).
        // 14. Else:
        //      a. Let 𝑏* be the byte sequence bytes_𝑡(𝑐).
        byte[] b;
        
        if (storageSize != 0)
        {
            var n = Wrap(c, N);
            b = BitConverter.GetBytes(n);
        }
        else
        {
            b = BitConverter.GetBytes(c);
        }
        
        // 15. Replace the bytes mem.data[ea : 𝑁/8] with 𝑏*.
        mem.Write(ea, b);
    }
    
    // TODO: refactor this to share code with MemoryStoreI32
    public void MemoryStoreI64(int dynamicOffset, long value, int staticOffset, int storageSize)
    {
        // WASM spec section 4.4.7
        
        // 1. Let 𝐹 be the current frame.
        // 2. Assert: due to validation, 𝐹.module.memaddrs[0] exists.
        // 3. Let 𝑎 be the memory address 𝐹.module.memaddrs[0].
        var a = _memoryAddresses[0];
        
        // 4. Assert: due to validation, 𝑆.mems[𝑎] exists.
        // 5. Let mem be the memory instance 𝑆.mems[𝑎].
        var mem = Store.Memory[a];
        
        // 6. Assert: due to validation, a value of value type 𝑡 is on the top of the stack.
        // 7. Pop the value 𝑡.const 𝑐 from the stack.
        //    (this is the value parameter)
        var c = value;
        
        // 8. Assert: due to validation, a value of value type i32 is on the top of the stack.
        // 9. Pop the value i32.const 𝑖 from the stack.
        //    (this is the dynamicOffset parameter)
        var i = dynamicOffset;
        
        // 10. Let ea be the integer 𝑖 + memarg.offset.
        var ea = i + staticOffset;
        
        // 11. If 𝑁 is not part of the instruction, then:
        //      a. Let 𝑁 be the bit width |𝑡| of number type 𝑡.
        //     (this is the storageSize parameter)
        var N = storageSize;

        if (N == 0)
        {
            N = 64;
        }
        
        // 12. If ea + 𝑁/8 is larger than the length of mem.data, then:
        //      a. Trap.
        if (ea + N / 8 > mem.Size)
        {
            throw new InvalidOperationException("Memory store out of bounds");
        }
        
        // 13. If 𝑁 is part of the instruction, then:
        //      a. Let 𝑛 be the result of computing wrap_|𝑡|,𝑁 (𝑐).
        //      b. Let 𝑏* be the byte sequence bytes_i𝑁(𝑛).
        // 14. Else:
        //      a. Let 𝑏* be the byte sequence bytes_𝑡(𝑐).
        byte[] b;
        
        if (storageSize != 0)
        {
            var n = Wrap(c, N);
            b = BitConverter.GetBytes(n);
        }
        else
        {
            b = BitConverter.GetBytes(c);
        }
        
        // 15. Replace the bytes mem.data[ea : 𝑁/8] with 𝑏*.
        mem.Write(ea, b);
    }

    public int MemoryLoadI32(int dynamicOffset, int staticOffset, int storageSize, bool signExtend)
    {
        // NOTE: this is a little out of order from the spec, but it's easier to follow this way
        // 9. If 𝑁 is not part of the instruction, then:
        //      a. Let 𝑁 be the bit width |𝑡| of number type 𝑡.
        //     (this is the storageSize parameter)
        var N = storageSize;
        
        if (N == 0)
        {
            N = 32;
        }
        
        var b = PerformMemoryLoad(dynamicOffset, staticOffset, N);
        
        // 12. If 𝑁 and sx are part of the instruction, then:
        //      a. Let 𝑛 be the integer for which bytes_i𝑁(𝑛) = 𝑏*.
        //      b. Let 𝑐 be the result of computing extendsx_𝑁,|𝑡|(𝑛).
        // 13. Else:
        //      a. Let 𝑐 be the constant for which bytes_𝑡(𝑐) = 𝑏*.
        int c;
        
        if (storageSize != 0 && signExtend)
        {
            var n = BitConverter.ToInt32(b);
            c = SignExtend(n, N);
        }
        else
        {
            c = BitConverter.ToInt32(b);
        }
        
        // 14. Push the value 𝑡.const 𝑐 to the stack.
        return c;
    }
    
    public long MemoryLoadI64(int dynamicOffset, int staticOffset, int storageSize, bool signExtend)
    {
        // NOTE: this is a little out of order from the spec, but it's easier to follow this way
        // 9. If 𝑁 is not part of the instruction, then:
        //      a. Let 𝑁 be the bit width |𝑡| of number type 𝑡.
        //     (this is the storageSize parameter)
        var N = storageSize;
        
        if (N == 0)
        {
            N = 64;
        }
        
        var b = PerformMemoryLoad(dynamicOffset, staticOffset, N);
        
        // 12. If 𝑁 and sx are part of the instruction, then:
        //      a. Let 𝑛 be the integer for which bytes_i𝑁(𝑛) = 𝑏*.
        //      b. Let 𝑐 be the result of computing extendsx_𝑁,|𝑡|(𝑛).
        // 13. Else:
        //      a. Let 𝑐 be the constant for which bytes_𝑡(𝑐) = 𝑏*.
        long c;
        
        if (storageSize != 0 && signExtend)
        {
            var n = BitConverter.ToInt64(b);
            c = SignExtend(n, N);
        }
        else
        {
            c = BitConverter.ToInt64(b);
        }
        
        // 14. Push the value 𝑡.const 𝑐 to the stack.
        return c;
    }

    private byte[] PerformMemoryLoad(int dynamicOffset, int staticOffset, int storageSize)
    {
        // 1. Let 𝐹 be the current frame.
        // 2. Assert: due to validation, 𝐹.module.memaddrs[0] exists.
        // 3. Let 𝑎 be the memory address 𝐹.module.memaddrs[0].
        var a = _memoryAddresses[0];
        
        // 4. Assert: due to validation, 𝑆.mems[𝑎] exists.
        // 5. Let mem be the memory instance 𝑆.mems[𝑎].
        var mem = Store.Memory[a];
        
        // 6. Assert: due to validation, a value of value type i32 is on the top of the stack.
        // 7. Pop the value i32.const 𝑖 from the stack.
        //    (this is the dynamicOffset parameter)
        var i = dynamicOffset;
        
        // 8. Let ea be the integer 𝑖 + memarg.offset.
        var ea = i + staticOffset;
        
        // 10. If ea + 𝑁/8 is larger than the length of mem.data, then:
        //      a. Trap.
        if (ea + storageSize / 8 > mem.Size)
        {
            throw new InvalidOperationException("Memory load out of bounds");
        }
        
        // 11. Let 𝑏* be the byte sequence mem.data[ea : 𝑁/8].
        return mem.Read(ea, storageSize / 8);
    }

    private static int SignExtend(int i, int N)
    {
        var mask = (int)Math.Pow(2, N) - 1;
        var signBit = (int)Math.Pow(2, N - 1);
        var sign = i & signBit;
        var value = i & mask;

        if (sign != 0)
        {
            value |= ~mask;
        }

        return value;
    }
    
    private static long SignExtend(long i, int N)
    {
        var mask = (long)Math.Pow(2, N) - 1;
        var signBit = (long)Math.Pow(2, N - 1);
        var sign = i & signBit;
        var value = i & mask;

        if (sign != 0)
        {
            value |= ~mask;
        }

        return value;
    }

    private static int Wrap(int i, int N) => i % (int)Math.Pow(2, N);
    
    private static long Wrap(long i, int N) => i % (long)Math.Pow(2, N);
}