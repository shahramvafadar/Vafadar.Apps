global using Microsoft.Maui.Controls;
using System.Globalization;

namespace Microsoft.Maui.Controls;

/// <summary>Headless contract shape for the unchanged input adapter; native review exercises the real binding engine.</summary>
public interface IValueConverter
{
    /// <summary>Converts the original source value into its control value.</summary>
    object Convert(object? value, Type targetType, object? parameter, CultureInfo culture);

    /// <summary>Converts the original control value back into its source value.</summary>
    object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture);
}
