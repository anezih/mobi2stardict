using System.Buffers.Binary;
using System.Text;

namespace MobiDict.Reader;

public class Sectionizer
{
    private Stream mobiStream;
    private byte[] PalmHeader;
    private List<uint> SectionOffsets;

    public Sectionizer(Stream mobiStream)
    {
        this.mobiStream = mobiStream;
        this.mobiStream.Seek(0, SeekOrigin.Begin);
        this.PalmHeader = _PalmHeader();
        this.SectionOffsets = _SectionOffsets();
    }

    private byte[] _PalmHeader()
    {
        byte[] buf = new byte[78];
        mobiStream.ReadExactly(buf, 0, 78);
        return buf;
    }

    public string Ident => Encoding.Latin1.GetString(PalmHeader[0x3C..(0x3C + 8)]);

    public byte[] PalmName => PalmHeader[..32];

    private uint FileLength => (uint)mobiStream.Length;

    internal int NumberOfSections => BinaryPrimitives.ReadUInt16BigEndian(PalmHeader.AsSpan(76));

    private List<uint> _SectionOffsets()
    {
        var num = NumberOfSections;
        List<uint> offsets = new List<uint>(num + 1);
        mobiStream.Seek(78, SeekOrigin.Begin);
        Span<byte> buf = stackalloc byte[8];
        for (int i = 0; i < num; i++)
        {
            mobiStream.ReadExactly(buf);
            var offset = BinaryPrimitives.ReadUInt32BigEndian(buf);
            offsets.Add(offset);
        }
        offsets.Add(FileLength);
        return offsets;
    }

    public byte[] LoadSection(int section)
    {
        var first = SectionOffsets[section];
        var second = SectionOffsets[section + 1];
        var length = second - first;
        byte[] buf = new byte[length];
        mobiStream.Seek(first, SeekOrigin.Begin);
        mobiStream.ReadExactly(buf, 0, (int)length);
        return buf;
    }
}
