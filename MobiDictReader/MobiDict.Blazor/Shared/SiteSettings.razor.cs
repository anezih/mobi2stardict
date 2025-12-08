using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;

namespace MobiDict.Blazor.Shared;

public partial class SiteSettings
{
    [Inject]
    private IDialogService? DialogService { get; set; }

    private IDialogReference? _dialog;

    private async Task OpenSiteSettingsAsync()
    {
        _dialog = await DialogService!.ShowPanelAsync<SiteSettingsPanel>(new DialogParameters()
        {
            ShowTitle = true,
            Title = "Site settings",
            Alignment = HorizontalAlignment.Right,
            PrimaryAction = "OK",
            SecondaryAction = null,
            ShowDismiss = true,
        });

        DialogResult result = await _dialog.Result;
    }
}