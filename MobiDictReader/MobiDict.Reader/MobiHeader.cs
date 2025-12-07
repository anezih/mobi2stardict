using System.Buffers.Binary;
using System.Text;

namespace MobiDict.Reader;

public class MobiHeader
{
    private Sectionizer sectionizer;
    private int sectionNumber;
    private byte[] header;
    private IUnpack? reader;
    private bool hasExth;
    private int exthOffset;
    private int exthLength;
    private uint metaOrthIndex = 0xFFFFFFFF;
    private uint metaInflIndex = 0xFFFFFFFF;
    private Dictionary<string, List<string>> metadata = new();

    public MobiHeader(Sectionizer sectionizer, int sectionNumber)
    {
        this.sectionizer = sectionizer;
        this.sectionNumber = sectionNumber;
        this.header = sectionizer.LoadSection(sectionNumber);
        if (!IsEncrypted)
        {
            GetReader();
            GetExthInfo();
            GetDictMetaIndexes();
            ParseMetadata();
        }
    }

    public string FileTitle => Encoding.Latin1.GetString(sectionizer.PalmName).Trim('\0');
    public string Title => GetTitle();
    public string DictInLanguage => GetDictInLanguage();
    public string DictOutLanguage => GetDictOutLanguage();
    public List<string> Creator => metadata.GetValueOrDefault("Creator", []);
    public List<string> Description => metadata.GetValueOrDefault("Description", []);
    public List<string> Published => metadata.GetValueOrDefault("Published", []);
    public List<string> Publisher => metadata.GetValueOrDefault("Publisher", []);

    public uint CodePage => BinaryPrimitives.ReadUInt32BigEndian(header.AsSpan(28));

    public Encoding Codec => CodePage == 65001 ? Encoding.UTF8 : Encoding.GetEncoding(1252);

    public uint Version => BinaryPrimitives.ReadUInt32BigEndian(header.AsSpan(36));

    public int Compression => BinaryPrimitives.ReadUInt16BigEndian(header);

    private int Records => BinaryPrimitives.ReadUInt16BigEndian(header.AsSpan(0x8));

    private int Length => (int)BinaryPrimitives.ReadUInt32BigEndian(header.AsSpan(20));

    public bool IsDictionary => metaOrthIndex != 0xFFFFFFFF;
    public bool IsEncrypted => BinaryPrimitives.ReadUInt16BigEndian(header.AsSpan(0xC)) != 0;

    public uint MetaOrthIndex => metaOrthIndex;
    public uint MetaInflIndex => metaInflIndex;

    private string GetTitle()
    {
        var titleOffset = BinaryPrimitives.ReadUInt32BigEndian(header.AsSpan(84));
        var titleLen = BinaryPrimitives.ReadUInt32BigEndian(header.AsSpan(88));
        var title = Codec.GetString(header[(int)titleOffset..(int)(titleOffset + titleLen)]);
        return title;
    }

    private string GetDictInLanguage()
    {
        if (!IsDictionary) return string.Empty;
        var langCode = BinaryPrimitives.ReadUInt32BigEndian(header.AsSpan(0x60));
        var langId = langCode & 0xFF;
        var subLangId = (langCode >> 10) & 0xFF;
        if (langId != 0)
        {
            return Utils.GetLangCode(langId, subLangId);
        }
        return string.Empty;
    }

    private string GetDictOutLanguage()
    {
        if (!IsDictionary) return string.Empty;
        var langCode = BinaryPrimitives.ReadUInt32BigEndian(header.AsSpan(0x64));
        var langId = langCode & 0xFF;
        var subLangId = (langCode >> 10) & 0xFF;
        if (langId != 0)
        {
            return Utils.GetLangCode(langId, subLangId);
        }
        return string.Empty;
    }

    private void GetExthInfo()
    {
        var exthFlag = BinaryPrimitives.ReadUInt32BigEndian(header.AsSpan(0x80));
        hasExth = (exthFlag & 0x40) > 0;
        exthOffset = Length + 16;
        if (hasExth)
        {
            var len = BinaryPrimitives.ReadUInt32BigEndian(header.AsSpan(exthOffset+4));
            exthLength = (int)(((len + 3) >> 2) << 2);
        }
    }

    private ReadOnlySpan<byte> Exth => hasExth ? header.AsSpan(exthOffset..(exthOffset + exthLength)) : [];

