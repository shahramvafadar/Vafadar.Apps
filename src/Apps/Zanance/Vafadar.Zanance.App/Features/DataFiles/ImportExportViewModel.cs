using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vafadar.Localization;
using Vafadar.Localization.Formatting;
using Vafadar.Maui.Mvvm;
using Vafadar.Zanance.App.Features.Entries;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.App.Interaction;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Budgets;
using Vafadar.Zanance.Core.DataFiles;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Settings;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;
using Vafadar.Zanance.Data.Importing;
using Vafadar.Zanance.Core.Holdings;

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
    private readonly IDataFileInteraction _files;
    private readonly IAppInteraction _interaction;
    private bool _previewAccepted;
    private readonly HashSet<Guid> _completedUndo = [];
    private readonly HoldingStore _holdings;
    private HoldingsCsvContent? _holdingsContent;
    private IReadOnlyList<IReadOnlyList<string>> _rows = [];
    private IReadOnlyList<ImportRow> _preview = [];
    private bool _ownFormat;
    private bool _previewStale;
    private ImportReadReview? _ledgerRead;
    private long _interpretationEpoch;
    private IReadOnlyList<LedgerEntry> _existingEntries = [];
    private IReadOnlyDictionary<Guid, Core.Accounts.Account> _previewAccounts = new Dictionary<Guid, Core.Accounts.Account>();
    private CategoryLookup? _previewCategories;

    public ImportExportViewModel(ZananceStore store, Translator translator, IDateFormatter dates, ILocalizationService localization, TimeProvider time, HoldingStore holdings, IDataFileInteraction files, IAppInteraction interaction)
    {
        _holdings = holdings;
        _files = files;
        _interaction = interaction;
        _translator = translator;
        PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is nameof(IsBusy) or nameof(CanImport) or nameof(HasPreview) or nameof(ImportError) or nameof(ImportResult) or nameof(UndoError)) RefreshActions();
            if (!_ownFormat && e.PropertyName is nameof(Account) or nameof(DateColumn) or nameof(AmountColumn)
                or nameof(TitleColumn) or nameof(CategoryColumn) or nameof(NoteColumn) or nameof(DateFormat)
                or nameof(CalendarIndex) or nameof(SeparatorIndex) or nameof(SignIndex))
            {
                Interlocked.Increment(ref _interpretationEpoch);
                if (HasLedgerPreview)
                { _previewStale = true; CanImport = false; ImportError = _translator["Import_InterpretationChanged"]; }
            }
        };
        _store = store;
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

    /// <summary>Opens explicit Germany accountant preparation without altering the CSV form.</summary>
    [RelayCommand] private Task AccountantExportAsync() => _interaction.NavigateAsync(AppRoutes.AccountantExportRoute);

    /// <summary>Gets whether the exact current, unaccepted review can be imported explicitly.</summary>
    public bool CanRunImport => CanImport && HasPreview && !IsBusy && !_previewAccepted;
    /// <summary>Gets whether original raw CSV/mapping may be explicitly reviewed again.</summary>
    public bool CanReviewPreview => !IsBusy && !_previewAccepted && _rows.Count > 1;
    /// <summary>Gets whether pinned review actions or a complete import outcome need their own growing row.</summary>
    public bool HasImportDock => HasPreview || ImportError is not null || ImportResult is not null || UndoError is not null;
    private bool CanUndo(BatchRow? batch) => batch is not null && !IsBusy && !_completedUndo.Contains(batch.BatchId)
        && Batches.Any(row => row.BatchId == batch.BatchId);
    private void RefreshActions()
    {
        OnPropertyChanged(nameof(CanRunImport)); OnPropertyChanged(nameof(CanReviewPreview)); OnPropertyChanged(nameof(HasImportDock));
        ImportCommand.NotifyCanExecuteChanged(); PreviewCommand.NotifyCanExecuteChanged(); RefreshPreviewCommand.NotifyCanExecuteChanged();
        PickStarMoneyCommand.NotifyCanExecuteChanged(); PickSupaCommand.NotifyCanExecuteChanged(); NotifyLedgerPreview();
        PickCommand.NotifyCanExecuteChanged(); ExportCommand.NotifyCanExecuteChanged(); ExportHoldingsCommand.NotifyCanExecuteChanged(); UndoCommand.NotifyCanExecuteChanged();
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
        // Publish only after the complete read succeeds; failure must not erase original history.
        var batches = await _store.GetImportBatchesAsync();
        Batches.Clear();
        foreach (var batch in batches)
        {
            Batches.Add(new BatchRow(batch.BatchId, _translator.Format("Import_Batch", _dates.Format(batch.ImportedAt, DateFormatStyle.Long), batch.Count)));
        }
        UndoCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand(CanExecute = nameof(IsNotBusy))]
    private async Task ExportAsync()
    {
        // Exports leave the app unencrypted: with the app lock on, the owner confirms first (SEC-02, AT-61).
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            if (!await _files.ConfirmExportAsync(_translator["Lock_ConfirmExport"])) return;
            var accounts = (await _store.GetAccountsAsync()).ToDictionary(a => a.Id);
            var lookup = new CategoryLookup(await _store.GetCategoriesAsync(), _translator);
            var entries = await _store.GetEntriesAsync(ExportFrom, ExportTo);
            var text = CsvExport.Write(entries, accounts, id => lookup.Name(id), IncludeNotes);

            // A neutral file name: no account names or amounts (BAK-08 applies to exports too).
            var name = string.Create(CultureInfo.InvariantCulture, $"zanance-export-{_time.GetLocalNow():yyyyMMdd-HHmm}.csv");
            await _files.ShareCsvAsync(name, _translator["Export_Title"], text);
            ExportResult = _translator.Format("Export_Done", entries.Count);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            await _interaction.ShowFailureAsync(ex);
        }
        finally
        {
            IsBusy = false;
        }
    }

    // The holdings file: types, locations, events and prices; the money of purchases and sales is in the entries file.
    [RelayCommand(CanExecute = nameof(IsNotBusy))]
    private async Task ExportHoldingsAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            if (!await _files.ConfirmExportAsync(_translator["Lock_ConfirmExport"])) return;
            var events = await _holdings.GetEventsAsync();
            var text = HoldingsCsv.Write(await _holdings.GetTypesAsync(), await _holdings.GetLocationsAsync(), events, await _holdings.GetValuationsAsync(), IncludeNotes);
            var name = string.Create(CultureInfo.InvariantCulture, $"zanance-holdings-{_time.GetLocalNow():yyyyMMdd-HHmm}.csv");
            await _files.ShareCsvAsync(name, _translator["Export_Title"], text);
            ExportResult = _translator.Format("Export_HoldingsDone", events.Count);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            await _interaction.ShowFailureAsync(ex);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand(CanExecute = nameof(IsNotBusy))]
    private async Task PickAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try { await PickFileAsync(); }
        catch (CsvReadLimitException ex) { ImportError = _translator.Format("Import_ReadLimit", _translator.Format("Import_Limit_" + ex.Limit,
            ex.Limit == CsvReadLimit.Bytes ? ex.Maximum / (1024 * 1024) : ex.Maximum)); }
        catch (CsvMalformedInputException) { ImportError = _translator["Import_ReadMalformed"]; }
        catch (Exception ex) when (ex is not OutOfMemoryException) { await _interaction.ShowFailureAsync(ex); }
        finally { IsBusy = false; }
    }

    private async Task PickFileAsync()
    {
        var epoch = _bankEpoch;
        var owner = await _store.CaptureImportFileAsync();
        var file = await _files.PickCsvAsync(_translator["Import_Pick"]);
        if (file is null || epoch != _bankEpoch) return;
        // D-385: a rejected new input must not retire the last complete source/review/result.
        var selectedRows = await Task.Run(() => Csv.ReadDetected(file.Text));
        if (epoch != _bankEpoch) return;
        if (selectedRows.Count < 2)
        {
            ImportError = _translator["Import_Empty"];
            return;
        }
        var selectedHoldings = selectedRows.Count > 1 && HoldingsCsv.IsHoldingsFormat(selectedRows[0]);
        var selectedOwn = selectedRows.Count > 1 && !selectedHoldings && CsvImport.IsOwnFormat(selectedRows[0]);
        var ledger = selectedOwn ? await PrepareLedgerPreviewAsync(selectedRows, true, null, owner) : null;
        var holdings = selectedHoldings ? await PrepareHoldingsPreviewAsync(selectedRows) : null;
        var currentOwner = await _store.CaptureImportFileAsync();
        if (epoch != _bankEpoch) return;
        if (owner != currentOwner) throw new InvalidOperationException("The selected CSV profile changed.");
        IsBankPreset = false; _bankRead = null; _ledgerRead = null; _bankOwner = null; BankAcknowledged = false; BankLines.Clear();
        // Only an actual new selection replaces the previous raw review or accepted result.
        ImportError = null; UndoError = null; ImportResult = null; HasPreview = false; CanImport = false; _previewAccepted = false;
        _preview = []; _holdingsContent = null; Overlaps.Clear(); ClearLedgerPreview();
        _rows = selectedRows;
        FileName = file.Name;
        RefreshActions();
        // A holdings file is read as it is; nothing to map (ZEX-S0409).
        _holdingsContent = null;
        if (holdings is not null)
        {
            ShowMapping = false;
            PublishHoldingsPreview(holdings);
            return;
        }

        _ownFormat = selectedOwn;
        ShowMapping = !_ownFormat;
        if (_ownFormat)
        {
            PublishLedgerPreview(ledger!);
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

    [RelayCommand(CanExecute = nameof(CanReviewPreview))]
    private async Task RefreshPreviewAsync()
    {
        if (!CanReviewPreview) return;
        IsBusy = true;
        try { if (_holdingsContent is not null) await PreviewHoldingsAsync(); else await ReadPreviewAsync(); }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            _previewStale = true; CanImport = false; ImportError = _translator["Import_ReviewFailed"];
            await _interaction.ShowFailureAsync(ex);
        }
        finally { IsBusy = false; }
    }

    [RelayCommand(CanExecute = nameof(CanReviewPreview))]
    private Task PreviewAsync() => RefreshPreviewAsync();

    /// <summary>Complete prepared values; no UI-bound collection is changed by the file-analysis worker.</summary>
    private sealed record LedgerPreviewData(IReadOnlyList<ImportRow> Preview, List<LedgerEntry> Existing,
        List<Account> Accounts, List<Core.Categories.Category> Categories, int Valid, int Known, int Invalid,
        int Duplicates, int BeforeOpening, IReadOnlyList<ImportRow> InvalidRows, ImportReadReview Read);

    /// <summary>Retains the original matching/rule algorithms and read order, running potentially synchronous provider work off-window.</summary>
    private Task<LedgerPreviewData> PrepareLedgerPreviewAsync(IReadOnlyList<IReadOnlyList<string>> rows, bool own, ImportMapping? mapping, string owner) => Task.Run(async () =>
    {
        // D-405: the opening boundary, names, duplicate history and overlap choices must describe
        // the same original file snapshot later checked by the atomic financial writer.
        var read = await _store.ReadImportReviewAsync(owner, reviewAccess: false);
        var (accounts, categories, existing, _) = read.Inputs;
        string Name(Core.Categories.Category category) => CategoryLookup.NameOf(category, _translator);
        var preview = own ? CsvImport.PreviewOwn(rows, accounts, categories, Name, existing, read.ConsumedIds)
            : CsvImport.PreviewGeneric(rows, mapping!, accounts, categories, Name, existing, await _store.GetCategoryRulesAsync());
        read.Bind(preview.Where(row => row.Entry is not null).Select(row => row.Entry!).DistinctBy(entry => entry.Id));
        read.EnsureCurrent();
        return new LedgerPreviewData(preview, existing, accounts, categories,
            preview.Count(row => row.Entry is not null && !row.AlreadyImported), preview.Count(row => row.AlreadyImported),
            preview.Count(row => row.Error is not null), preview.Count(row => row.PossibleDuplicate),
            preview.Count(row => row.BeforeOpening && !row.AlreadyImported), preview.Where(row => row.Error is not null).Take(8).ToArray(), read);
    });

    private async Task ReadPreviewAsync()
    {
        ImportError = null;
        if (IsBankPreset) { await ReadBankPreviewAsync(); return; }
        var interpretationEpoch = Interlocked.Read(ref _interpretationEpoch);
        ImportMapping? mapping = null;
        if (!_ownFormat)
        {
            if (Account is null || DateColumn?.Index is not { } date || AmountColumn?.Index is not { } amount || DateFormat is null)
            { ImportError = _translator["Import_MappingIncomplete"]; return; }
            mapping = new ImportMapping(Account.Id, date, DateFormat, Presentation.Calendars.All[Math.Clamp(CalendarIndex, 0, Presentation.Calendars.All.Count - 1)], amount,
                SeparatorIndex == 1 ? ',' : '.', (SignMode)SignIndex, TitleColumn?.Index, CategoryColumn?.Index, NoteColumn?.Index);
        }
        var epoch = _bankEpoch; var owner = await _store.CaptureImportFileAsync();
        var prepared = await PrepareLedgerPreviewAsync(_rows, _ownFormat, mapping, owner);
        var currentOwner = await _store.CaptureImportFileAsync();
        if (epoch != _bankEpoch) return;
        if (owner != currentOwner) throw new InvalidOperationException("The reviewed CSV profile changed.");
        // A user may change visible mapping while the worker runs; never publish that older interpretation as current.
        if (interpretationEpoch != Interlocked.Read(ref _interpretationEpoch))
        { _previewStale = true; CanImport = false; ImportError = _translator["Import_InterpretationChanged"]; return; }
        PublishLedgerPreview(prepared);
    }

    /// <summary>Publishes one fully prepared original review after successful input analysis.</summary>
    private void PublishLedgerPreview(LedgerPreviewData prepared)
    {
        var (preview, existing, accounts, categories, valid, known, invalid, duplicates, beforeOpening, invalidRows, read) = prepared;
        _ledgerRead = read;
        _previewStale = false; _preview = preview;
        _existingEntries = existing;
        _previewAccounts = accounts.ToDictionary(a => a.Id);
        _previewCategories = new CategoryLookup(categories, _translator);

        PreviewText = _translator.Format("Import_Preview", valid, known, invalid);
        InvalidLines.Clear();
        foreach (var row in invalidRows)
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
        _ledgerPreviewPage = 0; PublishLedgerPreviewPage();
    }

    /// <summary>Parses complete original holding records without running provider/CSV work on the window thread.</summary>
    private Task<HoldingsCsvContent> PrepareHoldingsPreviewAsync(IReadOnlyList<IReadOnlyList<string>> rows) => Task.Run(async () =>
        HoldingsCsv.Read(rows, await _holdings.GetTypesAsync(), await _holdings.GetLocationsAsync()));

    private async Task PreviewHoldingsAsync()
    {
        ImportError = null;
        var epoch = _bankEpoch; var owner = await _store.CaptureImportFileAsync();
        var content = await PrepareHoldingsPreviewAsync(_rows);
        var currentOwner = await _store.CaptureImportFileAsync();
        if (epoch != _bankEpoch) return;
        if (owner != currentOwner) throw new InvalidOperationException("The holding-import profile changed.");
        PublishHoldingsPreview(content);
    }

    /// <summary>Publishes only a complete original holding-file analysis.</summary>
    private void PublishHoldingsPreview(HoldingsCsvContent content)
    {
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

    partial void OnSkipDuplicatesChanged(bool value)
    {
        UpdateCount();
        if (HasLedgerPreview) PublishLedgerPreviewPage();
    }

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
        CanImport = ImportCount > 0 && !pending && !shared && !_previewStale
            && (!IsBankPreset || BankAcknowledged && _bankRead is not null && _preview.All(r => r.Error is null));
    }

    [RelayCommand(CanExecute = nameof(CanRunImport))]
    private async Task ImportAsync()
    {
        if (!CanRunImport) return;
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
            ImportResult result;
            Exception? publication = null;
            var bank = IsBankPreset ? _bankRead ?? throw new InvalidOperationException("Review the bank source again.") : null;
            var bankEpoch = _bankEpoch;
            var review = bank ?? _ledgerRead ?? throw new InvalidOperationException("Review the original CSV again.");
            review.EnsureCurrent();
            if (bank is not null)
            {
                bank.EnsureCurrent();
                if (!await _interaction.ConfirmAsync(_translator["BankCsv_Title"],
                    _translator.Format("BankCsv_Confirm", entries.Count, Account?.Name ?? "", Account?.CurrencyCode ?? ""),
                    ImportButtonText ?? _translator["BankCsv_Title"], _translator["Common_Cancel"])) return;
                if (bankEpoch != _bankEpoch || !BankAcknowledged || !ReferenceEquals(_bankRead, bank)) throw new InvalidOperationException("The original bank consent changed.");
                bank.EnsureCurrent();
            }
            try { result = await _store.ImportReviewedAsync(entries, Overlaps.Select(o => o.Decision).ToList(), review); }
            catch (CommittedWriteNotificationException<ImportResult> ex) { result = ex.Result; publication = ex; }
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

            CompleteImport(result.Imported, result.Skipped);
            try { await LoadBatchesAsync(); }
            catch (Exception ex) when (ex is not OutOfMemoryException) { publication ??= ex; }
            if (publication is not null)
            { ImportError = _translator["Import_SavedFollowUpFailed"]; await _interaction.ShowFailureAsync(publication); }
        }
        catch (ImportReviewChangedException) when (!IsBankPreset && !_previewAccepted)
        {
            // A changed reviewed baseline is an expected refusal, not an unexpected native error dialog.
            _previewStale = true; CanImport = false; _ledgerRead = null;
            ImportError = _translator["Import_Overlap_ChangedPreview"];
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            ImportError = _translator[_previewAccepted ? "Import_SavedFollowUpFailed" : "Import_SaveFailed"];
            if (!_previewAccepted) ImportError = CommercialFailureMessage.OrFallback(ex, _translator, "Import_SaveFailed", draftKept: true);
            if (IsBankPreset && !_previewAccepted)
            { RetireBankReview(); ImportError = CommercialFailureMessage.OrFallback(ex, _translator, "BankCsv_Failed", draftKept: true); }
            await _interaction.ShowFailureAsync(ex);
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
            (int Imported, int Skipped, HoldingConflict? Conflict) result;
            Exception? publication = null;
            try { result = await _holdings.ImportAsync(content); }
            catch (CommittedWriteNotificationException<(int Imported, int Skipped, HoldingConflict? Conflict)> ex)
            { result = ex.Result; publication = ex; }
            var (imported, skipped, conflict) = result;
            if (conflict is not null)
            {
                ImportError = _translator.Format("Import_HoldingsConflict", _dates.Format(conflict.Date, DateFormatStyle.Short));
                return;
            }

            CompleteImport(imported, skipped);
            if (publication is not null)
            { ImportError = _translator["Import_SavedFollowUpFailed"]; await _interaction.ShowFailureAsync(publication); }
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            ImportError = _previewAccepted ? _translator["Import_SavedFollowUpFailed"]
                : CommercialFailureMessage.OrFallback(ex, _translator, "Import_SaveFailed", draftKept: true);
            await _interaction.ShowFailureAsync(ex);
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>Retires the accepted reviewed payload before any fallible publication/history follow-through.</summary>
    private void CompleteImport(int imported, int skipped)
    {
        ImportError = null;
        _previewAccepted = true; CanImport = false; HasPreview = false; ShowMapping = false;
        _preview = []; _ledgerRead = null; _holdingsContent = null; ClearLedgerPreview();
        ImportResult = _translator.Format("Import_Done", imported, skipped);
        RefreshActions();
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
        await ReadPreviewAsync();
    }
#endif

    [RelayCommand(CanExecute = nameof(CanUndo))]
    private async Task UndoAsync(BatchRow batch)
    {
        if (!CanUndo(batch)) return;
        // Keep one busy lifetime through explicit consent and the original atomic Undo.
        IsBusy = true;
        var accepted = false;
        try
        {
            if (!await _interaction.ConfirmAsync(_translator["Import_Undo"], _translator["Import_UndoMessage"], _translator["Import_Undo"], _translator["Common_Cancel"])) return;
            UndoError = null; ImportResult = null;
            ImportUndoResult result;
            Exception? publication = null;
            try { result = await _store.TryUndoImportAsync(batch.BatchId); }
            catch (CommittedWriteNotificationException<ImportUndoResult> ex) { result = ex.Result; publication = ex; }
            if (result.Conflict) { UndoError = _translator["Import_UndoConflict"]; return; }
            accepted = true; ImportError = null; _completedUndo.Add(batch.BatchId);
            foreach (var row in Batches.Where(row => row.BatchId == batch.BatchId).ToList()) Batches.Remove(row);
            ImportResult = _translator["Import_UndoDone"];
            // Original overlap evidence is retired. An explicit fresh review may intentionally import again.
            HasPreview = false; CanImport = false; _previewStale = true; _previewAccepted = false;
            _preview = []; Overlaps.Clear(); ClearLedgerPreview();
            try { await LoadBatchesAsync(); }
            catch (Exception ex) when (ex is not OutOfMemoryException) { publication ??= ex; }
            if (publication is not null)
            { UndoError = _translator["Import_UndoFollowUpFailed"]; await _interaction.ShowFailureAsync(publication); }
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            UndoError = accepted ? _translator["Import_UndoFollowUpFailed"]
                : CommercialFailureMessage.OrFallback(ex, _translator, "Import_UndoFailed");
            await _interaction.ShowFailureAsync(ex);
        }
        finally { IsBusy = false; RefreshActions(); }
    }
}
