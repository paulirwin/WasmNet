using System.Text;

namespace WasmNet.Core;

public class WasmReader
{
    private readonly Stream _stream;
    
    public WasmReader(Stream stream)
    {
        _stream = stream;
    }

    public WasmModule ReadModule()
    {
        _stream.Seek(0, SeekOrigin.Begin);

        ValidateWasmHeader();
        
        var module = new WasmModule();

        while (_stream.Position < _stream.Length)
        {
            var section = ReadSection();

            switch (section)
            {
                case WasmTypeSection typeSection:
                    module.TypeSection = typeSection;
                    break;
                case WasmFunctionSection functionSection:
                    module.FunctionSection = functionSection;
                    break;
                case WasmTableSection tableSection:
                    module.TableSection = tableSection;
                    break;
                case WasmElementSection elementSection:
                    module.ElementsSection = elementSection;
                    break;
                case WasmExportSection exportSection:
                    module.ExportSection = exportSection;
                    break;
                case WasmCodeSection codeSection:
                    module.CodeSection = codeSection;
                    break;
                case WasmImportSection importSection:
                    module.ImportSection = importSection;
                    break;
                case WasmGlobalSection globalSection:
                    module.GlobalSection = globalSection;
                    break;
                case WasmDataSection dataSection:
                    module.DataSection = dataSection;
                    break;
                case WasmMemorySection memorySection:
                    module.MemorySection = memorySection;
                    break;
                case WasmDataCountSection dataCountSection:
                    module.DataCountSection = dataCountSection;
                    break;
                case WasmStartSection startSection:
                    module.StartSection = startSection;
                    break;
            }
        }

        if (module.DataCountSection is { Count: var dataCount }
            && module.DataSection?.Data.Count != dataCount)
        {
            throw new InvalidOperationException("Data count section does not match data section length.");
        }
        
        return module;
    }

    private WasmModuleSection ReadSection()
    {
        var id = _stream.ReadByte();

        if (id == -1)
        {
            throw new Exception("Invalid WASM file.");
        }

        var length = ReadVarUInt32();
        var sectionStart = _stream.Position;

        WasmModuleSection section = id switch
        {
            1 => ReadTypeSection(),
            2 => ReadImportSection(),
            3 => ReadFunctionSection(),
            4 => ReadTableSection(),
            5 => ReadMemorySection(),
            6 => ReadGlobalSection(),
            7 => ReadExportSection(),
            8 => ReadStartSection(),
            9 => ReadElementSection(),
            10 => ReadCodeSection(),
            11 => ReadDataSection(),
            12 => ReadDataCountSection(),
            _ => throw new Exception($"Unsupported WASM section: 0x{id:X}")
        };
        
        var sectionEnd = _stream.Position;
        
        if (sectionEnd - sectionStart != length)
        {
            throw new Exception("Invalid WASM section length.");
        }

        return section;
    }

    private WasmModuleSection ReadStartSection()
    {
        var index = ReadVarUInt32();

        return new WasmStartSection
        {
            FuncIndex = (int)index,
        };
    }

    private WasmModuleSection ReadDataCountSection()
    {
        var count = ReadVarUInt32();

        return new WasmDataCountSection
        {
            Count = (int)count,
        };
    }

    private WasmModuleSection ReadMemorySection()
    {
        var section = new WasmMemorySection();

        var count = ReadVarUInt32();

        for (var i = 0; i < count; i++)
        {
            var memory = ReadMemory();
            section.Memories.Add(memory);
        }

        return section;
    }

    private WasmMemory ReadMemory()
    {
        return new WasmMemory
        {
            Limits = ReadLimits()
        };
    }

    private WasmModuleSection ReadDataSection()
    {
        var section = new WasmDataSection();

        var count = ReadVarUInt32();

        for (var i = 0; i < count; i++)
        {
            var data = ReadData();
            section.Data.Add(data);
        }

        return section; 
    }

