using System.Buffers.Binary;
using System.Diagnostics;
using System.Numerics;

namespace MobiDict.Reader;

public class DictionaryReader
{
    private MobiHeader mobiHeader;
    private Sectionizer sectionizer;

    private readonly List<string> words = [
        "len",
        "nul1",
        "type",
        "gen",
        "start",
        "count",
        "code",
        "lng",
        "total",
        "ordt",
        "ligt",
        "nligt",
        "nctoc",
    ];

    // If the hasEntryLength is false we will calculate entry length by substracting start pos of n from n+1'th entry.
    private bool hasEntryLength = false;
    private List<(int StartPos, int EndPos, string Headword, List<string> Inflections)> posMap = [];

    public DictionaryReader(MobiHeader mobiHeader, Sectionizer sectionizer)
    {
        this.mobiHeader = mobiHeader;
        this.sectionizer = sectionizer;
    }

    private (Dictionary<string,uint> Header, List<byte> Ordt1, List<uint> Ordt2) ParseHeader(ReadOnlySpan<byte> data)
    {
        if (!data[..4].SequenceEqual("INDX"u8))
            throw new InvalidDataException("Index section is not \"INDX\"");
        var num = words.Count;

        Dictionary<string, uint> Header = new();
        List<byte> Ordt1 = [];
        List<uint> Ordt2 = [];

        ReadOnlySpan<byte> dataSlice = data[4..(4 * (num + 1))];
        List<uint> values = [];
        for (int i = 0, byteIndex = 0; i < num; i++, byteIndex += 4)
        {
            values.Add(BinaryPrimitives.ReadUInt32BigEndian(dataSlice.Slice(byteIndex, 4)));
        }

        for (int i = 0; i < num; i++)
        {
            Header[words[i]] = values[i];
        }
        
        var otype = BinaryPrimitives.ReadUInt32BigEndian(data[(0xA4 + 0)..]);
        var oentries = BinaryPrimitives.ReadUInt32BigEndian(data[(0xA4 + 4)..]);
        var op1 = BinaryPrimitives.ReadUInt32BigEndian(data[(0xA4 + 8)..]);
        var op2 = BinaryPrimitives.ReadUInt32BigEndian(data[(0xA4 + 12)..]);
        var otagx = BinaryPrimitives.ReadUInt32BigEndian(data[(0xA4 + 16)..]);

        Header["otype"] = otype;
        Header["oentries"] = oentries;

        if (Header["code"] == 0xFDEA | oentries > 0)
        {
            if (!data[(int)op1..((int)op1 + 4)].SequenceEqual("ORDT"u8) || !data[(int)op2..((int)op2 + 4)].SequenceEqual("ORDT"u8))
                throw new InvalidDataException("Ordt section is not \"ORDT\"");

            Ordt1.AddRange(data[((int)op1 + 4)..((int)op1 + 4 + (int)oentries)].ToArray());

            ReadOnlySpan<byte> ordt2Slice = data[((int)op2 + 4)..];
            for (int i = 0, byteIndex = 0; i < oentries; i++, byteIndex += 2)
            {
                Ordt2.Add(BinaryPrimitives.ReadUInt16BigEndian(ordt2Slice.Slice(byteIndex, 2)));
            }
        }
        return (Header, Ordt1, Ordt2);
    }

    private (int ControlByteCount, List<int[]> Tags) ReadTagSection(int startPos, byte[] data)
    {
        int controlByteCount = 0;
        List<int[]> tags = [];

        if (data[startPos..(startPos + 4)].SequenceEqual("TAGX"u8))
        {
            var firstEntryOffset = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(startPos + 0x04));
            controlByteCount = (int)BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(startPos + 0x08));

