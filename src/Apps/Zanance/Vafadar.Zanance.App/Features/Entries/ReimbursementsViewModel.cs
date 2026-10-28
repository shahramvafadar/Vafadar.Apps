using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vafadar.Localization.Formatting;
using Vafadar.Localization;
using Vafadar.Maui.Mvvm;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Money;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Features.Entries;

/// <summary>Money others still owe for reimbursable expenses (F2-TX-03).</summary>
public sealed partial class ReimbursementsViewModel(ZananceStore store, Translator translator, ILocalizationService localization, IDateFormatter dates) : ViewModelBase, Presentation.IThemeAware
{
    private bool _active = true;
    private long _revision;
    private string? _originalFile;
    private ReimbursementReadSnapshot? _published;
    private CancellationTokenSource _lifetime = new();

    /// <summary>Gets the complete result, published with one collection notification.</summary>
    public SnapshotCollection<EntryRow> Items { get; } = [];

    /// <summary>Gets the initial/reload cover and retry state.</summary>
    [ObservableProperty]
    public partial SnapshotLoadState Loading { get; private set; } = new();

    /// <summary>Gets the complete outstanding totals, separately for each currency.</summary>
    [ObservableProperty]
    public partial string? TotalText { get; set; }

    /// <summary>Gets whether a successfully published snapshot has no outstanding expenses.</summary>
    [ObservableProperty]
    public partial bool IsEmpty { get; set; }

    /// <summary>Retires pending work and row commands when the original page or root leaves.</summary>
    public void SetActive(bool active)
    {
        if (_active != active) { _active = active; if (!active) RetireRead(); }
        localization.Changed -= OnDisplayChanged;
        if (active) localization.Changed += OnDisplayChanged;
    }

    private string DisplayIdentity() => $"{localization.CurrentLanguage.CultureName}|{localization.CurrentCulture.Name}|{localization.CurrentCalendar}|{localization.CurrentDigits}|{localization.FormattingCultureName}";
    private void OnDisplayChanged(object? sender, EventArgs args) { RetireRead(); if (_active) _ = LoadAsync(); }
    async Task IThemeAware.RefreshThemeAsync() { RetireRead(); await LoadAsync(); }
    private void RetireRead()
    {
        _revision++; _published = null;
        var retired = _lifetime; _lifetime = new(); Loading = new(); retired.Cancel(); retired.Dispose();
    }

    /// <summary>Shares a pending covered read and publishes only the complete still-current original snapshot.</summary>
    public async Task LoadAsync()
    {
        if (!_active) return;
        var owner = Loading; var revision = _revision; var display = DisplayIdentity(); var token = _lifetime.Token;
        ReimbursementReadSnapshot? source = null; EntryRow[] rows = []; string? total = null;
        bool Current() => _active && revision == _revision && ReferenceEquals(owner, Loading) && display == DisplayIdentity();
        void RequireCurrent() { token.ThrowIfCancellationRequested(); if (!Current()) throw new OperationCanceledException("The original reimbursement page retired.", token); }
        try
        {
            await owner.RunAsync(async () =>
            {
                RequireCurrent(); _published = null;
                source = await store.ReadReimbursementsAsync(token, _originalFile); RequireCurrent();
                _originalFile ??= source.DatabasePath;
                if (_originalFile != source.DatabasePath) throw new InvalidOperationException("The original reimbursement page cannot follow another file.");
                // D-631: keep original arithmetic off the UI thread, then prepare the original themed rows privately.
                var open = await Task.Run(() => EntryActions.OpenReimbursements(source.Inputs.Entries), token); RequireCurrent();
                var culture = localization.CurrentCulture;
                var presenter = new EntryPresenter(source.Inputs.Accounts.ToDictionary(a => a.Id),
                    new CategoryLookup(source.Inputs.Categories, translator), translator, culture);
                rows = open.Select(item => presenter.Row(item.Expense) with
                {
                    Subtitle = $"{item.Expense.ReimbursedBy ?? translator["Entry_ReimbursedBySomeone"]} · {dates.Format(item.Expense.Date, DateFormatStyle.Long)}",
                    AmountText = MoneyText.Format(item.Open, presenter.CurrencyOf(item.Expense.AccountId), culture),
                }).ToArray();
                total = string.Join(" · ", open.GroupBy(o => presenter.CurrencyOf(o.Expense.AccountId))
                    .Select(g => MoneyText.Format(g.Sum(o => o.Open), g.Key, culture)));
                await store.ValidateReimbursementsAsync(source, token); RequireCurrent(); source.EnsureCurrent();
            }, () =>
            {
                var original = source ?? throw new InvalidOperationException("The original reimbursement source was not prepared.");
                void Publish(Action action) { RequireCurrent(); original.EnsureCurrent(); action(); RequireCurrent(); original.EnsureCurrent(); }
                Publish(() => TotalText = total);
                Publish(() => IsEmpty = rows.Length == 0);
                Publish(() => Items.ReplaceAll(rows));
                _published = original;
            });
        }
        // SnapshotLoadState already owns the local failed cover and Retry, including retired display-event reads.
        // Do not let a fire-and-forget localization callback turn that visible local failure into an unobserved task.
        catch (Exception exception) when (exception is not OutOfMemoryException) { }
    }

    [RelayCommand]
    private async Task OpenAsync(EntryRow row)
    {
        // Refuse retained native targets while loading or after a new snapshot has retired their row object.
        var original = _published; var owner = Loading; var revision = _revision; var token = _lifetime.Token;
        bool Current() => _active && original is not null && ReferenceEquals(original, _published)
            && ReferenceEquals(owner, Loading) && revision == _revision && Loading.IsReady
            && Items.Any(current => ReferenceEquals(current, row));
        if (!Current()) return;
        try { await store.ValidateReimbursementsAsync(original!, token); }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            if (!Current()) return;
            _published = null;
            // A stale retained target is an expected local refusal, not an unhandled asynchronous command.
            // Retain its original file binding and show the same failed cover/Retry without navigating or writing.
            try { await owner.RunAsync(() => Task.FromException(exception), () => { }); }
            catch (Exception failure) when (failure is not OutOfMemoryException) { }
            return;
        }
        if (!Current()) return;
        original!.EnsureCurrent();
        await Shell.Current.GoToAsync(AppShell.EntryDetailRoute, new Dictionary<string, object> { ["id"] = row.Id });
    }
}
