using FluentIcons.Common;
using Vafadar.Localization;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Core.Holdings;
using Vafadar.Zanance.Core.Money;

namespace Vafadar.Zanance.App.Features.Holdings;

/// <summary>
/// Texts of holdings in the current language (design §7): quantities with their unit ("50.000 g", "3 coins"), the derived
/// weight of coins, purity, prices per gram or unit and event descriptions. Digits follow the app's digit setting.
/// </summary>
public sealed class HoldingText(Translator translator, ILocalizationService localization, IDateFormatter dates)
{
    /// <summary>Returns the quantity with its unit, e.g. "50.000 g" or "3 coins".</summary>
    public string Quantity(long quantity, AssetType type) =>
        NativeDigits.Apply(Quantities.Format(quantity, type, localization.CurrentCulture, translator["Unit_Gram"], CountName(type)))!;

    /// <summary>Returns the derived weight of a count type with a unit weight, e.g. "(30.000 g)", or <see langword="null"/>.</summary>
    public string? DerivedWeight(long quantity, AssetType type) =>
        type.Dimension == AssetDimension.Count && Quantities.MassMg(quantity, type) is { } mg
            ? "(" + NativeDigits.Apply(((decimal)mg / Quantities.PerGramOrUnit).ToString("#,0.000", localization.CurrentCulture) + " " + translator["Unit_Gram"]) + ")"
            : null;

    /// <summary>Returns the name of one unit of a count type: its own name or "piece".</summary>
    public string CountName(AssetType type) => string.IsNullOrWhiteSpace(type.CountUnitName) ? translator["Holding_Piece"] : type.CountUnitName.Trim();

    /// <summary>Returns the unit of a price: "per g" or "per coin".</summary>
    public string PerUnit(AssetType type) => type.Dimension == AssetDimension.Mass ? translator["Holding_PerGram"] : translator.Format("Holding_PerUnit", CountName(type));

    /// <summary>Returns a price per gram or unit (minor × 1,000), e.g. "100.00 EUR per g".</summary>
    public string Price(long perUnitMilli, AssetType type) =>
        MoneyText.Format((long)Math.Round(perUnitMilli / 1_000m, MidpointRounding.AwayFromZero), type.PriceCurrencyCode, localization.CurrentCulture) + " " + PerUnit(type);

    /// <summary>Returns money in the type's price currency.</summary>
    public string Money(long minor, string currency, bool showPlus = false) => MoneyText.Format(minor, currency, localization.CurrentCulture, showPlus);

    /// <summary>Returns the purity as fineness, e.g. "750/1000", or <see langword="null"/>.</summary>
    public string? Purity(AssetType type) => type.PurityPer10000 is { } purity ? NativeDigits.Apply(Core.Holdings.Purity.Format(purity, localization.CurrentCulture)) : null;

    /// <summary>Returns a fine-metal line, e.g. "Fine gold: ≈ 34.998 g (types with known purity: 2)", or without the count for one type.</summary>
    public string FineMetal(FineMetal fine, bool withCount = true)
    {
        var grams = NativeDigits.Apply(((decimal)fine.FineMassMg / Quantities.PerGramOrUnit).ToString("#,0.000", localization.CurrentCulture))!;
        var text = withCount
            ? translator.Format("Holding_FineMetal", grams, translator[$"Metal_{fine.Metal}"], fine.TypeCount)
            : translator.Format("Holding_FineMetalOne", grams, translator[$"Metal_{fine.Metal}"]);
        return fine.ExcludedTypeCount > 0 ? text + " · " + translator.Format("Holding_FineExcluded", fine.ExcludedTypeCount) : text;
    }

    /// <summary>Returns the name of an event kind, e.g. "Purchase" or "Moved".</summary>
    public string EventKind(AssetEventKind kind) => translator[$"AssetEvent_{kind}"];

    /// <summary>Returns the signed change of an event on the total, e.g. "+10.000 g", or the moved quantity of a transfer.</summary>
    public string SignedQuantity(AssetEvent assetEvent, AssetType type)
    {
        ArgumentNullException.ThrowIfNull(assetEvent);
        var effect = assetEvent.TotalEffect;
        var text = Quantity(Math.Abs(effect == 0 ? assetEvent.Quantity : effect), type);
        var sign = effect switch
        {
            > 0 => "+",
            < 0 => "−",
            _ => string.Empty,
        };

        // Sign and number form one left-to-right run, so in Persian the sign stays in front of the digits like in amounts
        // ("+۱۰٫۰۰۰ گرم", not "۱۰٫۰۰۰+ گرم"); the unit stays outside, after the number in the reading direction.
        var space = text.IndexOf(' ', StringComparison.Ordinal);
        var (number, unit) = space < 0 ? (text, string.Empty) : (text[..space], text[space..]);
        return $"\u2066\u200E{sign}{number}\u200E\u2069{unit}";
    }

    /// <summary>Returns a date in the chosen calendar.</summary>
    public string Date(DateOnly date) => dates.Format(date, DateFormatStyle.Short);

    /// <summary>Returns the icon of a type: its own or one for its kind.</summary>
    public static Symbol Icon(AssetType type) => Icons.Parse(type.Icon, type.Kind switch
    {
        AssetKind.CoinOrBar => Symbol.Money,
        AssetKind.CountableItem => Symbol.Box,
        _ => Symbol.Diamond,
    });
}
