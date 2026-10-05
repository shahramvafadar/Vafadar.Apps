using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vafadar.Localization;
using Vafadar.Maui.Mvvm;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Money;
using Vafadar.Zanance.Core.Plans;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Features.Plans;

/// <summary>
/// Final settlement of advance payments (F2-CON-04): the actual bill is compared with the advances of a period; only
/// the extra payment is recorded as an expense, or the money back as refunds of the advances.
/// </summary>
public sealed partial class SettlementViewModel(ZananceStore store, PlanStore plans, Translator translator, ILocalizationService localization, TimeProvider time)
    : ViewModelBase, IQueryAttributable, Presentation.IUnsavedChanges
{
    private Schedule? _plan;
    private List<LedgerEntry> _entries = [];
    private string _currency = Currencies.Euro.Code;
    private SettlementResult? _result;

    [ObservableProperty]
    public partial string? PlanName { get; set; }

    [ObservableProperty]
    public partial DateOnly From { get; set; }

    [ObservableProperty]
    public partial DateOnly To { get; set; }

    [ObservableProperty]
    public partial string ActualText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? AdvancesText { get; set; }

    // Amounts are typed in the currency's display unit when one is defined (FX-07).
    [ObservableProperty]
    public partial string? UnitNote { get; set; }

    [ObservableProperty]
    public partial string? ResultText { get; set; }

    [ObservableProperty]
    public partial string? Error { get; set; }

    [ObservableProperty]
    public partial bool CanSave { get; set; }

    private DateOnly Today => DateOnly.FromDateTime(time.GetLocalNow().DateTime);

    public async void ApplyQueryAttributes(IDictionary<string, object> query) => await Presentation.Failures.GuardAsync(() => ApplyQueryAsync(query));

    private async Task ApplyQueryAsync(IDictionary<string, object> query)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (!query.TryGetValue("id", out var value) || value is not Guid id)
        {
            return;
        }

        query.Clear();
        _plan = await plans.GetScheduleAsync(id);
        if (_plan is null)
        {
            return;
        }

        var account = (await store.GetAccountsAsync()).FirstOrDefault(a => a.Id == _plan.AccountId);
        _currency = account?.CurrencyCode ?? Currencies.Euro.Code;
        UnitNote = DisplayUnitNote.For(translator, _currency);
        _entries = await store.GetEntriesAsync();
        PlanName = _plan.Name;
        To = Today;
        From = Today.AddYears(-1).AddDays(1);
        Update();
        _snapshot = Snapshot();
    }

    partial void OnFromChanged(DateOnly value) => Update();

    partial void OnToChanged(DateOnly value) => Update();

    partial void OnActualTextChanged(string value) => Update();

    private void Update()
    {
        if (_plan is null || From == default || To == default)
        {
            return;
        }

        var culture = localization.CurrentCulture;
        var currency = Currencies.Get(_currency);
        var actual = MoneyText.TryParse(ActualText, currency, culture, out var parsed) && parsed >= 0 ? parsed : (long?)null;
        _result = AdvanceSettlement.Compute(_plan, _entries, From, To, actual ?? 0);
        AdvancesText = translator.Format("Settlement_Advances", _result.Advances.Count, MoneyText.Format(_result.Paid, _currency, culture));
        if (actual is null)
        {
            ResultText = null;
            CanSave = false;
            return;
        }

        ResultText = _result.Difference switch
        {
            > 0 => translator.Format("Settlement_Extra", MoneyText.Format(_result.Difference, _currency, culture)),
            < 0 => translator.Format("Settlement_Back", MoneyText.Format(-_result.Difference, _currency, culture)),
            _ => translator["Settlement_Even"],
        };
        CanSave = _result.Difference != 0 && _result.Advances.Count > 0 && (_result.Difference > 0 || -_result.Difference <= _result.Paid);
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (_plan is null || _result is null || !CanSave || IsBusy)
        {
            return;
        }

        IsBusy = true;
        try
        {
            var title = translator.Format(_result.Difference > 0 ? "Settlement_ExtraTitle" : "Settlement_BackTitle", _plan.Name);
            var created = AdvanceSettlement.CreateEntries(_plan, _result, _entries, Today, title);
            var saved = await store.SaveEntriesAsync(created, []);
            if (!saved.Succeeded)
            {
                Error = string.Join(Environment.NewLine, saved.Errors.Distinct().Select(e => translator[$"LedgerError_{e}"]));
                return;
            }

            await Shell.Current.GoToAsync("..");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private Task CancelAsync() => Presentation.UnsavedChanges.LeaveAsync(this);

    // The input as it was loaded or saved; leaving with a change asks first (CR12).
    private string? _snapshot;

    /// <inheritdoc />
    public bool IsDirty => _snapshot is not null && Snapshot() != _snapshot;

    private string Snapshot() => Presentation.UnsavedChanges.Fingerprint(
        From, To, ActualText);
}
