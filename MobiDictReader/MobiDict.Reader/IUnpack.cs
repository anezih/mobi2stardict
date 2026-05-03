namespace MobiDict.Reader;

public interface IUnpack
{
    int Unpack(ReadOnlySpan<byte> data, Span<byte> destination);
}