            for (int i = 12; i < firstEntryOffset; i += 4)
            {
                var pos = startPos + i;
                int[] _tags = [data[pos], data[(pos + 1)], data[(pos + 2)], data[(pos + 3)]];
                tags.Add(_tags);
            }
        }

        return (controlByteCount, tags);
    }

    private bool HasTag(List<int[]> tagTable, int tag)
    {
        foreach (var currentTag in tagTable)
        {
            if (tag == currentTag[0])
                return true;
        }
        return false;
    }

    private (int Consumed, int Value) GetVariableWidthValue(byte[] data, int offset)
    {
        int value = 0;
        int consumed = 0;
        bool finished = false;
        while (!finished)
        {
            var v = data[(offset + consumed)];
            consumed++;
            if ((v & 0x80) != 0) finished = true;
            value = (int)((uint)(value << 7) | (uint)(v & 0x7F));
        }
        return (consumed, value);
    }

    private Dictionary<int,List<int>> GetTagMap(int controlByteCount, List<int[]> tagTable, byte[] entryData, int startPos, int endPos)
    {
        List<(int Tag, int ValueCount, int ValueBytes, int ValuesPerEntry)> tags = [];
        Dictionary<int,List<int>> tagMap = new();
        var controlByteIndex = 0;
        var dataStart = startPos + controlByteCount;

        foreach (var item in tagTable)
        {
            var tag = item[0];
            var valuesPerEntry = item[1];
            var mask = item[2];
            var endFlag = item[3];

            if (endFlag == 0x01)
            {
                controlByteIndex++;
                continue;
            }

            var value = entryData[(startPos + controlByteIndex)] & mask;
            if (value != 0)
            {
                if (value == mask)
                {
                    if (BitOperations.PopCount((uint)mask) > 1)
                    {
                        var varWidthVal = GetVariableWidthValue(entryData, dataStart);
                        dataStart += varWidthVal.Consumed;
                        value = varWidthVal.Value;
                        tags.Add((tag, -1, value, valuesPerEntry));
                    }
                    else
                    {
                        tags.Add((tag, 1, -1, valuesPerEntry));
                    }
                }
                else
                {
                    while ((mask & 0x01) == 0)
                    {
                        mask = mask >> 1;
                        value = value >> 1;
                    }
                    tags.Add((tag, value, -1, valuesPerEntry));
                }
            }
        }
        foreach (var (tag, valueCount, valueBytes, valuesPerEntry) in tags)
        {
            List<int> values = [];
            if (valueCount != -1)
            {
                for (int i = 0; i < valueCount; i++)
                {
                    for (int j = 0; j < valuesPerEntry; j++)
                    {
                        var (consumed, data) = GetVariableWidthValue(entryData, dataStart);
                        dataStart += consumed;
                        values.Add(data);
                    }
                }
            }
            else
            {
                int totalConsumed = 0;
                while (totalConsumed < valueBytes)
                {
                    var (consumed, data) = GetVariableWidthValue(entryData, dataStart);
                    dataStart += consumed;
                    totalConsumed += consumed;
                    values.Add(data);
                }
                if (totalConsumed != valueBytes)
                    Debug.WriteLine($"Error: Should have consumed {valueBytes} bytes, but consumed {totalConsumed}");
            }
            tagMap[tag] = values;
        }
        return tagMap;
    }

    private string? ApplyInflectionRule(byte[] mainEntry, byte[] inflectionRuleData, int start, int end)
    {
        int mode = -1;
        int pos = mainEntry.Length;
        int[] modeControl1 = [0x02, 0x03];
        int[] modeControl2 = [0x01, 0x04];
        List<byte> mainEntryList = new(mainEntry);
        for (int charOffSet = start; charOffSet < end; charOffSet++)
        {
            var _char = inflectionRuleData[charOffSet];
            if (_char >= 0x0A && _char <= 0x13)
            {
                var offset = _char - 0x0A;
                if (!modeControl1.Contains(mode))
                {
                    mode = 0x02;
                    pos = mainEntryList.Count;
                }
                pos -= offset;
            }
            else if (_char > 0x13)
            {
                if (mode == -1 | pos == -1)
                {
                    Debug.WriteLine($"Error: Unexpected first byte {_char} of inflection rule");
                    return null;
                }
                else
                {
                    if (mode == 0x01)
                    {
                        mainEntryList.Insert(pos, _char);
                        pos++;
                    }
                    else if (mode == 0x02)
                    {
                        mainEntryList.Insert(pos, _char);
                    }
                    else if (mode == 0x03)
                    {
                        pos--;
                        var deleted = mainEntryList[pos];
                        mainEntryList.RemoveAt(pos);
                        if (deleted != _char)
                        {
                            Debug.WriteLine("Error: Delete operation of inflection rule failed");
                            return null;
                        }
                    }
                    else if (mode == 0x04)
                    {
                        var deleted = mainEntryList[pos];
                        mainEntryList.RemoveAt(pos);
                        if (deleted != _char)
                        {
                            Debug.WriteLine("Error: Delete operation of inflection rule failed");
                            return null;
                        }
                    }
                    else
                    {
                        Debug.WriteLine($"Error: Inflection rule mode {mode:X2} is not implemented");
                        return null;
                    }
                }
            }
            else if (_char == 0x01)
            {
                if (!modeControl2.Contains(mode))
                    pos = 0;
                mode = _char;
            }
            else if (_char == 0x02)
            {
                if (!modeControl1.Contains(mode))
                    pos = mainEntryList.Count;
                mode = _char;
            }
            else if (_char == 0x03)
            {
                if (!modeControl1.Contains(mode))
                    pos = mainEntryList.Count;
                mode = _char;
            }
            else if (_char == 0x04)
            {
                if (!modeControl2.Contains(mode))
                    pos = 0;
                mode = _char;
            }
            else
            {
                Debug.WriteLine($"Error: Inflection rule mode {_char:X2} is not implemented");
                return null;
            }
        }
        return mobiHeader.Codec.GetString([.. mainEntryList]);
    }

    private List<string> GetInflectionGroups(byte[] mainEntry, int controlByteCount, List<int[]> tagTable, InflectionData inflData, byte[] inflectionNames, List<int> groupList)
    {
        List<string> infls = [];
        foreach (var item in groupList)
        {
            var (offset, nextOffset, offSetData) = inflData.OffSets((uint)item);
            var tagMap = GetTagMap(controlByteCount, tagTable, offSetData, (int)offset + 1, nextOffset);
            if (!tagMap.ContainsKey(0x05) | !tagMap.ContainsKey(0x1A))
            {
                Debug.WriteLine($"Error: Required tag 0x05 or 0x1A not found in tagMap");
                return infls;
            }
            for (int i = 0; i < tagMap[0x05].Count; i++)
            {
                var value = tagMap[0x05][i];
                var (consumed, textLength) = GetVariableWidthValue(inflectionNames, value);

                value = tagMap[0x1A][i];
                var (rvalue, start, _, data) = inflData.Lookup((uint)value);
                var offSet = BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan((int)(start + 4 + (2 * rvalue))));
                textLength = data[offSet];
                var inflection = ApplyInflectionRule(mainEntry, data, offSet + 1, offSet + 1 + textLength);
                if (inflection != null)
                    infls.Add(inflection);
            }
        }
        return infls;
    }

    private void BuildPositionMap()
    {
        bool decodeInflection = true;
        InflectionData? iData = null;
        byte[]? inflNameData = null;
        int inflectionControlByteCount = -1;
        List<int[]>? inflectionTagTable = null;
        if (!mobiHeader.IsDictionary)
            throw new NotSupportedException("MOBI file is not a dictionary.");
        
        if (mobiHeader.MetaInflIndex == 0xFFFFFFFF)
            decodeInflection = false;
        else
        {
            var metaInflIndexData = sectionizer.LoadSection((int)mobiHeader.MetaInflIndex);
            var (midxhdr, mhordt1, mhordt2) = ParseHeader(metaInflIndexData);
            var metaIndexCount = midxhdr["count"];
            List<byte[]> inflectionData = [];
            for (int i = 0; i < metaIndexCount; i++)
            {
                inflectionData.Add(sectionizer.LoadSection((int)(mobiHeader.MetaInflIndex + 1 + i)));
            }
            iData = new InflectionData(inflectionData);
            inflNameData = sectionizer.LoadSection((int)(mobiHeader.MetaInflIndex + 1 + metaIndexCount));
            var inflTagSectionStart = midxhdr["len"];
            (inflectionControlByteCount, inflectionTagTable) = ReadTagSection((int)inflTagSectionStart, metaInflIndexData);
            if (HasTag(inflectionTagTable, 0x07))
            {
                Debug.WriteLine("Obsolete inflection rule scheme.");
                decodeInflection = false;
            }
        }

        var data = sectionizer.LoadSection((int)mobiHeader.MetaOrthIndex);
        var (idxhdr, hordt1, hordt2) = ParseHeader(data);
        var tagSectionStart = idxhdr["len"];
        var (controlByteCount, tagTable) = ReadTagSection((int)tagSectionStart, data);
        var orthIndexCount = idxhdr["count"];
        hasEntryLength = HasTag(tagTable, 0x02);
        for (int i = (int)(mobiHeader.MetaOrthIndex + 1); i < mobiHeader.MetaOrthIndex + 1 + orthIndexCount; i++)
        {
            data = sectionizer.LoadSection(i);
            var (hdrinfo, ordt1, ordt2) = ParseHeader(data);
            var idxtPos = hdrinfo["start"];
            var entryCount = hdrinfo["count"];
            List<int> idxPositions = [];
            for (int j = 0; j < entryCount; j++)
            {
                var pos = BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan(((int)idxtPos) + 4 + (2*j)));
                idxPositions.Add((int)pos);
            }
            idxPositions.Add((int)idxtPos);
            for (int j = 0; j < entryCount; j++)
            {
                var startPos = idxPositions[j];
                var endPos = idxPositions[j + 1];
                var textLength = data[startPos];
                var text = data[(startPos + 1)..(startPos + 1 + textLength)];
                string headWord = mobiHeader.Codec.GetString(text);
                if (hordt2.Count > 0)
                {
                    List<char> utext = [];
                    if (idxhdr["otype"] == 0)
                    {
                        int pos = 0;
                        while (pos < textLength)
                        {
                            var offset = BinaryPrimitives.ReadUInt16BigEndian(text.AsSpan(pos));
                            if (offset < hordt2.Count)
                                utext.Add((char)hordt2[(int)offset]);
                            else
                                utext.Add((char)offset);
                            pos += 2;
                        }
                    }
                    else
                    {
                        int pos = 0;
                        while (pos < textLength)
                        {
                            var offset = text[pos];
                            if (offset < hordt2.Count)
                                utext.Add((char)hordt2[offset]);
                            else
                                utext.Add((char)offset);
                            pos++;
                        }
                    }
                    headWord = new string(utext.ToArray());
                    text = mobiHeader.Codec.GetBytes(headWord);
                }
                var tagMap = GetTagMap(controlByteCount, tagTable, data, startPos + 1 + textLength, endPos);
                List<string> inflections = [];
                if (tagMap.ContainsKey(0x01))
                {
                    if (decodeInflection && tagMap.ContainsKey(0x2A))
                    {
                        try
                        {
                            if (iData != null && inflNameData != null && inflectionControlByteCount >= 0 && inflectionTagTable != null)
                                inflections = GetInflectionGroups(text, inflectionControlByteCount, inflectionTagTable, iData, inflNameData, tagMap[0x2A]);
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Warning: Could not get inflections for {headWord}. Exception: {ex}");
                        }
                        
                    }

                    var entryStartPosition = tagMap[0x01][0];
                    if (hasEntryLength)
                    {
                        // FIXME: Could not find tagMap[0x02] in some dictionaries, mostly the ones
                        // with PalmDoc compression.
                        bool isLenghAvailable = tagMap.TryGetValue(0x02, out var len);
                        if (isLenghAvailable && len != null && len.Count > 0)
                        {
                            var entryEndPosition = entryStartPosition + len[0];
                            posMap.Add((entryStartPosition, entryEndPosition, headWord, inflections));
                        }
                        else
                        {
                            Debug.WriteLine($"Warning: Could not get entry length from tagMap[0x02] for {headWord}");
                        }
                    }
                    else
                    {
                        posMap.Add((entryStartPosition, -1, headWord, inflections));
                    }
                }
            }
        }
    }

    public List<DictionaryEntry> GetEntries()
    {
        BuildPositionMap();
        var rawML = mobiHeader.GetRawML();
        ReadOnlySpan<byte> rawMLSpan = rawML;
        var rawMLLength = rawMLSpan.Length;
        
        List<DictionaryEntry> entries = [];
        if (hasEntryLength)
        {
            foreach (var item in posMap)
            {
                try
                {
                    var definition = mobiHeader.Codec.GetString(rawMLSpan[item.StartPos..item.EndPos]);
                    entries.Add(new DictionaryEntry { Headword = item.Headword, Inflections = item.Inflections, Definition = definition });
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error: Could not get definition with startPos: {item.StartPos} and endPos: {item.EndPos} for {item.Headword}. Exception: {ex}");
                }
            }
        }
        else
        {
            var sortedPosMap = posMap.OrderBy(x => x.StartPos).ToList();
            for (int i = 0; i < sortedPosMap.Count - 1; i++)
            {
                var startPos = sortedPosMap[i].StartPos;
                var endPos = sortedPosMap[i + 1].StartPos;
                if (startPos > endPos)
                    continue;
                var definition = mobiHeader.Codec.GetString(rawMLSpan[startPos..endPos]);
                entries.Add(new DictionaryEntry { Headword = sortedPosMap[i].Headword, Inflections = sortedPosMap[i].Inflections, Definition = definition });
            }
            
            var last = sortedPosMap[^1];
            if (last.StartPos < rawMLLength)
            {
                var definition = mobiHeader.Codec.GetString(rawMLSpan[last.StartPos..rawMLLength]);
                entries.Add(new DictionaryEntry{ Headword = last.Headword, Inflections = last.Inflections, Definition = definition });
            }
        }
        return entries;
    }

    public List<Resource> GetResources()
    {
        var gifMagic = "GIF"u8;
        var bmpMagic = "BM"u8;
        ReadOnlySpan<byte> pngMagic = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
        ReadOnlySpan<byte> jpegMagic = [0xFF, 0xD8, 0xFF];

        var beginning = mobiHeader.FirstImageIndex;
        var end = sectionizer.NumberOfSections;
        List<Resource> resources = [];
        if (beginning < 0 || beginning >= end)
            return resources;
        for (int i = (int)beginning; i < end; i++)
        {
            try
            {
                var data = sectionizer.LoadSection(i);
                if (data.Length < 4)
                    continue;
                var type = data.AsSpan()[0..4];
                if (type.SequenceEqual("FLIS"u8)
                    || type.SequenceEqual("FCIS"u8)
                    || type.SequenceEqual("FDST"u8)
                    || type.SequenceEqual("DATP"u8)
                    || type.SequenceEqual("SRCS"u8)
                    || type.SequenceEqual("PAGE"u8)
                    || type.SequenceEqual("CMET"u8)
                    || type.SequenceEqual("FONT"u8)
                    || type.SequenceEqual("CRES"u8)
                    || type.SequenceEqual("CONT"u8)
                    || type.SequenceEqual("kind"u8)
                    || type.SequenceEqual("\xa0\xa0\xa0\xa0"u8)
                    || type.SequenceEqual("RESC"u8)
                    || type.SequenceEqual("\xe9\x8e\x0d\x0a"u8)
                    || type.SequenceEqual("BOUNDARY"u8))
                    continue;
                string ext = "";
                if (data.Length >= gifMagic.Length && data.AsSpan()[0..gifMagic.Length].SequenceEqual(gifMagic))
                    ext = ".gif";
                else if (data.Length >= bmpMagic.Length && data.AsSpan()[0..bmpMagic.Length].SequenceEqual(bmpMagic))
                    ext = ".bmp";
                else if (data.Length >= pngMagic.Length && data.AsSpan()[0..pngMagic.Length].SequenceEqual(pngMagic))
                    ext = ".png";
                else if (data.Length >= jpegMagic.Length && data.AsSpan()[0..jpegMagic.Length].SequenceEqual(jpegMagic))
                    ext = ".jpeg";
                if (!string.IsNullOrEmpty(ext))
                {
                    string fileName = $"image{i:D5}{ext}";
                    resources.Add(new Resource { FileName = fileName, Bytes = data });
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Could not extract resource for section: {i}. Exception message: {ex}");
            }
        }
        return resources;
    }
}
