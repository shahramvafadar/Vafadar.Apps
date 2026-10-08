using System.Security.Cryptography;

namespace Vafadar.Zanance.Core.Security;

/// <summary>
/// Optional four-digit app access gate (D-63). Device-protected verifiers and growing, restart-persistent delays
/// discourage interactive guessing. This gate does not encrypt the financial database or portable backups.
/// </summary>
public sealed class PinLock(IPinStorage storage, TimeProvider time)
{
    private const int Iterations = 600_000;
    private readonly SemaphoreSlim _gate = new(1, 1);

    /// <summary>Gets whether a PIN was configured when the state was last read or changed.</summary>
    public bool IsEnabled { get; private set; }

    /// <summary>Reads protected state. Storage failures propagate instead of silently disabling the lock.</summary>
    public async Task LoadAsync()
    {
        await _gate.WaitAsync();
        try { IsEnabled = await ReadAsync() is not null; }
        finally { _gate.Release(); }
    }

    /// <summary>Accepts exactly four ASCII digits, including leading zeroes. UI normalizes native digits first.</summary>
    public static bool IsValid(string? pin) => pin is { Length: 4 } && pin.All(c => c is >= '0' and <= '9');

    /// <summary>Checks a PIN and persists the attempt state before granting access.</summary>
    public async Task<PinResult> VerifyAsync(string pin)
    {
        await _gate.WaitAsync();
        try { return await VerifyAsync(await ReadAsync(), pin); }
        finally { _gate.Release(); }
    }

    /// <summary>Sets or changes a PIN. An existing PIN must match and respect the same attempt limit.</summary>
    public async Task<PinResult> SetAsync(string currentPin, string newPin)
    {
        if (!IsValid(newPin)) { return new(PinOutcome.Incorrect, TimeSpan.Zero); }
        await _gate.WaitAsync();
        try
        {
            var state = await ReadAsync();
            if (state is not null)
            {
                var check = await VerifyAsync(state, currentPin);
                if (check.Outcome != PinOutcome.Success) { return check; }
            }

            var salt = RandomNumberGenerator.GetBytes(16);
            var hash = Derive(newPin, salt, Iterations);
            await storage.WriteAsync(new(1, Iterations, salt, hash, 0, null));
            IsEnabled = true;
            return new(PinOutcome.Success, TimeSpan.Zero);
        }
        finally { _gate.Release(); }
    }

    /// <summary>Removes a PIN only after a successful check; cancelling or guessing never disables it.</summary>
    public async Task<PinResult> RemoveAsync(string currentPin)
    {
        await _gate.WaitAsync();
        try
        {
            var state = await ReadAsync();
            if (state is null) { return new(PinOutcome.NotConfigured, TimeSpan.Zero); }
            var check = await VerifyAsync(state, currentPin);
            if (check.Outcome == PinOutcome.Success)
            {
                await storage.WriteAsync(null);
                IsEnabled = false;
            }

            return check;
        }
        finally { _gate.Release(); }
    }

    /// <summary>Removes a lost PIN only after an explicit, successful trusted device-authentication callback.</summary>
    public async Task<bool> RecoverAsync(Func<Task<bool>> authenticate)
    {
        ArgumentNullException.ThrowIfNull(authenticate);
        if (!await authenticate()) { return false; }
        await _gate.WaitAsync();
        try
        {
            // Recovery is allowed even when protected state is unreadable; the device owner has authenticated.
            await storage.WriteAsync(null);
            IsEnabled = false;
            return true;
        }
        finally { _gate.Release(); }
    }

    private async Task<PinState?> ReadAsync()
    {
        var state = await storage.ReadAsync();
        if (state is not null && (state.Version != 1 || state.Iterations != Iterations || state.Salt.Length != 16
            || state.Hash.Length != 32 || state.Failures < 0))
        {
            throw new InvalidOperationException("The protected PIN state is invalid.");
        }

        return state;
    }

    private async Task<PinResult> VerifyAsync(PinState? state, string pin)
    {
        if (state is null) { return new(PinOutcome.NotConfigured, TimeSpan.Zero); }
        var now = time.GetUtcNow();
        if (state.RetryAt is { } retry && retry > now) { return new(PinOutcome.Delayed, retry - now); }
        var matches = false;
        if (IsValid(pin))
        {
            var actual = Derive(pin, state.Salt, state.Iterations);
            try { matches = CryptographicOperations.FixedTimeEquals(actual, state.Hash); }
            finally { CryptographicOperations.ZeroMemory(actual); }
        }

        if (matches)
        {
            await storage.WriteAsync(state with { Failures = 0, RetryAt = null });
            return new(PinOutcome.Success, TimeSpan.Zero);
        }

        // Five attempts, then one minute; each further failure doubles the wait up to fifteen minutes. Keeping the
        // deadline with the verifier prevents an app restart from resetting the limit (D-63).
        var failures = Math.Min(state.Failures + 1, 20);
        var delay = failures < 5 ? TimeSpan.Zero : TimeSpan.FromSeconds(Math.Min(60 * (1 << Math.Min(failures - 5, 4)), 900));
        await storage.WriteAsync(state with { Failures = failures, RetryAt = delay > TimeSpan.Zero ? now + delay : null });
        return new(delay > TimeSpan.Zero ? PinOutcome.Delayed : PinOutcome.Incorrect, delay);
    }

    private static byte[] Derive(string pin, byte[] salt, int iterations) =>
        Rfc2898DeriveBytes.Pbkdf2(pin, salt, iterations, HashAlgorithmName.SHA256, 32);
}
