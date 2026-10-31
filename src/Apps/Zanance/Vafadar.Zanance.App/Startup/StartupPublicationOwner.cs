namespace Vafadar.Zanance.App.Startup;

/// <summary>Retains one actual startup root through protected preparation and an optionally deferred first publication.</summary>
internal sealed class StartupPublicationOwner<TRoot> where TRoot : class
{
    private readonly Func<TRoot?> readRoot;
    private readonly Action<TRoot> writeRoot;
    private readonly Func<bool> ownsWindow;
    private readonly CancellationTokenSource lifetime = new();
    private TRoot expectedRoot;
    private bool preparationEnded;
    private bool retired;
    private bool released;

    /// <summary>Captures only presentation identity; original protected startup owns its own storage/legacy-replay guards.</summary>
    internal StartupPublicationOwner(TRoot originalRoot, Func<TRoot?> readRoot, Action<TRoot> writeRoot, Func<bool> ownsWindow)
    {
        expectedRoot = originalRoot; this.readRoot = readRoot; this.writeRoot = writeRoot; this.ownsWindow = ownsWindow;
        Token = lifetime.Token;
        try { RequireCurrent(); } catch { lifetime.Dispose(); throw; }
    }
    /// <summary>Gets the token used by actual protected preparation and settings/lock waits.</summary>
    internal CancellationToken Token { get; }
    /// <summary>Gets whether this owner actually published its prepared root, including a fallible setter's returned effect.</summary>
    internal bool HasPublished { get; private set; }
    /// <summary>Signals that UI observers can detach after preparation and publication/retirement both finish.</summary>
    internal event Action? Released;
    /// <summary>Reports live authority or its exact published-root receipt, never an unrelated replacement.</summary>
    internal bool IsCurrent => !retired && (!released || HasPublished) && ownsWindow() && ReferenceEquals(readRoot(), expectedRoot);
    /// <summary>Refuses stale startup before effects while leaving any other current root untouched.</summary>
    internal void RequireCurrent()
    {
        if (IsCurrent) return;
        Retire(); throw new OperationCanceledException("The original startup presentation retired.", Token);
    }
    /// <summary>Publishes only once to the captured window and follows its own actual root transition.</summary>
    internal void Publish(TRoot page)
    {
        RequireCurrent();
        if (HasPublished) throw new InvalidOperationException("The original startup page was already published.");
        var previous = expectedRoot; expectedRoot = page;
        try { writeRoot(page); }
        catch
        {
            // A native setter may throw after its real effect; never repeat an already visible publication.
            if (ReferenceEquals(readRoot(), page)) HasPublished = true;
            else expectedRoot = previous;
            if (!IsCurrent) Retire();
            ReleaseIfEnded(); throw;
        }
        RequireCurrent(); HasPublished = true; ReleaseIfEnded();
    }
    /// <summary>Observes real window/root/model changes without mistaking this owner's own publication for departure.</summary>
    internal void ObserveRoot() { if (!IsCurrent) Retire(); }
    /// <summary>Cancels only this startup; actual worker cleanup must still finish before its resources are released.</summary>
    internal void Retire()
    {
        if (retired) return;
        retired = true; if (!released) lifetime.Cancel(); ReleaseIfEnded();
    }
    /// <summary>Ends real preparation, retaining a valid still-covered publication callback until unlocking or retirement.</summary>
    internal void EndPreparation(bool succeeded)
    {
        preparationEnded = true;
        if (!succeeded) Retire();
        ReleaseIfEnded();
    }
    private void ReleaseIfEnded()
    {
        if (released || !preparationEnded || (!HasPublished && !retired)) return;
        released = true; Released?.Invoke(); lifetime.Dispose();
    }
}
