using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentIcons.Common;
using Vafadar.Localization;
using Vafadar.Maui.Mvvm;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Money;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Features.Templates;

/// <summary>A quick template in the list.</summary>
public sealed record TemplateRow(Guid Id, string Name, string Details, Symbol Icon, Color IconColor, Color IconBackground, Color IconStroke);

/// <summary>
/// Quick templates (TX-04): created from an entry ("Save as template"), used from the entry form, deleted here.
/// </summary>
public sealed partial class TemplatesViewModel : ViewModelBase, Presentation.IThemeAware
{
    private readonly ZananceStore _store;
    private readonly Translator _translator;
    private readonly ILocalizationService _localization;
    private bool _active;
    private long _revision;
    private string? _originalFile;
    private CancellationTokenSource _lifetime = new();
    private QuickResourceReview? _published;
    private TemplateRow[] _publishedRows = [];

    /// <summary>Creates the original template list with its complete read, presentation and native action dependencies.</summary>
    public TemplatesViewModel(ZananceStore store, Translator translator, ILocalizationService localization, Interaction.IAppInteraction interaction)
    {
        _store = store; _translator = translator; _localization = localization;
        Actions = new(store, translator, interaction);
        Actions.PropertyChanged += (_, _) =>
        { OnPropertyChanged(nameof(CanInteract)); OnPropertyChanged(nameof(CanReload)); };
    }
    /// <summary>Gets the actual reviewed action flow, independent of native color presentation.</summary>
    public Commerce.QuickResourceActions Actions { get; }

    /// <summary>Gets the complete original rows, replaced together only after source and owner validation.</summary>
    [ObservableProperty]
    public partial IReadOnlyList<TemplateRow> Templates { get; private set; } = [];

    [ObservableProperty]
    public partial bool HasTemplates { get; set; }

    /// <summary>Gets local original-file feedback, including a known deletion whose following refresh failed.</summary>
    [ObservableProperty] public partial string? Feedback { get; private set; }
    /// <summary>Gets whether the complete current original page admits row commands.</summary>
    public bool CanInteract => _active && Loading.IsReady && Actions.CanInteract;
    /// <summary>Gets whether an explicit original read is available outside a pending native action.</summary>
    public bool CanReload => _active && !Loading.IsLoading && !Actions.IsBusy;

    async Task Presentation.IThemeAware.RefreshThemeAsync() { RetireRead(); if (_active) await LoadAsync(); }

    /// <summary>Gets the shared covered publication; concurrent appearance/theme loads use the same read.</summary>
    [ObservableProperty, NotifyPropertyChangedFor(nameof(CanInteract)), NotifyPropertyChangedFor(nameof(CanReload))]
    public partial SnapshotLoadState Loading { get; private set; } = new();

    /// <summary>Retires actual page/tab/root owners, original rows and pending native confirmations together.</summary>
    public void SetActive(bool active)
    {
        if (_active != active) { _active = active; if (!active) { RetireRead(); Actions.Retire(); } }
        _localization.Changed -= OnDisplayChanged;
        if (active) _localization.Changed += OnDisplayChanged;
        OnPropertyChanged(nameof(CanInteract)); OnPropertyChanged(nameof(CanReload));
    }
    private string DisplayIdentity() => $"{_localization.CurrentLanguage.CultureName}|{_localization.CurrentCulture.Name}|{_localization.CurrentCalendar}|{_localization.CurrentDigits}|{_localization.FormattingCultureName}";
    private void OnDisplayChanged(object? sender, EventArgs args) { RetireRead(); if (_active) _ = LoadAsync(); }
    private void RetireRead()
    {
        ++_revision; _published = null; _publishedRows = []; Actions.CoverTemplateCenter();
        var retired = _lifetime; _lifetime = new(); Loading = new(); retired.Cancel(); retired.Dispose();
    }
    private static void EnsureAccess(QuickResourceReview review)
    { review.EnsureCurrent(); }

