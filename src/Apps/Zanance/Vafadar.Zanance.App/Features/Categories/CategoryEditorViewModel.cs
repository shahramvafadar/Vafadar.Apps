using Microsoft.Maui.Graphics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentIcons.Common;
using Vafadar.Localization;
using Vafadar.Maui.Mvvm;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Core.Categories;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Features.Categories;

/// <summary>A selectable icon or colour swatch.</summary>
public sealed partial class Swatch(string key, Symbol icon, Color color, string label = "", string selectedLabel = "") : ObservableObject
{
    public string Key { get; } = key;

    public Symbol Icon { get; } = icon;

    public Color Color { get; } = color;

    /// <summary>Gets what a screen reader says, e.g. "Colour 3, selected" – never a hex code or an icon key.</summary>
    public string Description => IsSelected && selectedLabel.Length > 0 ? selectedLabel : label;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Description))]
    public partial bool IsSelected { get; set; }

    /// <summary>
    /// Gets or sets the outline of an icon swatch: the category colour when selected, otherwise transparent. Set by the
    /// editor, because a trigger setter with a binding stays applied after the trigger turns off (two icons looked selected).
    /// </summary>
    [ObservableProperty]
    public partial Color Outline { get; set; } = Colors.Transparent;
}

/// <summary>A possible parent category.</summary>
public sealed record ParentChoice(Guid? Id, string Name)
{
    public override string ToString() => Name;
}

/// <summary>Original category form, complete source, raw draft and native command owners (CAT-01..03).</summary>
public sealed partial class CategoryEditorViewModel : ViewModelBase, IThemeAware
{
    private static readonly string[] ColorKeys =
    [
        "#2E7D32", "#00838F", "#0277BD", "#283593", "#4527A0", "#6A1B9A", "#AD1457", "#C62828", "#E65100", "#F9A825", "#5D4037", "#546E7A",
    ];
    private readonly ZananceStore _store;
    private readonly Translator _translator;
    private readonly ILocalizationService _localization;
    private readonly Interaction.IAppInteraction _interaction;
    private Category _category = new();
    private CategoryReadSnapshot? _published;
    private CategoryReadSnapshot? _navigationSource;
    private List<Category> _all = [];
    private ParentChoice[] _ownedParents = [], _ownedTargets = [];
    private Swatch[] _ownedIcons = [], _ownedPalette = [];
    private string _snapshot = string.Empty;
    private string? _originalFile, _publishedDisplay;
    private string _iconDraftKey = "Tag", _colorDraftKey = ColorKeys[0];
    private Guid? _requestedId, _parentDraftId, _mergeDraftId;
    private volatile bool _active;
    private bool _routeCaptured, _routeRetired, _formInitialized, _publishing, _leaveAfterAccepted;
    private int _revision, _draftRevision, _accepted, _acceptedDraftRevision;
    private CancellationTokenSource _lifetime = new();

    /// <summary>Builds the original local form and its bundled choices without database work.</summary>
    public CategoryEditorViewModel(ZananceStore store, Translator translator, ILocalizationService localization, Interaction.IAppInteraction interaction)
    {
        _store = store; _translator = translator; _localization = localization; _interaction = interaction;
        _publishing = true; Title = translator["Category_NewTitle"]; Name = string.Empty;
        SelectedColor = Color.FromArgb(ColorKeys[0]); RefreshTranslatedChoices();
        _snapshot = Snapshot(); _publishing = false;
    }

