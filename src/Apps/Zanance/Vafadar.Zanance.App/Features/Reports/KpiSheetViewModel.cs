using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vafadar.Localization;
using Vafadar.Maui.Mvvm;

namespace Vafadar.Zanance.App.Features.Reports;

/// <summary>What the KPI sheet shows: the number, the scope it was computed with, its parts and the way to its entries.</summary>
/// <param name="Id">The KPI id of the catalog, e.g. "K05"; its texts are <c>Kpi_{Id}_Title</c>, <c>_Question</c>, <c>_Definition</c>, <c>_Included</c>, <c>_Excluded</c>.</param>
/// <param name="Value">The number as shown on the card.</param>
/// <param name="Scope">The scope, e.g. "October · EUR · Accounts in totals".</param>
/// <param name="Lines">The number in words: its parts.</param>
/// <param name="DataStatus">The data status of the view.</param>
/// <param name="ShowEntries">Opens the entries or the screen behind the number, or <see langword="null"/>.</param>
public sealed record KpiExplanation(string Id, string Value, string Scope, IReadOnlyList<AmountLine> Lines, string? DataStatus, Func<Task>? ShowEntries,
    Func<Task>? ValidateSource = null, Func<bool>? SourceCurrent = null, Func<bool>? HandoffCurrent = null);

