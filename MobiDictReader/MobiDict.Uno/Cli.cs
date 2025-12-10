using MobiDict.Reader;

namespace MobiDict.Uno;

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

    private static async Task TsvTask(MobiHeader mh, List<DictionaryEntry> entries, string outFolder)
    {
        var tsv = MobiDict.Converter.Converter.ToTsv(mh, entries);
        await File.WriteAllBytesAsync(Path.Combine(outFolder, tsv.FileName), tsv.File);
    }

    private static async Task StardictTask(MobiHeader mh, List<DictionaryEntry> entries, string outFolder)
    {
        var stardict = MobiDict.Converter.Converter.ToStarDict(mh, entries);
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
            List<Task> tasks = new(2);
            if (Tsv)
            {
                tasks.Add(Task.Run(() => TsvTask(mh, entries, outFolder)));
            }
            if (Stardict)
            {
                tasks.Add(Task.Run(() => StardictTask(mh, entries, outFolder)));
            }
            await Task.WhenAll(tasks);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[!] An error has occured. Exception message: {ex}");
            return;
        }
    }
}