    private void GetDictMetaIndexes()
    {
        var valOrth = BinaryPrimitives.ReadUInt32BigEndian(header.AsSpan(0x28));
        if (valOrth != 0xFFFFFFFF)
            metaOrthIndex = valOrth + (uint)sectionNumber;

        var valInfl = BinaryPrimitives.ReadUInt32BigEndian(header.AsSpan(0x2C));
        if (valInfl != 0xFFFFFFFF)
            metaInflIndex = valInfl + (uint)sectionNumber;
    }

    private void ParseMetadata()
    {
        if (!hasExth) return;
        var numOfItems = BinaryPrimitives.ReadUInt32BigEndian(Exth[8..]);
        var exthHeader = Exth[12..];
        int pos = 0;
        for (int i = 0; i < numOfItems; i++)
        {
            var id = BinaryPrimitives.ReadUInt32BigEndian(exthHeader[pos..]);
            var size = BinaryPrimitives.ReadUInt32BigEndian(exthHeader[(pos+4)..]);
            var content = exthHeader[(pos + 8)..(pos + (int)size)];
            if (Utils.MetadataStrings.ContainsKey(id))
            {
                Utils.MetadataStrings.TryGetValue(id, out var name);
                if (!string.IsNullOrEmpty(name))
                {
                    if (metadata.ContainsKey(name))
                    {
                        metadata[name].Add(Codec.GetString(content));
                    }
                    else
                    {
                        metadata[name] = [Codec.GetString(content)];
                    }
                }

            }
            pos += (int)size;
        }
    }

    private void GetReader()
    {
        if (Compression == 17480)
        {
            var huffReader = new HuffCdicReader();
            var huffOffset = (int)BinaryPrimitives.ReadUInt32BigEndian(header.AsSpan(0x70));
            var huffNum = BinaryPrimitives.ReadUInt32BigEndian(header.AsSpan(0x74));
            huffOffset += sectionNumber;
            huffReader.LoadHuff(sectionizer.LoadSection(huffOffset));
            for (int i = 1; i < huffNum; i++)
            {
                huffReader.LoadCdic(sectionizer.LoadSection(huffOffset + i));
            }
            reader = huffReader;
        }
        else if (Compression == 2)
        {
            reader = new PalmdocReader();
        }
        else if (Compression == 1)
        {
            reader = new UncompressedReader();
        }
        else
        {
            throw new ArgumentException($"Unknown compression: {Compression}");
        }
    }

    public byte[] GetRawML()
    {
        int trailers = 0;
        bool multiByte = false;

        static int TrailingDataEntrySize(ReadOnlySpan<byte> data)
        {
            int num = 0;
            foreach (var item in data[^4..])
            {
                if (((int)item & 0x80) != 0)
                    num = 0;
                num = (num << 7) | (item & 0x7F);
            }
            return num;
        }

        static ReadOnlySpan<byte> TrimTrailingDataEntries(ReadOnlySpan<byte> data, int trailers, bool multiByte)
        {
            for (int i = 0; i < trailers; i++)
            {
                var del = TrailingDataEntrySize(data);
                data = data[..^del];
            }
            if (multiByte)
            {
                var del = (data[^1] & 3) + 1;
                data = data[..^del];
            }
            return data;
        }

        if (sectionizer.Ident == "BOOKMOBI")
        {
            var mobiLength = BinaryPrimitives.ReadUInt32BigEndian(header.AsSpan(0x14));
            var mobiVersion = BinaryPrimitives.ReadUInt32BigEndian(header.AsSpan(0x68));
            if (mobiLength >= 0xE4 & mobiVersion >= 5)
            {
                var flags = BinaryPrimitives.ReadUInt16BigEndian(header.AsSpan(0xF2));
                multiByte = (flags & 1) != 0;
                while (flags > 1)
                {
                    if ((flags & 2) != 0) trailers++;
                    flags = (ushort)(flags >> 1);
                }
            }
        }
        List<byte> dataList = new();
        for (int i = 1; i < Records+1; i++)
        {
            var data = TrimTrailingDataEntries(sectionizer.LoadSection(sectionNumber + i), trailers, multiByte);
            var unpacked = reader?.Unpack(data);
            dataList.AddRange(unpacked!);
        }
        return dataList.ToArray();
    }
}
