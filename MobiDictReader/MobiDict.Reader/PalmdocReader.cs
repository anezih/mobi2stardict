namespace MobiDict.Reader;

public class PalmdocReader : IUnpack
{
    public byte[] Unpack(ReadOnlySpan<byte> data)
    {
        List<byte> output = new();
        int p = 0;

        while (p < data.Length)
        {
            int c = data[p++];
            if (c >= 1 && c <= 8)
            {
                for (int k = 0; k < c; k++)
                {
                    output.Add(data[p + k]);
                }
                p += c;
            }
            else if (c < 128)
            {
                output.Add((byte)c);
            }
            else if (c >= 192)
            {
                output.AddRange(" "u8);
                output.Add((byte)(c ^ 128));
            }
            else
            {
                if (p >= data.Length)
                    break;
                c = (c << 8) | data[p++];
                int m = (c >> 3) & 0x07FF;
                int n = (c & 7) + 3;

                if (m > n)
                {
                    int start = output.Count - m;
                    for (int k = 0; k < n; k++)
                    {
                        output.Add(output[start + k]);
                    }
                }
                else
                {
                    for (int _ = 0; _ < n; _++)
                    {
                        if (m == 1)
                        {
                            byte b = output[output.Count - 1];
                            output.Add(b);
                        }
                        else
                        {
                            byte b = output[output.Count - m];
                            output.Add(b);
                        }
                    }
                }
            }
        }
        return output.ToArray();
    }
}