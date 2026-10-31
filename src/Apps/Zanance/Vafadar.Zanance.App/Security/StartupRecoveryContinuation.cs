namespace Vafadar.Zanance.App.Security;

/// <summary>Retains the original source and presentation across native startup recovery continuations.</summary>
internal static class StartupRecoveryContinuation
{
    /// <summary>Refuses retired source or presentation authority before another native or protected handoff.</summary>
    internal static void RequireCurrent(RecoverySetupAuthority source, Func<bool> ownsPresentation)
    {
        source.RequireCurrent();
        if (!ownsPresentation()) source.Retire();
        source.RequireCurrent();
    }

    /// <summary>Starts once only while current, cancels a departed waiter and rechecks before returning its real result.</summary>
    internal static async Task<T> AwaitAsync<T>(Func<Task<T>> action, RecoverySetupAuthority source, Func<bool> ownsPresentation)
    {
        RequireCurrent(source, ownsPresentation);
        var result = await action().WaitAsync(source.Token);
        RequireCurrent(source, ownsPresentation);
        return result;
    }
}
