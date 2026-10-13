namespace Vafadar.Data;

/// <summary>Owns active and tentative protected generation keys through an authenticated profile transition.</summary>
/// <remarks>Hosts serialize native work, keep UI covered and resolve through actual identity/history sources.
/// Initial activation and filesystem/key retention policy are separate; this class never deletes a file or key envelope.</remarks>
public sealed class SqlCipherProfileSession<TContext>(LocalDatabaseLocation<TContext> location,
    Func<string, DatabaseAccessGate.ExclusiveLease, CancellationToken, Task<SqlCipherProfileGeneration>> resolve,
    Func<string, string, DatabaseAccessGate.ExclusiveLease, CancellationToken, Task>? prepareNew = null)
    where TContext : LocalDbContext
{
    private readonly SemaphoreSlim operations = new(1, 1);
    private SqlCipherProfileGeneration? current;
    private SqlCipherProfileGeneration? pending;

    /// <summary>Prepares a verified unpublished new profile while retaining the active endpoint and key.</summary>
    public async Task PrepareNewAsync(string profileId, string originalPath, DatabaseAccessGate.ExclusiveLease owner, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(profileId); ArgumentException.ThrowIfNullOrWhiteSpace(originalPath);
        await operations.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            location.Access.RequireDrainedOwner(owner); RequireCurrent();
            if (pending is not null || prepareNew is null) throw new InvalidOperationException("Protected new-profile preparation is unavailable.");
            await prepareNew(profileId, originalPath, owner, ct).ConfigureAwait(false);
            ct.ThrowIfCancellationRequested(); location.Access.RequireDrainedOwner(owner); RequireCurrent();
        }
        finally { operations.Release(); }
    }

    /// <summary>Opens and publishes the initial active owner after existing bootstrap prerequisites are complete.</summary>
    public async Task StartAsync(string originalPath, DatabaseAccessGate.ExclusiveLease owner, CancellationToken ct = default)
    {
        await operations.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            location.Access.RequireDrainedOwner(owner);
            if (current is not null || pending is not null) throw new InvalidOperationException("Protected profile session already started.");
            var generation = await resolve(originalPath, owner, ct).ConfigureAwait(false);
            try { ct.ThrowIfCancellationRequested(); location.Access.RequireDrainedOwner(owner); generation.Publish(location, owner); current = generation; }
            catch { generation.Dispose(); throw; }
        }
        finally { operations.Release(); }
    }

    /// <summary>Publishes a verified destination while retaining the previous key until the host accepts or restores it.</summary>
    public async Task BeginSwitchAsync(string originalPath, DatabaseAccessGate.ExclusiveLease owner, CancellationToken ct = default)
    {
        await operations.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            location.Access.RequireDrainedOwner(owner); RequireCurrent();
            if (pending is not null) throw new InvalidOperationException("A protected profile transition is unresolved.");
            var publication = location.CapturePublication(owner);
            var generation = await resolve(originalPath, owner, ct).ConfigureAwait(false);
            try
            {
                ct.ThrowIfCancellationRequested(); location.Access.RequireDrainedOwner(owner);
                generation.Publish(location, owner); pending = generation;
            }
            catch
            {
                // Even a throwing Changed handler may have published the destination. Restore before clearing it.
                try { publication.Restore(owner); }
                finally { generation.Dispose(); }
                throw;
            }
        }
        finally { operations.Release(); }
    }

    /// <summary>Retires only the rejected destination or accepted previous key after the host's publication decision.</summary>
    public void FinishSwitch(bool accepted, DatabaseAccessGate.ExclusiveLease owner)
    {
        if (!operations.Wait(0)) throw new InvalidOperationException("Protected profile resolution is still running.");
        try
        {
            location.Access.RequireDrainedOwner(owner);
            if (pending is null) { RequireCurrent(); return; }
            var selected = accepted ? pending : current!;
            RequirePublication(selected);
            if (accepted) { current!.Dispose(); current = pending; }
            else pending.Dispose();
            pending = null;
        }
        finally { operations.Release(); }
    }

    /// <summary>Closes the active owner only under drained maintenance; subsequent factory construction fails closed.</summary>
    public void Close(DatabaseAccessGate.ExclusiveLease owner)
    {
        if (!operations.Wait(0)) throw new InvalidOperationException("Protected profile resolution is still running.");
        try
        {
            location.Access.RequireDrainedOwner(owner);
            if (pending is not null) throw new InvalidOperationException("Finish the protected transition before closing.");
            current?.Dispose(); current = null;
        }
        finally { operations.Release(); }
    }

    private void RequireCurrent()
    {
        if (current is null) throw new InvalidOperationException("Protected profile session has not started.");
        RequirePublication(current);
    }

    private void RequirePublication(SqlCipherProfileGeneration generation)
    {
        var endpoint = location.Capture();
        if (endpoint.Path != generation.Path || !ReferenceEquals(endpoint.CreateConnection?.Target, generation) || endpoint.CipherVersion != generation.CipherVersion)
            throw new InvalidOperationException("Protected session no longer owns the selected publication.");
    }
}
