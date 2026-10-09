using Vafadar.Localization;

namespace Vafadar.Zanance.App.Presentation;

/// <summary>An editor that knows whether the user changed something since it was opened or saved.</summary>
internal interface IUnsavedChanges
{
    /// <summary>Gets a value indicating whether leaving now would lose input.</summary>
    bool IsDirty { get; }
}

/// <summary>
/// The same question in every editor before input is lost (CR12): Cancel and the Android back button leave at once when
/// nothing changed, and ask "Discard changes?" otherwise.
/// </summary>
internal static class UnsavedChanges
{
    /// <summary>
    /// Joins the values an editor compares to see a change into one text, independent of the culture. A culture-dependent
    /// join failed in Persian: a date is then written in the Persian calendar, which cannot show an unset date (year 1),
    /// so opening the expense editor reported an unexpected error.
    /// </summary>
    public static string Fingerprint(params object?[] values) =>
        DraftFingerprint.Create(values);

    /// <summary>Asks whether unsaved changes may be discarded.</summary>
    public static Task<bool> ConfirmDiscardAsync()
    {
        var translator = Translator.Instance;
        return Shell.Current.DisplayAlertAsync(
            translator["Common_DiscardTitle"], translator["Common_DiscardMessage"], translator["Common_Discard"], translator["Common_KeepEditing"]);
    }

    /// <summary>Leaves the editor, after asking when something changed.</summary>
    public static async Task LeaveAsync(IUnsavedChanges editor)
    {
        ArgumentNullException.ThrowIfNull(editor);
        if (!editor.IsDirty || await ConfirmDiscardAsync())
        {
            await Shell.Current.GoToAsync("..");
        }
    }

    /// <summary>
    /// Handles the Android back button of an editor page: returns <see langword="true"/> (handled) when something changed
    /// and asks first; otherwise the page goes back as usual.
    /// </summary>
    public static bool OnBackButton(Page page, IUnsavedChanges editor)
    {
        ArgumentNullException.ThrowIfNull(page);
        ArgumentNullException.ThrowIfNull(editor);
        if (!editor.IsDirty)
        {
            return false;
        }

        page.Dispatcher.Dispatch(async () => await Failures.GuardAsync(() => LeaveAsync(editor)));
        return true;
    }
}
