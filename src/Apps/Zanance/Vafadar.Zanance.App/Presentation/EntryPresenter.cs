using System.Globalization;
using FluentIcons.Common;
using Vafadar.Localization;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Money;

namespace Vafadar.Zanance.App.Presentation;

/// <summary>How an entry looks in lists and details: sign, colour and icon always agree (VIS-01, VIS-02).</summary>
public sealed record EntryRow(
    Guid Id,
    string Title,
    string Subtitle,
    Symbol Icon,
    Color IconColor,
    Color IconBackground,
    string AmountText,
    Color AmountColor,
    bool IsUnreviewed)
{
    /// <summary>Gets the outline of the icon tile (the icon colour, lighter).</summary>
    public Color? IconStroke { get; init; }

    /// <summary>Gets the selection state of the row in lists that allow selecting several entries (F2-TX-04).</summary>
    public RowSelection Selection { get; init; } = new();

    /// <summary>Returns what a screen reader says for the row (Windows names list rows after it).</summary>
    public override string ToString() => $"{Title}, {AmountText}, {Subtitle}";
}

/// <summary>Whether a list row is selected; observable so a tap does not rebuild the list.</summary>
public sealed partial class RowSelection : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
{
    [CommunityToolkit.Mvvm.ComponentModel.ObservableProperty]
    public partial bool IsSelected { get; set; }
}

/// <summary>Formats entries for display.</summary>
internal sealed class EntryPresenter(
    IReadOnlyDictionary<Guid, Account> accounts,
    CategoryLookup categories,
    Translator translator,
    CultureInfo culture)
{
    public static Color IncomeColor => Palette.IncomeText;
    // Expenses stay neutral (sign and icon carry the meaning); red is kept for problems: negative results, overdue items,
    // exceeded limits and debt (D-27).
    public static Color ExpenseColor => Palette.AmountText;
    public static Color DangerColor => Palette.ExpenseText;
    public static Color NeutralColor => Palette.TransferText;

    // The arrow points from the source to the destination in the reading direction.
    private string Arrow => culture.TextInfo.IsRightToLeft ? "←" : "→";

    public string AccountName(Guid? id) => id is { } value && accounts.TryGetValue(value, out var account) ? account.Name : "?";

    public string CurrencyOf(Guid id) => accounts.TryGetValue(id, out var account) ? account.CurrencyCode : Currencies.Euro.Code;

    public string Title(LedgerEntry entry) => entry switch
    {
        { Title: { Length: > 0 } title } => title,
        { Kind: EntryKind.Transfer } => translator["EntryKind_Transfer"],
        { Kind: EntryKind.Adjustment } => translator["EntryKind_Adjustment"],
        { Kind: EntryKind.AssetPurchase } => translator["EntryKind_AssetPurchase"],
        { Kind: EntryKind.AssetSale } => translator["EntryKind_AssetSale"],
        _ => categories.Name(entry.CategoryId),
    };

    public string Subtitle(LedgerEntry entry) => entry.Kind switch
    {
        EntryKind.Transfer => $"{AccountName(entry.AccountId)} {Arrow} {AccountName(entry.ToAccountId)}",
        EntryKind.Refund => $"{translator["EntryKind_Refund"]} · {categories.Name(entry.CategoryId)} · {AccountName(entry.AccountId)}",
        EntryKind.IncomeReversal => $"{translator["EntryKind_IncomeReversal"]} · {AccountName(entry.AccountId)}",
        EntryKind.Adjustment => AccountName(entry.AccountId),
        // A holding bought or sold: neither spending nor income (ZEX-AS05).
        EntryKind.AssetPurchase or EntryKind.AssetSale => $"{translator[$"EntryKind_{entry.Kind}"]} · {AccountName(entry.AccountId)}",
        _ when string.IsNullOrEmpty(entry.Title) => AccountName(entry.AccountId),
        _ => $"{categories.Name(entry.CategoryId)} · {AccountName(entry.AccountId)}",
    };

    /// <summary>+ for money in, − for money out, no sign for transfers (they are neither income nor expense).</summary>
    public string Amount(LedgerEntry entry)
    {
        var currency = CurrencyOf(entry.AccountId);
        return entry.Kind switch
        {
            EntryKind.Income or EntryKind.Refund => MoneyText.Format(entry.Amount, currency, culture, showPlus: true),
            EntryKind.Expense or EntryKind.IncomeReversal => MoneyText.Format(-entry.Amount, currency, culture),
            EntryKind.Adjustment => MoneyText.Format(entry.Direction == AdjustmentDirection.Decrease ? -entry.Amount : entry.Amount, currency, culture, showPlus: true),
            EntryKind.AssetPurchase => MoneyText.Format(-entry.Amount, currency, culture),
            EntryKind.AssetSale => MoneyText.Format(entry.Amount, currency, culture, showPlus: true),
            _ => MoneyText.Format(entry.Amount, currency, culture),
        };
    }

    public static Color AmountColor(LedgerEntry entry) => entry.Kind switch
    {
        EntryKind.Income => IncomeColor,
        EntryKind.Refund => Palette.RefundText,
        EntryKind.Expense or EntryKind.IncomeReversal => ExpenseColor,
        _ => NeutralColor,
    };

    public Symbol Icon(LedgerEntry entry) => entry.Kind switch
    {
        EntryKind.Transfer => Symbol.ArrowSwap,
        EntryKind.Adjustment => Symbol.ArrowSync,
        EntryKind.AssetPurchase or EntryKind.AssetSale => Symbol.Diamond,
        _ when entry.Icon is not null => Icons.Parse(entry.Icon, categories.Icon(entry.CategoryId)),
        _ => categories.Icon(entry.CategoryId),
    };

    public Color IconColor(LedgerEntry entry) =>
        entry.Kind is EntryKind.Transfer or EntryKind.Adjustment or EntryKind.AssetPurchase or EntryKind.AssetSale ? NeutralColor : categories.Color(entry.CategoryId);

    public EntryRow Row(LedgerEntry entry)
    {
        var color = IconColor(entry);
        return new EntryRow(
            entry.Id,
            Title(entry),
            Subtitle(entry),
            Icon(entry),
            color,
            color.WithAlpha(0.12f),
            Amount(entry),
            AmountColor(entry),
            entry.Review == ReviewState.Unreviewed) { IconStroke = color.WithAlpha(0.3f) };
    }
}
