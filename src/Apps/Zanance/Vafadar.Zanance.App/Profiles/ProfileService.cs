using System.Text;
using Vafadar.Data;
using Vafadar.Localization;
using Vafadar.Core.Settings;
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
public sealed partial class ProfileService(
    LocalDatabaseLocation<ZananceDbContext> location,
    ZananceStore store,
    ISettingsStore preferences,
    IProfileRuntime runtime,
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
    public IReadOnlyList<LocalProfile> All => [new LocalProfile(string.Empty, preferences.Get(MainNameKey)), .. Others(preferences)];

    /// <summary>Gets the open profile.</summary>
    public LocalProfile Current => All.FirstOrDefault(p => p.Id == CurrentId(preferences)) ?? All[0];

    /// <summary>Gets a value indicating whether there is more than the main profile.</summary>
    public bool HasSeveral => Others(preferences).Count > 0;

    /// <summary>
    /// Returns the backup set of the open profile (<see cref="Vafadar.Backup.BackupOptions.FileSet"/>): its id, or
    /// <see langword="null"/> for the main profile, whose backups keep the plain file names.
    /// </summary>
    public static string? CurrentBackupSet(ISettingsStore preferences)
    {
        var id = CurrentId(preferences);
        return id.Length > 0 && Others(preferences).Any(p => p.Id == id) ? id : null;
    }

    /// <summary>Returns the database file to open at start: that of the last open profile, or the main one.</summary>
    public static string StartupDatabasePath(string mainPath, ISettingsStore preferences)
    {
        var id = CurrentId(preferences);
        return id.Length > 0 && Others(preferences).Any(p => p.Id == id) ? PathOf(mainPath, id) : mainPath;
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
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        var profile = new LocalProfile(Guid.NewGuid().ToString("N"), Clean(name));
        Save([.. Others(preferences), profile]);
        Changed?.Invoke(this, EventArgs.Empty);
        return profile;
    }

    /// <summary>Renames a profile.</summary>
    public void Rename(LocalProfile profile, string name)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        RequireKnown(profile);
        if (profile.IsMain)
        {
            preferences.Set(MainNameKey, Clean(name));
        }
        else
        {
            Save([.. Others(preferences).Select(p => p.Id == profile.Id ? p with { Name = Clean(name) } : p)]);
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
        RequireKnown(profile);
        if (profile.IsMain || profile.Id == Current.Id)
        {
            throw new InvalidOperationException("The main profile and the open profile cannot be deleted.");
        }

        Save([.. Others(preferences).Where(p => p.Id != profile.Id)]);
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
        RequireKnown(profile);
        if (profile.Id == Current.Id)
        {
            return true;
        }

        var previous = location.Path;
        runtime.DismissUndo();
        var opened = false;

        // Automatic posting never runs while the database moves, so no occurrence is posted into the wrong profile.
        await runtime.RunExclusiveAsync(async () =>
        {
            location.MoveTo(PathOf(MainPath(), profile.Id));
            try
            {
                await runtime.MigrateAsync();
                var settings = await store.GetSettingsAsync();
                opened = !settings.AppLockEnabled
                    || await runtime.AuthenticateAsync(translator["Profile_UnlockReason"]);
                if (opened)
                {
                    // Backups and the next start follow the open profile from the same moment on.
                    preferences.Set(CurrentKey, profile.Id);
                }
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

        store.ResetSession();
        await runtime.ReloadLockAsync();
        Changed?.Invoke(this, EventArgs.Empty);
        return true;
    }

    // Only catalogue identities may form a path or select a database; length alone does not validate stored ids.
    private void RequireKnown(LocalProfile profile)
    {
        if (!All.Any(p => p.Id == profile.Id)) { throw new ArgumentException("The profile is not in the local catalogue.", nameof(profile)); }
    }

    private string MainPath() => Path.Combine(Path.GetDirectoryName(location.Path)!, ZananceApp.DatabaseFileName);

    private static string PathOf(string mainPath, string id) =>
        id.Length == 0 ? mainPath : Path.Combine(Path.GetDirectoryName(mainPath)!, $"zanance-{id}.db");

    private static string CurrentId(ISettingsStore preferences) => preferences.Get(CurrentKey) ?? string.Empty;

    private static string Clean(string name)
    {
        var trimmed = (name ?? string.Empty).Trim();
        return trimmed.Length > MaxNameLength ? trimmed[..MaxNameLength] : trimmed;
    }

    // Stored as "id:base64(name);…": no JSON serializer needed and safe with any name.
    private static List<LocalProfile> Others(ISettingsStore preferences) =>
    [
        .. (preferences.Get(ListKey) ?? string.Empty)
            .Split(';', StringSplitOptions.RemoveEmptyEntries)
            .Select(item => item.Split(':', 2))
            .Where(parts => parts.Length == 2 && Guid.TryParseExact(parts[0], "N", out _))
            .Select(parts => new LocalProfile(parts[0], Decode(parts[1]))),
    ];

    private void Save(IEnumerable<LocalProfile> profiles) =>
        preferences.Set(ListKey, string.Join(';', profiles.Select(p => $"{p.Id}:{Convert.ToBase64String(Encoding.UTF8.GetBytes(p.Name ?? string.Empty))}")));

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
