using Avalonia.Platform.Storage;
using Avalonia.Threading;
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

    private static FilePickerFileType FileTypes { get; } = new("Kindle Dictionary Files")
    {
        Patterns = new[] { "*.mobi", "*.azw" }
    };

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ToTsvCommand), nameof(ToStarDictCommand))]
    private List<DictionaryEntry> dictionaryEntries = [];

    [ObservableProperty]
    private ObservableCollection<DictionaryEntry> previewTable = [];

    [ObservableProperty]
    private ObservableCollection<Metadata> metadata = [];

    [ObservableProperty]
    private string mobiPath = string.Empty;

    [ObservableProperty]
    private string errorMessage = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FilteredPreviewTable))]
    private string headerFilter = string.Empty;

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
            if (string.IsNullOrWhiteSpace(HeaderFilter))
            {
                return PreviewTable;
            }

            // Aksi takdirde, filtreleme işlemini yap
            ObservableCollection<DictionaryEntry> filtered = new(
                PreviewTable
                    .Where(item => item.Header!.Contains(HeaderFilter, StringComparison.OrdinalIgnoreCase) 
                        || item.Inflections!.Any(inflection => inflection.Contains(HeaderFilter, StringComparison.OrdinalIgnoreCase))
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
        try
        {
            var results = await this.OpenFileDialogAsync("Open MOBI dictionary", false, fileTypes: new[] { FileTypes });

            if (results is null || !results.Any())
                return;

            ErrorMessage = string.Empty;
            IsErrorMessageVisible = false;

            var file = results.First();
            MemoryStream ms = new();
            await using (Stream stream = await file.OpenReadAsync())
            {
                await stream.CopyToAsync(ms);
            }
            var section = new Sectionizer(ms);
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
            DictionaryEntries = await Task.Run(() => dictionaryReader.GetEntries());
            PreviewTable.Clear();
            foreach (var item in DictionaryEntries)
            {
                PreviewTable.Add(item);
            }
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
        }
    }

    [RelayCommand(CanExecute = nameof(CanConvert))]
    public async Task ToTsv()
    {
        ErrorMessage = string.Empty;
        IsErrorMessageVisible = false;
        IsBusy = true;

        try
        {
            var result = await Task.Run(() => MobiDict.Converter.Converter.ToTsv(mobiHeader!, DictionaryEntries));

            var saveFile = await this.SaveFileDialogAsync("Save as TSV", suggestedFileName: result.FileName, extension: "*.tsv");
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
        }
    }

    [RelayCommand(CanExecute = nameof(CanConvert))]
    public async Task ToStarDict()
    {
        ErrorMessage = string.Empty;
        IsErrorMessageVisible = false;
        IsBusy = true;

        try
        {
            var result = await Task.Run(() => MobiDict.Converter.Converter.ToStarDict(mobiHeader!, DictionaryEntries));
            var saveFile = await this.SaveFileDialogAsync("Save as StarDict", suggestedFileName: result.FileName, extension: "*.zip");
            if (saveFile is null)
                return;
            await using (Stream stream = await saveFile.OpenWriteAsync())
            {
                await stream.WriteAsync(result.File);
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
        Metadata.Add(new Metadata { Key = "Number of Entries", Value = $"{DictionaryEntries.Count:N0}" });
    }
}
