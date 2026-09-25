using System.Globalization;

namespace Vafadar.Maui.Controls;

/// <summary>Converts <see langword="true"/> to <see langword="false"/> and back (e.g. "visible when not empty").</summary>
public sealed class InvertedBoolConverter : IValueConverter
{
    /// <inheritdoc />
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value is not true;

    /// <inheritdoc />
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => value is not true;
}
