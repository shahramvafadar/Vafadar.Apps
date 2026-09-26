using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentIcons.Common;
using Vafadar.Finance.App.Presentation;
using Vafadar.Finance.Core.Ledger;
using Vafadar.Finance.Core.Money;
using Vafadar.Finance.Data;
using Vafadar.Localization;
using Vafadar.Localization.Formatting;
using Vafadar.Maui.Mvvm;

namespace Vafadar.Finance.App.Features.Entries;

/// <summary>A labelled value in the entry details.</summary>
public sealed record DetailLine(string Label, string Value);

/// <summary>Details of one entry with refund, duplicate, edit and delete (UI-04).</summary>
public sealed partial class EntryDetailViewModel(
    FinanceStore store,
    Translator translator,
    ILocalizationService localization,
    IDateFormatter dates,
    UndoService undo) : ViewModelBase, IQueryAttributable
{
    private Guid _id;
    private LedgerEntry? _entry;

    public ObservableCollection<DetailLine> Lines { get; } = [];

    public ObservableCollection<EntryRow> Refunds { get; } = [];

    [ObservableProperty]
    public partial string? Heading { get; set; }

    [ObservableProperty]
    public partial string? KindText { get; set; }

    [ObservableProperty]
    public partial string? AmountText { get; set; }

    [ObservableProperty]
    public partial Color? AmountColor { get; set; }

    [ObservableProperty]
    public partial Symbol Icon { get; set; }

    [ObservableProperty]
    public partial Color? IconColor { get; set; }

    [ObservableProperty]
    public partial Color? IconBackground { get; set; }

    [ObservableProperty]
    public partial bool CanRefund { get; set; }

    [ObservableProperty]
    public partial bool CanPayBack { get; set; }

    [ObservableProperty]
    public partial bool CanMakeRecurring { get; set; }

    [ObservableProperty]
    public partial bool IsUnreviewed { get; set; }

    [ObservableProperty]
    public partial bool HasRefunds { get; set; }

    [ObservableProperty]
    public partial string? RefundableText { get; set; }

    [ObservableProperty]
    public partial bool NotFound { get; set; }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (query.TryGetValue("id", out var value) && (value is Guid id || Guid.TryParse(value?.ToString(), out id)))
        {
            _id = id;
        }
    }

    public async Task LoadAsync()
    {
        _entry = await store.GetEntryAsync(_id);
        NotFound = _entry is null;
        if (_entry is null)
        {
            return;
        }

        var entry = _entry;
        var accounts = (await store.GetAccountsAsync()).ToDictionary(a => a.Id);
        var categories = new CategoryLookup(await store.GetCategoriesAsync(), translator);
        var culture = localization.CurrentCulture;
        var presenter = new EntryPresenter(accounts, categories, translator, culture);
        var row = presenter.Row(entry);

        Heading = row.Title;
        KindText = translator[$"EntryKind_{entry.Kind}"];
        AmountText = row.AmountText;
        AmountColor = row.AmountColor;
        Icon = row.Icon;
        IconColor = row.IconColor;
        IconBackground = row.IconBackground;
        IsUnreviewed = row.IsUnreviewed;
        CanPayBack = entry.Kind == EntryKind.Income;
        CanMakeRecurring = entry.Kind is EntryKind.Income or EntryKind.Expense or EntryKind.Transfer && entry.ScheduleId is null;

        Lines.Clear();
        Lines.Add(new DetailLine(translator["Entry_Date"], dates.Format(entry.Date, DateFormatStyle.Long)));
        if (entry.Kind == EntryKind.Transfer)
        {
            Lines.Add(new DetailLine(translator["Entry_FromAccount"], presenter.AccountName(entry.AccountId)));
            Lines.Add(new DetailLine(translator["Entry_ToAccount"], presenter.AccountName(entry.ToAccountId)));
            if (entry.ToAmount is { } toAmount && entry.ToAccountId is { } to)
            {
                Lines.Add(new DetailLine(translator.Format("Entry_ToAmount", presenter.CurrencyOf(to)), MoneyText.Format(toAmount, presenter.CurrencyOf(to), culture)));
            }

            if (entry.GroupId is { } group && EntryActions.FindTransferFee(entry, await store.GetGroupAsync(group)) is { } fee)
            {
                Lines.Add(new DetailLine(translator["Entry_Fee"], MoneyText.Format(fee.Amount, presenter.CurrencyOf(fee.AccountId), culture)));
            }
        }
        else
        {
            Lines.Add(new DetailLine(translator["Entry_Account"], presenter.AccountName(entry.AccountId)));
            if (entry.Kind != EntryKind.Adjustment)
            {
                Lines.Add(new DetailLine(translator["Entry_Category"], categories.Name(entry.CategoryId)));
            }
        }

        if (entry.Kind == EntryKind.Refund && entry.RefundOfId is { } purchaseId && await store.GetEntryAsync(purchaseId) is { } purchase)
        {
            Lines.Add(new DetailLine(translator["EntryKind_Refund"], translator.Format("Entry_RefundOf", presenter.Title(purchase))));
        }

        if (!string.IsNullOrEmpty(entry.Payee))
        {
            Lines.Add(new DetailLine(translator["Entry_Payee"], entry.Payee));
        }

        if (entry.OriginalAmount is { } original && entry.OriginalCurrencyCode is { } originalCurrency)
        {
            Lines.Add(new DetailLine(translator["Entry_ForeignAmount"], MoneyText.Format(original, originalCurrency, culture)));
        }

        if (!string.IsNullOrEmpty(entry.Note))
        {
            Lines.Add(new DetailLine(translator["Entry_Note"], entry.Note));
        }

        if (entry.Source == EntrySource.Schedule)
        {
            Lines.Add(new DetailLine(string.Empty, translator["Entry_FromPlan"]));
        }

        Refunds.Clear();
        if (entry.Kind == EntryKind.Expense)
        {
            var refunds = await store.GetRefundsAsync(entry.Id);
            foreach (var refund in refunds)
            {
                Refunds.Add(presenter.Row(refund) with { Subtitle = dates.Format(refund.Date, DateFormatStyle.Short) });
            }

            var refundable = EntryActions.Refundable(entry, refunds);
            CanRefund = refundable > 0 && accounts.ContainsKey(entry.AccountId);
            RefundableText = refunds.Count > 0 ? translator.Format("Entry_Refundable", MoneyText.Format(refundable, presenter.CurrencyOf(entry.AccountId), culture)) : null;
        }
        else
        {
            CanRefund = false;
            RefundableText = null;
        }

        HasRefunds = Refunds.Count > 0;
    }

    [RelayCommand]
    private Task EditAsync() => Shell.Current.GoToAsync(AppShell.EntryEditorRoute, new Dictionary<string, object> { ["id"] = _id });

    [RelayCommand]
    private Task RefundAsync() => Shell.Current.GoToAsync(AppShell.EntryEditorRoute, new Dictionary<string, object> { ["refundOf"] = _id });

    [RelayCommand]
    private Task DuplicateAsync() => Shell.Current.GoToAsync(AppShell.EntryEditorRoute, new Dictionary<string, object> { ["duplicate"] = _id });

    [RelayCommand]
    private Task PayBackAsync() => Shell.Current.GoToAsync(AppShell.EntryEditorRoute, new Dictionary<string, object> { ["kind"] = nameof(EntryKind.IncomeReversal), ["of"] = _id });

    // TX-04: an entry becomes the template of a plan; the entry itself stays as it is.
    [RelayCommand]
    private Task MakeRecurringAsync() => Shell.Current.GoToAsync(AppShell.PlanEditorRoute, new Dictionary<string, object> { ["fromEntry"] = _id });

    [RelayCommand]
    private Task OpenRefundAsync(EntryRow row) => Shell.Current.GoToAsync(AppShell.EntryDetailRoute, new Dictionary<string, object> { ["id"] = row.Id });

    [RelayCommand]
    private async Task DeleteAsync()
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        try
        {
            // No confirmation: deleting is undoable from the list for a few seconds (TX-05).
            var deleted = await store.DeleteEntryAsync(_id);
            if (deleted.Count > 0)
            {
                undo.Offer(deleted);
            }

            await Shell.Current.GoToAsync("..");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task MarkReviewedAsync()
    {
        if (_entry is null)
        {
            return;
        }

        _entry.Review = ReviewState.Confirmed;
        await store.SaveEntryAsync(_entry);
        await LoadAsync();
    }
}
