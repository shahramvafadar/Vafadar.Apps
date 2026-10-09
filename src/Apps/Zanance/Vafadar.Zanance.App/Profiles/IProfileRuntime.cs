namespace Vafadar.Zanance.App.Profiles;

/// <summary>Platform access and serialization around an actual local profile transition.</summary>
public interface IProfileRuntime
{
    /// <summary>Serializes the database move against automatic posting and restore.</summary>
    Task RunExclusiveAsync(Func<Task> action);

    /// <summary>Migrates the selected database after its path has changed.</summary>
    Task MigrateAsync();

    /// <summary>Confirms the destination profile's device lock; unavailable authentication fails.</summary>
    Task<bool> AuthenticateAsync(string reason);

    /// <summary>Reads the successfully selected profile's lock preference.</summary>
    Task ReloadLockAsync();

    /// <summary>Withdraws the previous profile's pending Undo offer.</summary>
    void DismissUndo();
}
