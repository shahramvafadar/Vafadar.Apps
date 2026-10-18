using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Money;

namespace Vafadar.Zanance.App.Features.DataFiles;

/// <summary>Read-only financial interpretation of one source line; each monetary packet keeps its own currency.</summary>
public sealed record ImportPreviewLine(int SourceLine, string Heading, string Title, string Accounts, string Category,
    string? Amount, string? DestinationAmount, string? OriginalAmount, string Status)
{
    /// <summary>Gets whether this source line has an interpreted title.</summary>
    public bool HasTitle => !string.IsNullOrEmpty(Title);
    /// <summary>Gets whether the interpreted row includes a destination amount.</summary>
    public bool HasDestinationAmount => DestinationAmount is not null;
    /// <summary>Gets whether a separately recorded original-currency amount exists.</summary>
    public bool HasOriginalAmount => OriginalAmount is not null;
}

public sealed partial class ImportExportViewModel
{
    private const int LedgerPreviewPageSize = 10;
    private int _ledgerPreviewPage;
    /// <summary>Signals an actual retained-page change after its rows are published; ordinary status refreshes do not move the viewport.</summary>
    public event EventHandler? LedgerPreviewPageChanged;
    /// <summary>Gets only the current bounded page of original own-format or generic interpretations.</summary>
    public ObservableCollection<ImportPreviewLine> LedgerPreviewRows { get; } = [];
    /// <summary>Gets whether a ledger interpretation is available, independently of bank and holdings previews.</summary>
    public bool HasLedgerPreview => HasPreview && !IsBankPreset && _holdingsContent is null && _preview.Count > 0;
    /// <summary>Gets the current page and complete source-row count without discarding invalid or duplicate lines.</summary>
    public string LedgerPreviewPageText => _translator.Format("BankCsv_Page", _ledgerPreviewPage + 1,
        Math.Max(1, (_preview.Count + LedgerPreviewPageSize - 1) / LedgerPreviewPageSize), _preview.Count);
    /// <summary>Gets whether an earlier retained page may be shown without reading or writing financial data.</summary>
    public bool CanShowPreviousLedgerPage => HasLedgerPreview && !IsBusy && _ledgerPreviewPage > 0;
    /// <summary>Gets whether another retained page may be shown without changing the reviewed source.</summary>
    public bool CanShowNextLedgerPage => HasLedgerPreview && !IsBusy
        && (_ledgerPreviewPage + 1) * LedgerPreviewPageSize < _preview.Count;

    /// <summary>Returns to a previous original page; this never imports or refreshes a financial source.</summary>
    [RelayCommand(CanExecute = nameof(CanShowPreviousLedgerPage))]
    private void PreviousLedgerPage()
    {
        if (!CanShowPreviousLedgerPage) return;
        _ledgerPreviewPage--; PublishLedgerPreviewPage(); LedgerPreviewPageChanged?.Invoke(this, EventArgs.Empty);
    }
    /// <summary>Shows the next original page, including invalid lines beyond the first summary errors.</summary>
    [RelayCommand(CanExecute = nameof(CanShowNextLedgerPage))]
    private void NextLedgerPage()
    {
        if (!CanShowNextLedgerPage) return;
        _ledgerPreviewPage++; PublishLedgerPreviewPage(); LedgerPreviewPageChanged?.Invoke(this, EventArgs.Empty);
    }

    private void ClearLedgerPreview()
    {
        _ledgerPreviewPage = 0; LedgerPreviewRows.Clear(); NotifyLedgerPreview();
    }
    private void NotifyLedgerPreview()
    {
        OnPropertyChanged(nameof(HasLedgerPreview)); OnPropertyChanged(nameof(LedgerPreviewPageText));
        OnPropertyChanged(nameof(CanShowPreviousLedgerPage)); OnPropertyChanged(nameof(CanShowNextLedgerPage));
        PreviousLedgerPageCommand.NotifyCanExecuteChanged(); NextLedgerPageCommand.NotifyCanExecuteChanged();
    }
    private void PublishLedgerPreviewPage()
    {
        LedgerPreviewRows.Clear();
        var culture = _localization.CurrentCulture;
        foreach (var row in _preview.Skip(_ledgerPreviewPage * LedgerPreviewPageSize).Take(LedgerPreviewPageSize))
        {
            if (row.Entry is not { } entry)
            {
                LedgerPreviewRows.Add(new(row.Line,
                    _translator.Format("Import_InvalidLine", row.Line, _translator[$"Import_Error_{row.Error}"]),
                    "", "", "", null, null, null, _translator["Import_ReviewedInvalid"]));
                continue;
            }
            // Use the captured interpreted account; a missing identity must not silently become EUR.
            var account = _previewAccounts[entry.AccountId];
            var kind = _translator[$"EntryKind_{entry.Kind}"];
            if (entry.Kind == EntryKind.Adjustment && entry.Direction is { } direction)
                kind += " · " + _translator[$"Sync_{direction}"];
            var accounts = _translator["Entry_Account"] + ": " + account.Name;
            string? destinationAmount = null;
            if (entry.ToAccountId is { } destinationId)
            {
                var destination = _previewAccounts[destinationId];
                accounts += Environment.NewLine + _translator["Entry_ToAccount"] + ": " + destination.Name;
                var received = entry.ToAmount ?? (account.CurrencyCode == destination.CurrencyCode ? entry.Amount : (long?)null);
                if (received is { } amount) destinationAmount = MoneyText.Format(amount, destination.CurrencyCode, culture);
            }
            var status = row.AlreadyImported ? _translator["BankCsv_Known"]
                : row.PossibleDuplicate ? _translator[SkipDuplicates ? "Import_ReviewedDuplicateSkipped" : "Import_ReviewedDuplicateIncluded"]
                : _translator["Import_ReviewedNew"];
            if (row.BeforeOpening && !row.AlreadyImported)
                status += Environment.NewLine + _translator.Format("Import_BeforeOpening", 1);
            LedgerPreviewRows.Add(new(row.Line,
                _translator.Format("Import_ReviewedLine", row.Line, _dates.Format(entry.Date, DateFormatStyle.Short), kind),
                entry.Title ?? "", accounts, entry.CategoryId is null ? "" : _previewCategories!.Name(entry.CategoryId),
                MoneyText.Format(entry.Amount, account.CurrencyCode, culture), destinationAmount,
                entry.OriginalAmount is { } original && entry.OriginalCurrencyCode is { } currency
                    ? MoneyText.Format(original, currency, culture) : null, status));
        }
        NotifyLedgerPreview();
    }
}
