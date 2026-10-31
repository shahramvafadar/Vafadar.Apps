using Vafadar.Localization;
using Vafadar.Zanance.App.Interaction;

namespace Vafadar.Zanance.App.Features.Receipts;

/// <summary>The user's explicit receipt source; help and cancellation never open a native source.</summary>
internal enum ReceiptSourceChoice
{
    /// <summary>The user closed source selection without opening a source.</summary>
    Cancelled,
    /// <summary>The user explicitly selected the camera.</summary>
    Photo,
    /// <summary>The user explicitly selected the file picker.</summary>
    File,
}

/// <summary>Offers source-specific reading help without adding a confirmation to the ordinary camera/file route.</summary>
internal static class ReceiptSourceChooser
{
    /// <summary>Returns only a chosen source, refusing an original page which retired while a native answer was pending.</summary>
    internal static async Task<ReceiptSourceChoice> ChooseAsync(Translator translator, IAppInteraction interaction,
        bool canTakePhoto, bool showLatinImageHelp, Func<bool>? isCurrent = null)
    {
        using var origin = new ReceiptReadOrigin(isCurrent ?? (() => true));
        return await RunAsync(translator, interaction, canTakePhoto, showLatinImageHelp, origin,
            action => action(), _ => false, () => Task.FromResult(ReceiptSourceChoice.Photo),
            () => Task.FromResult(ReceiptSourceChoice.File), ReceiptSourceChoice.Cancelled);
    }

    /// <summary>Owns the original source/help/native handoff across temporary cover, without repeating an answered choice.</summary>
    internal static async Task<T> RunAsync<T>(Translator translator, IAppInteraction interaction,
        bool canTakePhoto, bool showLatinImageHelp, ReceiptReadOrigin origin,
        Func<Func<Task>, Task> queueWhenUnlocked, Func<Func<Task>, bool> retainIfLocked,
        Func<Task<T>> openPhoto, Func<Task<T>> openFile, T cancelled)
    {
        if (!origin.IsCurrent) return cancelled;
        var completed = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var retirement = origin.Invalidated.Register(() => completed.TrySetResult(cancelled));
        ReceiptSourceChoice? choice = !canTakePhoto && !showLatinImageHelp ? ReceiptSourceChoice.File : null;
        var helpPending = false;
        // A queued gate returns before execution. Observe its complete delivery independently of the result lifetime.
        _ = DeliverAsync();
        return await completed.Task;

        async Task DeliverAsync()
        {
            try { await queueWhenUnlocked(ContinueAsync); }
            catch (Exception error) when (error is not OutOfMemoryException)
            { if (origin.IsCurrent) completed.TrySetException(error); else completed.TrySetResult(cancelled); }
        }
        async Task ContinueAsync()
        {
            try
            {
                while (origin.IsCurrent)
                {
                    // D-710: the current access drain retains this exact chosen next step ahead of later input.
                    // Reopening the source sheet would repeat consent; opening a native source above cover would leak it.
                    if (retainIfLocked(ContinueAsync)) return;
                    if (helpPending)
                    {
                        await interaction.AlertAsync(translator["Receipt_ImageHelp"], translator["Receipt_ImageHelpText"], translator["Common_Ok"]);
                        helpPending = false;
                        continue;
                    }
                    if (choice is { } selected)
                    {
                        if (selected == ReceiptSourceChoice.Cancelled) { completed.TrySetResult(cancelled); return; }
                        var result = await (selected == ReceiptSourceChoice.Photo ? openPhoto() : openFile());
                        completed.TrySetResult(origin.IsCurrent ? result : cancelled);
                        return;
                    }
                    var photo = translator["Receipt_TakePhoto"];
                    var file = translator["Receipt_ChooseFile"];
                    var help = translator["Receipt_ImageHelp"];
                    var actions = new List<string>();
                    if (canTakePhoto) actions.Add(photo);
                    actions.Add(file);
                    if (showLatinImageHelp) actions.Add(help);
                    var answer = await interaction.ChooseAsync(translator["Receipt_SourceTitle"], translator["Common_Cancel"], [.. actions]);
                    if (showLatinImageHelp && answer == help) helpPending = true;
                    else choice = canTakePhoto && answer == photo ? ReceiptSourceChoice.Photo
                        : answer == file ? ReceiptSourceChoice.File : ReceiptSourceChoice.Cancelled;
                }
                completed.TrySetResult(cancelled);
            }
            catch (Exception error) when (error is not OutOfMemoryException)
            { if (origin.IsCurrent) completed.TrySetException(error); else completed.TrySetResult(cancelled); }
        }
    }
}
