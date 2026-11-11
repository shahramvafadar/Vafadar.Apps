using System.Globalization;

namespace Vafadar.Zanance.App.Features.Sync;

/// <summary>Maps the input view's nullable empty text to the request field's non-null empty-string contract.</summary>
public sealed class PersonalSyncRequestTextConverter : IValueConverter
{
    /// <inheritdoc />
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value ?? string.Empty;

    /// <inheritdoc />
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => value ?? string.Empty;
}
