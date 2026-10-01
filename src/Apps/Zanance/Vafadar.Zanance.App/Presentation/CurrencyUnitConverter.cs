using System.Globalization;
using Vafadar.Zanance.Core.Money;

namespace Vafadar.Zanance.App.Presentation;

/// <summary>Shows a currency code as the label of amount fields: the user's display unit (e.g. toman) or the code (FX-07).</summary>
public sealed class CurrencyUnitConverter : IValueConverter
{
    /// <inheritdoc />
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => MoneyText.UnitName(value as string);

    /// <inheritdoc />
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}
