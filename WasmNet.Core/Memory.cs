using System.Runtime.InteropServices;

namespace WasmNet.Core;

public class Memory(int minPages, int maxPages)
{
    private const int PageSize = 65536;
    
    private byte[] _bytes = new byte[minPages * PageSize];

    public Memory(int minPages)
        : this(minPages, minPages)
    {
    }

    public byte this[int index]
    {
        get => _bytes[index];
        set => _bytes[index] = value;
    }
    
    public int Size => _bytes.Length;
    
    public int MinPages => minPages;

    public int MaxPages => maxPages;
    
    public void Grow(int pages)
    {
        var newBytes = new byte[_bytes.Length + pages * PageSize];
        _bytes.CopyTo(newBytes, 0);
        _bytes = newBytes;
    }

    public void Write(int offset, byte[] bytes)
    {
        bytes.CopyTo(_bytes, offset);
    }

    public byte[] Read(int offset, int count)
    {
        var bytes = new byte[count];
        Array.Copy(_bytes, offset, bytes, 0, count);
        return bytes;
    }

    public void Write(int destOffset, byte[] bytes, int srcOffset, int count) 
        => Array.Copy(bytes, srcOffset, _bytes, destOffset, count);

    public T ReadStruct<T>(int offset)
        where T : struct
    {
        var size = Marshal.SizeOf<T>();
        var bytes = Read(offset, size);
        var handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
        var result = Marshal.PtrToStructure<T>(handle.AddrOfPinnedObject());
        handle.Free();
        return result;
    }
}