/// <summary>
/// The explanation of a KPI (ZEX-UI14, ZEX-S0609): question, definition in words, what is included and left out, the
/// data status and "Show entries". It names the scope the number was computed with; there is no accuracy score.
/// </summary>
public sealed partial class KpiSheetViewModel : ViewModelBase, Presentation.IThemeAware
{
    private readonly Translator _translator;
    private readonly IKpiSheetInteraction _interaction;
    private KpiExplanation? _sheet;
    private long _revision;
    private bool _active = true, _retired, _closingForEntries;
    private Func<bool>? _rootOwner;
    /// <summary>Retains original text/close ports without performing work at construction.</summary>
    public KpiSheetViewModel(Translator translator, IKpiSheetInteraction? interaction = null)
    {
        _translator = translator; _interaction = interaction ?? new MauiKpiSheetInteraction();
        translator.PropertyChanged += OnDisplayChanged;
        PropertyChanged += (_, args) => { if (args.PropertyName == nameof(IsBusy)) PublishAvailability(); };
    }
    /// <summary>Binds only the actual original root, supplied through a native weak owner check.</summary>
    internal void BindRootOwner(Func<bool> owner) { _rootOwner = owner ?? throw new ArgumentNullException(nameof(owner)); PublishAvailability(); }
    /// <summary>Retires native query commands on ordinary departure, preserving only an explicitly pending own-close handoff.</summary>
    public void SetActive(bool active)
    {
        if (_active == active) return; _active = active;
        if (!active && !_closingForEntries) ++_revision;
        _translator.PropertyChanged -= OnDisplayChanged; if (active && !_retired) _translator.PropertyChanged += OnDisplayChanged;
        PublishAvailability();
    }
    /// <summary>Permanently retires original root and all pending explanation callbacks.</summary>
    public void RetireRoot() { _retired = true; ++_revision; SetActive(false); PublishAvailability(); }
    private bool OwnsRoot => !_retired && (_rootOwner?.Invoke() ?? true);
    /// <summary>Gets whether this original explanation has a captured entries target, even while temporarily busy.</summary>
    public bool HasEntries => _sheet?.ShowEntries is not null;
    /// <summary>Gets native entries authority for only the current query/page/root/source.</summary>
    public bool CanShowEntries => OwnsRoot && _active && !IsBusy && HasEntries && _sheet?.SourceCurrent?.Invoke() != false;
    /// <summary>Gets whether normal explanation Back may start without racing another native close.</summary>
    public bool CanClose => OwnsRoot && _active && !IsBusy;
    /// <summary>Gets current translated source-expiry feedback without replacing original displayed values.</summary>
    [ObservableProperty] public partial string? Feedback { get; private set; }
    private void OnDisplayChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs args)
    {
        if (!_active || !OwnsRoot || _sheet is null) return; RefreshCaptions(_sheet);
        if (_sheet.SourceCurrent?.Invoke() == false || Feedback is not null) Feedback = _translator["Report_ExplanationStale"];
        PublishAvailability();
    }
    /// <summary>Refreshes current explanation captions and source-expiry feedback without rebuilding its captured values.</summary>
    Task Presentation.IThemeAware.RefreshThemeAsync() { OnDisplayChanged(this, new System.ComponentModel.PropertyChangedEventArgs(string.Empty)); return Task.CompletedTask; }
    private void PublishAvailability()
    {
        OnPropertyChanged(nameof(HasEntries)); OnPropertyChanged(nameof(CanShowEntries)); OnPropertyChanged(nameof(CanClose));
        ShowEntriesCommand.NotifyCanExecuteChanged(); CloseCommand.NotifyCanExecuteChanged();
    }
    private void RefreshCaptions(KpiExplanation sheet)
    {
        Title = _translator[$"Kpi_{sheet.Id}_Title"]; Question = _translator[$"Kpi_{sheet.Id}_Question"];
        Definition = _translator[$"Kpi_{sheet.Id}_Definition"]; Included = Split(_translator[$"Kpi_{sheet.Id}_Included"]); Excluded = Split(_translator[$"Kpi_{sheet.Id}_Excluded"]);
    }

    [ObservableProperty]
    public partial string? Title { get; set; }

    [ObservableProperty]
    public partial string? Value { get; set; }

    [ObservableProperty]
    public partial string? Scope { get; set; }

    [ObservableProperty]
    public partial string? Question { get; set; }

    [ObservableProperty]
    public partial string? Definition { get; set; }

    [ObservableProperty]
    public partial IReadOnlyList<AmountLine> Lines { get; set; } = [];

    [ObservableProperty]
    public partial bool HasLines { get; set; }

    [ObservableProperty]
    public partial IReadOnlyList<string> Included { get; set; } = [];

    [ObservableProperty]
    public partial IReadOnlyList<string> Excluded { get; set; } = [];

    [ObservableProperty]
    public partial string? DataStatus { get; set; }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (query.TryGetValue("sheet", out var value) && value is KpiExplanation sheet)
        {
            ++_revision; _sheet = sheet with { Lines = sheet.Lines.ToArray() }; Feedback = null;
            RefreshCaptions(_sheet); Value = string.IsNullOrEmpty(sheet.Value) ? null : sheet.Value; Scope = sheet.Scope;
            Lines = _sheet.Lines; HasLines = Lines.Count > 0; DataStatus = sheet.DataStatus; PublishAvailability();
        }

        else
        {
            ++_revision; _sheet = null; Title = null; Value = null; Scope = null; Question = null; Definition = null;
            Lines = []; HasLines = false; Included = []; Excluded = []; DataStatus = null;
            Feedback = _translator["Report_ExplanationStale"]; PublishAvailability();
        }

        query.Clear();
    }

    // Lines of a list are separated by "|" in the resources, so each language keeps its own count of items.
    private static string[] Split(string text) => [.. text.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)];

    [RelayCommand(CanExecute = nameof(CanShowEntries))]
    private async Task ShowEntriesAsync()
    {
        if (!CanShowEntries || _sheet is not { ShowEntries: { } show } sheet)
        {
            if (OwnsRoot && _active && !IsBusy && HasEntries && _sheet?.SourceCurrent?.Invoke() == false) Feedback = _translator["Report_ExplanationStale"];
            PublishAvailability(); return;
        }
        var revision = _revision;
        bool Current(bool requireActive) => OwnsRoot && revision == _revision && ReferenceEquals(sheet, _sheet)
            && (!requireActive || _active) && sheet.SourceCurrent?.Invoke() != false;
        IsBusy = true;
        try
        {
            if (sheet.ValidateSource is { } validate) await validate();
            if (!Current(true)) return;
            _closingForEntries = true; await _interaction.CloseAsync();
            if (!Current(false) || sheet.HandoffCurrent?.Invoke() == false) return;
            if (sheet.ValidateSource is { } revalidate) await revalidate();
            if (Current(false) && sheet.HandoffCurrent?.Invoke() != false) await show();
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        { if (OwnsRoot && _active && revision == _revision) Feedback = _translator["Report_ExplanationStale"]; }
        finally { _closingForEntries = false; IsBusy = false; PublishAvailability(); }
    }

    [RelayCommand(CanExecute = nameof(CanClose))]
    private async Task CloseAsync()
    {
        if (!CanClose) return; var revision = _revision; IsBusy = true;
        try { await _interaction.CloseAsync(); }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        { if (OwnsRoot && _active && revision == _revision) Feedback = _translator["Report_ExplanationStale"]; }
        finally { IsBusy = false; PublishAvailability(); }
    }
}
