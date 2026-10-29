using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentIcons.Common;
using Vafadar.Localization;
using Vafadar.Maui.Mvvm;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Core.Categories;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Features.Categories;

/// <summary>A category in the list; sub-categories are indented under their parent.</summary>
public sealed record CategoryItem(Guid Id, string Name, Symbol Icon, Color Color, Color Background, bool IsChild);

/// <summary>Original category overview with one complete file, privately staged rows and exact navigation owners.</summary>
public sealed partial class CategoriesViewModel(ZananceStore store, Translator translator,
    ILocalizationService localization, Interaction.IAppInteraction interaction) : ViewModelBase, IThemeAware
{
    private CategoryReadSnapshot? _published;
    private CategoryItem[] _ownedRows = [];
    private string? _originalFile, _publishedDisplay;
    private volatile bool _active;
    private int _revision;
    private CancellationTokenSource _lifetime = new();
    /// <summary>Gets all original active expense rows, published together.</summary>
    [ObservableProperty] public partial IReadOnlyList<CategoryItem> Expense { get; private set; } = [];
    /// <summary>Gets all original active income rows, published together.</summary>
    [ObservableProperty] public partial IReadOnlyList<CategoryItem> Income { get; private set; } = [];
    /// <summary>Gets all original retained archived definitions.</summary>
    [ObservableProperty] public partial IReadOnlyList<CategoryItem> Archived { get; private set; } = [];
    [ObservableProperty] public partial bool HasArchived { get; private set; }
    /// <summary>Gets one local read failure without interactive stale rows.</summary>
    [ObservableProperty] public partial string? Feedback { get; private set; }
    /// <summary>Gets the whole original source's loading/error and explicit Retry state.</summary>
    [ObservableProperty, NotifyPropertyChangedFor(nameof(CanInteract)), NotifyPropertyChangedFor(nameof(CanReload))]
    public partial SnapshotLoadState Loading { get; private set; } = new();
    /// <summary>Gets whether original published rows and commands still belong to this actual page/display.</summary>
    public bool CanInteract => _active && Loading.IsReady && _published is not null && _publishedDisplay == DisplayIdentity();
    /// <summary>Gets whether the original first file may be explicitly reread.</summary>
    public bool CanReload => _active && !Loading.IsLoading;
    private string DisplayIdentity() => $"{localization.CurrentLanguage.CultureName}|{localization.CurrentCulture.Name}|{localization.CurrentCalendar}|{localization.CurrentDigits}|{localization.FormattingCultureName}";
    /// <summary>Retires original source and row navigation on actual page/tab/root departure.</summary>
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
    private void NotifyAvailability() { OnPropertyChanged(nameof(CanInteract)); OnPropertyChanged(nameof(CanReload)); }

    /// <summary>Stages the unchanged original hierarchy privately and publishes one freshly validated actual-file packet.</summary>
    public async Task LoadAsync()
    {
        if (!_active) return;
        var owner = Loading; var revision = Volatile.Read(ref _revision); var display = DisplayIdentity(); var token = _lifetime.Token;
        bool Current() => _active && revision == Volatile.Read(ref _revision) && display == DisplayIdentity() && ReferenceEquals(owner, Loading);
        void RequireCurrent() { token.ThrowIfCancellationRequested(); if (!Current()) throw new OperationCanceledException("The original category overview retired.", token); }
        CategoryReadSnapshot? source = null; Prepared? prepared = null;
        try
        {
            var pending = owner.RunAsync(async () =>
            {
                _published = null; RequireCurrent();
                source = await store.ReadCategorySnapshotAsync(path => { RequireCurrent(); _originalFile ??= path; return _originalFile == path; }, token);
                RequireCurrent(); prepared = await Task.Run(() => Prepare(source.Categories), token); RequireCurrent();
                await store.ValidateCategorySnapshotAsync(source, token); RequireCurrent();
            }, () =>
            {
                RequireCurrent();
                // Native color resolution uses this actual current theme; no financial query or incremental row publication.
                var expense = prepared!.Expense.Select(item => ToItem(item.Category, item.IsChild)).ToArray();
                var income = prepared.Income.Select(item => ToItem(item.Category, item.IsChild)).ToArray();
                var archived = prepared.Archived.Select(item => ToItem(item, false)).ToArray(); RequireCurrent();
                Expense = Array.AsReadOnly(expense); RequireCurrent(); Income = Array.AsReadOnly(income); RequireCurrent();
                Archived = Array.AsReadOnly(archived); RequireCurrent(); HasArchived = archived.Length > 0; RequireCurrent();
                _ownedRows = [.. expense, .. income, .. archived]; Feedback = null; RequireCurrent();
                _publishedDisplay = display; _published = source;
            });
            NotifyAvailability(); await pending;
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        { if (Current()) Feedback = CommercialFailureMessage.OrFallback(exception, translator, "Error_Unexpected"); }
        finally { NotifyAvailability(); }
    }
    private sealed record Row(Category Category, bool IsChild);
    private sealed record Prepared(List<Row> Expense, List<Row> Income, List<Category> Archived);
    private static Prepared Prepare(IReadOnlyList<Category> categories)
    {
        var expense = new List<Row>(); var income = new List<Row>();
        foreach (var parent in categories.Where(item => !item.IsArchived && item.ParentId is null).OrderBy(item => item.SortOrder))
        {
            var target = parent.Kind == CategoryKind.Income ? income : expense; target.Add(new(parent, false));
            foreach (var child in categories.Where(item => !item.IsArchived && item.ParentId == parent.Id).OrderBy(item => item.SortOrder)) target.Add(new(child, true));
        }
        var shownParents = categories.Where(item => !item.IsArchived && item.ParentId is null).Select(item => item.Id).ToHashSet();
        foreach (var orphan in categories.Where(item => !item.IsArchived && item.ParentId is { } parent && !shownParents.Contains(parent)).OrderBy(item => item.SortOrder))
            (orphan.Kind == CategoryKind.Income ? income : expense).Add(new(orphan, false));
        return new(expense, income, [.. categories.Where(item => item.IsArchived)]);
    }
    private CategoryItem ToItem(Category category, bool isChild)
    {
        var color = CategoryLookup.DisplayColor(category.Color);
        return new(category.Id, CategoryLookup.NameOf(category, translator), Presentation.Icons.Parse(category.Icon, Symbol.Tag), color, color.WithAlpha(0.12f), isChild);
    }
    private async Task NavigateAsync(CategoryItem? row, string route, bool editor)
    {
        if (!CanInteract || _published is not { } source || row is not null && !_ownedRows.Any(item => ReferenceEquals(item, row))) return;
        var revision = Volatile.Read(ref _revision); var display = DisplayIdentity();
        bool Current() => _active && revision == Volatile.Read(ref _revision) && display == DisplayIdentity() && ReferenceEquals(source, _published);
        try
        {
            await store.ValidateCategorySnapshotAsync(source); if (!Current() || !CanInteract) return;
            IDictionary<string, object>? query = editor ? new Dictionary<string, object> { ["categorySource"] = source } : null;
            if (row is not null) query!["id"] = row.Id;
            await interaction.NavigateAsync(route, query);
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            if (!Current()) return;
            _published = null; var failed = Loading = new(); Feedback = CommercialFailureMessage.OrFallback(exception, translator, "Error_Unexpected");
            try { await failed.RunAsync(() => Task.FromException(exception), () => { }); }
            catch (Exception original) when (ReferenceEquals(original, exception)) { }
            NotifyAvailability();
        }
    }
    [RelayCommand] private Task OpenAsync(CategoryItem item) => NavigateAsync(item, AppShell.CategoryEditorRoute, true);
    [RelayCommand] private Task AddAsync() => NavigateAsync(null, AppShell.CategoryEditorRoute, true);
    [RelayCommand] private Task OpenRulesAsync() => NavigateAsync(null, AppShell.RulesRoute, false);
    [RelayCommand] private Task ReloadAsync() => CanReload ? LoadAsync() : Task.CompletedTask;
}
