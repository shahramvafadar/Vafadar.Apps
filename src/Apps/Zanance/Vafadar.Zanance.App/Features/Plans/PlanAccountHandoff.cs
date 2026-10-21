using Vafadar.Zanance.App.Features.Accounts;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Settings;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Features.Plans;

/// <summary>One original raw plan's explicit account child and its known additive reference publication.</summary>
public sealed class PlanAccountHandoff(PlanStore plans, PlanEditorReadSnapshot source, Func<bool> current) : IOriginalAccountHandoff
{
    private TaskCompletionSource? _idle;
    /// <summary>Gets the original plan packet, independent of later selected-profile changes.</summary>
    public PlanEditorReadSnapshot Source { get; } = source;
    /// <summary>Gets only this original child's known committed replacement references.</summary>
    public PlanEditorReadSnapshot? Accepted { get; private set; }
    /// <summary>Gets the explicitly created account after its known actual commit.</summary>
    public Guid? AccountId { get; private set; }
    /// <inheritdoc />
    public IReadOnlyList<Account> Accounts => Source.Inputs.Accounts;
    /// <inheritdoc />
    public ZananceSettings? Settings => Source.Inputs.Settings;
    /// <inheritdoc />
    public string? SuggestedCurrencyCode => Source.Debt?.CurrencyCode;
    /// <inheritdoc />
    public bool IsOwned => current();
    /// <inheritdoc />
    public bool NotificationFailed { get; private set; }
    /// <inheritdoc />
    public void EnsureCurrent()
    {
        Source.EnsureCurrent();
        if (!current()) throw new InvalidOperationException("The original plan account visit retired.");
    }
    /// <inheritdoc />
    public Task<EditorWorkReview> ReadWorkAsync(ZananceStore store) => plans.ReadEditorAccountWorkAsync(Source);
    /// <inheritdoc />
    public Task WaitForIdleAsync() => _idle?.Task ?? Task.CompletedTask;
    /// <inheritdoc />
    public async Task SaveAsync(ZananceStore store, Account account, bool makeDefault, Func<bool>? isPresented = null)
    {
        EnsureCurrent();
        if (_idle is not null || Accepted is not null) throw new InvalidOperationException("The original plan account child is pending or accepted.");
        var idle = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously); _idle = idle;
        try
        {
            await plans.CreateEditorAccountAsync(Source, account, makeDefault, () => current() && isPresented?.Invoke() != false,
                next => { AccountId = account.Id; Accepted = next; });
        }
        catch (CommittedWriteNotificationException) when (Accepted is not null) { NotificationFailed = true; }
        finally { _idle = null; idle.TrySetResult(); }
    }
}
