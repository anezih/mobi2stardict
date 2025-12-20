using MobiDict.Reader;

namespace MobiDict.Cli;

public static class Cli
{
    private static string GetOutputFolder(string? path)
    {
        if (string.IsNullOrEmpty(path) || !Path.Exists(path))
        {
            var cwd = Environment.CurrentDirectory;
            Console.WriteLine($"[!] Output folder is not valid, files will be saved to current working directory: {cwd}");
            return cwd;
        }
        return path;
    }

    private static string SafeFileName(string fileName, char replacementChar = '_')
    {
        var invalidChars = Path.GetInvalidFileNameChars();

        string safeFileName = new string(fileName
            .Select(c => invalidChars.Contains(c) ? replacementChar : c)
            .ToArray())
            .Trim();
        return safeFileName;
    }

    private static async Task TsvTask(MobiHeader mh, List<DictionaryEntry> entries, string outFolder)
    {
        var tsv = Converter.Converter.ToTsv(mh, entries);
        await File.WriteAllBytesAsync(Path.Combine(outFolder, tsv.FileName), tsv.File);
    }

    private static async Task StardictTask(MobiHeader mh, List<DictionaryEntry> entries, List<string> imageNames, string outFolder)
    {
        var stardict = Converter.Converter.ToStarDict(mh, entries, imageNames);
        await File.WriteAllBytesAsync(Path.Combine(outFolder, stardict.FileName), stardict.File);
    }

    /// <summary>
    /// Convert Kindle MOBI dictionaries to TSV and StarDict formats.
    /// </summary>
    /// <param name="Input">-i, MOBI file path</param>
    /// <param name="Tsv">Save as TSV (tab-separated-values)</param>
    /// <param name="Stardict">Save as StarDict</param>
    /// <param name="OutputFolder">-o, Output folder where the files will be saved</param>
    public static async Task Run(string Input, bool Tsv, bool Stardict, string? OutputFolder = null)
    {
        if (!(Tsv | Stardict))
        {
            Console.WriteLine($"[!] At least one output format should be specified.");
            return;
        }
        var outFolder = GetOutputFolder(OutputFolder);
        try
        {
            using var fs = new FileStream(Input, FileMode.Open, FileAccess.Read);
            var section = new Sectionizer(fs);
            var mh = new MobiHeader(section, 0);
            if (mh.IsEncrypted)
            {
                Console.WriteLine("[!] File is encrypted.");
                return;
            }
            if (!mh.IsDictionary)
            {
                Console.WriteLine("[!] File is not a dictionary.");
                return;
            }
            var dict = new DictionaryReader(mh, section);
            var entries = dict.GetEntries();
            var resources = dict.GetResources();
            var imageNames = resources.Select(x => x.FileName!).ToList();
            List<Task> tasks = new(2);
            if (Tsv)
            {
                tasks.Add(Task.Run(() => TsvTask(mh, entries, outFolder)));
            }
            if (Stardict)
            {
                tasks.Add(Task.Run(() => StardictTask(mh, entries, imageNames, outFolder)));
            }
            await Task.WhenAll(tasks);
            if (resources.Count > 0)
            {
                var title = SafeFileName(mh.Title);
                var resPath = Path.Combine(outFolder, $"{title}_res");
                if (!Directory.Exists(resPath))
                    Directory.CreateDirectory(resPath);
                foreach (var item in resources)
                {
                    await File.WriteAllBytesAsync(Path.Combine(resPath, item.FileName!), item.Bytes!);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[!] An error has occured. Exception message: {ex}");
            return;
        }
    }
}
