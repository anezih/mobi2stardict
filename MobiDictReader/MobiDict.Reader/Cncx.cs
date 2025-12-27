namespace MobiDict.Reader;

public class Cncx
{
    private readonly byte[] data;
    private readonly int baseOffset;

    public Cncx(byte[] cncXSection)
    {
        data = cncXSection ?? throw new ArgumentNullException(nameof(cncXSection));
        baseOffset = (data.Length >= 4 && data.AsSpan(0, 4).SequenceEqual("CNCX"u8))
            ? 8
            : 0;
    }

    public bool TrySlice(int offset, int len, out ReadOnlySpan<byte> slice)
    {
        int start = baseOffset + offset;
        if (start >= 0 && len >= 0 && start + len <= data.Length)
        {
            slice = data.AsSpan(start, len);
            return true;
        }

        start = offset;
        if (start >= 0 && len >= 0 && start + len <= data.Length)
        {
            slice = data.AsSpan(start, len);
            return true;
        }

        slice = default;
        return false;
    }
}
