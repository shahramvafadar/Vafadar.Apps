using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vafadar.Localization;
using Vafadar.Localization.Formatting;
using Vafadar.Maui.Mvvm;
using Vafadar.Zanance.App.Features.Entries;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.App.Security;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Budgets;
using Vafadar.Zanance.Core.DataFiles;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Settings;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Features.DataFiles;

/// <summary>A column choice; <see cref="Index"/> is <see langword="null"/> for "not in the file".</summary>
public sealed record ColumnChoice(int? Index, string Name)
{
    public override string ToString() => Name;
}

/// <summary>A past import with undo.</summary>
public sealed record BatchRow(Guid BatchId, string Text);

/// <summary>
/// CSV export and import (UI-12, IO-01..13). Export warns that the file is not encrypted (IO-05); import always shows a
/// preview with valid, invalid, already imported and possibly duplicate rows before anything is saved (IO-09), and each
/// import can be undone as a whole (IO-11).
/// </summary>
public sealed partial class ImportExportViewModel : ViewModelBase
{
    private readonly ZananceStore _store;
    private readonly Translator _translator;
    private readonly IDateFormatter _dates;
    private readonly ILocalizationService _localization;
    private readonly TimeProvider _time;
    private readonly AppLockService _lock;
    private readonly HoldingStore _holdings;
    private HoldingsCsvContent? _holdingsContent;
    private IReadOnlyList<IReadOnlyList<string>> _rows = [];
    private IReadOnlyList<ImportRow> _preview = [];
    private bool _ownFormat;
    private bool _previewStale;
    private IReadOnlyList<LedgerEntry> _existingEntries = [];
    private IReadOnlyDictionary<Guid, Core.Accounts.Account> _previewAccounts = new Dictionary<Guid, Core.Accounts.Account>();
    private CategoryLookup? _previewCategories;

    public ImportExportViewModel(ZananceStore store, Translator translator, IDateFormatter dates, ILocalizationService localization, TimeProvider time, AppLockService appLock, HoldingStore holdings)
    {
        _holdings = holdings;
        _lock = appLock;
        _store = store;
        _translator = translator;
        _dates = dates;
        _localization = localization;
        _time = time;
        var today = DateOnly.FromDateTime(time.GetLocalNow().DateTime);
        ExportFrom = new DateOnly(today.Year, 1, 1);
        ExportTo = today;
        IncludeNotes = true;
        Accounts = [];
        Columns = [];
        CalendarNames = Presentation.Calendars.Names(translator);
        SeparatorNames = [translator["Import_DecimalPoint"], translator["Import_DecimalComma"]];
        SignNames = [translator["Import_SignNegativeExpense"], translator["Import_SignAllExpenses"], translator["Import_SignAllIncome"]];
    }

    public IReadOnlyList<string> DateFormats { get; } = CsvImport.DateFormats;

    public IReadOnlyList<string> CalendarNames { get; }

    public IReadOnlyList<string> SeparatorNames { get; }

    public IReadOnlyList<string> SignNames { get; }

    public ObservableCollection<string> InvalidLines { get; } = [];

    public ObservableCollection<BatchRow> Batches { get; } = [];

    /// <summary>Gets the explicit choices for every aggregate affected by the accepted rows.</summary>
    public ObservableCollection<ImportOverlapRow> Overlaps { get; } = [];

    /// <summary>Gets or sets the reason the overlap decisions are incomplete or conflict.</summary>
    [ObservableProperty]
    public partial string? OverlapError { get; set; }

    /// <summary>Gets or sets whether the current preview is ready for an explicit import.</summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ImportCommand))]
    public partial bool CanImport { get; set; }

    [ObservableProperty]
    public partial DateOnly ExportFrom { get; set; }

    [ObservableProperty]
    public partial DateOnly ExportTo { get; set; }

    [ObservableProperty]
    public partial bool IncludeNotes { get; set; }

    [ObservableProperty]
    public partial string? ExportResult { get; set; }

    [ObservableProperty]
    public partial string? FileName { get; set; }

    [ObservableProperty]
    public partial bool ShowMapping { get; set; }

    [ObservableProperty]
    public partial IReadOnlyList<AccountChoice> Accounts { get; set; }

    [ObservableProperty]
    public partial AccountChoice? Account { get; set; }

