using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Settings;
using Vafadar.Zanance.App.Features.Accounts;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Features.Entries;

/// <summary>One retained parent draft's explicit account child; accepted metadata never becomes a second account Save.</summary>
public sealed class EntryAccountHandoff(EntryEditorReadSnapshot source, Func<bool> current) : IOriginalAccountHandoff
{
    private TaskCompletionSource? _idle;
    /// <summary>Gets the original immutable parent packet, independent of a subsequently selected profile.</summary>
    public EntryEditorReadSnapshot Source { get; } = source;
    /// <inheritdoc />
    public IReadOnlyList<Account> Accounts => Source.Inputs.Resources.Accounts;
    /// <inheritdoc />
    public ZananceSettings? Settings => Source.Inputs.Resources.Settings;
    /// <inheritdoc />
    public Task<EditorWorkReview> ReadWorkAsync(ZananceStore store) => store.ReadEditorAccountWorkAsync(Source);
    /// <summary>Gets the known committed replacement reference packet; raw parent inputs are never replaced.</summary>
    public EntryEditorReadSnapshot? Accepted { get; private set; }
    /// <summary>Gets the explicitly created account identity after its actual commit.</summary>
    public Guid? AccountId { get; private set; }
    /// <summary>Gets a separate observer failure after known metadata acceptance; it never permits another Save.</summary>
    public bool NotificationFailed { get; private set; }
    /// <summary>Gets routed ownership for Close/Cancel without requiring a still-current financial capability.</summary>
    public bool IsOwned => current();
    /// <summary>Checks that the original routed parent still owns this child visit.</summary>
    public void EnsureCurrent()
    {
        Source.EnsureCurrent();
        if (!current()) throw new InvalidOperationException("The original account handoff retired.");
    }
    /// <summary>Waits only for this child's actual pending metadata writer.</summary>
    public Task WaitForIdleAsync() => _idle?.Task ?? Task.CompletedTask;
    /// <summary>Commits an explicit new account/default once, preserving acceptance before change notification.</summary>
    public async Task SaveAsync(ZananceStore store, Account account, bool makeDefault, Func<bool>? isPresented = null)
    {
        EnsureCurrent();
        if (_idle is not null || Accepted is not null) throw new InvalidOperationException("The original account child is pending or accepted.");
        var idle = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously); _idle = idle;
        try
        {
            await store.CreateEditorAccountAsync(Source, account, makeDefault, () => current() && isPresented?.Invoke() != false,
                next => { AccountId = account.Id; Accepted = next; });
        }
        catch (CommittedWriteNotificationException) when (Accepted is not null) { NotificationFailed = true; }
        finally { _idle = null; idle.TrySetResult(); }
    }
}
