using Vafadar.Authentication;
using Vafadar.Backup;

namespace Vafadar.Zanance.App.Features.Backup;

/// <summary>One original destination consent and effect, bound to its native source and immutable destination identity.</summary>
public sealed class BackupDestinationRequest(IExternalSignInService signIn, IBackupStorage storage,
    Action validateOriginal, CancellationToken cancellationToken = default, ExternalAccount? originalAccount = null)
{
    private bool _started;
    /// <summary>Gets the actual accepted sign-in result before fallible native publication or listing.</summary>
    public ExternalAccount? ConnectedAccount { get; private set; }
    /// <summary>Gets whether the actual original sign-out completed, independently of a later native departure.</summary>
    public bool Disconnected { get; private set; }
    /// <summary>Gets the actual original deleted file only after storage confirms deletion.</summary>
    public BackupFileInfo? DeletedFile { get; private set; }

    /// <summary>Requests original consent before the actual provider service; genuine credentials remain with its native flow.</summary>
    public async Task<ExternalAccount?> ConnectAsync(Func<Task<bool>> confirm, IReadOnlyCollection<string> scopes)
    {
        Begin(); var originalScopes = scopes.ToArray();
        if (originalAccount is not null) throw new InvalidOperationException("The original destination is already connected.");
        await RequireOriginalAccountAsync();
        if (!await confirm()) return null;
        await RequireOriginalAccountAsync();
        ConnectedAccount = await signIn.SignInAsync(originalScopes, cancellationToken);
        return ConnectedAccount;
    }

    /// <summary>Signs out only after the original disclosure is accepted by the same current source and destination.</summary>
    public async Task<bool> DisconnectAsync(Func<Task<bool>> confirm)
    {
        Begin(); RequireConnected(); await RequireOriginalAccountAsync();
        if (!await confirm()) return false;
        await RequireOriginalAccountAsync(); await signIn.SignOutAsync(cancellationToken); Disconnected = true; return true;
    }

    /// <summary>Deletes exactly the original selected file after consent; a known deletion is never resubmitted.</summary>
    public async Task<bool> DeleteAsync(BackupFileInfo file, Func<Task<bool>> confirm)
    {
        ArgumentNullException.ThrowIfNull(file);
        Begin(); RequireConnected(); await RequireOriginalAccountAsync();
        if (!await confirm()) return false;
        await RequireOriginalAccountAsync(); await storage.DeleteAsync(file.Id, cancellationToken); DeletedFile = file; return true;
    }

    /// <summary>Rechecks publication after a known effect without erasing that actual receipt.</summary>
    public Task EnsurePublicationCurrentAsync() => RequireAccountAsync(ConnectedAccount ?? (Disconnected ? null : originalAccount));

    private Task RequireOriginalAccountAsync() => RequireAccountAsync(originalAccount);
    private async Task RequireAccountAsync(ExternalAccount? expected)
    {
        Check(); ExternalAccount? actual;
        try { actual = await signIn.GetCurrentAccountAsync(cancellationToken); }
        // The original explicit connect path remains the recovery for a provider requesting genuine interactive sign-in.
        catch (AuthenticationRequiredException) when (expected is null) { actual = null; }
        Check();
        if (actual?.Provider != expected?.Provider || actual?.Id != expected?.Id)
            throw new BackupDestinationChangedException(cancellationToken);
    }
    private void RequireConnected()
    { if (originalAccount is null) throw new InvalidOperationException("The original backup account is not connected."); }

    private void Begin()
    {
        if (_started) throw new InvalidOperationException("The original backup destination request already started.");
        _started = true; Check();
    }
    private void Check() { cancellationToken.ThrowIfCancellationRequested(); validateOriginal(); }
}

/// <summary>Requires explicit original workspace reload when the actual provider identity changed.</summary>
public sealed class BackupDestinationChangedException(CancellationToken cancellationToken = default)
    : OperationCanceledException("The actual original backup destination changed.", cancellationToken);
