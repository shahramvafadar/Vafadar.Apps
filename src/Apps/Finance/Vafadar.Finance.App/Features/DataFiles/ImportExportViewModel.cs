using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vafadar.Finance.App.Features.Entries;
using Vafadar.Finance.App.Presentation;
using Vafadar.Finance.App.Security;
using Vafadar.Finance.Core.Accounts;
using Vafadar.Finance.Core.Budgets;
using Vafadar.Finance.Core.DataFiles;
using Vafadar.Finance.Data;
using Vafadar.Localization;
using Vafadar.Localization.Formatting;
using Vafadar.Maui.Mvvm;

namespace Vafadar.Finance.App.Features.DataFiles;

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
    private readonly FinanceStore _store;
    private readonly Translator _translator;
    private readonly IDateFormatter _dates;
    private readonly ILocalizationService _localization;
    private readonly TimeProvider _time;
    private readonly AppLockService _lock;
    private IReadOnlyList<IReadOnlyList<string>> _rows = [];
    private IReadOnlyList<ImportRow> _preview = [];
    private bool _ownFormat;

    public ImportExportViewModel(FinanceStore store, Translator translator, IDateFormatter dates, ILocalizationService localization, TimeProvider time, AppLockService appLock)
    {
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
        CalendarNames = [translator["Calendar_Gregorian"], translator["Calendar_Persian"]];
        SeparatorNames = [translator["Import_DecimalPoint"], translator["Import_DecimalComma"]];
        SignNames = [translator["Import_SignNegativeExpense"], translator["Import_SignAllExpenses"], translator["Import_SignAllIncome"]];
    }

    public IReadOnlyList<string> DateFormats { get; } = CsvImport.DateFormats;

    public IReadOnlyList<string> CalendarNames { get; }

    public IReadOnlyList<string> SeparatorNames { get; }

    public IReadOnlyList<string> SignNames { get; }

    public ObservableCollection<string> InvalidLines { get; } = [];

    public ObservableCollection<BatchRow> Batches { get; } = [];

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

    public async Task LoadAsync()
    {
        var accounts = await _store.GetAccountsAsync(includeArchived: false);
        Accounts = [.. accounts.Select(a => new AccountChoice(a.Id, a.Name, a.CurrencyCode))];
        Account ??= Accounts.FirstOrDefault();
        DateFormat ??= DateFormats[0];
        CalendarIndex = _localization.CurrentCalendar == CalendarSystem.Persian ? 1 : 0;
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
            var path = Path.Combine(FileSystem.CacheDirectory, string.Create(CultureInfo.InvariantCulture, $"finance-export-{_time.GetLocalNow():yyyyMMdd-HHmm}.csv"));
            await File.WriteAllTextAsync(path, text, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
            ExportResult = _translator.Format("Export_Done", entries.Count);
            await Share.Default.RequestAsync(new ShareFileRequest { Title = _translator["Export_Title"], File = new ShareFile(path, "text/csv") });
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task PickAsync()
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
    private async Task PreviewAsync()
    {
        ImportError = null;
        var accounts = await _store.GetAccountsAsync();
        var categories = await _store.GetCategoriesAsync();
        var existing = await _store.GetEntriesAsync();
        string Name(Core.Categories.Category c) => CategoryLookup.NameOf(c, _translator);

        if (_ownFormat)
        {
            _preview = CsvImport.PreviewOwn(_rows, accounts, categories, Name, existing);
        }
        else
        {
            if (Account is null || DateColumn?.Index is not { } date || AmountColumn?.Index is not { } amount || DateFormat is null)
            {
                ImportError = _translator["Import_MappingIncomplete"];
                return;
            }

            var mapping = new ImportMapping(Account.Id, date, DateFormat, CalendarIndex == 1 ? PeriodCalendar.Persian : PeriodCalendar.Gregorian, amount,
                SeparatorIndex == 1 ? ',' : '.', (SignMode)SignIndex, TitleColumn?.Index, CategoryColumn?.Index, NoteColumn?.Index);
            _preview = CsvImport.PreviewGeneric(_rows, mapping, accounts, categories, Name, existing);
        }

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

    partial void OnSkipDuplicatesChanged(bool value) => UpdateCount();

    private void UpdateCount()
    {
        ImportCount = _preview.Count(r => r.Entry is not null && !r.AlreadyImported && !(SkipDuplicates && r.PossibleDuplicate));
        ImportButtonText = _translator.Format("Import_Button", ImportCount);
    }

    [RelayCommand]
    private async Task ImportAsync()
    {
        var entries = _preview.Where(r => r.Entry is not null && !r.AlreadyImported && !(SkipDuplicates && r.PossibleDuplicate)).Select(r => r.Entry!).ToList();
        if (entries.Count == 0)
        {
            return;
        }

        IsBusy = true;
        try
        {
            var result = await _store.ImportAsync(entries);
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
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task UndoAsync(BatchRow batch)
    {
        if (!await Shell.Current.DisplayAlertAsync(_translator["Import_Undo"], _translator["Import_UndoMessage"], _translator["Import_Undo"], _translator["Common_Cancel"]))
        {
            return;
        }

        await _store.UndoImportAsync(batch.BatchId);
        await LoadBatchesAsync();
    }
}