    [ObservableProperty]
    public partial IReadOnlyList<ColumnChoice> Columns { get; set; }

    [ObservableProperty]
    public partial ColumnChoice? DateColumn { get; set; }

    [ObservableProperty]
    public partial ColumnChoice? AmountColumn { get; set; }

    [ObservableProperty]
    public partial ColumnChoice? TitleColumn { get; set; }

    [ObservableProperty]
    public partial ColumnChoice? CategoryColumn { get; set; }

    [ObservableProperty]
    public partial ColumnChoice? NoteColumn { get; set; }

    [ObservableProperty]
    public partial string? DateFormat { get; set; }

    [ObservableProperty]
    public partial int CalendarIndex { get; set; }

    [ObservableProperty]
    public partial int SeparatorIndex { get; set; }

    [ObservableProperty]
    public partial int SignIndex { get; set; }

    [ObservableProperty]
    public partial bool HasPreview { get; set; }

    [ObservableProperty]
    public partial string? PreviewText { get; set; }

    [ObservableProperty]
    public partial string? WarningText { get; set; }

    [ObservableProperty]
    public partial bool HasDuplicates { get; set; }

    [ObservableProperty]
    public partial bool SkipDuplicates { get; set; }

    [ObservableProperty]
    public partial int ImportCount { get; set; }

    [ObservableProperty]
    public partial string? ImportButtonText { get; set; }

    [ObservableProperty]
    public partial string? ImportResult { get; set; }

    [ObservableProperty]
    public partial string? ImportError { get; set; }

    /// <summary>Gets or sets the inline conflict next to the import history.</summary>
    [ObservableProperty]
    public partial string? UndoError { get; set; }

    /// <summary>Gets a value indicating whether holdings can be exported (Advanced, with holdings; ZEX-S0409).</summary>
    [ObservableProperty]
    public partial bool CanExportHoldings { get; set; }

    public async Task LoadAsync()
    {
        var accounts = await _store.GetAccountsAsync(includeArchived: false);
        Accounts = [.. accounts.Select(a => new AccountChoice(a.Id, a.Name, a.CurrencyCode))];
        Account ??= Accounts.FirstOrDefault();
        DateFormat ??= DateFormats[0];
        CalendarIndex = (int)Presentation.Calendars.ToPeriod(_localization.CurrentCalendar);
        CanExportHoldings = (await _store.GetSettingsAsync()).Shows(Feature.HoldingsExport) && (await _holdings.GetTypesAsync()).Count > 0;
        await LoadBatchesAsync();
    }

    private async Task LoadBatchesAsync()
    {
        Batches.Clear();
        foreach (var batch in await _store.GetImportBatchesAsync())
        {
            Batches.Add(new BatchRow(batch.BatchId, _translator.Format("Import_Batch", _dates.Format(batch.ImportedAt, DateFormatStyle.Long), batch.Count)));
        }
    }