    private WasmData ReadData()
    {
        var dataKind = (WasmDataKind)ReadVarUInt32();
        Expression? offsetExpr = null;
        int? memIndex = null;
        byte[] data;
        
        switch (dataKind)
        {
            case WasmDataKind.ActiveOffsetZero:
                offsetExpr = ReadExpression();
                memIndex = 0;
                data = ReadByteVector();
                break;
            case WasmDataKind.Passive:
                data = ReadByteVector();
                break;
            case WasmDataKind.ActiveOffsetSpecified:
                memIndex = (int)ReadVarUInt32();
                offsetExpr = ReadExpression();
                data = ReadByteVector();
                break;
            default:
                throw new Exception($"Unexpected data kind: {dataKind:X}");
        }

        return new WasmData
        {
            DataKind = dataKind,
            MemoryIndex = memIndex,
            OffsetExpr = offsetExpr,
            Data = data
        };
    }
    
    private uint[] ReadUInt32Vector()
    {
        var size = ReadVarUInt32();
        var data = new uint[size];

        for (var i = 0; i < size; i++)
        {
            data[i] = ReadVarUInt32();
        }

        return data;
    } 

    private byte[] ReadByteVector()
    {
        var size = ReadVarUInt32();
        var data = new byte[size];

        if (_stream.Read(data, 0, (int)size) != size)
        {
            throw new Exception("Invalid WASM file.");
        }

        return data;
    }

    private WasmModuleSection ReadGlobalSection()
    {
        var section = new WasmGlobalSection();

        var count = ReadVarUInt32();

        for (var i = 0; i < count; i++)
        {
            var global = ReadGlobal();
            section.Globals.Add(global);
        }

        return section;
    }

    private WasmGlobal ReadGlobal()
    {
        var type = ReadValueType();
        var mutable = _stream.ReadByte() == 1;
        var body = ReadExpression();

        return new WasmGlobal(type, mutable, body);
    }

    private WasmModuleSection ReadImportSection()
    {
        var section = new WasmImportSection();

        var count = ReadVarUInt32();

        for (var i = 0; i < count; i++)
        {
            var import = ReadImport();
            section.Imports.Add(import);
        }

        return section;
    }

    private WasmImport ReadImport()
    {
        var moduleNameLength = ReadVarUInt32();
        var moduleName = new byte[moduleNameLength];

        if (_stream.Read(moduleName, 0, (int)moduleNameLength) != moduleNameLength)
        {
            throw new Exception("Invalid WASM file.");
        }

        var functionNameLength = ReadVarUInt32();
        var functionName = new byte[functionNameLength];

        if (_stream.Read(functionName, 0, (int)functionNameLength) != functionNameLength)
        {
            throw new Exception("Invalid WASM file.");
        }

        var kind = _stream.ReadByte();

        if (kind == -1)
        {
            throw new Exception("Invalid WASM file.");
        }

        WasmImportDescriptor desc;

        switch ((WasmImportKind)kind)
        {
            case WasmImportKind.Function:
                var index = ReadVarUInt32();
                desc = new WasmFunctionImportDescriptor
                {
                    TypeIndex = (int)index
                };
                break;
            case WasmImportKind.Global:
                var valueType = ReadValueType();
                var mutable = _stream.ReadByte() == 1;
                desc = new WasmGlobalImportDescriptor
                {
                    Type = valueType,
                    Mutable = mutable
                };
                break;
            case WasmImportKind.Memory:
                var limits = ReadLimits();
                desc = new WasmMemoryImportDescriptor
                {
                    Limits = limits
                };
                break;
            default:
                throw new NotImplementedException($"Imports of kind {(WasmImportKind)kind} not yet implemented");
        }

        return new WasmImport
        {
            ModuleName = Encoding.UTF8.GetString(moduleName),
            Name = Encoding.UTF8.GetString(functionName),
            Kind = (WasmImportKind)kind,
            Descriptor = desc,
        };
    }

    private WasmLimits ReadLimits()
    {
        var flags = (WasmLimitsFlags)_stream.ReadByte();
        
        if (!Enum.IsDefined(flags))
        {
            throw new Exception($"Invalid WASM limits flags value: {flags}");
        }

        var min = ReadVarUInt32();
        var max = flags == WasmLimitsFlags.MinAndMax ? ReadVarUInt32() : int.MaxValue;

        return new WasmLimits
        {
            Min = (int)min,
            Max = (int)max
        };
    }

    private WasmCodeSection ReadCodeSection()
    {
        var section = new WasmCodeSection();

        var count = ReadVarUInt32();

        for (var i = 0; i < count; i++)
        {
            var code = ReadCode();
            section.Codes.Add(code);
        }

        return section;
    }

