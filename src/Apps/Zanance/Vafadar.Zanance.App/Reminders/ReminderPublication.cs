namespace Vafadar.Zanance.App.Reminders;

/// <summary>Preserves the native SDK's explicit request acceptance result before consuming device threshold levels.</summary>
internal static class ReminderPublication
{
    /// <summary>A completed native Task can still reject the request; rejection must remain retryable.</summary>
    internal static async Task RequireAcceptedAsync(Task<bool> publication)
    {
        ArgumentNullException.ThrowIfNull(publication);
        if (!await publication) throw new InvalidOperationException("The native notification request was not accepted.");
    }
}
