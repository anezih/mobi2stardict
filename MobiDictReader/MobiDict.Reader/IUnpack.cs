namespace MobiDict.Reader;

public interface IUnpack
{
    byte[] Unpack(ReadOnlySpan<byte> data);
}
