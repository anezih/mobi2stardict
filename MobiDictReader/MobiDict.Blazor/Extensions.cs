using Microsoft.AspNetCore.Components;

namespace MobiDict.Blazor;

public static class Extensions
{
    public static MarkupString ToMarkup(this string str) => (MarkupString)str.Replace("<br>", "");
}