    [RelayCommand]
    private async Task ExportAsync()
    {
        // Exports leave the app unencrypted: with the app lock on, the owner confirms first (SEC-02, AT-61).
        if (!await _lock.ConfirmAsync(_translator["Lock_ConfirmExport"]))
        {
            return;
        }

        IsBusy = true;
        try
        {
            var accounts = (await _store.GetAccountsAsync()).ToDictionary(a => a.Id);
            var lookup = new CategoryLookup(await _store.GetCategoriesAsync(), _translator);
            var entries = await _store.GetEntriesAsync(ExportFrom, ExportTo);
            var text = CsvExport.Write(entries, accounts, id => lookup.Name(id), IncludeNotes);

            // A neutral file name: no account names or amounts (BAK-08 applies to exports too).
            var path = Path.Combine(FileSystem.CacheDirectory, string.Create(CultureInfo.InvariantCulture, $"zanance-export-{_time.GetLocalNow():yyyyMMdd-HHmm}.csv"));
            await File.WriteAllTextAsync(path, text, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
            ExportResult = _translator.Format("Export_Done", entries.Count);
            await Share.Default.RequestAsync(new ShareFileRequest { Title = _translator["Export_Title"], File = new ShareFile(path, "text/csv") });
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            await Failures.ShowAsync(ex);
        }
        finally
        {
            IsBusy = false;
        }
    }

    // The holdings file: types, locations, events and prices; the money of purchases and sales is in the entries file.
    [RelayCommand]
    private async Task ExportHoldingsAsync()
    {
        if (!await _lock.ConfirmAsync(_translator["Lock_ConfirmExport"]))
        {
            return;
        }

        IsBusy = true;
        try
        {
            var events = await _holdings.GetEventsAsync();
            var text = HoldingsCsv.Write(await _holdings.GetTypesAsync(), await _holdings.GetLocationsAsync(), events, await _holdings.GetValuationsAsync(), IncludeNotes);
            var path = Path.Combine(FileSystem.CacheDirectory, string.Create(CultureInfo.InvariantCulture, $"zanance-holdings-{_time.GetLocalNow():yyyyMMdd-HHmm}.csv"));
            await File.WriteAllTextAsync(path, text, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
            ExportResult = _translator.Format("Export_HoldingsDone", events.Count);
            await Share.Default.RequestAsync(new ShareFileRequest { Title = _translator["Export_Title"], File = new ShareFile(path, "text/csv") });
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            await Failures.ShowAsync(ex);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private Task PickAsync() => Failures.GuardAsync(PickFileAsync);

    private async Task PickFileAsync()
    {
        ImportError = null;
        ImportResult = null;
        HasPreview = false;
        var file = await FilePicker.Default.PickAsync(new PickOptions { PickerTitle = _translator["Import_Pick"] });
        if (file is null)
        {
            return;
        }

        string text;
        await using (var stream = await file.OpenReadAsync())
        using (var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true))
        {
            text = await reader.ReadToEndAsync();
        }

        _rows = Csv.Read(text, Csv.DetectSeparator(text));
        FileName = file.FileName;
        if (_rows.Count < 2)
        {
            ImportError = _translator["Import_Empty"];
            ShowMapping = false;
            return;
        }

        // A holdings file is read as it is; nothing to map (ZEX-S0409).
        _holdingsContent = null;
        if (HoldingsCsv.IsHoldingsFormat(_rows[0]))
        {
            ShowMapping = false;
            await PreviewHoldingsAsync();
            return;
        }

        _ownFormat = CsvImport.IsOwnFormat(_rows[0]);
        ShowMapping = !_ownFormat;
        if (_ownFormat)
        {
            await PreviewAsync();
            return;
        }

        // Suggest columns by name, but let the user confirm everything (IO-08).
        var header = _rows[0];
        Columns = [new ColumnChoice(null, _translator["Import_NoColumn"]), .. header.Select((name, i) => new ColumnChoice(i, $"{i + 1}: {name}"))];
        ColumnChoice? Guess(params string[] names) =>
            Columns.Skip(1).FirstOrDefault(c => names.Any(n => header[c.Index!.Value].Contains(n, StringComparison.OrdinalIgnoreCase)));
        DateColumn = Guess("date", "datum", "تاریخ") ?? Columns.ElementAtOrDefault(1);
        AmountColumn = Guess("amount", "betrag", "مبلغ") ?? Columns.ElementAtOrDefault(2);
        TitleColumn = Guess("title", "description", "text", "verwendung", "شرح", "عنوان") ?? Columns[0];
        CategoryColumn = Guess("category", "kategorie", "دسته") ?? Columns[0];
        NoteColumn = Guess("note", "notiz", "یادداشت") ?? Columns[0];
    }

    [RelayCommand]
    private Task RefreshPreviewAsync() => _holdingsContent is not null ? PreviewHoldingsAsync() : PreviewAsync();

    [RelayCommand]
    private async Task PreviewAsync()
    {
        ImportError = null;
        _previewStale = false;
        var accounts = await _store.GetAccountsAsync();
        var categories = await _store.GetCategoriesAsync();
        var existing = await _store.GetEntriesAsync();
        string Name(Core.Categories.Category c) => CategoryLookup.NameOf(c, _translator);

        if (_ownFormat)
        {
            _preview = CsvImport.PreviewOwn(_rows, accounts, categories, Name, existing, await _store.GetConsumedImportIdsAsync());
        }
        else
        {
            if (Account is null || DateColumn?.Index is not { } date || AmountColumn?.Index is not { } amount || DateFormat is null)
            {
                ImportError = _translator["Import_MappingIncomplete"];
                return;
            }

            var mapping = new ImportMapping(Account.Id, date, DateFormat, Presentation.Calendars.All[Math.Clamp(CalendarIndex, 0, Presentation.Calendars.All.Count - 1)], amount,
                SeparatorIndex == 1 ? ',' : '.', (SignMode)SignIndex, TitleColumn?.Index, CategoryColumn?.Index, NoteColumn?.Index);
            _preview = CsvImport.PreviewGeneric(_rows, mapping, accounts, categories, Name, existing, await _store.GetCategoryRulesAsync());
        }

        _existingEntries = existing;
        _previewAccounts = accounts.ToDictionary(a => a.Id);
        _previewCategories = new CategoryLookup(categories, _translator);

        var valid = _preview.Count(r => r.Entry is not null && !r.AlreadyImported);
        var known = _preview.Count(r => r.AlreadyImported);
        var invalid = _preview.Where(r => r.Error is not null).ToList();
        var duplicates = _preview.Count(r => r.PossibleDuplicate);
        var beforeOpening = _preview.Count(r => r.BeforeOpening && !r.AlreadyImported);

        PreviewText = _translator.Format("Import_Preview", valid, known, invalid.Count);
        InvalidLines.Clear();
        foreach (var row in invalid.Take(8))
        {
            InvalidLines.Add(_translator.Format("Import_InvalidLine", row.Line, _translator[$"Import_Error_{row.Error}"]));
        }

        HasDuplicates = duplicates > 0;
        WarningText = string.Join(Environment.NewLine, new[]
        {
            duplicates > 0 ? _translator.Format("Import_Duplicates", duplicates) : null,
            beforeOpening > 0 ? _translator.Format("Import_BeforeOpening", beforeOpening) : null,
        }.Where(t => t is not null));
        if (WarningText.Length == 0)
        {
            WarningText = null;
        }

        UpdateCount();
        HasPreview = true;
    }

    private async Task PreviewHoldingsAsync()
    {
        var content = HoldingsCsv.Read(_rows, await _holdings.GetTypesAsync(), await _holdings.GetLocationsAsync());
        _holdingsContent = content;
        _preview = [];
        Overlaps.Clear();
        OverlapError = null;
        PreviewText = _translator.Format("Import_HoldingsPreview", content.Types.Count, content.Events.Count, content.Valuations.Count, content.Errors.Count);
        InvalidLines.Clear();
        foreach (var error in content.Errors.Take(8))
        {
            InvalidLines.Add(_translator.Format("Import_InvalidLine", error.Line, _translator[$"Import_Error_{error.Error}"]));
        }

        HasDuplicates = false;
        WarningText = content.Errors.Count > 0 ? _translator["Import_HoldingsInvalid"] : null;
        ImportCount = content.Errors.Count > 0 ? 0 : content.Types.Count + content.Locations.Count + content.Events.Count + content.Valuations.Count;
        ImportButtonText = _translator.Format("Import_Button", ImportCount);
        CanImport = ImportCount > 0;
        HasPreview = true;
    }

    partial void OnSkipDuplicatesChanged(bool value) => UpdateCount();

    private void UpdateCount()
    {
        if (_holdingsContent is not null)
        {
            return;
        }

        var entries = AcceptedEntries();
        ImportCount = entries.Count;
        ImportButtonText = _translator.Format("Import_Button", ImportCount);
        // A duplicate switch changes the exact affected rows. Reset choices rather than reuse an outdated decision.
        Overlaps.Clear();
        foreach (var overlap in AggregatedEntries.FindForImport(entries, _existingEntries))
        {
            var account = _previewAccounts[overlap.Aggregate.AccountId];
            var refunds = _existingEntries.Concat(entries).Any(e => e.RefundOfId == overlap.Aggregate.Id);
            Overlaps.Add(new ImportOverlapRow(overlap, account.Name, _previewCategories!.Name(overlap.Aggregate.CategoryId), account.CurrencyCode,
                _translator, _dates, _localization, refunds, UpdateOverlapChoices));
        }
        UpdateOverlapChoices();
    }

    private List<LedgerEntry> AcceptedEntries() => _preview.Where(r => r.Entry is not null && !r.AlreadyImported
        && !(SkipDuplicates && r.PossibleDuplicate)).Select(r => r.Entry!).DistinctBy(e => e.Id).ToList();

    private void UpdateOverlapChoices()
    {
        var pending = Overlaps.Any(o => !o.HasChoice);
        var shared = Overlaps.Where(o => o.HasChoice && o.Decision.Link).SelectMany(o => o.Preview.Detailed)
            .GroupBy(e => e.Id).Any(g => g.Count() > 1);
        OverlapError = shared ? _translator["Import_Overlap_SharedDetail"] : null;
        CanImport = ImportCount > 0 && !pending && !shared && !_previewStale;
    }

    [RelayCommand(CanExecute = nameof(CanImport))]
    private async Task ImportAsync()
    {
        if (_holdingsContent is { } holdings)
        {
            await ImportHoldingsAsync(holdings);
            return;
        }

        var entries = AcceptedEntries();
        if (entries.Count == 0)
        {
            return;
        }

        IsBusy = true;
        try
        {
            ImportError = null;
            ImportResult = null;
            var result = await _store.ImportAsync(entries, Overlaps.Select(o => o.Decision).ToList());
            if (result.Conflict is { } conflict)
            {
                ImportError = _translator[$"Import_Overlap_{conflict}"];
                _previewStale = conflict is not ImportOverlapConflict.SharedDetail;
                UpdateOverlapChoices();
                return;
            }
            if (result.Errors.Count > 0)
            {
                ImportError = _translator.Format("Import_Rejected", result.Errors.Count,
                    string.Join(", ", result.Errors.Values.SelectMany(e => e).Distinct().Select(e => _translator[$"LedgerError_{e}"])));
                return;
            }

            ImportResult = _translator.Format("Import_Done", result.Imported, result.Skipped);
            HasPreview = false;
            ShowMapping = false;
            await LoadBatchesAsync();
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            // E.g. a constraint the checks did not catch: the batch is rolled back, nothing was imported.
            await Failures.ShowAsync(ex);
        }
        finally
        {
            IsBusy = false;
        }
    }

    // All or nothing: a file with unreadable rows or a negative history is refused as a whole.
    private async Task ImportHoldingsAsync(HoldingsCsvContent content)
    {
        if (content.Errors.Count > 0)
        {
            return;
        }

        IsBusy = true;
        try
        {
            var (imported, skipped, conflict) = await _holdings.ImportAsync(content);
            if (conflict is not null)
            {
                ImportError = _translator.Format("Import_HoldingsConflict", _dates.Format(conflict.Date, DateFormatStyle.Short));
                return;
            }

            ImportResult = _translator.Format("Import_Done", imported, skipped);
            HasPreview = false;
            _holdingsContent = null;
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            await Failures.ShowAsync(ex);
        }
        finally
        {
            IsBusy = false;
        }
    }

#if DEBUG
    /// <summary>Loads fictitious rows for the development walk-through without a file picker or import side effects.</summary>
    internal async Task PreviewFixtureAsync(IReadOnlyList<LedgerEntry> entries)
    {
        await LoadAsync();
        var accounts = (await _store.GetAccountsAsync()).ToDictionary(a => a.Id);
        var categories = new CategoryLookup(await _store.GetCategoriesAsync(), _translator);
        _rows = Csv.Read(CsvExport.Write(entries, accounts, id => categories.Name(id), includeNotes: true), ',');
        _ownFormat = true;
        _holdingsContent = null;
        FileName = "fictitious-import.csv";
        await PreviewAsync();
    }
#endif

    [RelayCommand]
    private async Task UndoAsync(BatchRow batch)
    {
        if (!await Shell.Current.DisplayAlertAsync(_translator["Import_Undo"], _translator["Import_UndoMessage"], _translator["Import_Undo"], _translator["Common_Cancel"]))
        {
            return;
        }

        IsBusy = true;
        try
        {
            UndoError = null;
            ImportResult = null;
            var result = await _store.TryUndoImportAsync(batch.BatchId);
            if (result.Conflict) { UndoError = _translator["Import_UndoConflict"]; }
            else
            {
                ImportResult = _translator["Import_UndoDone"];
                // Preview choices may refer to the old remainder; obtain fresh evidence before another import.
                HasPreview = false;
                await LoadBatchesAsync();
            }
        }
        catch (Exception ex) when (ex is not OutOfMemoryException) { await Failures.ShowAsync(ex); }
        finally { IsBusy = false; }
    }
}
