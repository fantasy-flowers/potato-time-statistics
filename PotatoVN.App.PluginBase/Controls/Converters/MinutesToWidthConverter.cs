using System;
using Microsoft.UI.Xaml.Data;

namespace PotatoVN.App.PluginBase.Controls.Converters;

/// <summary>
/// Converts a 0–1 width ratio to an actual pixel width.
/// ConverterParameter is the maximum width in pixels (e.g. 280).
/// </summary>
public class MinutesToWidthConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var ratio = value is double d ? d : 0;
        var maxWidth = 280.0;
        if (parameter is string s && double.TryParse(s, out var parsed))
            maxWidth = parsed;
        var width = ratio * maxWidth;
        return Math.Max(2, width); // minimum 2px so zero-value bars are still visible
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) => 0;
}