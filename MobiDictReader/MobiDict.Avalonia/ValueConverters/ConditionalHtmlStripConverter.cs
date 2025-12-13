using Avalonia.Data.Converters;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace MobiDict.Avalonia.ValueConverters;

public partial class ConditionalHtmlStripConverter : IMultiValueConverter
{
    // HTML etiketlerini temizlemek için Regex
    private static readonly Regex HtmlStripRegex = HtmlRegex();

    // IValueConverter yerine IMultiValueConverter kullanılır
    public object Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        // Gelen değerleri kontrol et
        if (values.Count < 2 || !(values[0] is string htmlString) || !(values[1] is bool isStripChecked))
        {
            return string.Empty;
        }

        if (isStripChecked)
        {
            string plainText = HtmlStripRegex.Replace(htmlString, string.Empty);
            plainText = Regex.Replace(plainText, @"\s+", " ").Trim();
            return plainText;
        }
        else
        {
            return htmlString;
        }
    }

    public IList<object> ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }

    [GeneratedRegex("<[^>]*>", RegexOptions.Compiled)]
    private static partial Regex HtmlRegex();
}
