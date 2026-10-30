using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.Core.DataFiles;
using Vafadar.Zanance.Core.Money;
using Vafadar.Zanance.Data.Importing;
using Vafadar.Zanance.App.Presentation;

namespace Vafadar.Zanance.App.Features.DataFiles;

/// <summary>A complete read-only source-row review, paged rather than silently limited to the first few records.</summary>
public sealed record BankCsvLine(string Heading, string Detail);

public sealed partial class ImportExportViewModel
{
    private ImportReadReview? _bankRead;
    private string? _bankOwner;
    private bool _supaPreset;
    private int _bankEpoch, _bankPage;
    private IReadOnlyList<BankCsvLine> _bankLines = [];
    /// <summary>Gets or sets whether the picked source uses an explicitly selected original cashflow contract.</summary>
    [ObservableProperty] public partial bool IsBankPreset { get; private set; }
    /// <summary>Gets or sets the explicit encoding for the next original bank file.</summary>
    [ObservableProperty] public partial int BankEncodingIndex { get; set; }
    /// <summary>Gets or sets explicit confirmation that these are posted ordinary income/expenses, not inferred relationships.</summary>
    [ObservableProperty] public partial bool BankAcknowledged { get; set; }
    /// <summary>Gets original-source identities that this complete preview will disclose locally.</summary>
    [ObservableProperty] public partial string? BankSourceText { get; private set; }
    /// <summary>Gets the current page of complete bank-source review rows.</summary>
    public ObservableCollection<BankCsvLine> BankLines { get; } = [];
    /// <summary>Gets explicit native encoding captions for the next picker.</summary>
    public IReadOnlyList<string> BankEncodings => ["UTF-8", "Windows-1252"];
    /// <summary>Gets the limitations of the explicitly selected format, rather than another software export.</summary>
    public string BankPresetWarning => _translator[_supaPreset ? "SupaCsv_Warning" : "BankCsv_Warning"];
    /// <summary>Gets explicit consent for this original format, including its software-history identity boundary.</summary>
    public string BankPresetAcknowledgement => _translator[_supaPreset ? "SupaCsv_Acknowledge" : "BankCsv_Acknowledge"];
    /// <summary>Gets the translated current page and complete row count.</summary>
    public string BankPageText => _translator.Format("BankCsv_Page", _bankPage + 1, Math.Max(1, (_bankLines.Count + 24) / 25), _bankLines.Count);

    partial void OnBankAcknowledgedChanged(bool value) { if (IsBankPreset) { UpdateOverlapChoices(); RefreshActions(); } }
    partial void OnAccountChanged(Features.Entries.AccountChoice? value) { if (IsBankPreset) InvalidateBankReview(); }
    /// <summary>Retires continuations from a departed original native form; raw data is retained for a fresh review.</summary>
    public void RetireBankReview() => Deactivate();

    // Internal account/permission changes retire their review without deactivating the still-current original page.
    private void InvalidateBankReview()
    {
        Interlocked.Increment(ref _bankEpoch); _bankRead = null; BankAcknowledged = false;
        if (_ledgerRead is not null) { _ledgerRead = null; _previewStale = true; CanImport = false; }
        if (IsBankPreset) { _previewStale = true; CanImport = false; HasPreview = false; BankLines.Clear(); }
        RefreshActions();
    }

    /// <summary>Chooses one original preset file without inventing a bank or treating CSV-CAMT as XML.</summary>
    [RelayCommand(CanExecute = nameof(CanUseDataFiles))]
    private Task PickStarMoneyAsync() => PickOriginalBankCsvAsync(supa: false);

    /// <summary>Selects the original SUPA CSV contract with its prescribed UTF-8 encoding.</summary>
    [RelayCommand(CanExecute = nameof(CanUseDataFiles))]
    private Task PickSupaAsync() => PickOriginalBankCsvAsync(supa: true);

    private async Task PickOriginalBankCsvAsync(bool supa)
    {
        if (!CanUseDataFiles) return;
        using var work = BeginWork(); var frame = work.Frame; var epoch = _bankEpoch;
        try
        {
            var originalFile = await CaptureFileAsync(frame);
            var selected = await _files.PickBankCsvAsync(_translator[supa ? "SupaCsv_Pick" : "BankCsv_Pick"],
                supa || BankEncodingIndex == 0 ? BankCsvEncoding.Utf8 : BankCsvEncoding.Windows1252, frame.EnsureCurrent, frame.Token);
            frame.EnsureCurrent();
            if (selected is null || epoch != _bankEpoch) return;
            var rows = await Task.Run(() => supa ? SupaCsv.Read(selected.Text) : StarMoneyCsv.Read(selected.Text), frame.Token);
            var currentFile = await CaptureFileAsync(frame);
            if (epoch != _bankEpoch) return;
            if (originalFile != currentFile) throw new InvalidOperationException("The original bank-import profile changed.");
            _rows = rows; _bankOwner = originalFile; IsBankPreset = true; _bankRead = null; _holdingsContent = null; _ownFormat = false;
            _supaPreset = supa; OnPropertyChanged(nameof(BankPresetWarning)); OnPropertyChanged(nameof(BankPresetAcknowledgement));
            FileName = selected.Name; ShowMapping = false; _previewAccepted = false; _previewStale = true; _preview = [];
            HasPreview = false; CanImport = false; ImportError = null; ImportResult = null; UndoError = null;
            BankAcknowledged = false; Overlaps.Clear(); BankLines.Clear();
            await ReadPreviewAsync(frame);
        }
        catch (Exception error) when (error is not OutOfMemoryException && !frame.IsCurrent) { }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        { ImportError = _translator["BankCsv_Failed"]; if (IsBankPreset) InvalidateBankReview(); }
    }

