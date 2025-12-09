using MobiDict.Reader;
using StarDictNet;
using System.Text;
using System.Text.RegularExpressions;

namespace MobiDict.Converter;

public record Result(string FileName, byte[] File);

public partial class Converter
{
    private static Regex filePosRegex = FilePosRegex();
    private static string SafeFileName(string fileName, char replacementChar = '_')
    {
        var invalidChars = Path.GetInvalidFileNameChars();

        string safeFileName = new string(fileName
            .Select(c => invalidChars.Contains(c) ? replacementChar : c)
            .ToArray())
            .Trim();
        return safeFileName;
    }

    private static void StarDictPreprocess(List<DictionaryEntry> entries)
    {
        foreach (var item in entries)
        {
            if (item.Definition != null && item.Definition.Contains("filepos"))
            {
                string replacement = filePosRegex.Replace(item.Definition, m =>
                {
                    string capture = m.Groups[1].Value;
                    var trimmed = capture.AsSpan().Trim();
                    return $"<a href=\"bword://{trimmed}\">{trimmed}</a>";
                });
                item.Definition = replacement;
            }
        }
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
        StarDictPreprocess(entries);
        var outputEntries = entries.Select(x => x.Inflections!.Count > 0
            ? new OutputEntry(x.Header!, x.Definition!, x.Inflections.ToHashSet())
            : new OutputEntry(x.Header!, x.Definition!))
            .ToList();
        var zipMs = StarDictNet.StarDictNet.Write(outputEntries, fileName, mh.Title, string.Join(", ", mh.Creator), string.Join(", ", mh.Description));
        return new Result($"{fileName}.zip", zipMs.ToArray());
    }

    [GeneratedRegex(@"<a\s+filepos[^>]+>(.*?)</a>")]
    private static partial Regex FilePosRegex();
}