    /// <summary>Reads/calculates privately and validates complete original work before one covered publication.</summary>
    public async Task LoadAsync()
    {
        if (!_active) return;
        var owner = Loading; var revision = _revision; var display = DisplayIdentity(); var token = _lifetime.Token;
        bool Current() => _active && revision == _revision && display == DisplayIdentity() && ReferenceEquals(owner, Loading);
        void RequireCurrent() { token.ThrowIfCancellationRequested(); if (!Current()) throw new OperationCanceledException("The original template-center publication retired.", token); }
        QuickResourceReview? review = null; TemplateRow[] rows = [];
        try
        {
            var pending = owner.RunAsync(async () =>
            {
                RequireCurrent(); _published = null; Actions.CoverTemplateCenter();
                review = await _store.ReadTemplateCenterAsync(path =>
                { RequireCurrent(); _originalFile ??= path; return _originalFile == path; }, token);
                RequireCurrent();
                var originals = await Task.Run(() => review.Templates.OrderBy(t => t.SortOrder).ThenBy(t => t.Name).ToArray(), token);
                RequireCurrent(); rows = RenderRows(review, originals); RequireCurrent();
                await _store.ValidateTemplateCenterAsync(review, token); RequireCurrent(); EnsureAccess(review);
            }, () =>
            {
                void Publish(Action action) { RequireCurrent(); EnsureAccess(review!); action(); RequireCurrent(); EnsureAccess(review!); }
                Publish(() => Templates = rows);
                if (!ReferenceEquals(Templates, rows)) throw new InvalidOperationException("The original template rows changed during presentation.");
                Publish(() => HasTemplates = rows.Length > 0); Publish(() => Feedback = null);
                Publish(() => Actions.PublishTemplateCenter(review!));
                _publishedRows = rows; _published = review;
            });
            OnPropertyChanged(nameof(CanInteract)); OnPropertyChanged(nameof(CanReload)); await pending;
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            if (Current()) Feedback = Actions.Accepted ? _translator["Templates_DeletedReadFailed"]
                : CommercialFailureMessage.OrFallback(exception, _translator, "Templates_ReadFailed");
        }
        finally { OnPropertyChanged(nameof(CanInteract)); OnPropertyChanged(nameof(CanReload)); }
    }
    private TemplateRow[] RenderRows(QuickResourceReview review, EntryTemplate[] originals)
    {
        var accounts = review.Accounts.ToDictionary(a => a.Id);
        var categories = new CategoryLookup(review.Categories, _translator);
        var culture = _localization.CurrentCulture;
        var prepared = new List<TemplateRow>();
        foreach (var template in originals)
        {
            var account = accounts.GetValueOrDefault(template.AccountId);
            var parts = new List<string> { _translator[$"EntryKind_{template.Kind}"] };
            if (template.Kind != EntryKind.Transfer)
            {
                parts.Add(categories.Name(template.CategoryId));
            }

            if (account is not null)
            {
                parts.Add(account.Name);
            }

            if (template.Amount is { } amount && account is not null)
            {
                parts.Add(MoneyText.Format(amount, account.CurrencyCode, culture));
            }

            // The same icon and colour as on Home and in the entry lists (category colour; slate for a transfer).
            var transfer = template.Kind == EntryKind.Transfer;
            var icon = transfer ? Symbol.ArrowSwap : Icons.Parse(template.Icon, categories.Get(template.CategoryId) is { } category ? Icons.Parse(category.Icon, Symbol.Flash) : Symbol.Flash);
            var color = transfer ? Presentation.Palette.TransferText : categories.Color(template.CategoryId);
            prepared.Add(new TemplateRow(template.Id, template.Name, string.Join(" · ", parts), icon, color, color.WithAlpha(0.12f), color.WithAlpha(0.3f)));
        }

        return prepared.ToArray();
    }

    [RelayCommand]
    private Task ReloadAsync() => LoadAsync();

    [RelayCommand]
    private async Task DeleteAsync(TemplateRow row)
    {
        var review = _published; var owner = Loading; var revision = _revision; var display = DisplayIdentity(); var token = _lifetime.Token;
        bool Current() => _active && owner.IsReady && ReferenceEquals(owner, Loading) && revision == _revision
            && display == DisplayIdentity() && review is not null && ReferenceEquals(review, _published)
            && ReferenceEquals(review, Actions.Review) && ReferenceEquals(Templates, _publishedRows)
            && _publishedRows.Any(item => ReferenceEquals(item, row));
        if (!Current() || Actions.IsBusy) return;
        try
        {
            await _store.ValidateTemplateCenterAsync(review!, token); if (!Current()) return; EnsureAccess(review!);
            await Actions.DeleteTemplateAsync(row.Id, LoadAsync);
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            if (!Current()) return;
            RetireRead(); Feedback = CommercialFailureMessage.OrFallback(exception, _translator, "Templates_ReadFailed");
            try { await Loading.RunAsync(() => Task.FromException(exception), () => { }); }
            catch (Exception failure) when (failure is not OutOfMemoryException) { /* The local original-file Retry owns expected refusal. */ }
            OnPropertyChanged(nameof(CanInteract)); OnPropertyChanged(nameof(CanReload));
        }
    }
}
