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
                case WasmExportSection exportSection:
                    module.ExportSection = exportSection;
                    break;
                case WasmCodeSection codeSection:
                    module.CodeSection = codeSection;
                    break;
            }
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
            3 => ReadFunctionSection(),
            7 => ReadExportSection(),
            10 => ReadCodeSection(),
            _ => throw new Exception($"Unsupported WASM section: 0x{id:X}")
        };
        
        var sectionEnd = _stream.Position;

        if (length == 0)
        {
            // read FIXUP
            length = ReadVarUInt32();
        }
        
        if (sectionEnd - sectionStart != length)
        {
            throw new Exception("Invalid WASM section length.");
        }

        return section;
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
        
        //var locals = new List<WasmLocal>();
        var localCount = ReadVarUInt32();

        // for (var i = 0; i < count; i++)
        // {
        //     var localCount = ReadVarUInt32();
        //     var localType = ReadValueType();
        //
        //     for (var j = 0; j < localCount; j++)
        //     {
        //         locals.Add(new WasmLocal
        //         {
        //             Count = localCount,
        //             Type = localType
        //         });
        //     }
        // }
        
        var body = new List<WasmInstruction>();
        WasmInstruction instruction;
        
        do
        {
            instruction = ReadInstruction();
            body.Add(instruction);
        }
        while (instruction.Opcode != WasmOpcode.End);

        var codeEnd = _stream.Position;

        if (size == 0)
        {
            // read FIXUP
            size = ReadVarUInt32();
        }

        if (codeEnd - codeStart != size)
        {
            throw new Exception("Invalid WASM code length.");
        }

        return new WasmCode
        {
            LocalDeclarationCount = localCount,
            Body = body,
        };
    }

    private WasmInstruction ReadInstruction()
    {
        var opcode = (WasmOpcode)_stream.ReadByte();
        
        switch (opcode)
        {
            case (WasmOpcode)(-1):
                throw new Exception("Invalid WASM file.");
            // TODO: handle signed/unsigned
            case WasmOpcode.I32Const:
            {
                var arg = (int)ReadVarUInt32();
                return new WasmInstruction(WasmOpcode.I32Const, new WasmNumberValue<int>(WasmNumberTypeKind.I32, arg));
            }
            case WasmOpcode.End:
                return new WasmInstruction(WasmOpcode.End);
            case WasmOpcode.LocalGet:
            {
                var arg = (int)ReadVarUInt32();
                return new WasmInstruction(WasmOpcode.LocalGet, new WasmNumberValue<int>(WasmNumberTypeKind.I32, arg));
            }
            case WasmOpcode.I32Add:
                return new WasmInstruction(WasmOpcode.I32Add);
            case WasmOpcode.I64Add:
                return new WasmInstruction(WasmOpcode.I64Add);
            default:
                throw new Exception($"Unsupported WASM opcode: 0x{opcode:X}");
        }
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
            Index = index
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
            0x7F => new WasmNumberType(WasmNumberTypeKind.I32),
            0x7E => new WasmNumberType(WasmNumberTypeKind.I64),
            0x7D => new WasmNumberType(WasmNumberTypeKind.F32),
            0x7C => new WasmNumberType(WasmNumberTypeKind.F64),
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