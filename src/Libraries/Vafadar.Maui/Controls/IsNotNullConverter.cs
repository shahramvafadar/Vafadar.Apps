using System.Globalization;

namespace Vafadar.Maui.Controls;

/// <summary>Returns <see langword="true"/> when the value is not null (and not an empty string).</summary>
public sealed class IsNotNullConverter : IValueConverter
{
    /// <inheritdoc />
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is string text ? text.Length > 0 : value is not null;

    /// <inheritdoc />
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
