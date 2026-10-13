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
    Translator translator,
    IProfileWorkAccessSource? workAccess = null) : IProfileCatalogueChanges
{
    private const string ListKey = "profiles.list";
    private const string CurrentKey = "profiles.current";
    private const string MainNameKey = "profiles.main.name";

    /// <summary>The longest profile name.</summary>
    public const int MaxNameLength = 40;

    /// <summary>Raised after another profile was opened or the list changed.</summary>
    public event EventHandler? Changed;

    /// <summary>Gets all profiles, the main one first.</summary>
    public IReadOnlyList<LocalProfile> All => CatalogueProfiles(ReadCatalogue(preferences));

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
    public LocalProfile Create(string name) => CreateReviewed(ReadWorkReview(), name);

    /// <summary>Renames a complete original profile using the current reviewed catalogue.</summary>
    public void Rename(LocalProfile profile, string name) => RenameReviewed(ReadWorkReview(), profile, name);

    /// <summary>
    /// Deletes a profile that is not open, with all its data. The main profile cannot be deleted; its data can be
    /// deleted in the settings.
    /// </summary>
    public void Delete(LocalProfile profile) => DeleteReviewed(ReadWorkReview(), profile);

    /// <summary>Deletes only the reviewed inactive original and releases its selected device slot atomically.</summary>
    public void DeleteReviewed(ProfileCatalogueReview review, LocalProfile profile)
    {
        using var owner = location.Access.AcquireExclusive();
        DeleteCore(review, profile, owner);
    }

    /// <summary>Deletes an inactive profile after active context/file work drains; rechecks the original reviewed catalogue.</summary>
    public Task DeleteAsync(LocalProfile profile, CancellationToken cancellationToken = default) =>
        DeleteReviewedAsync(ReadWorkReview(), profile, cancellationToken);

    /// <summary>Waits without blocking UI, then deletes only the still-reviewed inactive original under maintenance ownership.</summary>
    public async Task DeleteReviewedAsync(ProfileCatalogueReview review, LocalProfile profile, CancellationToken cancellationToken = default)
    {
        ValidateWorkReview(review); RequireReviewedProfile(review, profile);
        using var owner = await location.Access.AcquireExclusiveAsync(cancellationToken);
        cancellationToken.ThrowIfCancellationRequested(); DeleteCore(review, profile, owner);
    }

    private void DeleteCore(ProfileCatalogueReview review, LocalProfile profile, DatabaseAccessGate.ExclusiveLease owner)
    {
        owner.Validate();
        lock (CatalogueGate)
        {
            owner.Validate();
            ValidateWorkCore(review, true); RequireReviewedProfile(review, profile);
            if (profile.IsMain || profile.Id == Current.Id)
            {
                throw new InvalidOperationException("The main profile and the open profile cannot be deleted.");
            }

            // The legacy filename does not identify a protected generation's retained journal/history,
            // keys or safety copies. Until the native retirement transaction handles that complete set,
            // do not publish a removed catalogue row and strand its encryption identities (D-230).
            var original = PathOf(MainPath(), profile.Id);
            if (location.IsProtected || HasProtectedProfileState(original))
                throw new InvalidOperationException("Protected profile deletion requires complete native retirement.");

            var state = ReadCatalogue(preferences) with { List = EncodeProfiles(review.Profiles.Where(p => !p.IsMain && p.Id != profile.Id)) };
            if (review.Maximum is not null && !review.RequiresSelection)
                state = state with { Choice = EncodeField(string.Join(';', review.SelectedIds.Where(id => id != profile.Id).Select(id => id.Length == 0 ? "~" : id))), ChoiceRevision = Guid.NewGuid().ToString("N") };
            PublishCatalogue(state);
            var path = PathOf(MainPath(), profile.Id);
            LocalDatabasePools.ClearFile(path);

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
    }

    private static bool HasProtectedProfileState(string original)
    {
        // Guard partially migrated profiles even when the current profile still has a legacy endpoint.
        foreach (var suffix in new[] { ".generation.sqlite", ".generation.sqlite.creating", ".new-profile.sqlite",
            ".new-profile.sqlite.creating", ".restore-history.sqlite", ".restore-history.sqlite.creating" })
            if (File.Exists(original + suffix) || Directory.Exists(original + suffix)) return true;
        return Directory.EnumerateFiles(Path.GetDirectoryName(original)!, Path.GetFileName(original) + ".*.cipher.db*").Any();
    }

    /// <summary>
    /// Opens another profile. A profile with the app lock asks for the device owner first (SEC-01); when that fails,
    /// the open profile stays.
    /// </summary>
    /// <returns><see langword="true"/> when the profile is open.</returns>
    public Task<bool> SwitchToAsync(LocalProfile profile) => SwitchReviewedAsync(ReadWorkReview(), profile);

    /// <summary>Opens any reviewed original for retained work; rechecks catalogue and rights before committing after authentication.</summary>
    public async Task<bool> SwitchReviewedAsync(ProfileCatalogueReview review, LocalProfile profile)
    {
        ValidateWorkReview(review); RequireReviewedProfile(review, profile);
        if (profile.Id == Current.Id)
        {
            return true;
        }

        runtime.DismissUndo();
        var opened = false;

        // Automatic posting never runs while the database moves, so no occurrence is posted into the wrong profile.
        await runtime.RunExclusiveAsync(async () =>
        {
            using var owner = await location.Access.AcquireExclusiveAsync();
            ValidateWorkReview(review);
            var previous = location.CapturePublication(owner);
            try
            {
                await runtime.OpenDatabaseAsync(location, PathOf(MainPath(), profile.Id), owner);
                var settings = await store.GetSettingsForMaintenanceAsync(owner);
                var authenticated = !settings.AppLockEnabled
                    || await runtime.AuthenticateAsync(translator["Profile_UnlockReason"]);
                if (authenticated)
                {
                    // Backups and the next start follow the open profile from the same moment on.
                    lock (CatalogueGate)
                    {
                        ValidateWorkCore(review, checkCurrentPath: false);
                        preferences.Set(CurrentKey, profile.Id);
                        // Successful authentication alone is not a committed switch. A retired catalogue must
                        // restore the previous path even when the device authentication itself succeeded.
                        opened = true;
                    }
                }
            }
            finally
            {
                try
                {
                    if (!opened) previous.Restore(owner);
                }
                finally { runtime.FinishDatabaseTransition(opened, owner); }
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
    private static List<LocalProfile> Others(ISettingsStore preferences) => ParseOthers(ReadCatalogue(preferences).List);

    private static List<LocalProfile> ParseOthers(string? list) =>
    [
        .. (list ?? string.Empty)
            .Split(';', StringSplitOptions.RemoveEmptyEntries)
            .Select(item => item.Split(':', 2))
            .Where(parts => parts.Length == 2 && Guid.TryParseExact(parts[0], "N", out _))
            .Select(parts => new LocalProfile(parts[0], Decode(parts[1]))),
    ];

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