    private WasmCode ReadCode()
    {
        var size = ReadVarUInt32();
        var codeStart = _stream.Position;
        
        var locals = new List<WasmLocal>();
        var localDeclCount = ReadVarUInt32();

        for (var i = 0; i < localDeclCount; i++)
        {
            var localTypeCount = ReadVarUInt32();
            var localType = ReadValueType();
        
            for (var j = 0; j < localTypeCount; j++)
            {
                locals.Add(new WasmLocal
                {
                    Count = (int)localTypeCount,
                    Type = localType
                });
            }
        }
        
        var body = ReadExpression();

        var codeEnd = _stream.Position;

        if (codeEnd - codeStart != size)
        {
            throw new Exception("Invalid WASM code length.");
        }

        return new WasmCode
        {
            Locals = locals,
            Body = body,
        };
    }

    private Expression ReadExpression()
    {
        var body = new List<WasmInstruction>();

        while (true)
        {
            var instruction = ReadInstruction();
            
            if (instruction.Opcode == WasmOpcode.End)
            {
                break;
            }
            
            body.Add(instruction);
        }

        return new Expression
        {
            Instructions = body,
        };
    }
    
    private (Expression IfBody, Expression? ElseBody) ReadIfElseExpressions()
    {
        // This function is a rudimentary state machine that first reads
        // the "if" part of the expression body, and if it encounters an
        // "else" opcode (0x05), it switches state to start reading the
        // remaining instructions as the "else" body.
        var ifBody = new List<WasmInstruction>();
        List<WasmInstruction>? elseBody = null;

        var currentBody = ifBody;

        while (true)
        {
            var instruction = ReadInstruction();
            
            if (instruction.Opcode == WasmOpcode.End)
            {
                break;
            }

            if (instruction.Opcode == WasmOpcode.Else)
            {
                elseBody = new List<WasmInstruction>();
                currentBody = elseBody;
                continue;
            }

            currentBody.Add(instruction);
        }

        return (new Expression
        {
            Instructions = ifBody,
        }, elseBody != null
            ? new Expression
            {
                Instructions = elseBody
            }
            : null);
    }

