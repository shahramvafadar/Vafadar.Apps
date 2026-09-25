using System.Globalization;
using FluentIcons.Common;
using Vafadar.Finance.Core.Accounts;
using Vafadar.Finance.Core.Ledger;
using Vafadar.Finance.Core.Money;
using Vafadar.Localization;

namespace Vafadar.Finance.App.Presentation;

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
    bool IsUnreviewed);

/// <summary>Formats entries for display.</summary>
internal sealed class EntryPresenter(
    IReadOnlyDictionary<Guid, Account> accounts,
    CategoryLookup categories,
    Translator translator,
    CultureInfo culture)
{
    public static readonly Color IncomeColor = Color.FromArgb("#1B5E20");
    public static readonly Color ExpenseColor = Color.FromArgb("#B71C1C");
    public static readonly Color NeutralColor = Color.FromArgb("#37474F");

    public string AccountName(Guid? id) => id is { } value && accounts.TryGetValue(value, out var account) ? account.Name : "?";

    public string CurrencyOf(Guid id) => accounts.TryGetValue(id, out var account) ? account.CurrencyCode : Currencies.Euro.Code;

    public string Title(LedgerEntry entry) => entry switch
    {
        { Title: { Length: > 0 } title } => title,
        { Kind: EntryKind.Transfer } => translator["EntryKind_Transfer"],
        { Kind: EntryKind.Adjustment } => translator["EntryKind_Adjustment"],
        _ => categories.Name(entry.CategoryId),
    };

    public string Subtitle(LedgerEntry entry) => entry.Kind switch
    {
        EntryKind.Transfer => $"{AccountName(entry.AccountId)} → {AccountName(entry.ToAccountId)}",
        EntryKind.Refund => $"{translator["EntryKind_Refund"]} · {categories.Name(entry.CategoryId)} · {AccountName(entry.AccountId)}",
        EntryKind.IncomeReversal => $"{translator["EntryKind_IncomeReversal"]} · {AccountName(entry.AccountId)}",
        EntryKind.Adjustment => AccountName(entry.AccountId),
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
            _ => MoneyText.Format(entry.Amount, currency, culture),
        };
    }

    public static Color AmountColor(LedgerEntry entry) => entry.Kind switch
    {
        EntryKind.Income or EntryKind.Refund => IncomeColor,
        EntryKind.Expense or EntryKind.IncomeReversal => ExpenseColor,
        _ => NeutralColor,
    };

    public Symbol Icon(LedgerEntry entry) => entry.Kind switch
    {
        EntryKind.Transfer => Symbol.ArrowSwap,
        EntryKind.Adjustment => Symbol.ArrowSync,
        _ when entry.Icon is not null => Icons.Parse(entry.Icon, categories.Icon(entry.CategoryId)),
        _ => categories.Icon(entry.CategoryId),
    };

    public Color IconColor(LedgerEntry entry) =>
        entry.Kind is EntryKind.Transfer or EntryKind.Adjustment ? NeutralColor : categories.Color(entry.CategoryId);

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
            entry.Review == ReviewState.Unreviewed);
    }
}
