namespace Vafadar.Backup.GoogleDrive;

/// <summary>
/// OAuth scopes required by <see cref="GoogleDriveBackupStorage"/>.
/// </summary>
public static class GoogleDriveScopes
{
    /// <summary>
    /// Access to the app's own hidden data folder only. The app cannot see any other file in the user's Drive.
    /// </summary>
    public const string AppData = "https://www.googleapis.com/auth/drive.appdata";

    /// <summary>Gets all scopes needed for backups.</summary>
    public static IReadOnlyCollection<string> All { get; } = [AppData];
}
