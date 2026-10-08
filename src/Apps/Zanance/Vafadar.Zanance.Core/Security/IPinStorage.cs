namespace Vafadar.Zanance.Core.Security;

/// <summary>Device-protected storage for the app PIN verifier and durable attempt limit; never part of a backup.</summary>
public interface IPinStorage
{
    /// <summary>Reads the verifier, or null only when no PIN is configured. Unreadable required state must throw.</summary>
    Task<PinState?> ReadAsync();

    /// <summary>Atomically saves the verifier and attempt state, or removes the PIN.</summary>
    Task WriteAsync(PinState? state);
}

/// <summary>A versioned, salted verifier and the persisted retry deadline; contains no plain PIN.</summary>
public sealed record PinState(int Version, int Iterations, byte[] Salt, byte[] Hash, int Failures, DateTimeOffset? RetryAt);

/// <summary>The outcome of checking a PIN.</summary>
public enum PinOutcome
{
    /// <summary>The supplied PIN matched.</summary>
    Success,
    /// <summary>The PIN is incorrect or malformed.</summary>
    Incorrect,
    /// <summary>Too many attempts; wait before trying again.</summary>
    Delayed,
    /// <summary>No PIN has been configured.</summary>
    NotConfigured,
}

/// <summary>The check result and the remaining wait, if any.</summary>
public sealed record PinResult(PinOutcome Outcome, TimeSpan Wait);