    /// <summary>Gets original translated choices, replaced together beneath the read cover.</summary>
    [ObservableProperty] public partial IReadOnlyList<string> KindNames { get; private set; } = [];
    /// <summary>Gets the unchanged flex-method choices in original enum order.</summary>
    [ObservableProperty] public partial IReadOnlyList<string> SpendingTypeNames { get; private set; } = [];
    /// <summary>Gets original bundled icon identities and native accessible captions.</summary>
    [ObservableProperty] public partial IReadOnlyList<Swatch> Icons { get; private set; } = [];
    /// <summary>Gets original stored color choices and native accessible captions.</summary>
    [ObservableProperty] public partial IReadOnlyList<Swatch> Palette { get; private set; } = [];
    [ObservableProperty] public partial string Title { get; private set; }
    [ObservableProperty] public partial string Name { get; set; }
    [ObservableProperty] public partial string? DefaultName { get; private set; }
    [ObservableProperty] public partial string? NameError { get; private set; }
    [ObservableProperty] public partial int KindIndex { get; set; }
    [ObservableProperty] public partial bool IsExisting { get; private set; }
    [ObservableProperty] public partial int SpendingTypeIndex { get; set; }
    [ObservableProperty] public partial bool IsEssential { get; set; }
    [ObservableProperty] public partial bool IsArchived { get; private set; }
    [ObservableProperty] public partial bool CanHaveParent { get; private set; }
    [ObservableProperty] public partial IReadOnlyList<ParentChoice> Parents { get; private set; } = [];
    [ObservableProperty] public partial ParentChoice? Parent { get; set; }
    [ObservableProperty] public partial Color SelectedColor { get; private set; }
    [ObservableProperty] public partial IReadOnlyList<ParentChoice> MergeTargets { get; private set; } = [];
    [ObservableProperty] public partial ParentChoice? MergeTarget { get; set; }
    /// <summary>Gets one local source/write failure while the raw form remains retained.</summary>
    [ObservableProperty] public partial string? Feedback { get; private set; }
    /// <summary>Gets the complete original read cover and explicit Retry state.</summary>
    [ObservableProperty, NotifyPropertyChangedFor(nameof(CanInteract)), NotifyPropertyChangedFor(nameof(CanReload))]
    public partial SnapshotLoadState Loading { get; private set; } = new();
    /// <summary>Gets whether this actual original form may issue a command.</summary>
    public bool CanInteract => _active && !_routeRetired && !IsBusy && Loading.IsReady && _published is not null && _publishedDisplay == DisplayIdentity();
    /// <summary>Gets whether the same original file may be explicitly reread.</summary>
    public bool CanReload => _active && !_routeRetired && !IsBusy && !Loading.IsLoading;
    /// <summary>Gets known writer acceptance independently of subsequent read, notification or navigation failure.</summary>
    public bool Accepted => Volatile.Read(ref _accepted) != 0;
    /// <summary>Gets whether the original raw user draft differs from its accepted baseline.</summary>
    public bool IsDirty => Snapshot() != _snapshot;
    /// <summary>Gets whether original expense-specific fields are available for this draft.</summary>
    public bool ShowSpendingType => Kind == CategoryKind.Expense && (!CanHaveParent || Parent?.Id is null);
    private CategoryKind Kind => KindIndex == 1 ? CategoryKind.Income : CategoryKind.Expense;
    private string DisplayIdentity() => $"{_localization.CurrentLanguage.CultureName}|{_localization.CurrentCulture.Name}|{_localization.CurrentCalendar}|{_localization.CurrentDigits}|{_localization.FormattingCultureName}";
    private void Edited() { if (!_publishing) { Interlocked.Increment(ref _draftRevision); OnPropertyChanged(nameof(IsDirty)); } }
    partial void OnNameChanged(string value) => Edited();
    partial void OnSpendingTypeIndexChanged(int value) => Edited();
    partial void OnIsEssentialChanged(bool value) => Edited();
    partial void OnParentChanged(ParentChoice? value)
    { OnPropertyChanged(nameof(ShowSpendingType)); if (!_publishing) _parentDraftId = value?.Id; Edited(); }
    partial void OnMergeTargetChanged(ParentChoice? value)
    { if (!_publishing) _mergeDraftId = value?.Id; Edited(); }
    partial void OnCanHaveParentChanged(bool value) => OnPropertyChanged(nameof(ShowSpendingType));
    partial void OnKindIndexChanged(int value)
    {
        OnPropertyChanged(nameof(ShowSpendingType)); Edited();
        if (!_publishing && !IsExisting && _formInitialized)
        {
            _parentDraftId = null; _publishing = true;
            try { BuildParents(); } finally { _publishing = false; }
        }
    }
    partial void OnSelectedColorChanged(Color value) => UpdateIconOutlines();

