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
    private string? failedStartupOriginal;
    private bool pendingRestore;

    /// <summary>Adopts the exact durably selected same-profile restore, keeping both owners until notification and drain succeed.</summary>
    /// <remarks>Caller already verified restore history and replay through the actual receiver. Resolution rechecks
    /// those durable facts. A post-publication observer failure retains the new owner for exact retry, never rolls
    /// financial data back beneath already applied preferences, and never deletes either generation's envelope.</remarks>
    public async Task AdoptRestoredAsync(string originalPath, string candidatePath, DatabaseKeyBinding binding,
        DatabaseAccessGate.ExclusiveLease owner, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(binding);
        ArgumentException.ThrowIfNullOrWhiteSpace(originalPath); ArgumentException.ThrowIfNullOrWhiteSpace(candidatePath);
        await operations.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            location.Access.RequireDrainedOwner(owner);
            if (current is null || failedStartupOriginal is not null || (pending is not null && !pendingRestore))
                throw new InvalidOperationException("Restore requires a completed active profile session.");
            if (binding.ProfileId != current.Binding.ProfileId || binding.InstallationId != current.Binding.InstallationId)
                throw new InvalidOperationException("Restore belongs to another active profile or installation.");
            var expectedPath = Path.GetFullPath(candidatePath);
            if (pending is null && current.Binding == binding && current.Path == expectedPath) { RequireCurrent(); return; }
            if (pendingRestore)
            {
                if (pending!.Binding != binding || pending.Path != expectedPath)
                    throw new InvalidOperationException("A different restored publication is unresolved.");
                RequirePublication(pending);
                location.RepeatProtectedNotification(location.Capture(), owner);
            }
            else
            {
                RequireCurrent();
                var generation = await resolve(originalPath, owner, ct).ConfigureAwait(false);
                try
                {
                    ct.ThrowIfCancellationRequested(); location.Access.RequireDrainedOwner(owner);
                    if (generation.Binding != binding || generation.Path != expectedPath || generation.CipherVersion != current.CipherVersion)
                        throw new InvalidOperationException("Resolved restore differs from the captured selected generation.");
                    pending = generation; pendingRestore = true;
                    generation.Publish(location, owner);
                }
                catch
                {
                    var endpoint = location.Capture();
                    if (!ReferenceEquals(endpoint.CreateConnection?.Target, generation) || endpoint.Path != generation.Path)
                    { pending = null; pendingRestore = false; generation.Dispose(); }
                    throw;
                }
            }
            location.Access.RequireDrainedOwner(owner); RequirePublication(pending!);
            current.Dispose(); current = pending; pending = null; pendingRestore = false;
        }
        finally { operations.Release(); }
    }

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
            if (current is not null && pending is null && failedStartupOriginal == Path.GetFullPath(originalPath))
            {
                // Initial publication can succeed before a Changed observer throws. Retain that exact
                // live key owner and finish only its failed startup; ordinary duplicate starts still fail.
                RequireCurrent();
                location.RepeatProtectedNotification(location.Capture(), owner);
                RequireCurrent(); failedStartupOriginal = null; return;
            }
            if (current is not null || pending is not null) throw new InvalidOperationException("Protected profile session already started.");
            var generation = await resolve(originalPath, owner, ct).ConfigureAwait(false);
            try { ct.ThrowIfCancellationRequested(); location.Access.RequireDrainedOwner(owner); generation.Publish(location, owner); current = generation; }
            catch
            {
                var endpoint = location.Capture();
                if (endpoint.Path == generation.Path && ReferenceEquals(endpoint.CreateConnection?.Target, generation)
                    && endpoint.CipherVersion == generation.CipherVersion)
                {
                    current = generation; failedStartupOriginal = Path.GetFullPath(originalPath);
                }
                else generation.Dispose();
                throw;
            }
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
            if (pendingRestore) throw new InvalidOperationException("Restored publication requires its exact adoption retry.");
            if (pending is null) { RequireCurrent(); return; }
            var selected = accepted ? pending : current!;
            RequirePublication(selected);
            if (accepted) { current!.Dispose(); current = pending; }
            else pending.Dispose();
            pending = null;
        }
        finally { operations.Release(); }
    }

    /// <summary>Requires completed current publication before the host records another financial restore intent.</summary>
    public void RequireStable(DatabaseAccessGate.ExclusiveLease owner)
    {
        if (!operations.Wait(0)) throw new InvalidOperationException("Protected session resolution is still running.");
        try
        {
            location.Access.RequireDrainedOwner(owner);
            if (pending is not null || failedStartupOriginal is not null)
                throw new InvalidOperationException("Protected publication must finish before another restore intent.");
            RequireCurrent();
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
            current?.Dispose(); current = null; failedStartupOriginal = null;
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
