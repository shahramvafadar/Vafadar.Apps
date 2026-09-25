using System.Globalization;

namespace Vafadar.Maui.Controls;

/// <summary>Returns <see langword="true"/> when a number is greater than zero (e.g. "show only when there are items").</summary>
public sealed class IsPositiveConverter : IValueConverter
{
    /// <inheritdoc />
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value switch
    {
        int number => number > 0,
        long number => number > 0,
        double number => number > 0,
        decimal number => number > 0,
        _ => false,
    };

    /// <inheritdoc />
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
