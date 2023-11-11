using System.Text;

namespace WasmNet.Core.Wasi;

public static partial class Preview1
{
    public static int FdWrite(WasmRuntime runtime, int fd, int iovs, int iovsLen, int nWritten)
    {
        // HACK.PI: this will obviously not work for anything other than stdout
        if (fd != 1)
        {
            throw new NotImplementedException("File descriptor other than stdout is not yet supported");
        }

        var memory = runtime.Store.Memory[0];
        int written = 0;

        for (int i = 0; i < iovsLen; i++)
        {
            var iov = memory.ReadStruct<WasiIovec>(iovs + i * 8);
            var ptr = iov.buf;
            var len = iov.buf_len;

            if (len > 0)
            {
                var bytes = memory.Read(ptr, len);

                var zeroIndex = IndexOfZero(bytes);

                if (zeroIndex < 0)
                    zeroIndex = bytes.Length;

                var str = Encoding.UTF8.GetString(bytes[..zeroIndex]);

                Console.Write(str);
                written += len;
            }
        }

        memory.Write(nWritten, BitConverter.GetBytes(written));

        return written;
    }

    private static int IndexOfZero(Span<byte> bytes)
    {
        for (int i = 0; i < bytes.Length; i++)
        {
            if (bytes[i] == 0)
            {
                return i;
            }
        }

        return -1;
    }
}