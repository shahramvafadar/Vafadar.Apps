using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vafadar.Localization;
using Vafadar.Maui.Mvvm;
using Vafadar.Zanance.Core.Goals;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.App.Interaction;
using Vafadar.Zanance.App.Presentation;

namespace Vafadar.Zanance.App.Features.Goals;

/// <summary>Earmarked money per currency.</summary>
public sealed record EarmarkLine(string Text);

/// <summary>Complete original savings goals and funding, published under one covered page lifetime without posting money.</summary>
public sealed partial class GoalsViewModel : ViewModelBase, IThemeAware
{
    private readonly GoalStore goals;
    private readonly GoalPresenter presenter;
    private readonly Translator translator;
    private readonly TimeProvider time;
    private readonly IAppInteraction interaction;
    private CancellationTokenSource _lifetime = new();
    private int _revision;
    private GoalListSnapshot? _displayed;
    private string? _originalFile;
    private Dictionary<Guid, GoalRow> _displayedRows = [];
    private bool _navigating;
    private IAsyncRelayCommand? _add;
    private IAsyncRelayCommand<GoalRow>? _open;

    /// <summary>Retains the established construction surface; the goal store now owns every captured list input.</summary>
    public GoalsViewModel(ZananceStore store, GoalStore goals, GoalPresenter presenter, Translator translator,
        TimeProvider time, IAppInteraction interaction)
    {
        ArgumentNullException.ThrowIfNull(store);
        this.goals = goals; this.presenter = presenter; this.translator = translator; this.time = time; this.interaction = interaction;
        ObserveLoading();
    }

    /// <summary>Gets complete active/paused rows with one native reset per publication.</summary>
    public SnapshotCollection<GoalRow> Active { get; } = [];
    /// <summary>Gets complete historical rows from that same original source.</summary>
    public SnapshotCollection<GoalRow> Finished { get; } = [];
    /// <summary>Gets original per-currency earmarked/free summaries.</summary>
    public SnapshotCollection<EarmarkLine> Earmarks { get; } = [];
    /// <summary>Gets the original page's initial/loading/error/retry cover.</summary>
    [ObservableProperty]
    public partial SnapshotLoadState Loading { get; private set; } = new();
    /// <summary>Gets whether the complete original source has active or paused goals.</summary>
    [ObservableProperty]
    public partial bool HasActive { get; set; }
    /// <summary>Gets whether historical goals are present in that same source.</summary>
    [ObservableProperty]
    public partial bool HasFinished { get; set; }
    /// <summary>Gets whether a complete successful source read has no goals.</summary>
    [ObservableProperty]
    public partial bool IsEmpty { get; set; }

    /// <summary>Gets an Add action owned by this actual page lifetime.</summary>
    public IAsyncRelayCommand AddCommand => _add ??= NewAddCommand();
    /// <summary>Gets an Open action that accepts only the exact currently displayed original row.</summary>
    public IAsyncRelayCommand<GoalRow> OpenCommand => _open ??= NewOpenCommand();
    private IAsyncRelayCommand NewAddCommand()
    {
        var revision = _revision;
        return new AsyncRelayCommand(() => NavigateAsync(revision, null), () => CanNavigate(revision, null));
    }
    private IAsyncRelayCommand<GoalRow> NewOpenCommand()
    {
        var revision = _revision;
        return new AsyncRelayCommand<GoalRow>(row => row is null ? Task.CompletedTask : NavigateAsync(revision, row),
            row => row is not null && CanNavigate(revision, row));
    }
    private void ObserveLoading() => Loading.PropertyChanged += (_, _) => NotifyActions();
    private void NotifyActions() { _add?.NotifyCanExecuteChanged(); _open?.NotifyCanExecuteChanged(); }
    private bool OwnsRow(GoalRow row) => _displayedRows.TryGetValue(row.Id, out var current) && ReferenceEquals(current, row);
    private bool CanNavigate(int revision, GoalRow? row)
    {
        if (revision != _revision || !Loading.IsReady || _navigating || _displayed is null
            || row is not null && !OwnsRow(row)) return false;
        try { _displayed.EnsureCurrent(); return true; }
        catch (InvalidOperationException) { return false; }
    }

    Task IThemeAware.RefreshThemeAsync() => LoadAsync();