    private WasmInstruction ReadInstruction()
    {
        var opcode = (WasmOpcode)_stream.ReadByte();

        switch (opcode)
        {
            case (WasmOpcode)(-1):
                throw new Exception("Invalid WASM file.");
            case WasmOpcode.ExtendedOpcodes:
                return ReadExtendedOpcode();
            case WasmOpcode.I32Const:
            {
                var arg = ReadVarInt32();
                return new WasmInstruction(WasmOpcode.I32Const, new WasmI32Value(arg));
            }
            case WasmOpcode.I64Const:
            {
                var arg = ReadVarInt64();
                return new WasmInstruction(WasmOpcode.I64Const, new WasmI64Value(arg));
            }
            case WasmOpcode.F32Const:
            {
                var arg = ReadVarFloat32();
                return new WasmInstruction(WasmOpcode.F32Const, new WasmF32Value(arg));
            }
            case WasmOpcode.F64Const:
            {
                var arg = ReadVarFloat64();
                return new WasmInstruction(WasmOpcode.F64Const, new WasmF64Value(arg));
            }
            case WasmOpcode.LocalSet 
                or WasmOpcode.LocalGet 
                or WasmOpcode.LocalTee
                or WasmOpcode.GlobalGet
                or WasmOpcode.GlobalSet
                or WasmOpcode.Call
                or WasmOpcode.BrIf
                or WasmOpcode.Br:
            {
                var arg = (int)ReadVarUInt32();
                return new WasmInstruction(opcode, new WasmI32Value(arg));
            }
            case WasmOpcode.CallIndirect:
            {
                var y = (int)ReadVarUInt32();
                var x = (int)ReadVarUInt32();
                return new WasmInstruction(WasmOpcode.CallIndirect,
                    new WasmI32Value(x),
                    new WasmI32Value(y));
            }
            case WasmOpcode.I32Store
                or WasmOpcode.I32Load
                or WasmOpcode.I64Store
                or WasmOpcode.I64Load
                or WasmOpcode.I32Store8
                or WasmOpcode.I32Store16
                or WasmOpcode.I32Load8S
                or WasmOpcode.I64Load8S
                or WasmOpcode.I32Load8U
                or WasmOpcode.I64Load8U
                or WasmOpcode.I32Load16S
                or WasmOpcode.I64Load16S
                or WasmOpcode.I32Load16U
                or WasmOpcode.I64Load16U
                or WasmOpcode.I64Load32S
                or WasmOpcode.I64Load32U
                or WasmOpcode.I64Store8
                or WasmOpcode.I64Store16
                or WasmOpcode.I64Store32
                or WasmOpcode.F32Store
                or WasmOpcode.F64Store
                or WasmOpcode.F32Load
                or WasmOpcode.F64Load:
            {
                var align = (int)ReadVarUInt32();
                var offset = (int)ReadVarUInt32();
                return new WasmInstruction(opcode,
                    new WasmI32Value(offset),
                    new WasmI32Value(align));
            }
            case WasmOpcode.Block or WasmOpcode.Loop:
            {
                var blockType = ReadBlockType();
                var expr = ReadExpression();
                return new WasmInstruction(opcode, blockType, new WasmExpressionValue(expr));
            }
            case WasmOpcode.BrTable:
            {
                var labels = ReadUInt32Vector();
                var defaultLabel = (int)ReadVarUInt32();
                return new WasmInstruction(WasmOpcode.BrTable,
                    new WasmI32VectorValue(labels.Select(i => (int)i).ToArray()),
                    new WasmI32Value(defaultLabel));
            }
            case WasmOpcode.If:
            {
                // Spec 5.4.1
                // 0x04 bt:blocktype (in:instr)* 0x0B ⇒ if bt in* else 𝜖 end
                // 0x04 bt:blocktype (in1:instr)* 0x05 (in2:instr)* 0x0B ⇒ if bt in1* else in2* end
                var blockType = ReadBlockType();
                var (ifBody, elseBody) = ReadIfElseExpressions();

                return elseBody == null
                    ? new WasmInstruction(WasmOpcode.If, 
                        blockType,
                        new WasmExpressionValue(ifBody))
                    : new WasmInstruction(WasmOpcode.If, 
                        blockType,
                        new WasmExpressionValue(ifBody),
                        new WasmExpressionValue(elseBody));
            }
            case WasmOpcode.I32Add:
            case WasmOpcode.I32Sub:
            case WasmOpcode.I32Mul:
            case WasmOpcode.I32DivS:
            case WasmOpcode.I32DivU:
            case WasmOpcode.I32RemS:
            case WasmOpcode.I32RemU:
            case WasmOpcode.I32And:
            case WasmOpcode.I32Or:
            case WasmOpcode.I32Xor:
            case WasmOpcode.I32Shl:
            case WasmOpcode.I32ShrU:
            case WasmOpcode.I32ShrS:
            case WasmOpcode.I32Eqz:
            case WasmOpcode.I32Eq:
            case WasmOpcode.I32Ne:
            case WasmOpcode.I32LtS:
            case WasmOpcode.I32GtS:
            case WasmOpcode.I32LeS:
            case WasmOpcode.I32GeS:
            case WasmOpcode.I32GeU:
            case WasmOpcode.I32LtU:
            case WasmOpcode.I32LeU:
            case WasmOpcode.I32GtU:
            case WasmOpcode.I64Add:
            case WasmOpcode.I64Sub:
            case WasmOpcode.I64Mul:
            case WasmOpcode.I64DivS:
            case WasmOpcode.I64DivU:
            case WasmOpcode.I64RemS:
            case WasmOpcode.I64RemU:
            case WasmOpcode.I64And:
            case WasmOpcode.I64Or:
            case WasmOpcode.I64Xor:
            case WasmOpcode.I64Shl:
            case WasmOpcode.I64ShrU:
            case WasmOpcode.I64ShrS:
            case WasmOpcode.I64Eqz:
            case WasmOpcode.I64Eq:
            case WasmOpcode.I64Ne:
            case WasmOpcode.I64LtS:
            case WasmOpcode.I64LtU:
            case WasmOpcode.I64GtS:
            case WasmOpcode.I64GtU:
            case WasmOpcode.I64LeS:
            case WasmOpcode.I64LeU:
            case WasmOpcode.I64GeS:
            case WasmOpcode.I64GeU:
            case WasmOpcode.F32Add:
            case WasmOpcode.F32Sub:
            case WasmOpcode.F32Mul:
            case WasmOpcode.F32Div:
            case WasmOpcode.F64Add:
            case WasmOpcode.F64Sub:
            case WasmOpcode.F64Mul:
            case WasmOpcode.F64Div:
            case WasmOpcode.F32Eq:
            case WasmOpcode.F32Ne:
            case WasmOpcode.F32Lt:
            case WasmOpcode.F32Gt:
            case WasmOpcode.F32Le:
            case WasmOpcode.F32Ge:
            case WasmOpcode.F32Abs:
            case WasmOpcode.F32Neg:
            case WasmOpcode.F64Eq:
            case WasmOpcode.F64Ne:
            case WasmOpcode.F64Lt:
            case WasmOpcode.F64Gt:
            case WasmOpcode.F64Le:
            case WasmOpcode.F64Ge:
            case WasmOpcode.F64Abs:
            case WasmOpcode.F64Neg:
            case WasmOpcode.I32WrapI64:
            case WasmOpcode.I32TruncF32S:
            case WasmOpcode.I32TruncF32U:
            case WasmOpcode.I32TruncF64S:
            case WasmOpcode.I32TruncF64U:
            case WasmOpcode.I64ExtendI32S:
            case WasmOpcode.I64ExtendI32U:
            case WasmOpcode.I64TruncF32S:
            case WasmOpcode.I64TruncF32U:
            case WasmOpcode.I64TruncF64S:
            case WasmOpcode.I64TruncF64U:
            case WasmOpcode.F32ConvertI32S:
            case WasmOpcode.F32ConvertI32U:
            case WasmOpcode.F32ConvertI64S:
            case WasmOpcode.F32ConvertI64U:
            case WasmOpcode.F64ConvertI32S:
            case WasmOpcode.F64ConvertI32U:
            case WasmOpcode.F64ConvertI64S:
            case WasmOpcode.F64ConvertI64U:
            case WasmOpcode.I32ReinterpretF32:
            case WasmOpcode.I64ReinterpretF64:
            case WasmOpcode.F32ReinterpretI32:
            case WasmOpcode.F64ReinterpretI64:
            case WasmOpcode.End:
            case WasmOpcode.Return:
            case WasmOpcode.Drop:
            case WasmOpcode.Select:
            case WasmOpcode.Unreachable:
            case WasmOpcode.Nop:
            case WasmOpcode.Else:
                return new WasmInstruction(opcode);
            default:
                throw new Exception($"Unsupported WASM opcode: 0x{opcode:X}");
        }
    }

