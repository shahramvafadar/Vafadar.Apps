using Vafadar.Localization;
using Vafadar.Zanance.App.Interaction;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Plans;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Features.Plans;

/// <summary>Runs actual occurrence writes with one gate across consent, failure feedback and publication (D-136).</summary>
public sealed class OccurrenceCommands(PlanStore plans, Translator translator, IAppInteraction interaction)
{
    private bool _pending;

    /// <summary>Records the explicit actual payment; ledger validation remains beside the payment field.</summary>
    public Task<bool> PayAsync(Occurrence occurrence, long amount, DateOnly date, bool partial,
        Action<string> validationError, Func<Task> completed) => RunAsync(async () =>
    {
        var entry = Occurrences.CreateEntry(occurrence, amount, date, ReviewState.Confirmed);
        var result = partial ? await plans.PayPartAsync(occurrence, entry) : await plans.SettleAsync(occurrence, entry);
        if (result.Succeeded) { return true; }
        validationError(string.Join(Environment.NewLine, result.Errors.Select(e => translator[$"LedgerError_{e}"])));
        return false;
    }, completed);

    /// <summary>Links the reviewed actual entry only after explicit consent.</summary>
    public Task<bool> LinkAsync(Occurrence occurrence, Guid entryId, string title, string amount, Func<Task> completed) => RunAsync(async () =>
    {
        if (!await interaction.ConfirmAsync(translator["Occurrence_Link"], translator.Format("Occurrence_LinkMessage", title, amount),
            translator["Occurrence_Link"], translator["Common_Cancel"])) { return false; }
        await plans.LinkAsync(occurrence, entryId);
        return true;
    }, completed);

    /// <summary>Skips the reviewed occurrence without posting money.</summary>
    public Task<bool> SkipAsync(Occurrence occurrence, Func<Task> completed) => RunAsync(async () =>
    {
        await plans.SkipAsync(occurrence);
        return true;
    }, completed);

    /// <summary>Saves explicit date/amount/note overrides without replacing a rejected draft.</summary>
    public Task<bool> ChangeAsync(Occurrence occurrence, DateOnly due, long? amount, string? note, Func<Task> completed) => RunAsync(async () =>
    {
        await plans.ChangeOccurrenceAsync(occurrence, due, amount, note);
        return true;
    }, completed);

    /// <summary>Reopens a skipped occurrence, or explains and confirms reopening its reviewed settlement.</summary>
    public Task<bool> UndoAsync(Occurrence occurrence, Func<Task> completed) => RunAsync(async () =>
    {
        if (occurrence.Status == OccurrenceView.Skipped) { await plans.UnskipAsync(occurrence); }
        else if (occurrence.Status == OccurrenceView.Settled)
        {
            if (!await interaction.ConfirmAsync(translator["Occurrence_Undo"], translator["Occurrence_UndoMessage"],
                translator["Occurrence_ReopenConfirm"], translator["Common_Cancel"])) { return false; }
            await plans.UnsettleAsync(occurrence);
        }
        else { return false; }
        return true;
    }, completed);

    /// <summary>Separates rejected writes from a committed write whose display/navigation could not finish.</summary>
    private async Task<bool> RunAsync(Func<Task<bool>> write, Func<Task> completed)
    {
        if (_pending) { return false; }
        _pending = true;
        try
        {
            bool committed;
            try { committed = await write(); }
            catch (Exception exception) when (exception is not OutOfMemoryException)
            {
                await interaction.ShowFailureAsync(exception);
                return false;
            }
            if (!committed) { return false; }
            // The caller invalidates the reviewed snapshot before refreshing. A failed refresh must not permit
            // another payment from the old form or misreport an already committed write as rolled back.
            try { await completed(); }
            catch (Exception exception) when (exception is not OutOfMemoryException)
            {
                await interaction.AlertAsync(translator["Error_Title"], translator["Occurrence_SavedRefreshFailed"], translator["Common_Ok"]);
            }
            return true;
        }
        finally { _pending = false; }
    }
}
