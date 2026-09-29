using System.Text;
using Vafadar.Data;
using Vafadar.Localization;
using Vafadar.Maui.Security;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.App.Security;
using Vafadar.Zanance.Core;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Profiles;

/// <summary>A local profile: its own database with its own data and settings (§3).</summary>
/// <param name="Id">The id; empty for the main profile (the original database).</param>
/// <param name="Name">The name the user gave it; <see langword="null"/> for an unnamed main profile.</param>
public sealed record LocalProfile(string Id, string? Name)
{
    /// <summary>Gets a value indicating whether this is the main profile, which always exists.</summary>
    public bool IsMain => Id.Length == 0;
}

/// <summary>
/// Several independent local profiles on one device (§3), e.g. personal and business. Each profile is a database file
/// of its own with its data, settings, app lock and backups; nothing is shared between them. The list lives in the
/// app preferences, outside every profile. Reminders are those of the open profile.
/// </summary>
public sealed class ProfileService(
    LocalDatabaseLocation<ZananceDbContext> location,
    IServiceProvider services,
    ZananceStore store,
    UndoService undo,
    AppLockService appLock,
    AutoPostProcessor autoPost,
    Translator translator)
{
    private const string ListKey = "profiles.list";
    private const string CurrentKey = "profiles.current";
    private const string MainNameKey = "profiles.main.name";

    /// <summary>The longest profile name.</summary>
    public const int MaxNameLength = 40;

    /// <summary>Raised after another profile was opened or the list changed.</summary>
    public event EventHandler? Changed;

    /// <summary>Gets all profiles, the main one first.</summary>
    public IReadOnlyList<LocalProfile> All => [new LocalProfile(string.Empty, Preferences.Default.Get<string?>(MainNameKey, null)), .. Others()];

    /// <summary>Gets the open profile.</summary>
    public LocalProfile Current => All.FirstOrDefault(p => p.Id == CurrentId()) ?? All[0];

    /// <summary>Gets a value indicating whether there is more than the main profile.</summary>
    public bool HasSeveral => Others().Count > 0;

    /// <summary>
    /// Returns the backup set of the open profile (<see cref="Vafadar.Backup.BackupOptions.FileSet"/>): its id, or
    /// <see langword="null"/> for the main profile, whose backups keep the plain file names.
    /// </summary>
    public static string? CurrentBackupSet()
    {
        var id = CurrentId();
        return id.Length > 0 && Others().Any(p => p.Id == id) ? id : null;
    }

    /// <summary>Returns the database file to open at start: that of the last open profile, or the main one.</summary>
    public static string StartupDatabasePath(string mainPath)
    {
        var id = CurrentId();
        return id.Length > 0 && Others().Any(p => p.Id == id) ? PathOf(mainPath, id) : mainPath;
    }

    /// <summary>Returns the display name of a profile.</summary>
    public string NameOf(LocalProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        return string.IsNullOrWhiteSpace(profile.Name) ? translator["Profile_Main"] : profile.Name;
    }

    /// <summary>Adds a profile; it is opened separately and starts with onboarding.</summary>
    public LocalProfile Create(string name)
    {
        var profile = new LocalProfile(Guid.NewGuid().ToString("N"), Clean(name));
        Save([.. Others(), profile]);
        Changed?.Invoke(this, EventArgs.Empty);
        return profile;
    }

    /// <summary>Renames a profile.</summary>
    public void Rename(LocalProfile profile, string name)
    {
        ArgumentNullException.ThrowIfNull(profile);
        if (profile.IsMain)
        {
            Preferences.Default.Set(MainNameKey, Clean(name));
        }
        else
        {
            Save([.. Others().Select(p => p.Id == profile.Id ? p with { Name = Clean(name) } : p)]);
        }

        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Deletes a profile that is not open, with all its data. The main profile cannot be deleted; its data can be
    /// deleted in the settings.
    /// </summary>
    public void Delete(LocalProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        if (profile.IsMain || profile.Id == Current.Id)
        {
            throw new InvalidOperationException("The main profile and the open profile cannot be deleted.");
        }

        Save([.. Others().Where(p => p.Id != profile.Id)]);
        var path = PathOf(MainPath(), profile.Id);
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();

        // Its backups on this device go too (its backup set, see CurrentBackupSet); cloud backups stay in the account.
        var backups = new[] { "backups", "backups-safety" }
            .Select(folder => Path.Combine(Path.GetDirectoryName(MainPath())!, folder))
            .Where(Directory.Exists)
            .SelectMany(folder => Directory.EnumerateFiles(folder, $"{ZananceApp.AppId}~{profile.Id}_*"));
        foreach (var file in new[] { path, path + "-wal", path + "-shm", path + "-journal" }.Concat(backups))
        {
            try
            {
                File.Delete(file);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                System.Diagnostics.Debug.WriteLine($"Profile file not deleted: {ex.GetType().Name}");
            }
        }

        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Opens another profile. A profile with the app lock asks for the device owner first (SEC-01); when that fails,
    /// the open profile stays.
    /// </summary>
    /// <returns><see langword="true"/> when the profile is open.</returns>
    public async Task<bool> SwitchToAsync(LocalProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        if (profile.Id == Current.Id)
        {
            return true;
        }

        var previous = location.Path;
        undo.Dismiss();
        var opened = false;

        // Automatic posting never runs while the database moves, so no occurrence is posted into the wrong profile.
        await autoPost.RunExclusiveAsync(async () =>
        {
            location.MoveTo(PathOf(MainPath(), profile.Id));
            try
            {
                await services.MigrateLocalDatabaseAsync<ZananceDbContext>();
                var settings = await store.GetSettingsAsync();
                opened = !settings.AppLockEnabled
                    || await appLock.Authenticator.AuthenticateAsync(translator["Profile_UnlockReason"]) == AuthenticationOutcome.Success;
            }
            finally
            {
                if (!opened)
                {
                    location.MoveTo(previous);
                }
            }
        });

        if (!opened)
        {
            return false;
        }

        Preferences.Default.Set(CurrentKey, profile.Id);
        store.ResetSession();
        await appLock.ReloadAsync();
        Changed?.Invoke(this, EventArgs.Empty);
        return true;
    }

    private string MainPath() => Path.Combine(Path.GetDirectoryName(location.Path)!, ZananceApp.DatabaseFileName);

    private static string PathOf(string mainPath, string id) =>
        id.Length == 0 ? mainPath : Path.Combine(Path.GetDirectoryName(mainPath)!, $"zanance-{id}.db");

    private static string CurrentId() => Preferences.Default.Get(CurrentKey, string.Empty);

    private static string Clean(string name)
    {
        var trimmed = (name ?? string.Empty).Trim();
        return trimmed.Length > MaxNameLength ? trimmed[..MaxNameLength] : trimmed;
    }

    // Stored as "id:base64(name);…": no JSON serializer needed and safe with any name.
    private static List<LocalProfile> Others() =>
    [
        .. Preferences.Default.Get(ListKey, string.Empty)
            .Split(';', StringSplitOptions.RemoveEmptyEntries)
            .Select(item => item.Split(':', 2))
            .Where(parts => parts.Length == 2 && parts[0].Length == 32)
            .Select(parts => new LocalProfile(parts[0], Decode(parts[1]))),
    ];

    private static void Save(IEnumerable<LocalProfile> profiles) =>
        Preferences.Default.Set(ListKey, string.Join(';', profiles.Select(p => $"{p.Id}:{Convert.ToBase64String(Encoding.UTF8.GetBytes(p.Name ?? string.Empty))}")));

    private static string? Decode(string value)
    {
        try
        {
            var name = Encoding.UTF8.GetString(Convert.FromBase64String(value));
            return name.Length == 0 ? null : name;
        }
        catch (FormatException)
        {
            return null;
        }
    }
}
