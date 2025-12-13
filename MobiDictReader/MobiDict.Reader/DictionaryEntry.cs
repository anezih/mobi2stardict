namespace MobiDict.Reader;

public class DictionaryEntry
{
    public string? Header { get; set; }
    public List<string>? Inflections { get; set; }
    public string? Definition { get; set; }
}