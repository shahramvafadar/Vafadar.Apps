using Vafadar.Zanance.Data.Sync;

namespace Vafadar.Zanance.App.Features.Commerce.Billing;

/// <summary>Explicit owned identity lifecycle, separate from local Plus, financial profiles and native payment launch.</summary>
public interface IBillingIdentityEnvironment
{
    /// <summary>Whether this selected mobile edition offers the owned identity connection.</summary>
    bool Available { get; }
    /// <summary>Reads the actual private session subject only, without creating an identity.</summary>
    Task<string?> ReadSubjectAsync(CancellationToken ct);
    /// <summary>Reads the original subject's pending deletion receipt.</summary>
    Task<AccountDeletionReceipt?> ReadDeletionAsync(string subject, CancellationToken ct) => throw new NotSupportedException();
    /// <summary>Requests deletion for the original subject with fresh password proof.</summary>
    Task<AccountDeletionReceipt> RequestDeletionAsync(string subject, string password, CancellationToken ct) => throw new NotSupportedException();
    /// <summary>Explicit sign-in; a password is request-only and never remembered.</summary>
    Task SignInAsync(string email, string password, CancellationToken ct);
    /// <summary>Retires this actual session and local credential; false means remote revocation was not confirmed.</summary>
    Task<bool> SignOutAsync(CancellationToken ct);
    /// <summary>Uniform account registration, never automatic sign-in or financial-account creation.</summary>
    Task RegisterAsync(string email, string password, CancellationToken ct);
    /// <summary>Uniform recovery delivery through the configured service, without recipient-existence disclosure.</summary>
    Task RecoverAsync(string email, CancellationToken ct);
    /// <summary>Consumes one user-supplied challenge in a body, with a new password only for reset.</summary>
    Task CompleteAsync(Guid challenge, string token, string? newPassword, CancellationToken ct);
}