    // Extended opcodes starting with 0xFC have a sub-opcode form
    private WasmInstruction ReadExtendedOpcode()
    {
        var subOpcode = ReadVarUInt32();

        switch (subOpcode)
        {
            case 8: // 0xFC 8:u32 𝑥:dataidx 0x00 => memory.init 𝑥
            {
                var x = (int)ReadVarUInt32();
                
                if (_stream.ReadByte() != 0)
                {
                    throw new Exception("Invalid WASM file, expected 0x00 at end of memory.init instruction");
                }
                
                return new WasmInstruction(WasmOpcode.MemoryInit, new WasmI32Value(x));
            }
            case 9: // 0xFC 9:u32 𝑥:dataidx => data.drop 𝑥
            {
                var x = (int)ReadVarUInt32();
                
                return new WasmInstruction(WasmOpcode.DataDrop, new WasmI32Value(x));
            }
            default:
                throw new NotImplementedException($"Bulk memory sub-instruction {subOpcode} not yet implemented");
        }
    }

    private WasmBlockType ReadBlockType()
    {
        var type = _stream.ReadByte();

        if (type == -1)
        {
            throw new Exception("Invalid WASM file.");
        }

        return type switch
        {
            0x40 => new WasmBlockType.EmptyBlockType(),
            0x7F => new WasmBlockType.ValueTypeBlockType(WasmNumberType.I32),
            0x7E => new WasmBlockType.ValueTypeBlockType(WasmNumberType.I64),
            0x7D => new WasmBlockType.ValueTypeBlockType(WasmNumberType.F32),
            0x7C => new WasmBlockType.ValueTypeBlockType(WasmNumberType.F64),
            _ => throw new Exception($"Unsupported WASM block type: 0x{type:X}")
        };
    }

