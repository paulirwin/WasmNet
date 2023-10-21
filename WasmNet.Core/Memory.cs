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

    public byte[] Read(int offset, int size)
    {
        var bytes = new byte[size];
        Array.Copy(_bytes, offset, bytes, 0, size);
        return bytes;
    }
}