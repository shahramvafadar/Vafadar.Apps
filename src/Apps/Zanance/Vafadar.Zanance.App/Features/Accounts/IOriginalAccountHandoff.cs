using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Settings;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Features.Accounts;

/// <summary>Explicit original parent's account visit; the shared child never substitutes a currently selected profile.</summary>
public interface IOriginalAccountHandoff
{
    /// <summary>Gets the original detached account references used by the child form.</summary>
    IReadOnlyList<Account> Accounts { get; }
    /// <summary>Gets original optional settings; display never initializes defaults.</summary>
    ZananceSettings? Settings { get; }
    /// <summary>Gets an original monetary context's suggested ISO currency, without restricting an explicit account choice.</summary>
    string? SuggestedCurrencyCode => null;
    /// <summary>Gets original parent query ownership while its native form is covered.</summary>
    bool IsOwned { get; }
    /// <summary>Gets a known postcommit observer failure without allowing a second account Save.</summary>
    bool NotificationFailed { get; }
    /// <summary>Rechecks the original parent source before any child action.</summary>
    void EnsureCurrent();
    /// <summary>Reads original-file quota readiness without adopting new source facts.</summary>
    Task<EditorWorkReview> ReadWorkAsync(ZananceStore store);
    /// <summary>Waits only for this original metadata writer.</summary>
    Task WaitForIdleAsync();
    /// <summary>Creates one explicit account/default with original parent and child presentation guards.</summary>
    Task SaveAsync(ZananceStore store, Account account, bool makeDefault, Func<bool>? isPresented = null);
}
