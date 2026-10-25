namespace Vafadar.Zanance.Data;

/// <summary>
/// Owns one bounded Home publication connection. Snapshot and validation use separate transactions;
/// disposal closes the original context and releases its factory lease, with no pooled key lifetime.
/// </summary>
public sealed class HomeReadSession : IAsyncDisposable
{
    private readonly SemaphoreSlim gate = new(1, 1);
    private IAsyncDisposable? context;
    private Func<CancellationToken, Task>? validate;

    /// <summary>Creates only an original store-owned operation after its first read transaction has ended.</summary>
    internal HomeReadSession(HomeReadSnapshot snapshot, IAsyncDisposable context, Func<CancellationToken, Task> validate)
    { Snapshot = snapshot; this.context = context; this.validate = validate; }

    /// <summary>Gets the complete original data packet; it does not expose the connection or key.</summary>
    public HomeReadSnapshot Snapshot { get; }

    /// <summary>Rechecks original source, inputs and work rights in a fresh transaction on the captured connection.</summary>
    public async Task ValidateAsync(CancellationToken cancellationToken = default)
    {
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            ObjectDisposedException.ThrowIf(context is null, this);
            await validate!(cancellationToken).ConfigureAwait(false);
        }
        finally { gate.Release(); }
    }

    /// <summary>Drains any active validation and releases the original context exactly once, including failed publication.</summary>
    public async ValueTask DisposeAsync()
    {
        await gate.WaitAsync().ConfigureAwait(false);
        try
        {
            if (context is not { } owned) return;
            context = null; validate = null;
            await owned.DisposeAsync().ConfigureAwait(false);
        }
        finally { gate.Release(); }
    }
}
