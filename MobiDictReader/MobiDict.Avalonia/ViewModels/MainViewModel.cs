using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MobiDict.Avalonia.Services;
using MobiDict.Reader;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MobiDict.Avalonia.ViewModels;

public class Metadata
{
    public string? Key { get; set; }
    public string? Value { get; set; }
}

public partial class MainViewModel : ObservableObject, IDialogParticipant
{
    private MobiHeader? mobiHeader;
    private List<Resource> resources = [];
    private static FilePickerFileType FileTypes { get; } = new("Kindle Dictionary Files")
    {
        Patterns = new[] { "*.mobi", "*.azw" }
    };

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ToTsvCommand), nameof(ToStarDictCommand))]
    private List<DictionaryEntry> dictionaryEntries = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FilteredPreviewTable))]
    private ObservableCollection<DictionaryEntry> previewTable = [];

    [ObservableProperty]
    private ObservableCollection<Metadata> metadata = [];

    [ObservableProperty]
    private string mobiPath = "";

    [ObservableProperty]
    private string errorMessage = "";

    [ObservableProperty]
    private string busyText = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FilteredPreviewTable))]
    private string headwordFilter = "";

    [ObservableProperty]
    private bool isErrorMessageVisible = false;

    [ObservableProperty]
    private bool isBusy = false;

    public bool IsMetadataTableVisible => Metadata.Count > 0;

    public bool IsPreviewTableVisible => PreviewTable.Count > 0;

    public ObservableCollection<DictionaryEntry> FilteredPreviewTable
    {
        get
        {
            if (string.IsNullOrWhiteSpace(HeadwordFilter))
            {
                return PreviewTable;
            }

            ObservableCollection<DictionaryEntry> filtered = new(
                PreviewTable
                    .Where(item => item.Headword!.Contains(HeadwordFilter, StringComparison.OrdinalIgnoreCase) 
                        || item.Inflections!.Any(inflection => inflection.Contains(HeadwordFilter, StringComparison.OrdinalIgnoreCase))
            ));

            return filtered;
        }
    }

    public MainViewModel()
    {
        Metadata.CollectionChanged += (sender, e) =>
        {
            OnPropertyChanged(nameof(IsMetadataTableVisible));
        };

        PreviewTable.CollectionChanged += (sender, e) =>
        {
            OnPropertyChanged(nameof(IsPreviewTableVisible));
        };
    }

    [RelayCommand]
    public async Task Read()
    {
        IsBusy = true;
        BusyText = "Reading MOBI Dictionary...";
        try
        {
            var results = await this.OpenFileDialogAsync("Open MOBI dictionary", false, fileTypes: new[] { FileTypes });

            if (results is null || !results.Any())
                return;

            ErrorMessage = string.Empty;
            IsErrorMessageVisible = false;

            var file = results.First();
            await using var stream = await file.OpenReadAsync();
            using var section = new Sectionizer(stream);
            mobiHeader = new MobiHeader(section, 0);
            if (mobiHeader.IsEncrypted)
            {
                ErrorMessage = "File is encrypted.";
                IsErrorMessageVisible = true;
                return;
            }
            if (!mobiHeader.IsDictionary)
            {
                ErrorMessage = "File is not a dictionary.";
                IsErrorMessageVisible = true;
                return;
            }
            var dictionaryReader = new DictionaryReader(mobiHeader, section);
            var entries = await Task.Run(() => dictionaryReader.GetEntries());
            resources = await Task.Run(() => dictionaryReader.GetResources());
            DictionaryEntries = entries;
            PreviewTable.Clear();
            PreviewTable = new(entries);
            SetMetadata(mobiHeader);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"An error occured. Exception message: {ex}";
            IsErrorMessageVisible = true;
            return;
        }
        finally
        {
            IsBusy = false;
            BusyText = "";
        }
    }

    [RelayCommand(CanExecute = nameof(CanConvert))]
    public async Task ToTsv()
    {
        ErrorMessage = string.Empty;
        IsErrorMessageVisible = false;
        IsBusy = true;
        BusyText = "Converting to TSV...";

        try
        {
            var result = await Task.Run(() => MobiDict.Converter.Converter.ToTsv(mobiHeader!, DictionaryEntries));

            var saveFile = await this.SaveFileDialogAsync("Save as TSV", suggestedFileName: result.FileName, fileTypeName: "Tab Separated Values", fileType: "*.tsv");
            if (saveFile is null)
                return;
            await using (Stream stream = await saveFile.OpenWriteAsync())
            {
                await stream.WriteAsync(result.File);
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"An error occured while saving as Tsv. Exception message: {ex}";
            IsErrorMessageVisible = true;
            return;
        }
        finally
        {
            IsBusy = false;
            BusyText = "";
        }
    }

    [RelayCommand(CanExecute = nameof(CanConvert))]
    public async Task ToStarDict()
    {
        ErrorMessage = string.Empty;
        IsErrorMessageVisible = false;
        IsBusy = true;
        BusyText = "Converting to StarDict...";

        try
        {
            var folders = await this.PickFolderAsync("Pick a folder to save StarDict files.");
            if (folders is null || folders.Count == 0)
                return;
            var folder = folders.First();
            var path = folder.Path.AbsolutePath;
            await Task.Run(() => MobiDict.Converter.Converter.ToStarDict(mobiHeader!, DictionaryEntries, resources.Select(x => x.FileName!).ToList(), path));
            if (resources.Count > 0)
            {
                var resPath = Path.Combine(path, "res");
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
            ErrorMessage = $"An error occured while saving as StarDict. Exception message: {ex}";
            IsErrorMessageVisible = true;
            return;
        }
        finally
        {
            IsBusy = false;
            BusyText = "";
        }
    }

    private bool CanConvert()
    {
        return DictionaryEntries.Count > 0;
    }

    private void SetMetadata(MobiHeader mh)
    {
        Metadata.Clear();

        Metadata.Add(new Metadata { Key = "Title", Value = mh.Title });
        if (mh.Creator.Count > 0)
            Metadata.Add(new Metadata { Key = "Creator", Value = string.Join(", ", mh.Creator) });
        if (mh.Description.Count > 0)
            Metadata.Add(new Metadata { Key = "Description", Value = string.Join(", ", mh.Description) });
        if (!string.IsNullOrEmpty(mh.DictInLanguage))
            Metadata.Add(new Metadata { Key = "From", Value = mh.DictInLanguage });
        if (!string.IsNullOrEmpty(mh.DictOutLanguage))
            Metadata.Add(new Metadata { Key = "To", Value = mh.DictOutLanguage });
        if (mh.Published.Count > 0)
            Metadata.Add(new Metadata { Key = "Published", Value = string.Join(", ", mh.Published) });
        if (mh.Publisher.Count > 0)
            Metadata.Add(new Metadata { Key = "Publisher", Value = string.Join(", ", mh.Publisher) });
        Metadata.Add(new Metadata { Key = "MOBI Version", Value = $"{mh.Version}" });
        Metadata.Add(new Metadata
        {
            Key = "Compression",
            Value = mh.Compression switch
            {
                17480 => "HUFF/CDIC",
                2 => "PalmDOC",
                1 => "No compression",
                _ => "Unknown compression",
            }
        });
        Metadata.Add(new Metadata { Key = "Encoding", Value = $"{mh.Codec.EncodingName} (Code Page: {mh.CodePage})" });
        Metadata.Add(new Metadata { Key = "Number of Entries", Value = $"{DictionaryEntries.Count:N0}" });
    }
}