    private WasmElementSection ReadElementSection()
    {
        /*
         Section:
         https://webassembly.github.io/spec/core/binary/modules.html#binary-elemsec

            ; section "Elem" (9)
            0000031: 09                                        ; section code
            0000032: 00                                        ; section size (guess)
            0000033: 01                                        ; num elem segments
        */
        var section = new WasmElementSection();

        var count = ReadVarUInt32();

        for (var i = 0; i < count; i++)
        {
            var element = ReadElement();
            section.Elements.Add(element);
        }
        
        return section;
    }

    private WasmElement ReadElement()
    {
        // https://webassembly.github.io/spec/core/syntax/modules.html#element-segments
        // https://webassembly.github.io/spec/core/binary/modules.html#element-section
        
        var format = _stream.ReadByte();
            
        Expression? offsetExpr;
        List<Expression>? init;
        WasmReferenceKind kind;
        WasmElementMode mode;
        int? tableIndex;

        switch (format)
        {
            // 0:u32 e:expr y*:vec(funcidx)
            // => { type funcref, init ((ref.func y) end)*, mode active { table 0, offset e } }
            case 0:
                kind = WasmReferenceKind.FuncRef;
                mode = WasmElementMode.Active;
                tableIndex = 0;
                offsetExpr = ReadExpression();
                var funcIndexes = ReadUInt32Vector();
                init = funcIndexes.Select(i => new Expression
                {
                    Instructions = new List<WasmInstruction> 
                    {
                        new(WasmOpcode.RefFunc, new WasmI32Value((int)i)),
                    }
                }).ToList();
                break;
            default: 
                throw new NotImplementedException($"Element segment format {format} not yet implemented");
        }
        
        return new WasmElement
        {
            Kind = kind,
            Init = init,
            Mode = mode,
            TableIndex = tableIndex,
            Offset = offsetExpr,
        };
    }

    private WasmExportSection ReadExportSection()
    {
        var section = new WasmExportSection();

        var count = ReadVarUInt32();

        for (var i = 0; i < count; i++)
        {
            var export = ReadExport();
            section.Exports.Add(export);
        }

        return section;
    }

    private WasmExport ReadExport()
    {
        var nameLength = ReadVarUInt32();
        var name = new byte[nameLength];

        if (_stream.Read(name, 0, (int)nameLength) != nameLength)
        {
            throw new Exception("Invalid WASM file.");
        }

        var kind = _stream.ReadByte();

        if (kind == -1)
        {
            throw new Exception("Invalid WASM file.");
        }

        var index = ReadVarUInt32();

        return new WasmExport
        {
            Name = Encoding.UTF8.GetString(name),
            Kind = (WasmExportKind)kind,
            Index = (int)index
        };
    }

    private WasmFunctionSection ReadFunctionSection()
    {
        var section = new WasmFunctionSection();

        var count = ReadVarUInt32();

        for (var i = 0; i < count; i++)
        {
            var typeIndex = ReadVarUInt32();
            
            var function = new WasmFunction
            {
                FunctionSignatureIndex = typeIndex
            };
            
            section.Functions.Add(function);
        }

        return section;
    }

    private WasmReferenceKind ReadReferenceType()
    {
        var refType = (WasmReferenceKind)_stream.ReadByte();

        if (!Enum.IsDefined(refType))
        {
            throw new Exception($"Invalid WASM Reference Type: {refType}");
        }

        return refType;
    }
    
    private WasmTableSection ReadTableSection()
    {
        var section = new WasmTableSection();

        var count = ReadVarUInt32();

        for (var i = 0; i < count; i++)
        {
            var refType = ReadReferenceType();

            if (refType != WasmReferenceKind.FuncRef)
            {
                throw new Exception($"Unsupported WASM Reference Type: {refType}");
            }
           
            var table = new WasmTable
            {
                TableReferenceKind = refType, 
                Limits = ReadLimits()
            };
            
            section.Tables.Add(table);
        }

        return section;
    }

    private WasmTypeSection ReadTypeSection()
    {
        var section = new WasmTypeSection();

        var count = ReadVarUInt32();

        for (var i = 0; i < count; i++)
        {
            var type = ReadType();
            section.Types.Add(type);
        }

        return section;
    }

