namespace MobiDict.Reader;

public class UncompressedReader : IUnpack
{
    public byte[] Unpack(ReadOnlySpan<byte> data) => data.ToArray();
}
