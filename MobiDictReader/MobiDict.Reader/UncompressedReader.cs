namespace MobiDict.Reader;

public class UncompressedReader : IUnpack
{
    public int Unpack(ReadOnlySpan<byte> data, Span<byte> destination)
    {
        data.CopyTo(destination);
        return data.Length;
    }
}
