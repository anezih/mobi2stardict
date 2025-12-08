using MobiDict.Reader;
using StarDictNet;
using System.Text;

namespace MobiDict.Converter;

public record Result(string FileName, byte[] File);

public class Converter
{
    private static string SafeFileName(string fileName, char replacementChar = '_')
    {
        var invalidChars = Path.GetInvalidFileNameChars();

        string safeFileName = new string(fileName
            .Select(c => invalidChars.Contains(c) ? replacementChar : c)
            .ToArray())
            .Trim();
        return safeFileName;
    }

    public static Result ToTsv(MobiHeader mh, List<DictionaryEntry> entries)
    {
        var fileName = SafeFileName(mh.Title);
        using var memoryStream = new MemoryStream();
        using var writer = new StreamWriter(memoryStream, Encoding.UTF8, bufferSize: 1024, leaveOpen: true);
        foreach (var item in entries)
        {
            var line = $"{item.Header!.Trim()}\t{string.Join('|', item.Inflections!)}\t{item.Definition}\n";
            writer.WriteLine(line);
        }
        writer.Flush();
        return new Result($"{fileName}.tsv", memoryStream.ToArray());
    }

    public static Result ToStarDict(MobiHeader mh, List<DictionaryEntry> entries)
    {
        var fileName = SafeFileName(mh.Title);
        var outputEntries = entries.Select(x => x.Inflections!.Count > 0
            ? new OutputEntry(x.Header!, x.Definition!, x.Inflections.ToHashSet())
            : new OutputEntry(x.Header!, x.Definition!))
            .ToList();
        var zipMs = StarDictNet.StarDictNet.Write(outputEntries, fileName, mh.Title, string.Join(", ", mh.Creator), string.Join(", ", mh.Description));
        return new Result($"{fileName}.zip", zipMs.ToArray());
    }
}
