using System.Buffers.Binary;

namespace MobiDict.Reader;

public class HuffCdicReader : IUnpack
{
    private List<(uint CodeLen, uint Term, uint MaxCode)> dict1 = new(256);
    private List<uint> mincode = new(33);
    private List<uint> maxcode = new(33);
    private List<(byte[] Slice, int Blen)> Dictionary = [];

    private (uint CodeLen, uint Term, uint MaxCode) Dict1Unpack(uint v)
    {
        var codelen = v & 0x1f;
        var term = v & 0x80;
        var maxcode = v >> 8;

        maxcode = ((maxcode + 1) << (int)(32 - codelen)) - 1;
        return (codelen, term, maxcode);
    }

    public byte[] Unpack(ReadOnlySpan<byte> data)
    {
        var bitsleft = data.Length * 8;
        ReadOnlySpan<byte> data2 = [..data, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0];
        int pos = 0;
        var x = BinaryPrimitives.ReadUInt64BigEndian(data2[pos..]);
        int n = 32;

        List<byte> s = [];
        while (true)
        {
            if (n <= 0)
            {
                pos += 4;
                x = BinaryPrimitives.ReadUInt64BigEndian(data2[pos..]);
                n += 32;
            }
            var code = (uint)(x >> n);
            var (codelen, term, _maxcode) = dict1[(int)(code >> 24)];
            if (term == 0)
            {
                while (code < mincode[(int)codelen])
                {
                    codelen += 1;
                }
                _maxcode = maxcode[(int)codelen];
            }
            n -= (int)codelen;
            bitsleft -= (int)codelen;
            if (bitsleft < 0) 
                break;
            var r = (_maxcode - code) >> (int)(32 - codelen);
            var (slice, flag) = Dictionary[(int)r];
            if (flag == 0)
            {
                Dictionary[(int)r] = ([], 0);
                slice = Unpack(slice);
                Dictionary[(int)r] = (slice, 1);
            }
            s.AddRange(slice);
        }
        return s.ToArray();
    }

    public void LoadHuff(byte[] data)
    {
        if (!data[..8].SequenceEqual("HUFF\x00\x00\x00\x18"u8))
            throw new InvalidDataException();
        var offset1 = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(8));
        var offset2 = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(12));

        ReadOnlySpan<byte> huffSlice = data.AsSpan((int)offset1);
        for (int i = 0, byteIndex = 0; i < 256; i++, byteIndex += 4)
        {
            var unpacked = Dict1Unpack(BinaryPrimitives.ReadUInt32BigEndian(huffSlice.Slice(byteIndex, 4)));
            dict1.Insert(i, unpacked);
        }

        ReadOnlySpan<byte> huffSlice2 = data.AsSpan((int)offset2);
        List<uint> dict2 = new(64);
        for (int i = 0, byteIndex = 0; i < 64; i++, byteIndex += 4)
        {
            var el = BinaryPrimitives.ReadUInt32BigEndian(huffSlice2.Slice(byteIndex, 4));
            dict2.Insert(i, el);
        }

        mincode.Add(0);
        maxcode.Add(0);

        for (int i = 0; i < 32; i++)
        {
            int dict2Index = 2 * i;
            int codelen = i + 1;

            uint currentMinCode = dict2[dict2Index];
            int shiftAmount = 32 - codelen;

            mincode.Add(currentMinCode << shiftAmount);
        }

        for (int i = 0; i < 32; i++)
        {
            int dict2Index = 2 * i + 1;
            int codelen = i + 1;

            uint currentMaxCode = dict2[dict2Index];
            int shiftAmount = 32 - codelen;

            maxcode.Add(((currentMaxCode + 1) << shiftAmount) - 1);
        }
    }

    public void LoadCdic(byte[] data)
    {
        if (!data[..8].SequenceEqual("CDIC\x00\x00\x00\x10"u8))
            throw new InvalidDataException();
        var phrases = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(8));
        var bits = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(12));
        var n = Math.Min(1 << (int)bits, phrases - Dictionary.Count);

        ReadOnlySpan<byte> temp = data.AsSpan(16);
        for (int i = 0, byteIndex = 0; i < n; i++, byteIndex += 2)
        {
            var offset = BinaryPrimitives.ReadUInt16BigEndian(temp.Slice(byteIndex, 4));
            var blen = BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan(16+offset));
            var slice = data[(18 + offset)..(18 + offset + (blen & 0x7FFF))];
            Dictionary.Add((slice, blen & 0x8000));
        }
    }
}
