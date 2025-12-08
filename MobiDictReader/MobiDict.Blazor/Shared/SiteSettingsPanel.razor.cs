using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components.Extensions;
using Microsoft.JSInterop;

namespace MobiDict.Blazor.Shared;

public partial class SiteSettingsPanel : IDialogContentComponent
{
    public DesignThemeModes Mode { get; set; }
    public OfficeColor? OfficeColor { get; set; }

    [Inject]
    private IJSRuntime? JSRuntime { get; set; }

    private static IEnumerable<DesignThemeModes> AllModes => Enum.GetValues<DesignThemeModes>();

    private static string? GetCustomColor(OfficeColor? color)
    {
        return color switch
        {
            null => OfficeColorUtilities.GetRandom(true).ToAttributeValue(),
            Microsoft.FluentUI.AspNetCore.Components.OfficeColor.Default => "#036ac4",
            _ => color.ToAttributeValue(),
        };
    }
}