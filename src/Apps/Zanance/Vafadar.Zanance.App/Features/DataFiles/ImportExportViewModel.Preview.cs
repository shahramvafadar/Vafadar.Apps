using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.Core.Categories;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Data.Importing;
using Vafadar.Zanance.Core.Money;

namespace Vafadar.Zanance.App.Features.DataFiles;

/// <summary>Read-only financial interpretation of one source line; each monetary packet keeps its own currency.</summary>
public sealed record ImportPreviewLine(int SourceLine, string Heading, string Title, string Accounts, string Category,
    string? Amount, string? DestinationAmount, string? OriginalAmount, string Status)
{
    /// <summary>Gets the local draft reason for the current generic source row.</summary>
    public string? CategoryHint { get; init; }
    /// <summary>Gets whether this original ambiguous generic draft supports manual category selection.</summary>
    public bool CanChooseCategory { get; init; }
    /// <summary>Gets whether the present local candidate can be explicitly adopted.</summary>
    public bool CanUseCategory { get; init; }
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

    /// <summary>Uses only the candidate from the exact currently displayed generic review row.</summary>
    [RelayCommand]
    private void UseCsvCategory(ImportPreviewLine? line)
    {
        if (line?.CanUseCategory != true || GetEditableRow(line) is not { Entry: { } entry, CategoryReview.CategoryId: { } category } ||
            _ledgerRead is not { } review) return;
        ApplyDraftCategory(review, entry, category);
    }

    /// <summary>Lets the user choose an eligible category or explicitly keep the row uncategorized.</summary>
    [RelayCommand]
    private async Task ChooseCsvCategoryAsync(ImportPreviewLine? line)
    {
        if (line is null || GetEditableRow(line) is not { Entry: { } entry } row || _ledgerRead is not { } review) return;
        var kind = entry.Kind == EntryKind.Income ? CategoryKind.Income : CategoryKind.Expense;
        var categories = review.Inputs.Categories.Where(category => !category.IsArchived && category.Kind == kind)
            .OrderBy(category => category.SortOrder).ThenBy(category => category.Id).ToArray();
        var lookup = _previewCategories!;
        var unassigned = _translator["CategoryReview_Unassigned"];
        var options = categories.Select((category, index) => $"{index + 1}. {lookup.Name(category.Id)}").ToArray();
        var epoch = Interlocked.Read(ref _interpretationEpoch);
        using var work = BeginWork(); var frame = work.Frame;
        try
        {
            var selected = await _interaction.ChooseAsync(_translator["Entry_Category"], _translator["Common_Cancel"],
                [unassigned, .. options]);
            if (!frame.IsCurrent || epoch != Interlocked.Read(ref _interpretationEpoch) || !ReferenceEquals(_ledgerRead, review) ||
                !ReferenceEquals(_preview.FirstOrDefault(item => item.Line == line.SourceLine), row)) return;
            var index = Array.IndexOf(options, selected);
            if (selected == unassigned)
                ApplyDraftCategory(review, entry, categories.FirstOrDefault(category => category.SystemKey == DefaultCategories.Uncategorized)?.Id);
            else if (index >= 0) ApplyDraftCategory(review, entry, categories[index].Id);
        }
        catch (Exception error) when (error is not OutOfMemoryException)
        { if (frame.IsCurrent) ImportError = CommercialFailureMessage.OrFallback(error, _translator, "Import_ReviewFailed"); }
    }

    private Core.DataFiles.ImportRow? GetEditableRow(ImportPreviewLine line) =>
        CanUseDataFiles && !_previewStale && !_ownFormat && !IsBankPreset && !_previewAccepted &&
        HasLedgerPreview && LedgerPreviewRows.Any(item => ReferenceEquals(item, line)) && line.CanChooseCategory
            ? _preview.FirstOrDefault(row => row.Line == line.SourceLine) : null;

    private void ApplyDraftCategory(ImportReadReview review, LedgerEntry entry, Guid? id)
    {
        try
        {
            review.SelectCategory(entry, id);
            ImportError = null;
            PublishLedgerPreviewPage();
        }
        catch (ImportReviewChangedException)
        {
            _previewStale = true; CanImport = false;
            ImportError = _translator["Import_InterpretationChanged"];
        }
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
            var category = row.CategoryReview;
            string? hint = category?.CategoryId is { } candidate
                ? _translator.Format("CategoryReview_Signal", _previewCategories!.Name(candidate))
                : category is null ? null : _translator[$"CategoryReview_{category.Reason}"];
            LedgerPreviewRows.Add(new(row.Line,
                _translator.Format("Import_ReviewedLine", row.Line, _dates.Format(entry.Date, DateFormatStyle.Short), kind),
                entry.Title ?? "", accounts, entry.CategoryId is null ? "" : _previewCategories!.Name(entry.CategoryId),
                MoneyText.Format(entry.Amount, account.CurrencyCode, culture), destinationAmount,
                entry.OriginalAmount is { } original && entry.OriginalCurrencyCode is { } currency
                    ? MoneyText.Format(original, currency, culture) : null, status)
            {
                CategoryHint = hint,
                CanChooseCategory = category is not null && !row.AlreadyImported,
                CanUseCategory = category?.CategoryId is { } proposed && proposed != entry.CategoryId && !row.AlreadyImported,
            });
        }
        NotifyLedgerPreview();
    }
}
