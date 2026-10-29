using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vafadar.Localization;
using Vafadar.Maui.Mvvm;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Core.Categories;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Features.Categories;

/// <summary>A rule in the list.</summary>
public sealed record RuleRow(Guid Id, string Match, string CategoryName);

/// <summary>A category a rule can suggest.</summary>
public sealed record RuleCategory(Guid Id, CategoryKind Kind, string Name)
{
    public override string ToString() => Name;
}

/// <summary>
/// Local categorization rules (F2-TX-04): the text to look for in payee or title and the category to suggest. Rules only
/// suggest categories for new entries and imports; saved entries never change.
/// </summary>
public sealed partial class RulesViewModel(ZananceStore store, Translator translator,
    ILocalizationService localization, Interaction.IAppInteraction interaction) : ViewModelBase, IThemeAware
{
    private CategoryRulesReadSnapshot? _published;
    private RuleRow[] _ownedRows = [];
    private RuleCategory[] _ownedOptions = [];
    private string? _originalFile;
    private string? _publishedDisplay;
    private volatile bool _active;
    private int _revision;
    private int _draftRevision;
    private int _accepted;
    private bool _saving;
    private bool _publishing;
    private Guid? _categoryDraftId;
    private bool _categoryDraftInitialized;
    private CancellationTokenSource _lifetime = new();

    /// <summary>Gets all original definitions published together beneath the whole-page cover.</summary>
    [ObservableProperty] public partial IReadOnlyList<RuleRow> Rules { get; private set; } = [];
    /// <summary>Gets the original read failure without replacing the user's raw form draft.</summary>
    [ObservableProperty] public partial string? Feedback { get; private set; }
    /// <summary>Gets the complete source's loading, error and explicit Retry state.</summary>
    [ObservableProperty, NotifyPropertyChangedFor(nameof(CanInteract)), NotifyPropertyChangedFor(nameof(CanReload))]
    public partial SnapshotLoadState Loading { get; private set; } = new();
    /// <summary>Gets whether commands belong to the current original publication.</summary>
    public bool CanInteract => _active && !_saving && Loading.IsReady && _published is not null && _publishedDisplay == DisplayIdentity();
    /// <summary>Gets whether an explicit source-only Retry is available.</summary>
    public bool CanReload => _active && !_saving && !Loading.IsLoading;
    /// <summary>Gets known writer acceptance independently of a later read or notification failure.</summary>
    public bool Accepted => Volatile.Read(ref _accepted) != 0;
    private string DisplayIdentity() => $"{localization.CurrentLanguage.CultureName}|{localization.CurrentCulture.Name}|{localization.CurrentCalendar}|{localization.CurrentDigits}|{localization.FormattingCultureName}";

    partial void OnMatchTextChanged(string value) { if (!_publishing) Interlocked.Increment(ref _draftRevision); }
    partial void OnSelectedCategoryChanged(RuleCategory? value)
    {
        if (_publishing) return;
        _categoryDraftInitialized = true; _categoryDraftId = value?.Id; Interlocked.Increment(ref _draftRevision);
    }
    /// <summary>Retires actual reads, native consent and command owners on page, tab or root departure.</summary>
    public void SetActive(bool active)
    {
        localization.Changed -= OnDisplayChanged;
        if (active) localization.Changed += OnDisplayChanged;
        if (_active != active) { _active = active; if (!active) RetireRead(); }
        NotifyAvailability();
    }
    private void RetireRead()
    {
        Interlocked.Increment(ref _revision); _published = null;
        var old = _lifetime; _lifetime = new(); Loading = new(); old.Cancel(); old.Dispose();
    }
    private void OnDisplayChanged(object? sender, EventArgs args) { RetireRead(); if (_active) _ = LoadAsync(); }
    async Task IThemeAware.RefreshThemeAsync() { RetireRead(); if (_active) await LoadAsync(); }
    private void NotifyAvailability()
    { OnPropertyChanged(nameof(CanInteract)); OnPropertyChanged(nameof(CanReload)); OnPropertyChanged(nameof(Accepted)); }

    [ObservableProperty]
    public partial IReadOnlyList<RuleCategory> CategoryOptions { get; set; } = [];

    [ObservableProperty]
    public partial RuleCategory? SelectedCategory { get; set; }

    [ObservableProperty]
    public partial string MatchText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? Error { get; set; }

    [ObservableProperty]
    public partial bool HasRules { get; set; }

    public async Task LoadAsync()
    {
        if (!_active) return;
        var owner = Loading; var revision = Volatile.Read(ref _revision); var display = DisplayIdentity(); var token = _lifetime.Token;
        bool Current() => _active && revision == Volatile.Read(ref _revision) && display == DisplayIdentity() && ReferenceEquals(owner, Loading);
        void RequireCurrent() { token.ThrowIfCancellationRequested(); if (!Current()) throw new OperationCanceledException("The original rule publication retired.", token); }
        CategoryRulesReadSnapshot? source = null; RuleCategory[] options = []; RuleRow[] rows = [];
        try
        {
            var pending = owner.RunAsync(async () =>
            {
                _published = null; RequireCurrent();
                source = await store.ReadCategoryRulesSnapshotAsync(path => { RequireCurrent(); _originalFile ??= path; return _originalFile == path; }, token);
                RequireCurrent();
                (options, rows) = await Task.Run(() => Prepare(source), token); RequireCurrent();
                await store.ValidateCategoryRulesSnapshotAsync(source, token); RequireCurrent();
            }, () =>
            {
                RequireCurrent(); _publishing = true;
                try
                {
                    // Rebind the same category identity; never silently retarget an existing draft after a reload.
                    if (!_categoryDraftInitialized) { _categoryDraftId = options.FirstOrDefault()?.Id; _categoryDraftInitialized = true; }
                    CategoryOptions = Array.AsReadOnly(options); RequireCurrent();
                    // Record value equality otherwise retains an old Picker object that no longer owns this source.
                    SelectedCategory = null; RequireCurrent();
                    SelectedCategory = options.FirstOrDefault(option => option.Id == _categoryDraftId); RequireCurrent();
                    Rules = Array.AsReadOnly(rows); RequireCurrent(); HasRules = rows.Length > 0; RequireCurrent();
                    _ownedOptions = options; _ownedRows = rows; Feedback = null; RequireCurrent();
                    _publishedDisplay = display; _published = source;
                }
                finally { _publishing = false; }
            });
            NotifyAvailability(); await pending;
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        { if (Current()) Feedback = Accepted ? translator["Occurrence_SavedRefreshFailed"] : CommercialFailureMessage.OrFallback(exception, translator, "Error_Unexpected"); }
        finally { NotifyAvailability(); }
    }

    private (RuleCategory[] Options, RuleRow[] Rows) Prepare(CategoryRulesReadSnapshot source)
    {
        var lookup = new CategoryLookup(source.Categories, translator);
        return ([.. lookup.All.Where(c => !c.IsArchived && c.SystemKey != DefaultCategories.Uncategorized)
            .OrderBy(c => c.Kind).ThenBy(c => c.SortOrder)
            .Select(c => new RuleCategory(c.Id, c.Kind, $"{translator[c.Kind == CategoryKind.Income ? "KindFilter_Income" : "KindFilter_Expenses"]} · {lookup.Name(c.Id)}"))],
            [.. source.Rules.OrderBy(rule => rule.Match, StringComparer.CurrentCultureIgnoreCase)
                .Select(rule => new RuleRow(rule.Id, rule.Match, lookup.Name(rule.CategoryId)))]);
    }
    private bool Owns(CategoryRulesReadSnapshot source, int revision, string display) =>
        _active && revision == Volatile.Read(ref _revision) && display == DisplayIdentity() && ReferenceEquals(source, _published);

    [RelayCommand]
    private async Task AddAsync()
    {
        if (!CanInteract || _published is not { } source) return;
        Error = null;
        if (MatchText.Trim().Length < CategoryRules.MinLength || SelectedCategory is null)
        {
            Error = translator["Rule_TextRequired"];
            return;
        }

        var category = SelectedCategory;
        if (!_ownedOptions.Any(original => ReferenceEquals(original, category))) return;
        var raw = MatchText; var draft = Volatile.Read(ref _draftRevision);
        var revision = Volatile.Read(ref _revision); var display = DisplayIdentity();
        bool Current() => Owns(source, revision, display) && draft == Volatile.Read(ref _draftRevision)
            && MatchText == raw && ReferenceEquals(SelectedCategory, category);
        _saving = true; Volatile.Write(ref _accepted, 0); NotifyAvailability();
        try
        {
            await store.SaveReviewedCategoryRuleAsync(source,
                new CategoryRule { Match = raw, CategoryId = category.Id, Kind = category.Kind }, Current,
                accepted: () => Volatile.Write(ref _accepted, 1));
            if (Owns(source, revision, display))
            {
                if (Current()) MatchText = string.Empty;
                await LoadAsync();
            }
        }
        catch (OperationCanceledException) when (!Current()) { }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            if (Owns(source, revision, display))
            {
                if (Accepted && Current()) MatchText = string.Empty;
                await CoverFailureAsync(exception);
            }
        }
        finally { _saving = false; NotifyAvailability(); }
    }

    [RelayCommand]
    private async Task DeleteAsync(RuleRow row)
    {
        if (!CanInteract || _published is not { } source || !_ownedRows.Any(original => ReferenceEquals(original, row))) return;
        var revision = Volatile.Read(ref _revision); var display = DisplayIdentity();
        bool Current() => Owns(source, revision, display) && _ownedRows.Any(original => ReferenceEquals(original, row));
        _saving = true; Volatile.Write(ref _accepted, 0); NotifyAvailability();
        try
        {
            await store.ValidateCategoryRulesSnapshotAsync(source);
            if (!Current() || !await interaction.ConfirmAsync(translator["Rule_Delete"], translator.Format("Rule_Summary", row.Match, row.CategoryName), translator["Common_Delete"], translator["Common_Cancel"]) || !Current()) return;
            await store.DeleteReviewedCategoryRuleAsync(source, row.Id, Current, accepted: () => Volatile.Write(ref _accepted, 1));
            if (Current()) await LoadAsync();
        }
        catch (OperationCanceledException) when (!Current()) { }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        { if (Current()) await CoverFailureAsync(exception); }
        finally { _saving = false; NotifyAvailability(); }
    }
    private async Task CoverFailureAsync(Exception exception)
    {
        _published = null; var failed = Loading = new();
        Feedback = Accepted ? translator["Occurrence_SavedRefreshFailed"] : CommercialFailureMessage.OrFallback(exception, translator, "Error_Unexpected");
        try { await failed.RunAsync(() => Task.FromException(exception), () => { }); }
        catch (Exception original) when (ReferenceEquals(original, exception)) { }
        NotifyAvailability();
    }
    [RelayCommand] private Task ReloadAsync() => CanReload ? LoadAsync() : Task.CompletedTask;
}