    private WasmType ReadType()
    {
        var form = (WasmTypeKind)ReadVarUInt32();

        if (form != WasmTypeKind.Function)
        {
            throw new Exception("Unsupported WASM type.");
        }

        var paramCount = ReadVarUInt32();
        var paramTypes = new List<WasmValueType>((int)paramCount);

        for (var i = 0; i < paramCount; i++)
        {
            var paramType = ReadValueType();
            paramTypes.Add(paramType);
        }

        var returnCount = ReadVarUInt32();
        var returnTypes = new List<WasmValueType>((int)returnCount);

        if (returnCount > 1)
        {
            throw new Exception("Invalid WASM file.");
        }

        for (var i = 0; i < returnCount; i++)
        {
            var returnType = ReadValueType();
            returnTypes.Add(returnType);
        }

        return new WasmType
        {
            Kind = form,
            Parameters = paramTypes,
            Results = returnTypes
        };
    }

    private WasmValueType ReadValueType()
    {
        var type = _stream.ReadByte();

        if (type == -1)
        {
            throw new Exception("Invalid WASM file.");
        }

        return type switch
        {
            0x7F => WasmNumberType.I32,
            0x7E => WasmNumberType.I64,
            0x7D => WasmNumberType.F32,
            0x7C => WasmNumberType.F64,
            _ => throw new Exception("Unsupported WASM value type.")
        };
    }

    private uint ReadVarUInt32()
    {
        var result = 0u;
        var shift = 0;

        while (true)
        {
            var b = _stream.ReadByte();

            if (b == -1)
            {
                throw new Exception("Invalid WASM file.");
            }

            result |= (uint) (b & 0x7F) << shift;

            if ((b & 0x80) == 0)
            {
                break;
            }

            shift += 7;
        }

        return result;
    }
    
    private int ReadVarInt32()
    {
        var result = 0;
        var shift = 0;
        int b;
        
        do
        {
            b = _stream.ReadByte();

            if (b == -1)
            {
                throw new Exception("Invalid WASM file.");
            }

            result |= (b & 0x7F) << shift;
            shift += 7;
        } while ((b & 0x80) != 0);

        if (shift < 32 && (b & 0x40) != 0)
        {
            result |= ~0 << shift;
        }

        return result;
    }
    
    private ulong ReadVarUInt64()
    {
        var result = 0ul;
        var shift = 0;

        while (true)
        {
            var b = _stream.ReadByte();

            if (b == -1)
            {
                throw new Exception("Invalid WASM file.");
            }

            result |= (ulong) (b & 0x7F) << shift;

            if ((b & 0x80) == 0)
            {
                break;
            }

            shift += 7;
        }

        return result;
    }
    
    private long ReadVarInt64()
    {
        var result = 0L;
        var shift = 0;
        long b;
        
        do
        {
            b = _stream.ReadByte();

            if (b == -1)
            {
                throw new Exception("Invalid WASM file.");
            }

            result |= (b & 0x7F) << shift;
            shift += 7;
        } while ((b & 0x80) != 0);
    
        if (shift < 64 && (b & 0x40) != 0)
        {
            result |= ~0L << shift;
        }

        return result;
    }

    private float ReadVarFloat32()
    {
        var bytes = new byte[4];
        
        if (_stream.Read(bytes, 0, 4) != 4)
        {
            throw new Exception("Invalid WASM file.");
        }

        return BitConverter.ToSingle(bytes);
    }
    
    private double ReadVarFloat64()
    {
        var bytes = new byte[8];
        
        if (_stream.Read(bytes, 0, 8) != 8)
        {
            throw new Exception("Invalid WASM file.");
        }

        return BitConverter.ToDouble(bytes);
    }

    private void ValidateWasmHeader()
    {
        var header = new byte[8];

        if (_stream.Read(header, 0, 8) != 8)
        {
            throw new Exception("Invalid WASM file.");
        }

        if (header[0] != 0x00 || header[1] != 0x61 || header[2] != 0x73 || header[3] != 0x6D)
        {
            throw new Exception("Invalid WASM file.");
        }

        var version = BitConverter.ToInt32(header[4..8]);

        if (version != 1)
        {
            throw new Exception($"Unsupported WASM version: {version}");
        }
    }
}