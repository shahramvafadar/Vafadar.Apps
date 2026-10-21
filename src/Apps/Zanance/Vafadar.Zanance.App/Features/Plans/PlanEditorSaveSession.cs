using Vafadar.Zanance.Core.Plans;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Features.Plans;

/// <summary>Retains one original plan/split submission, stable destination identity and known commit before observers.</summary>
public sealed class PlanEditorSaveSession(PlanStore plans, PlanEditorReadSnapshot source)
{
    private TaskCompletionSource? _idle;
    private Guid? _destination;
    /// <summary>Gets the exact source packet bound to this writer; explicit access review creates a separate unaccepted session.</summary>
    public PlanEditorReadSnapshot Source { get; } = source;
    /// <summary>Gets known acceptance, even when a later notification or native return fails.</summary>
    public bool Accepted { get; private set; }
    /// <summary>Gets a separate postcommit observer failure; it never rearms the original Save.</summary>
    public bool NotificationFailed { get; private set; }
    /// <summary>Waits for only this original pending writer before another query or read-only return.</summary>
    public Task WaitForIdleAsync() => _idle?.Task ?? Task.CompletedTask;
    /// <summary>Submits detached original rows once; an uncommitted retry cannot change its destination identity.</summary>
    public async Task<bool> SaveAsync(Schedule? previous, Schedule target, Func<bool> current)
    {
        if (_idle is not null || Accepted || _destination is { } id && id != target.Id)
            throw new InvalidOperationException("The original plan destination is pending, changed or already accepted.");
        _destination ??= target.Id;
        var before = previous?.CopyForEditing(); var proposal = target.CopyForEditing();
        var idle = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously); _idle = idle;
        try
        {
            try { return await plans.SaveEditorAsync(Source, before, proposal, current, () => Accepted = true); }
            catch (CommittedWriteNotificationException) when (Accepted) { NotificationFailed = true; return true; }
        }
        finally { if (ReferenceEquals(_idle, idle)) _idle = null; idle.TrySetResult(); }
    }
}
