using System.Buffers.Binary;
using System.Diagnostics;

namespace MobiDict.Reader;

public class InflectionData
{
    private List<byte[]> data;

    private List<uint> Starts = [];
    private List<uint> Counts = [];

    public InflectionData(List<byte[]> data)
    {
        this.data = data;
        Populate();
    }

    private void Populate()
    {
        foreach (var item in data)
        {
            var start = BinaryPrimitives.ReadUInt32BigEndian(item.AsSpan(0x14));
            var count = BinaryPrimitives.ReadUInt32BigEndian(item.AsSpan(0x18));
            Starts.Add(start);
            Counts.Add(count);
        }
    }

    public (uint RValue, uint Start, uint Count, byte[] InflData) Lookup(uint lookupValue)
    {
        int i = 0;
        var rValue = lookupValue;
        while (rValue >= Counts[i])
        {
            rValue -= Counts[i];
            i++;
            if (i == Counts.Count)
            {
                Debug.WriteLine("Error: Problem with multiple inflections data sections");
                return (lookupValue, Starts[0], Counts[0], data[0]);
            }
        }
        return (rValue, Starts[i], Counts[i], data[i]);
    }

    public (uint OffSet, int NextOffSet, byte[] Data) OffSets(uint value)
    {
        var (rValue, start, count, data) = Lookup(value);
        var offset = BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan((int)(start + 4 + (2 * rValue))));
        int nextOffSet = 0;
        if (rValue + 1 < count)
            nextOffSet = BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan((int)(start + 4 + (2 * (rValue + 1)))));
        else
            nextOffSet = -1;
        return (offset, nextOffSet, data);
    }
}
