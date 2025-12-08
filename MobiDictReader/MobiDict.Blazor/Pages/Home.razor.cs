using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;
using MobiDict.Reader;

namespace MobiDict.Blazor.Pages;

public partial class Home
{
    [Inject]
    private IToastService? ToastService { get; set; }

    private bool isDictionaryRead = false;
    private string headwordFilter = string.Empty;
    private PaginationState pagination = new PaginationState { ItemsPerPage = 10 };

    private Dictionary<string, string> metadata = new();
    IQueryable<DictionaryEntry>? previewEntries;
    private IQueryable<DictionaryEntry>? FilteredItems => previewEntries?.Where(x => x.Header!.Contains(headwordFilter, StringComparison.CurrentCultureIgnoreCase));

    private async Task OnCompletedAsync(IEnumerable<FluentInputFileEventArgs> files)
    {
        var mobiStream = files.First().Stream;
        if (mobiStream == null) return;
        MemoryStream mobiMs = new();
        await mobiStream.CopyToAsync(mobiMs);

        var section = new Sectionizer(mobiMs);
        var mh = new MobiHeader(section, 0);
        if (mh.IsEncrypted)
        {
            Console.WriteLine("File is encrypted!");
            ToastService!.ShowToast(ToastIntent.Error, $"File is encrypted!");
            return;
        }
        if (!mh.IsDictionary)
        {
            Console.WriteLine("File is not a dictionary!");
            ToastService!.ShowToast(ToastIntent.Error, $"File is not a dictionary!");
            return;
        }
        var dict = new DictionaryReader(mh, section);
        var entries = dict.GetEntries();

        previewEntries = entries.AsQueryable();
        PopulateMetadata(mh, entries.Count);
    }

    private void PopulateMetadata(MobiHeader mh, int numOfEntries)
    {
        metadata.Clear();
        if (!string.IsNullOrEmpty(mh.Title))
            metadata["Title"] = mh.Title;
        if (mh.Description.Count > 0)
            metadata["Description"] = string.Join(", ", mh.Description);
        if (mh.Creator.Count > 0)
            metadata["Creator"] = string.Join(", ", mh.Creator);
        if (mh.Publisher.Count > 0)
            metadata["Creator"] = string.Join(", ", mh.Publisher);
        if (mh.Published.Count > 0)
            metadata["Published"] = string.Join(", ", mh.Published);
        if (!string.IsNullOrEmpty(mh.DictInLanguage))
            metadata["Dictionary Input Language"] = mh.DictInLanguage;
        if (!string.IsNullOrEmpty(mh.DictOutLanguage))
            metadata["Dictionary Output Language"] = mh.DictOutLanguage;
        metadata["Number of Entries"] = $"{numOfEntries:N0}";
        isDictionaryRead = true;
        StateHasChanged();
    }

    private void HandleHeadwordFilter(ChangeEventArgs args)
    {
        if (args.Value is string value)
        {
            headwordFilter = value;
        }
    }

    private void HandleClear()
    {
        if (string.IsNullOrWhiteSpace(headwordFilter))
        {
            headwordFilter = string.Empty;
        }
    }

    private async Task ExportToTsv()
    {

    }

    private async Task ExportToStarDict()
    {

    }
}