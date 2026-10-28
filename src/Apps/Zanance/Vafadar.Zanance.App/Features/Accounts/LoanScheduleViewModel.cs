using CommunityToolkit.Mvvm.ComponentModel;
using Vafadar.Localization.Formatting;
using Vafadar.Localization;
using Vafadar.Maui.Mvvm;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Money;
using Vafadar.Zanance.Data.Commerce;
using Vafadar.Zanance.App.Presentation;
using CommunityToolkit.Mvvm.Input;
using Vafadar.Zanance.Core.Commerce;

namespace Vafadar.Zanance.App.Features.Accounts;

/// <summary>One estimated installment in the schedule.</summary>
public sealed record LoanRow(string Title, string Split, string Payment, string Remaining)
{
    /// <summary>Returns what a screen reader says for the row (Windows names list rows after it).</summary>
    public override string ToString() => $"{Title}, {Payment}, {Split}, {Remaining}";
}

/// <summary>
/// The estimated repayment schedule of a loan or money lent (F2-DEBT-02), calculated from the current balance and the
/// rate and installment the user entered. It is an estimate, never the contract.
/// </summary>
public sealed partial class LoanScheduleViewModel(
    LoanWorkStore reader,
    Translator translator,
    ILocalizationService localization,
    IDateFormatter dates) : ViewModelBase, IThemeAware
{
    private Guid _id;
    private LoanReadReview? _handoff;
    private string? _originalFile;
    private bool _active = true;
    private long _revision;
    private CancellationTokenSource _lifetime = new();

    /// <summary>Gets the replaceable complete publication owner; retired reads can never uncover its replacement.</summary>
    [ObservableProperty] public partial SnapshotLoadState Loading { get; private set; } = new();
    /// <summary>Gets translated original read/access feedback without changing source or financial history.</summary>
    [ObservableProperty] public partial string? Feedback { get; private set; }
    /// <summary>Gets the original essential underpayment warning independently of paid analysis.</summary>
    [ObservableProperty] public partial string? EssentialWarning { get; private set; }
    /// <summary>Gets one complete original schedule publication with one Reset under its loading cover.</summary>
    public SnapshotCollection<LoanRow> Rows { get; } = [];
    /// <summary>Gets the current original account identity; another query cannot keep its predecessor's caption.</summary>
    [ObservableProperty] public partial string? Name { get; set; }
    /// <summary>Gets the original calculator's complete translated summary.</summary>
    [ObservableProperty] public partial string? SummaryText { get; set; }
    /// <summary>Gets whether the original account has no usable permitted schedule.</summary>
    [ObservableProperty] public partial bool IsEmpty { get; set; }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        ArgumentNullException.ThrowIfNull(query);
        var handoff = query.TryGetValue("loanReview", out var supplied)
            ? supplied as LoanReadReview ?? throw new ArgumentException("Unsupported loan review.", nameof(query)) : null;
        if (query.TryGetValue("id", out var value) && value is Guid id && _id != id)
        {
            RetireRead(); _id = id; _handoff = null;
            Name = SummaryText = Feedback = EssentialWarning = null; IsEmpty = true; Rows.ReplaceAll([]);
        }
        if (handoff is not null)
        {
            RetireRead(); _handoff = handoff;
            _originalFile ??= handoff.OriginalFilePath;
            if (_originalFile != handoff.OriginalFilePath)
                throw new InvalidOperationException("The original schedule handoff belongs to another file.");
        }
    }

    /// <summary>Retires a departed original page or root while keeping its immutable original file binding.</summary>
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
        _revision++; var retired = _lifetime; _lifetime = new(); Loading = new(); retired.Cancel(); retired.Dispose();
    }

    /// <summary>Privately projects and validates the complete original estimate before publishing any native property or row.</summary>
    public async Task LoadAsync()
    {
        if (!_active || _id == Guid.Empty) return;
        var owner = Loading; var revision = _revision; var id = _id; var display = DisplayIdentity();
        var token = _lifetime.Token; var handoff = _handoff; Projection? prepared = null;
        bool Current() => _active && id == _id && revision == _revision && ReferenceEquals(owner, Loading) && display == DisplayIdentity();
        void RequireCurrent() { token.ThrowIfCancellationRequested(); if (!Current()) throw new OperationCanceledException("The original schedule publication retired.", token); }
        try
        {
            await owner.RunAsync(async () =>
            {
                RequireCurrent();
                if (handoff is not null) { await reader.ValidateAsync(handoff, token); RequireCurrent(); }
                var reviewed = await reader.ReadAsync(token); RequireCurrent();
                _originalFile ??= reviewed.OriginalFilePath;
                if (_originalFile != reviewed.OriginalFilePath)
                    throw new InvalidOperationException("The original schedule cannot follow another opened file.");
                var result = await Task.Run(() => Prepare(reviewed, id, token), token);
                await reader.ValidateAsync(reviewed, token); RequireCurrent();
                if (handoff is not null) { await reader.ValidateAsync(handoff, token); RequireCurrent(); }
                reviewed.EnsureCurrent(); prepared = result;
            }, () =>
            {
                var result = prepared ?? throw new InvalidOperationException("The original complete schedule was not prepared.");
                void Publish(Action action) { RequireCurrent(); result.Source.EnsureCurrent(); action(); RequireCurrent(); result.Source.EnsureCurrent(); }
                Publish(() => Name = result.Name);
                Publish(() => SummaryText = result.Summary);
                Publish(() => Feedback = result.Feedback);
                Publish(() => EssentialWarning = result.Warning);
                Publish(() => IsEmpty = result.Rows.Count == 0);
                Publish(() => Rows.ReplaceAll(result.Rows));
                _handoff = null;
            });
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        { if (Current()) Feedback = CommercialFailureMessage.OrFallback(exception, translator, "Loan_ReadFailed"); }
    }

    private Projection Prepare(LoanReadReview reviewed, Guid id, CancellationToken token)
    {
        token.ThrowIfCancellationRequested(); var rows = new List<LoanRow>();
        if (reviewed.Accounts.FirstOrDefault(item => item.Id == id) is not { Installment: > 0 } account
            || !account.Type.IsDebt() || account.IsArchived) return new(reviewed, null, null, null, null, rows);
        var warning = reviewed.HasUnderpaymentWarning(account) ? translator["Loan_NeverPaidOff"] : null;
        if (reviewed.Permission(CommercialFeature.LoanAnalysis) is var permission && permission != FeaturePermission.Allowed)
            return new(reviewed, account.Name, null, CommercialFailureMessage.Describe(new CommercialWriteRejectedException(
                CommercialFeature.LoanAnalysis, permission), translator), warning, rows);
        var schedule = reviewed.Schedule(account);
        string Money(long value) => MoneyText.Format(value, account.CurrencyCode, localization.CurrentCulture);
        var summary = schedule.PaysOff ? translator.Format("Loan_ScheduleSummary", Money(reviewed.Outstanding(account)), schedule.Installments.Count, Money(schedule.TotalInterest))
            : translator["Loan_NeverPaidOff"];
        foreach (var row in schedule.Installments)
        {
            token.ThrowIfCancellationRequested();
            rows.Add(new LoanRow(translator.Format("Loan_Row", row.Number, dates.Format(row.Date, DateFormatStyle.MonthYear)),
                translator.Format("Loan_RowSplit", Money(row.Principal), Money(row.Interest)), Money(row.Payment), translator.Format("Loan_RowRemaining", Money(row.Remaining))));
        }
        reviewed.EnsureCurrent(); return new(reviewed, account.Name, summary, null, warning, rows);
    }

    /// <summary>Retains privately staged original facts until full source and current native owner have been checked.</summary>
    private sealed record Projection(LoanReadReview Source, string? Name, string? Summary, string? Feedback, string? Warning, List<LoanRow> Rows);

    /// <summary>Explicit Retry accepts current facts only inside the first original file; it never posts a calculated installment.</summary>
    [RelayCommand]
    private async Task RefreshAsync()
    {
        if (!_active || Loading.IsLoading) return;
        // Source may have changed after a failed handoff: the retained file binding still rejects another profile.
        _handoff = null; await LoadAsync();
    }
}
