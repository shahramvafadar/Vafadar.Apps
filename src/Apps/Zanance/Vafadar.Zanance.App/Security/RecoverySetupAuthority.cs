using Vafadar.Data;

namespace Vafadar.Zanance.App.Security;

/// <summary>Retains one original recovery setup lifetime and non-secret catalogue capture across protected waits.</summary>
internal sealed class RecoverySetupAuthority : IDisposable
{
    private readonly Action validateSource;
    private readonly CancellationTokenSource lifetime;
    private readonly Lock gate = new();
    private bool disposed;

    /// <summary>Captures the original no-write source validator; no code, key or private material is retained here.</summary>
    internal RecoverySetupAuthority(Action validateSource, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(validateSource);
        this.validateSource = validateSource;
        lifetime = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        Token = lifetime.Token;
    }

    /// <summary>Gets the original cancellation token, which cannot be revived by another appearance.</summary>
    internal CancellationToken Token { get; }

    /// <summary>Refuses a retired presentation or replaced original storage catalogue.</summary>
    internal void RequireCurrent()
    {
        lock (gate) ObjectDisposedException.ThrowIf(disposed, this);
        Token.ThrowIfCancellationRequested();
        validateSource();
        Token.ThrowIfCancellationRequested();
    }

    /// <summary>Acquires the actual exclusive gate and rechecks the same source before any protected operation.</summary>
    internal async Task<DatabaseAccessGate.ExclusiveLease> AcquireAsync(DatabaseAccessGate access)
    {
        RequireCurrent();
        var owner = await access.AcquireExclusiveAsync(Token).ConfigureAwait(false);
        try { RequireCurrent(); return owner; }
        catch { owner.Dispose(); throw; }
    }

    /// <summary>Retires only this original lifetime; it does not dispose an in-flight draft or another gate owner.</summary>
    internal void Retire()
    {
        lock (gate) { if (!disposed) lifetime.Cancel(); }
    }

    /// <summary>Ends the owned cancellation lifetime without exposing or changing recovery material.</summary>
    public void Dispose()
    {
        lock (gate)
        {
            if (disposed) return;
            lifetime.Cancel(); disposed = true; lifetime.Dispose();
        }
    }
}
