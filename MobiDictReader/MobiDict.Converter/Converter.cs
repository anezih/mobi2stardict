using MobiDict.Reader;
using StarDictNet.Core;
using System.Text;
using System.Text.RegularExpressions;

namespace MobiDict.Converter;

public record Result(string FileName, byte[] File);

public partial class Converter
{
    private static Regex filePosRegex = FilePosRegex();
    private static Regex imageRegex = ImageRegex();
    private readonly static Encoding encoding = new UTF8Encoding(false);

    private static string SafeFileName(string fileName, char replacementChar = '_')
    {
        var invalidChars = Path.GetInvalidFileNameChars();

        string safeFileName = new string(fileName
            .Select(c => invalidChars.Contains(c) ? replacementChar : c)
            .ToArray())
            .Trim();
        return safeFileName;
    }

    private static void StarDictPreprocess(List<DictionaryEntry> entries, List<string> imageNames)
    {
        foreach (var item in entries)
        {
            if (string.IsNullOrEmpty(item.Definition)) continue;
            if (item.Definition.Contains("filepos"))
            {
                string replacement = filePosRegex.Replace(item.Definition, m =>
                {
                    string capture = m.Groups[1].Value;
                    var trimmed = capture.AsSpan().Trim();
                    return $"<a href=\"bword://{trimmed}\">{trimmed}</a>";
                });
                item.Definition = replacement;
            }
            if (item.Definition.Contains("<img"))
            {
                string replacement = imageRegex.Replace(item.Definition, m =>
                {
                    string strIndex = m.Groups[1].Value;
                    var success = int.TryParse(strIndex, out var index);
                    if (success && index > 0 && index <= imageNames.Count)
                    {
                        var imageName = imageNames[index - 1];
                        return $"src=\"{imageName}\"";
                    }
                    return "";
                });
                item.Definition = replacement;
            }
        }
    }

    public static Result ToTsv(MobiHeader mh, List<DictionaryEntry> entries)
    {
        var fileName = SafeFileName(mh.Title);
        using var memoryStream = new MemoryStream();
        using var writer = new StreamWriter(memoryStream, encoding, bufferSize: 1024, leaveOpen: true);
        foreach (var item in entries)
        {
            var line = $"{item.Headword!.Trim()}\t{string.Join('|', item.Inflections!)}\t{item.Definition}\n";
            writer.Write(line);
        }
        writer.Flush();
        return new Result($"{fileName}.tsv", memoryStream.ToArray());
    }

    public static async Task ToStarDict(MobiHeader mh, List<DictionaryEntry> entries, List<string> imageNames, string folder, bool preprocessDefinitions = true)
    {
        var fileName = SafeFileName(mh.Title);
        if (preprocessDefinitions)
            StarDictPreprocess(entries, imageNames);
        var outputEntries = entries.Select(x => x.Inflections!.Count > 0
            ? new OutputEntry(x.Headword!.Trim(), x.Definition!, x.Inflections.Select(x => x.Trim()).ToHashSet())
            : new OutputEntry(x.Headword!.Trim(), x.Definition!))
            .ToList();
        var writer = new StarDictWriter();
        await writer.WriteAsync(entries:outputEntries, folder: folder, fileName: fileName,
            title: mh.Title, author: string.Join(", ", mh.Creator), description: string.Join(", ", mh.Description));
    }

    [GeneratedRegex(@"<a\s+filepos[^>]+>(.*?)</a>")]
    private static partial Regex FilePosRegex();

    [GeneratedRegex(@"(?:hi|low)?recindex=['""]{0,1}([0-9]+)['""]{0,1}")]
    private static partial Regex ImageRegex();
}
