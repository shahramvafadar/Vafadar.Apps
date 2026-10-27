namespace Vafadar.Zanance.App;

/// <summary>Independent shell-to-ready boundary; it never substitutes a PID for a refused SDK startup record.</summary>
internal sealed record LoadedColdShellFacts(string Request, string IntentRequest, string Run, string IntentRun,
    string Shape, string IntentShape, int ActualPid, int ActualUid, bool ProcessWasAbsent, bool RequestConsumedOnce,
    long ShellMonotonic, long ShellBootTime, long ActivityMonotonic, long ActivityBootTime,
    long ReadyMonotonic, long ReadyBootTime, string BeforeBoot, string ActualBoot,
    bool OriginalHomeReady, bool ExactProfileAndSource);

/// <summary>Keeps a conservative native command-dispatch boundary separate from the OS launch metric.</summary>
internal static class LoadedColdShellPolicy
{
    /// <summary>Rejects replay, another request/owner/boot, sleep and incompatible monotonic evidence.</summary>
    internal static double RequireShellToReadyMilliseconds(LoadedColdShellFacts facts)
    {
        ArgumentNullException.ThrowIfNull(facts);
        static bool Identifier(string value) => value.Length == 32 && value.All(c => c is >= '0' and <= '9' or >= 'a' and <= 'f');
        if (!Identifier(facts.Request) || facts.Request != facts.IntentRequest || !Identifier(facts.Run)
            || facts.Run != facts.IntentRun || facts.Shape is not ("reference" or "tenfold") || facts.Shape != facts.IntentShape
            || facts.ActualPid <= 0 || facts.ActualUid != 10381 || !facts.ProcessWasAbsent || !facts.RequestConsumedOnce
            || !Guid.TryParseExact(facts.BeforeBoot, "D", out var boot) || boot == Guid.Empty || facts.BeforeBoot != facts.ActualBoot
            || facts.ShellMonotonic <= 0 || facts.ActivityMonotonic <= facts.ShellMonotonic
            || facts.ActivityMonotonic - facts.ShellMonotonic > 60_000_000_000L
            || facts.ReadyMonotonic <= facts.ActivityMonotonic || facts.ReadyBootTime <= facts.ActivityBootTime
            || facts.ShellBootTime < facts.ShellMonotonic || facts.ActivityBootTime < facts.ActivityMonotonic
            || !facts.OriginalHomeReady || !facts.ExactProfileAndSource)
            throw new InvalidOperationException("Independent native shell request does not belong to this complete ready lifetime.");
        // CLOCK_BOOTTIME includes suspend; CLOCK_MONOTONIC/nanoTime do not. A real
        // suspend or inconsistent clock pairing cannot silently shorten this interval.
        var offset = facts.ShellBootTime - facts.ShellMonotonic;
        if (Math.Abs((facts.ActivityBootTime - facts.ActivityMonotonic) - offset) > 5_000_000L
            || Math.Abs((facts.ReadyBootTime - facts.ReadyMonotonic) - offset) > 5_000_000L)
            throw new InvalidOperationException("Independent native clocks disagree or the device slept during startup.");
        return (facts.ReadyMonotonic - facts.ShellMonotonic) / 1_000_000d;
    }
}
