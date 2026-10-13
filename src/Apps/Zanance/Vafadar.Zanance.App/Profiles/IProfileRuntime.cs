namespace Vafadar.Zanance.App.Profiles;

/// <summary>Platform access and serialization around an actual local profile transition.</summary>
public interface IProfileRuntime
{
    /// <summary>Serializes the database move against automatic posting and restore.</summary>
    Task RunExclusiveAsync(Func<Task> action);

    /// <summary>Migrates the selected database through contexts owned by the current maintenance lease.</summary>
    Task MigrateAsync(Vafadar.Data.DatabaseAccessGate.ExclusiveLease owner);

    /// <summary>Prepares a new protected database without moving the active endpoint or publishing the profile catalogue.</summary>
    Task PrepareNewDatabaseAsync(Vafadar.Data.LocalDatabaseLocation<Vafadar.Zanance.Data.ZananceDbContext> location,
        string profileId, string originalPath, Vafadar.Data.DatabaseAccessGate.ExclusiveLease owner, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested(); _ = location.CapturePublication(owner);
        if (location.IsProtected) throw new InvalidOperationException("Protected profile preparation requires the native host.");
        return Task.CompletedTask;
    }

    /// <summary>Opens the captured destination under maintenance ownership; protected hosts publish its verified key owner.</summary>
    /// <remarks>The default handles existing plaintext storage only. A protected location refuses that path;
    /// final native hosts override this effect rather than stripping protection or recreating a plaintext database.</remarks>
    async Task OpenDatabaseAsync(Vafadar.Data.LocalDatabaseLocation<Vafadar.Zanance.Data.ZananceDbContext> location,
        string originalPath, Vafadar.Data.DatabaseAccessGate.ExclusiveLease owner)
    {
        location.MoveTo(originalPath, owner);
        await MigrateAsync(owner);
    }

    /// <summary>Retires tentative/previous protected keys after the actual publication decision; plaintext hosts own none.</summary>
    void FinishDatabaseTransition(bool accepted, Vafadar.Data.DatabaseAccessGate.ExclusiveLease owner) { }

    /// <summary>Confirms the destination profile's device lock; unavailable authentication fails.</summary>
    Task<bool> AuthenticateAsync(string reason);

    /// <summary>Reads the successfully selected profile's lock preference.</summary>
    Task ReloadLockAsync();

    /// <summary>Withdraws the previous profile's pending Undo offer.</summary>
    void DismissUndo();
}
