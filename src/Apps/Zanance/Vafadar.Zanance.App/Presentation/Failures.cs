using Vafadar.Localization;

namespace Vafadar.Zanance.App.Presentation;

/// <summary>
/// Turns an unexpected failure of a page load or a user action into a short message instead of a crash. Used where
/// an exception would otherwise end an <c>async void</c> method or an async command.
/// </summary>
internal static class Failures
{
    /// <summary>Runs <paramref name="action"/> and shows a message if it fails.</summary>
    public static async Task GuardAsync(Func<Task> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        try
        {
            await action();
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            await ShowAsync(ex);
        }
    }

    /// <summary>Shows that an action could not be completed; the details go to the debug output only.</summary>
    public static async Task ShowAsync(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        System.Diagnostics.Debug.WriteLine($"Unexpected failure: {exception}");
        try
        {
            if (Shell.Current is { } shell)
            {
                var translator = Translator.Instance;
                await shell.DisplayAlertAsync(translator["Error_Title"], translator["Error_Unexpected"], translator["Common_Ok"]);
            }
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            // Nothing left to show the message on (e.g. while the window is rebuilt).
            System.Diagnostics.Debug.WriteLine($"Could not show the failure: {ex.GetType().Name}");
        }
    }
}