    /// <summary>Reads one original file, computes finances on a worker and publishes a complete current packet.</summary>
    public async Task LoadAsync()
    {
        var revision = _revision; var loading = Loading; var token = _lifetime.Token;
        Prepared? prepared = null; _displayed = null; NotifyActions();
        void RequireCurrent()
        {
            token.ThrowIfCancellationRequested();
            if (revision != _revision || !ReferenceEquals(loading, Loading)) throw new OperationCanceledException("Original goal list retired.", token);
        }
        try
        {
            await loading.RunAsync(async () =>
            {
                RequireCurrent(); var snapshot = await goals.ReadListSnapshotAsync(token); RequireCurrent();
                _originalFile ??= snapshot.DatabasePath;
                if (!string.Equals(_originalFile, snapshot.DatabasePath, StringComparison.Ordinal))
                    throw new InvalidOperationException("The original goal list cannot follow another selected profile.");
                var today = DateOnly.FromDateTime(time.GetLocalNow().DateTime);
                var computation = await Task.Run(() => Prepare(snapshot, today), token);
                await goals.ValidateListSnapshotAsync(snapshot, token); RequireCurrent(); prepared = computation;
            }, () =>
            {
                RequireCurrent(); var result = prepared ?? throw new InvalidOperationException("Complete original goal list is missing.");
                result.Source.EnsureCurrent();
                var rows = presenter.ListRows(result.Source, result.Progress);
                var active = result.Source.Goals.Goals.Where(goal => goal.State is GoalState.Active or GoalState.Paused).Select(goal => goal.Id).ToHashSet();
                var summaries = result.Earmarks.Select(group =>
                {
                    var text = translator.Format("Goals_Earmarked", presenter.Money(group.Assigned, group.Currency), presenter.Money(group.Free, group.Currency));
                    return new EarmarkLine(group.Shortfall > 0 ? text + " · " + translator.Format("Goals_Shortfall", presenter.Money(group.Shortfall, group.Currency)) : text);
                }).ToArray();
                void Publish(Action change) { RequireCurrent(); result.Source.EnsureCurrent(); change(); RequireCurrent(); result.Source.EnsureCurrent(); }
                Publish(() => Active.ReplaceAll(rows.Where(row => active.Contains(row.Id))));
                Publish(() => Finished.ReplaceAll(rows.Where(row => !active.Contains(row.Id))));
                Publish(() => Earmarks.ReplaceAll(summaries));
                Publish(() => HasActive = Active.Count != 0); Publish(() => HasFinished = Finished.Count != 0);
                Publish(() => IsEmpty = rows.Length == 0); _displayedRows = rows.ToDictionary(row => row.Id); _displayed = result.Source;
            });
        }
        catch (Exception error) when (token.IsCancellationRequested && error is not OutOfMemoryException)
        {
            System.Diagnostics.Debug.WriteLine("Retired goal list read: " + error.GetType().FullName);
        }
        finally { if (revision == _revision) NotifyActions(); }
    }

    /// <summary>Retires pending work and old native commands before this page leaves the window.</summary>
    public void Retire()
    {
        _revision++; _displayed = null; _originalFile = null; _displayedRows.Clear(); _navigating = false;
        var old = _lifetime; _lifetime = new(); old.Cancel(); old.Dispose();
        Loading = new(); ObserveLoading(); _add = null; _open = null;
        OnPropertyChanged(nameof(AddCommand)); OnPropertyChanged(nameof(OpenCommand));
    }

    private async Task NavigateAsync(int revision, GoalRow? row)
    {
        if (!CanNavigate(revision, row)) return;
        var original = _displayed!; var token = _lifetime.Token; _navigating = true; NotifyActions();
        try
        {
            await goals.ValidateListSnapshotAsync(original, token);
            if (revision != _revision || token.IsCancellationRequested || !ReferenceEquals(original, _displayed)
                || !Loading.IsReady || row is not null && !OwnsRow(row)) return;
            original.EnsureCurrent();
            await interaction.NavigateAsync(row is null ? AppRoutes.GoalEditorRoute : AppRoutes.GoalDetailRoute,
                row is null ? new Dictionary<string, object> { ["goalEditorOrigin"] = original.CreateEditorOrigin() }
                    : new Dictionary<string, object> { ["id"] = row.Id });
        }
        catch (Exception error) when (error is not OutOfMemoryException)
        {
            if (revision != _revision || token.IsCancellationRequested) return;
            _displayed = null;
            try { await Loading.RunAsync(() => Task.FromException(error), () => { }); }
            catch (Exception same) when (ReferenceEquals(same, error)) { System.Diagnostics.Debug.WriteLine("Original goal list action refused: " + same.GetType().FullName); }
        }
        finally { if (revision == _revision) { _navigating = false; NotifyActions(); } }
    }

    private static Prepared Prepare(GoalListSnapshot snapshot, DateOnly today)
    {
        var balances = GoalPresenter.Balances(snapshot.Accounts, snapshot.Entries, today);
        var progress = GoalProgressService.Evaluate(snapshot.Goals.Goals, snapshot.Goals.Allocations, snapshot.Accounts,
            snapshot.Entries, snapshot.Goals.ContributionPlans, today, snapshot.AssetEvents, snapshot.AssetTypes).ToDictionary(item => item.Goal.Id);
        var currencies = snapshot.Accounts.ToDictionary(account => account.Id, account => account.CurrencyCode);
        var earmarks = GoalCalculator.Accounts(snapshot.Goals.Goals, snapshot.Goals.Allocations, balances)
            .GroupBy(item => currencies.GetValueOrDefault(item.AccountId, "EUR"))
            .Select(group => new Funding(group.Key, group.Sum(item => item.Earmarked), group.Sum(item => item.Unallocated), group.Sum(item => item.Shortfall))).ToArray();
        return new(snapshot, progress, earmarks);
    }
    /// <summary>Original pure financial facts; localized strings are produced only by the presentation caller.</summary>
    private sealed record Prepared(GoalListSnapshot Source, IReadOnlyDictionary<Guid, GoalProgress> Progress, Funding[] Earmarks);
    /// <summary>One original ISO group's funding values, never converted across currencies.</summary>
    private sealed record Funding(string Currency, long Assigned, long Free, long Shortfall);
}