    /// <summary>Captures the native route once; a missing requested category is never a request to create another one.</summary>
    public async Task ApplyQueryAsync(IDictionary<string, object> query, bool load = true)
    {
        ArgumentNullException.ThrowIfNull(query);
        var hasId = query.TryGetValue("id", out var value);
        var requested = value is Guid id ? id : (Guid?)null;
        var hasSource = query.TryGetValue("categorySource", out var originalSource);
        var expected = originalSource as CategoryReadSnapshot;
        query.Clear();
        if (_routeCaptured)
        {
            if (requested != _requestedId || hasId && requested is null || hasSource && expected is null
                || expected is not null && _originalFile is not null && expected.DatabasePath != _originalFile)
            { _routeRetired = true; RetireRead(); await CoverFailureAsync(new CategorySourceUnavailableException()); }
            return;
        }
        _routeCaptured = true; _requestedId = requested;
        // A refused existing-category read must not present itself as the new-category route.
        Title = _translator[requested is null ? "Category_NewTitle" : "Category_EditTitle"];
        _navigationSource = expected; _originalFile = expected?.DatabasePath;
        if (hasId && requested is null || hasSource && expected is null) _routeRetired = true;
        if (_active && load) await LoadAsync();
    }
    /// <summary>Retires original read, draft/consent and writer authority on actual page/tab/root departure.</summary>
    public void SetActive(bool active)
    {
        _localization.Changed -= OnDisplayChanged;
        if (active) _localization.Changed += OnDisplayChanged;
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
    { OnPropertyChanged(nameof(CanInteract)); OnPropertyChanged(nameof(CanReload)); OnPropertyChanged(nameof(Accepted)); OnPropertyChanged(nameof(IsDirty)); }

    /// <summary>Publishes one complete actual-file form source while preserving an already initialized raw draft.</summary>
    public async Task LoadAsync()
    {
        if (!_active) return;
        if (!_routeCaptured) { _routeCaptured = true; _requestedId = null; }
        var owner = Loading; var revision = Volatile.Read(ref _revision); var display = DisplayIdentity(); var token = _lifetime.Token;
        bool Current() => _active && revision == Volatile.Read(ref _revision) && display == DisplayIdentity() && ReferenceEquals(owner, Loading);
        void RequireCurrent() { token.ThrowIfCancellationRequested(); if (!Current()) throw new OperationCanceledException("The original category editor read retired.", token); }
        CategoryReadSnapshot? source = null; Category? category = null;
        try
        {
            var pending = owner.RunAsync(async () =>
            {
                _published = null; RequireCurrent();
                if (_routeRetired) throw new CategorySourceUnavailableException();
                source = await _store.ReadCategorySnapshotAsync(path => { RequireCurrent(); _originalFile ??= path; return _originalFile == path; }, token, _navigationSource);
                RequireCurrent(); category = _requestedId is { } id ? source.Categories.SingleOrDefault(item => item.Id == id) : null;
                if (_requestedId is not null && category is null || _formInitialized && IsExisting && (category is null || !SameCategory(_category, category)))
                    throw new CategorySourceUnavailableException();
                if (!_formInitialized && _requestedId is null && source.Categories.Any(item => item.Id == _category.Id))
                    throw new CategorySourceUnavailableException();
                await _store.ValidateCategorySnapshotAsync(source, token); RequireCurrent();
            }, () =>
            {
                RequireCurrent(); _publishing = true;
                try
                {
                    _all = source!.Categories.ToList();
                    if (!_formInitialized)
                    {
                        IsExisting = category is not null; RequireCurrent();
                        if (category is not null) _category = Clone(category);
                        KindIndex = _category.Kind == CategoryKind.Income ? 1 : 0; RequireCurrent();
                        SpendingTypeIndex = (int)_category.SpendingType; RequireCurrent(); IsEssential = _category.IsEssential; RequireCurrent();
                        Name = _category.Name ?? string.Empty; RequireCurrent();
                        _parentDraftId = _category.ParentId; _iconDraftKey = _category.Icon ?? "Tag"; _colorDraftKey = _category.Color ?? ColorKeys[0];
                        SelectedColor = IsExisting ? CategoryLookup.ParseColor(_category.Color) : Color.FromArgb(_colorDraftKey); RequireCurrent();
                    }
                    Title = _translator[IsExisting ? "Category_EditTitle" : "Category_NewTitle"]; RequireCurrent();
                    DefaultName = _category.SystemKey is { } key ? _translator[$"Category_{key}"] : null; RequireCurrent();
                    IsArchived = _category.IsArchived; RequireCurrent();
                    CanHaveParent = _category.SystemKey != DefaultCategories.Uncategorized && !_all.Any(item => item.ParentId == _category.Id); RequireCurrent();
                    RefreshTranslatedChoices(); RequireCurrent(); BuildParents(); RequireCurrent(); BuildMergeTargets(); RequireCurrent();
                    if (!_formInitialized) { _snapshot = Snapshot(); _formInitialized = true; _navigationSource = null; }
                    Feedback = null; RequireCurrent(); _publishedDisplay = display; _published = source;
                }
                finally { _publishing = false; }
            });
            NotifyAvailability(); await pending;
            if (Current() && Loading.IsReady && _leaveAfterAccepted)
            {
                _leaveAfterAccepted = false;
                if (_acceptedDraftRevision == Volatile.Read(ref _draftRevision)) await _interaction.NavigateAsync("..");
            }
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            if (Current()) Feedback = Accepted ? _translator["Occurrence_SavedRefreshFailed"] : exception is CategorySourceUnavailableException
                ? _translator["Category_Unavailable"] : CommercialFailureMessage.OrFallback(exception, _translator, "Error_Unexpected");
        }
        finally { NotifyAvailability(); }
    }
    private void RefreshTranslatedChoices()
    {
        KindNames = [_translator["CategoryKind_Expense"], _translator["CategoryKind_Income"]];
        SpendingTypeNames = [_translator["SpendingType_Flexible"], _translator["SpendingType_Fixed"], _translator["SpendingType_NonMonthly"]];
        _ownedIcons = [.. IconCatalog.Keys.Select(key => Named(key, Presentation.Icons.Parse(key, Symbol.Tag), _translator[$"Icon_{key}"]))];
        _ownedPalette = [.. ColorKeys.Select((key, i) => Named(key, Symbol.Circle, _translator.Format("Category_ColorOption", i + 1), Color.FromArgb(key)))];
        Select(_ownedIcons, _iconDraftKey); Select(_ownedPalette, _colorDraftKey);
        Icons = Array.AsReadOnly(_ownedIcons); Palette = Array.AsReadOnly(_ownedPalette); UpdateIconOutlines();
    }
    private void BuildParents()
    {
        var choices = _all.Where(item => item.Kind == Kind && item.ParentId is null && !item.IsArchived
            && item.Id != _category.Id && item.SystemKey != DefaultCategories.Uncategorized).OrderBy(item => item.SortOrder).ToList();
        // Retain the actual stored parent even when archived; changing a name must not silently detach a child.
        if (_category.ParentId is { } retained && choices.All(item => item.Id != retained)
            && _all.SingleOrDefault(item => item.Id == retained) is { } originalParent) choices.Add(originalParent);
        _ownedParents = [new(null, _translator["Category_NoParent"]), .. choices.Select(item => new ParentChoice(item.Id, CategoryLookup.NameOf(item, _translator)))];
        Parents = Array.AsReadOnly(_ownedParents); Parent = null; Parent = _ownedParents.FirstOrDefault(item => item.Id == _parentDraftId);
    }
    private void BuildMergeTargets()
    {
        _ownedTargets = [.. _all.Where(item => IsExisting && item.Kind == _category.Kind && item.Id != _category.Id && !item.IsArchived)
            .OrderBy(item => item.SortOrder).Select(item => new ParentChoice(item.Id, CategoryLookup.NameOf(item, _translator)))];
        MergeTargets = Array.AsReadOnly(_ownedTargets); MergeTarget = null; MergeTarget = _ownedTargets.FirstOrDefault(item => item.Id == _mergeDraftId);
    }
    private static void Select(IEnumerable<Swatch> swatches, string key)
    { foreach (var swatch in swatches) swatch.IsSelected = string.Equals(swatch.Key, key, StringComparison.OrdinalIgnoreCase); }
    private Swatch Named(string key, Symbol icon, string label, Color? color = null) =>
        new(key, icon, color ?? Colors.Transparent, label, _translator.Format("Common_SelectedItem", label));
    private void UpdateIconOutlines()
    { foreach (var icon in Icons) icon.Outline = icon.IsSelected ? SelectedColor : Colors.Transparent; }
    [RelayCommand] private void SelectIcon(Swatch swatch)
    {
        if (!CanInteract || !_ownedIcons.Any(item => ReferenceEquals(item, swatch))) return;
        _iconDraftKey = swatch.Key; Select(_ownedIcons, swatch.Key); UpdateIconOutlines(); Edited();
    }
    [RelayCommand] private void SelectColor(Swatch swatch)
    {
        if (!CanInteract || !_ownedPalette.Any(item => ReferenceEquals(item, swatch))) return;
        _colorDraftKey = swatch.Key; Select(_ownedPalette, swatch.Key); SelectedColor = swatch.Color; Edited();
    }
    private bool Owns(CategoryReadSnapshot source, int revision, string display) =>
        _active && !_routeRetired && revision == Volatile.Read(ref _revision) && display == DisplayIdentity() && ReferenceEquals(source, _published);

    [RelayCommand] private async Task SaveAsync()
    {
        if (!CanInteract) return;
        NameError = string.IsNullOrWhiteSpace(Name) && DefaultName is null ? _translator["Category_NameRequired"] : null;
        var wanted = Vafadar.Core.Text.SearchText.Normalize(string.IsNullOrWhiteSpace(Name) ? DefaultName : Name);
        var kind = IsExisting ? _category.Kind : Kind; var parent = CanHaveParent ? Parent?.Id : null;
        if (NameError is null && _all.Any(item => item.Id != _category.Id && item.Kind == kind && item.ParentId == parent
            && string.Equals(Vafadar.Core.Text.SearchText.Normalize(CategoryLookup.NameOf(item, _translator)), wanted, StringComparison.CurrentCultureIgnoreCase)))
            NameError = _translator["Category_NameTaken"];
        if (NameError is not null) return;
        if (CanHaveParent && (Parent is null || !_ownedParents.Any(item => ReferenceEquals(item, Parent))))
        { await CoverFailureAsync(new CategorySourceUnavailableException()); return; }
        var input = Clone(_category);
        input.Name = string.IsNullOrWhiteSpace(Name) || Name.Trim() == DefaultName ? null : Name.Trim();
        if (!IsExisting) { input.Kind = Kind; input.SortOrder = _all.Count == 0 ? 0 : _all.Max(item => item.SortOrder) + 1; }
        input.Icon = _iconDraftKey; input.Color = _colorDraftKey; input.ParentId = parent;
        input.SpendingType = Enum.IsDefined((SpendingType)SpendingTypeIndex) ? (SpendingType)SpendingTypeIndex : SpendingType.Flexible;
        input.IsEssential = ShowSpendingType && IsEssential;
        var originalId = IsExisting ? _category.Id : (Guid?)null;
        await ExecuteWriteAsync(async (source, current, accepted) => await _store.SaveReviewedCategoryAsync(source, input, originalId, current, accepted: accepted), savedDraft: true);
    }
    [RelayCommand] private Task ToggleArchiveAsync()
    {
        if (!CanInteract || !IsExisting) return Task.CompletedTask;
        var input = Clone(_category); input.IsArchived = !IsArchived; var archive = !IsArchived;
        return ExecuteWriteAsync(async (source, current, accepted) =>
        {
            if (archive && (!await _interaction.ConfirmAsync(_translator["Category_Archive"], _translator["Category_ArchiveMessage"], _translator["Category_Archive"], _translator["Common_Cancel"]) || !current())) return null;
            return await _store.SaveReviewedCategoryAsync(source, input, _category.Id, current, accepted: accepted);
        });
    }
    [RelayCommand] private Task MergeAsync()
    {
        if (!CanInteract || !IsExisting || MergeTarget is not { Id: { } target } choice || !_ownedTargets.Any(item => ReferenceEquals(item, choice))) return Task.CompletedTask;
        return ExecuteWriteAsync(async (source, current, accepted) =>
        {
            var review = await _store.ReadCategoryMergeReviewAsync(source, _category.Id, target, current, _lifetime.Token);
            if (!current() || !await _interaction.ConfirmAsync(_translator["Category_Merge"], _translator.Format("Category_MergeMessage", choice.Name), _translator["Category_Merge"], _translator["Common_Cancel"]) || !current()) return null;
            return await _store.MergeReviewedCategoryAsync(review, current, accepted: accepted);
        });
    }
    private async Task ExecuteWriteAsync(Func<CategoryReadSnapshot, Func<bool>, Action, Task<CategoryWriteReceipt?>> write, bool savedDraft = false)
    {
        if (!CanInteract || _published is not { } source) return;
        var revision = Volatile.Read(ref _revision); var draft = Volatile.Read(ref _draftRevision); var display = DisplayIdentity();
        var submittedFingerprint = Snapshot();
        bool PageOwner() => _active && !_routeRetired && revision == Volatile.Read(ref _revision) && display == DisplayIdentity();
        bool Current() => Owns(source, revision, display) && draft == Volatile.Read(ref _draftRevision);
        IsBusy = true; Volatile.Write(ref _accepted, 0); NotifyAvailability();
        void Adopt(CategoryWriteReceipt receipt)
        {
            _category = Clone(receipt.Source.Categories.Single(item => item.Id == receipt.CategoryId));
            _requestedId = _category.Id; _published = receipt.Source; IsExisting = true; IsArchived = _category.IsArchived;
            _acceptedDraftRevision = draft; _leaveAfterAccepted = true;
            // Acceptance changes the baseline, not newer typing; a later revert must still remain an unsaved draft.
            if (savedDraft) _snapshot = submittedFingerprint;
        }
        try
        {
            var receipt = await write(source, Current, () => Volatile.Write(ref _accepted, 1));
            if (receipt is null || !PageOwner()) return;
            Adopt(receipt);
            if (draft == Volatile.Read(ref _draftRevision)) { await _interaction.NavigateAsync(".."); _leaveAfterAccepted = false; }
            else await LoadAsync();
        }
        catch (CommittedWriteNotificationException<CategoryWriteReceipt> exception)
        { if (PageOwner()) { Adopt(exception.Result); await CoverFailureAsync(exception); } }
        catch (OperationCanceledException) when (!Current()) { }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        { if (PageOwner()) await CoverFailureAsync(exception); }
        finally { IsBusy = false; NotifyAvailability(); }
    }
    [RelayCommand] private Task MoveUpAsync() => MoveAsync(-1);
    [RelayCommand] private Task MoveDownAsync() => MoveAsync(1);
    private async Task MoveAsync(int step)
    {
        if (!CanInteract || !IsExisting || _published is not { } source) return;
        var revision = Volatile.Read(ref _revision); var draft = Volatile.Read(ref _draftRevision); var display = DisplayIdentity();
        bool PageOwner() => _active && !_routeRetired && revision == Volatile.Read(ref _revision) && display == DisplayIdentity();
        bool Current() => Owns(source, revision, display) && draft == Volatile.Read(ref _draftRevision);
        IsBusy = true; Volatile.Write(ref _accepted, 0); NotifyAvailability();
        void Adopt(CategoryMoveReceipt receipt)
        { _category = Clone(receipt.Source.Categories.Single(item => item.Id == _category.Id)); _published = receipt.Source; }
        try
        {
            var receipt = await _store.MoveReviewedCategoryAsync(source, _category.Id, step, Current, accepted: () => Volatile.Write(ref _accepted, 1));
            if (!PageOwner() || !receipt.Changed) return;
            Adopt(receipt); await LoadAsync();
        }
        catch (CommittedWriteNotificationException<CategoryMoveReceipt> exception)
        { if (PageOwner()) { Adopt(exception.Result); await CoverFailureAsync(exception); } }
        catch (OperationCanceledException) when (!Current()) { }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        { if (PageOwner()) await CoverFailureAsync(exception); }
        finally { IsBusy = false; NotifyAvailability(); }
    }
    private async Task CoverFailureAsync(Exception exception)
    {
        _published = null; var failed = Loading = new();
        Feedback = Accepted ? _translator["Occurrence_SavedRefreshFailed"] : exception is CategorySourceUnavailableException
            ? _translator["Category_Unavailable"] : CommercialFailureMessage.OrFallback(exception, _translator, "Error_Unexpected");
        try { await failed.RunAsync(() => Task.FromException(exception), () => { }); }
        catch (Exception original) when (ReferenceEquals(original, exception)) { }
        NotifyAvailability();
    }
    [RelayCommand] private Task ReloadAsync() => CanReload ? LoadAsync() : Task.CompletedTask;
    [RelayCommand] private async Task CancelAsync()
    { if (await ConfirmDiscardAsync()) await _interaction.NavigateAsync(".."); }
    /// <summary>Returns consent only while the same actual page, route, display and raw draft still own it.</summary>
    public async Task<bool> ConfirmDiscardAsync()
    {
        var revision = Volatile.Read(ref _revision); var draft = Volatile.Read(ref _draftRevision); var display = DisplayIdentity();
        bool Current() => _active && revision == Volatile.Read(ref _revision) && draft == Volatile.Read(ref _draftRevision) && display == DisplayIdentity();
        if (!Current()) return false;
        if (!IsDirty) return true;
        return await _interaction.ConfirmAsync(_translator["Common_DiscardTitle"], _translator["Common_DiscardMessage"], _translator["Common_Discard"], _translator["Common_KeepEditing"]) && Current();
    }
    private string Snapshot() => DraftFingerprint.Create(Name, KindIndex, SpendingTypeIndex, IsEssential, _parentDraftId, _iconDraftKey, _colorDraftKey);
    private static Category Clone(Category item) => new(item.Id)
    {
        Kind = item.Kind, Name = item.Name, SystemKey = item.SystemKey, ParentId = item.ParentId, Color = item.Color, Icon = item.Icon,
        SpendingType = item.SpendingType, IsEssential = item.IsEssential, IsArchived = item.IsArchived, SortOrder = item.SortOrder,
        CreatedAt = item.CreatedAt, UpdatedAt = item.UpdatedAt,
    };
    private static bool SameCategory(Category a, Category b) => a.Id == b.Id && a.Kind == b.Kind && a.Name == b.Name
        && a.SystemKey == b.SystemKey && a.ParentId == b.ParentId && a.Color == b.Color && a.Icon == b.Icon
        && a.SpendingType == b.SpendingType && a.IsEssential == b.IsEssential && a.IsArchived == b.IsArchived
        && a.SortOrder == b.SortOrder && a.CreatedAt.EqualsExact(b.CreatedAt) && a.UpdatedAt.EqualsExact(b.UpdatedAt);
    private sealed class CategorySourceUnavailableException : InvalidOperationException { }
}