    private async Task ReadBankPreviewAsync(FrameOwner frame)
    {
        if (_bankOwner is null || Account is null) throw new InvalidOperationException("Choose an original bank file and target account.");
        var epoch = _bankEpoch; var owner = _bankOwner; var accountId = Account.Id; var source = _rows; var supa = _supaPreset; var selected = Account;
        // SQLite async reads and AOT serialization still perform synchronous native work (D-297).
        // Keep original input preparation off the UI thread while the native picker returns focus.
        var prepared = await Task.Run(async () =>
        {
            void CurrentReview()
            {
                frame.EnsureCurrent();
                if (epoch != _bankEpoch || Account?.Id != accountId)
                    throw new InvalidOperationException("The original bank form was retired.");
            }
            var original = await _store.ReadImportReviewAsync(owner, frame.Token, validateNative: CurrentReview);
            var target = original.Inputs.Accounts.SingleOrDefault(a => a.Id == accountId && !a.IsArchived)
                ?? throw new InvalidOperationException("The reviewed target account changed.");
            if (target.Name != selected.Name || target.CurrencyCode != selected.CurrencyCode)
                throw new InvalidOperationException("Choose the current original bank account caption and currency again.");
            var proposed = supa ? SupaCsv.Preview(source, target, original.Inputs.Categories, original.Inputs.Entries, original.ConsumedIds)
                : StarMoneyCsv.Preview(source, target, original.Inputs.Categories, original.Inputs.Entries, original.ConsumedIds);
            original.Bind(proposed.Where(r => r.Entry is not null).Select(r => r.Entry!)); original.EnsureCurrent();
            return (Read: original, Account: target, Preview: proposed);
        }, frame.Token);
        frame.EnsureCurrent();
        if (epoch != _bankEpoch || Account?.Id != accountId) throw new InvalidOperationException("The original bank form was retired.");
        var (read, account, preview) = prepared;
        OnPropertyChanged(nameof(BankPresetWarning)); OnPropertyChanged(nameof(BankPresetAcknowledgement));
        _bankRead = read; _preview = preview; _existingEntries = read.Inputs.Entries;
        _previewAccounts = read.Inputs.Accounts.ToDictionary(a => a.Id);
        _previewCategories = new CategoryLookup(read.Inputs.Categories, _translator);
        _bankPage = 0;
        _bankLines = preview.Select(row => row.Entry is { } entry ? new BankCsvLine(
            _translator.Format("BankCsv_Row", row.Line, _dates.Format(entry.Date, DateFormatStyle.Short),
                MoneyText.Format(entry.Kind == Core.Ledger.EntryKind.Expense ? -entry.Amount : entry.Amount, account.CurrencyCode, _localization.CurrentCulture)),
            _translator.Format("BankCsv_Detail", entry.Title ?? "", entry.Payee ?? "", entry.Note ?? "",
                row.AlreadyImported ? _translator["BankCsv_Known"] : row.PossibleDuplicate ? _translator["BankCsv_Duplicate"] : _translator["BankCsv_New"]))
            : new BankCsvLine(_translator.Format("Import_InvalidLine", row.Line, _translator[$"Import_Error_{row.Error}"]), "")).ToArray();
        PublishBankPage();
        if (supa)
        {
            var suffix = SupaCsv.SourceSuffix(source);
            BankSourceText = suffix is null ? _translator.Format("SupaCsv_SourceMissing", account.Name, account.CurrencyCode)
                : _translator.Format("SupaCsv_Source", account.Name, account.CurrencyCode, suffix);
        }
        else
        {
            var sourceColumn = source[0].Select((name, index) => (name, index)).Single(p => p.name.Trim() == "QkIBAN").index;
            var sourceAccount = source[1][sourceColumn].Replace(" ", "", StringComparison.Ordinal).Trim();
            BankSourceText = _translator.Format("BankCsv_Source", account.Name, account.CurrencyCode, sourceAccount[^4..]);
        }
        PreviewText = _translator.Format("Import_Preview", preview.Count(r => r.Entry is not null && !r.AlreadyImported), preview.Count(r => r.AlreadyImported), preview.Count(r => r.Error is not null));
        InvalidLines.Clear(); foreach (var row in preview.Where(r => r.Error is not null).Take(8))
            InvalidLines.Add(_translator.Format("Import_InvalidLine", row.Line, _translator[$"Import_Error_{row.Error}"]));
        HasDuplicates = preview.Any(r => r.PossibleDuplicate && !r.AlreadyImported);
        var beforeOpening = preview.Count(r => r.BeforeOpening && !r.AlreadyImported);
        WarningText = BankPresetWarning + (beforeOpening == 0 ? "" : Environment.NewLine + _translator.Format("Import_BeforeOpening", beforeOpening));
        _previewStale = false; HasPreview = true; UpdateCount();
    }
    private void PublishBankPage()
    {
        BankLines.Clear(); foreach (var line in _bankLines.Skip(_bankPage * 25).Take(25)) BankLines.Add(line);
        OnPropertyChanged(nameof(BankPageText));
    }
    /// <summary>Moves through the complete original bank preview without changing financial or source data.</summary>
    [RelayCommand] private void BankNext() { if (!IsBusy && (_bankPage + 1) * 25 < _bankLines.Count) { _bankPage++; PublishBankPage(); } }
    /// <summary>Returns to an earlier original preview page without selecting a different source.</summary>
    [RelayCommand] private void BankPrevious() { if (!IsBusy && _bankPage > 0) { _bankPage--; PublishBankPage(); } }
}
