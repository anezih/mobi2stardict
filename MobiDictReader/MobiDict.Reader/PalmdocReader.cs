namespace MobiDict.Reader;

public class PalmdocReader : IUnpack
{
    public int Unpack(ReadOnlySpan<byte> data, Span<byte> destination)
    {
        int p = 0;
        int written = 0;

        while (p < data.Length)
        {
            int c = data[p++];
            if (c >= 1 && c <= 8)
            {
                for (int k = 0; k < c; k++)
                {
                    destination[written++] = (data[p + k]);
                }
                p += c;
            }
            else if (c < 128)
            {
                destination[written++] = (byte)c;
            }
            else if (c >= 192)
            {
                destination[written++] = (byte)' ';
                destination[written++] = (byte)(c ^ 128);
            }
            else
            {
                if (p >= data.Length)
                    break;
                c = (c << 8) | data[p++];
                int m = (c >> 3) & 0x07FF;
                int n = (c & 7) + 3;

                for (int k = 0; k < n; k++)
                {
                    destination[written] = destination[written - m];
                    written++;
                }
            }
        }
        return written;
    }
}