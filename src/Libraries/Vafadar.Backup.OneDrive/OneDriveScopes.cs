namespace Vafadar.Backup.OneDrive;

/// <summary>
/// Microsoft Graph permissions required by <see cref="OneDriveBackupStorage"/>.
/// </summary>
public static class OneDriveScopes
{
    /// <summary>
    /// Access to the app's own folder (<c>OneDrive/Apps/&lt;app name&gt;</c>) only. The app cannot see any other file.
    /// </summary>
    public const string AppFolder = "Files.ReadWrite.AppFolder";

    /// <summary>Gets all scopes needed for backups.</summary>
    public static IReadOnlyCollection<string> All { get; } = [AppFolder];
}
