using Vafadar.Localization;
using Vafadar.Zanance.App.Interaction;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Plans;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Features.Plans;

/// <summary>Runs actual occurrence writes with one gate across consent, failure feedback and publication (D-136).</summary>
public sealed class OccurrenceCommands(PlanStore plans, Translator translator, IAppInteraction interaction)
{
    private bool _pending;

    /// <summary>Records the explicit actual payment; ledger validation remains beside the payment field.</summary>
    public Task<bool> PayAsync(Occurrence occurrence, long amount, DateOnly date, bool partial,
        Action<string> validationError, Func<Task> completed, string? expectedPath = null, string? expectedCurrency = null,
        Func<bool>? isCurrent = null, OccurrenceReadSnapshot? original = null, Func<bool, bool>? canNotify = null, Action<Exception>? rejected = null) => RunAsync(async () =>
    {
        var entry = Occurrences.CreateEntry(occurrence, amount, date, ReviewState.Confirmed);
        var result = partial ? await plans.PayPartAsync(occurrence, entry, expectedPath: expectedPath, expectedCurrency: expectedCurrency, isCurrent: isCurrent, original: original)
            : await plans.SettleAsync(occurrence, entry, expectedPath: expectedPath, expectedCurrency: expectedCurrency, isCurrent: isCurrent, original: original);
        if (result.Succeeded) { return true; }
        validationError(string.Join(Environment.NewLine, result.Errors.Select(e => translator[$"LedgerError_{e}"])));
        return false;
    }, completed, canNotify, rejected);

    /// <summary>Links the reviewed actual entry only after explicit consent.</summary>
    public Task<bool> LinkAsync(Occurrence occurrence, Guid entryId, string title, string amount, Func<Task> completed,
        OccurrenceReadSnapshot? original = null, Func<bool>? isCurrent = null,
        Func<bool, bool>? canNotify = null, Action<Exception>? rejected = null) => RunAsync(async () =>
    {
        if (!await interaction.ConfirmAsync(translator["Occurrence_Link"], translator.Format("Occurrence_LinkMessage", title, amount),
            translator["Occurrence_Link"], translator["Common_Cancel"])) { return false; }
        await plans.LinkAsync(occurrence, entryId, original: original, isCurrent: isCurrent);
        return true;
    }, completed, canNotify, rejected);

    /// <summary>Skips the reviewed occurrence without posting money.</summary>
    public Task<bool> SkipAsync(Occurrence occurrence, Func<Task> completed,
        OccurrenceReadSnapshot? original = null, Func<bool>? isCurrent = null,
        Func<bool, bool>? canNotify = null, Action<Exception>? rejected = null) => RunAsync(async () =>
    {
        await plans.SkipAsync(occurrence, original: original, isCurrent: isCurrent);
        return true;
    }, completed, canNotify, rejected);

    /// <summary>Saves explicit date/amount/note overrides without replacing a rejected draft.</summary>
    public Task<bool> ChangeAsync(Occurrence occurrence, DateOnly due, long? amount, string? note, Func<Task> completed,
        string? expectedPath = null, string? expectedCurrency = null, Func<bool>? isCurrent = null, OccurrenceReadSnapshot? original = null, Func<bool, bool>? canNotify = null, Action<Exception>? rejected = null) => RunAsync(async () =>
    {
        await plans.ChangeOccurrenceAsync(occurrence, due, amount, note, expectedPath: expectedPath, expectedCurrency: expectedCurrency, isCurrent: isCurrent, original: original);
        return true;
    }, completed, canNotify, rejected);

    /// <summary>Reopens a skipped occurrence, or explains and confirms reopening its reviewed settlement.</summary>
    public Task<bool> UndoAsync(Occurrence occurrence, Func<Task> completed,
        OccurrenceReadSnapshot? original = null, Func<bool>? isCurrent = null,
        Func<bool, bool>? canNotify = null, Action<Exception>? rejected = null) => RunAsync(async () =>
    {
        if (occurrence.Status == OccurrenceView.Skipped) { await plans.UnskipAsync(occurrence, original: original, isCurrent: isCurrent); }
        else if (occurrence.Status == OccurrenceView.Settled)
        {
            if (!await interaction.ConfirmAsync(translator["Occurrence_Undo"], translator["Occurrence_UndoMessage"],
                translator["Occurrence_ReopenConfirm"], translator["Common_Cancel"])) { return false; }
            await plans.UnsettleAsync(occurrence, original: original, isCurrent: isCurrent);
        }
        else { return false; }
        return true;
    }, completed, canNotify, rejected);

    /// <summary>Separates rejected writes from a committed write whose display/navigation could not finish.</summary>
    private async Task<bool> RunAsync(Func<Task<bool>> write, Func<Task> completed, Func<bool, bool>? canNotify, Action<Exception>? rejected)
    {
        // D-937: a departed form cannot show errors in a replacement root. A known
        // accepted write can still notify its same original root after normal Back.
        if (_pending || canNotify?.Invoke(false) == false) { return false; }
        _pending = true;
        try
        {
            bool committed; var notificationFailed = false;
            try { committed = await write(); }
            catch (CommittedWriteNotificationException<SaveResult> accepted) when (accepted.Result.Succeeded)
            { committed = true; notificationFailed = true; }
            catch (CommittedWriteNotificationException<IReadOnlyList<LedgerEntry>>)
            { committed = true; notificationFailed = true; }
            catch (CommittedWriteNotificationException)
            { committed = true; notificationFailed = true; }
            catch (Exception exception) when (exception is not OutOfMemoryException)
            {
                if (canNotify?.Invoke(false) != false)
                {
                    rejected?.Invoke(exception);
                    await interaction.ShowFailureAsync(exception);
                }
                return false;
            }
            if (!committed) { return false; }
            // The caller invalidates the reviewed snapshot before refreshing. A failed refresh must not permit
            // another payment from the old form or misreport an already committed write as rolled back.
            var refreshed = false;
            try { await completed(); refreshed = true; }
            catch (Exception exception) when (exception is not OutOfMemoryException)
            {
                if (canNotify?.Invoke(true) != false)
                    await interaction.AlertAsync(translator["Error_Title"], translator["Occurrence_SavedRefreshFailed"], translator["Common_Ok"]);
            }
            // D-399: retirement runs before fallible feedback. Busy remains held through the original native alert.
            if (notificationFailed && refreshed && canNotify?.Invoke(true) != false)
                await interaction.AlertAsync(translator["Error_Title"], translator["Occurrence_SavedNotificationFailed"], translator["Common_Ok"]);
            return true;
        }
        finally { _pending = false; }
    }
}
