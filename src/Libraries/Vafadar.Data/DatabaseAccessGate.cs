namespace Vafadar.Data;

/// <summary>Drains factory context lifetimes before exclusive migration/profile work and prevents new readers from starving waiting writers.</summary>
/// <remarks>This covers contexts created by the associated factory, not arbitrary native connections. Synchronous
/// callers fail closed during maintenance; asynchronous callers wait with cancellation. Hosts must keep maintenance UI covered.</remarks>
public sealed class DatabaseAccessGate
{
    private readonly Lock sync = new();
    private readonly SemaphoreSlim writers = new(1, 1);
    private TaskCompletionSource changed = NewSignal();
    private int readers;
    private int pendingWriters;
    private ExclusiveLease? exclusive;

    /// <summary>Captures exclusive ownership only when immediately available; never blocks a synchronous/UI caller.</summary>
    public ExclusiveLease AcquireExclusive()
    {
        lock (sync)
        {
            if (readers != 0 || exclusive is not null || pendingWriters != 0 || !writers.Wait(0))
                throw new InvalidOperationException("Database maintenance is pending, active or waiting for contexts.");
            exclusive = new(this); Pulse(); return exclusive;
        }
    }

    /// <summary>Waits for existing factory contexts to close and captures exclusive maintenance ownership.</summary>
    public async Task<ExclusiveLease> AcquireExclusiveAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        lock (sync) { pendingWriters++; Pulse(); }
        var acquired = false;
        try
        {
            await writers.WaitAsync(ct).ConfigureAwait(false); acquired = true;
            while (true)
            {
                Task wait;
                lock (sync)
                {
                    ct.ThrowIfCancellationRequested();
                    if (readers == 0)
                    {
                        exclusive = new(this); pendingWriters--; Pulse(); return exclusive;
                    }
                    wait = changed.Task;
                }
                await wait.WaitAsync(ct).ConfigureAwait(false);
            }
        }
        catch
        {
            lock (sync) { pendingWriters--; Pulse(); }
            if (acquired) writers.Release();
            throw;
        }
    }

    internal IDisposable EnterRead()
    {
        lock (sync)
        {
            if (exclusive is not null || pendingWriters != 0) throw new InvalidOperationException("Database maintenance is pending or active.");
            readers++; return new ContextLease(this, null);
        }
    }

    internal async Task<IDisposable> EnterReadAsync(CancellationToken ct)
    {
        while (true)
        {
            Task wait;
            lock (sync)
            {
                ct.ThrowIfCancellationRequested();
                if (exclusive is null && pendingWriters == 0) { readers++; return new ContextLease(this, null); }
                wait = changed.Task;
            }
            await wait.WaitAsync(ct).ConfigureAwait(false);
        }
    }

    internal IDisposable EnterOwnedContext(ExclusiveLease owner)
    {
        lock (sync) { RequireOwner(owner); owner.Contexts++; return new ContextLease(this, owner); }
    }

    internal void RequireOwner(ExclusiveLease owner)
    {
        lock (sync)
            if (!ReferenceEquals(exclusive, owner) || owner.Closing) throw new InvalidOperationException("Database maintenance ownership retired or belongs to another factory.");
    }

    internal void RequirePublication(ExclusiveLease? owner)
    {
        lock (sync)
        {
            if (owner is not null) RequireOwner(owner);
            else if (exclusive is not null || pendingWriters != 0) throw new InvalidOperationException("Database publication requires current maintenance ownership.");
        }
    }

    private void LeaveContext(ExclusiveLease? owner)
    {
        lock (sync)
        {
            if (owner is null) readers--; else owner.Contexts--;
            if (owner is { Closing: true, Contexts: 0 }) ReleaseWriter(owner);
            Pulse();
        }
    }

    private void Close(ExclusiveLease owner)
    {
        lock (sync)
        {
            if (owner.Closing) return;
            RequireOwner(owner); owner.Closing = true;
            // A prematurely disposed owner cannot unlock other work while its native context is still alive.
            if (owner.Contexts == 0) ReleaseWriter(owner);
            Pulse();
        }
    }

    private void ReleaseWriter(ExclusiveLease owner)
    {
        if (!ReferenceEquals(exclusive, owner)) throw new InvalidOperationException("Database maintenance identity changed.");
        exclusive = null; writers.Release();
    }
    private static TaskCompletionSource NewSignal() => new(TaskCreationOptions.RunContinuationsAsynchronously);
    private void Pulse() { var previous = changed; changed = NewSignal(); previous.TrySetResult(); }

    /// <summary>Unforgeable ownership of one gate; dispose only after owned contexts and native work finish.</summary>
    public sealed class ExclusiveLease : IDisposable
    {
        private readonly DatabaseAccessGate gate;
        internal int Contexts;
        internal bool Closing;
        internal ExclusiveLease(DatabaseAccessGate gate) => this.gate = gate;
        /// <summary>Refuses a retired maintenance operation before its next native effect.</summary>
        public void Validate() => gate.RequireOwner(this);
        /// <inheritdoc />
        public void Dispose() => gate.Close(this);
    }

    private sealed class ContextLease(DatabaseAccessGate gate, ExclusiveLease? owner) : IDisposable
    {
        private int disposed;
        public void Dispose() { if (Interlocked.Exchange(ref disposed, 1) == 0) gate.LeaveContext(owner); }
    }
}
