using System.Collections.Concurrent;
using System.Text;
using Vafadar.Core.Settings;
using Vafadar.Zanance.Core.Commerce;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Profiles;

public sealed partial class ProfileService
{
    // The device catalogue has one synchronous preference publication, shared across service instances. No await
    // occurs under this gate; database switching keeps its existing automatic-posting exclusive boundary.
    private static readonly ConcurrentDictionary<string, object> CatalogueGates = new(StringComparer.Ordinal);
    private const string CatalogueKey = "profiles.work.catalogue";
    private object CatalogueGate => CatalogueGates.GetOrAdd(Path.GetFullPath(MainPath()), _ => new());
    private IProfileWorkAccessSource WorkAccess => workAccess ?? DefaultWorkAccess;
    private static readonly IProfileWorkAccessSource DefaultWorkAccess = new InactiveProfileWorkAccessSource();

    /// <summary>Reads complete original device metadata and cached policy without opening another database or writing preferences.</summary>
    public ProfileCatalogueReview ReadWorkReview()
    {
        lock (CatalogueGate)
        {
            var path = Path.GetFullPath(MainPath()); var access = CaptureWork(path); var state = ReadCatalogue(preferences);
            var originals = CatalogueProfiles(state);
            var (availability, valid) = ResolveCatalogueWork(state, originals, access, path);
            var result = new ProfileCatalogueReview(path, Path.GetFullPath(location.Path), CurrentId(preferences), preferences.Get(CatalogueKey),
                preferences.Get(ListKey), preferences.Get(MainNameKey), originals, access, availability, valid);
            ValidateWorkCore(result, checkCurrentPath: true); return result;
        }
    }

    /// <summary>Rechecks complete catalogue/choice/current-file/provider facts before publishing or continuing a native prompt.</summary>
    public void ValidateWorkReview(ProfileCatalogueReview review)
    { lock (CatalogueGate) ValidateWorkCore(review, checkCurrentPath: true); }

    private ProfileWorkAccess CaptureWork(string path)
    {
        var access = WorkAccess.Capture(path) ?? throw new InvalidOperationException("Missing profile policy snapshot.");
        if (access.Enforced && access.CataloguePath != path) throw new InvalidOperationException("The profile policy belongs to another catalogue.");
        return access;
    }
    private void ValidateWorkCore(ProfileCatalogueReview review, bool checkCurrentPath)
    {
        ArgumentNullException.ThrowIfNull(review);
        if (review.CataloguePath != Path.GetFullPath(MainPath()) || review.CurrentId != CurrentId(preferences)
            || checkCurrentPath && review.CurrentPath != Path.GetFullPath(location.Path)
            || review.Packet != preferences.Get(CatalogueKey) || review.LegacyList != preferences.Get(ListKey)
            || review.LegacyName != preferences.Get(MainNameKey) || review.Access != CaptureWork(review.CataloguePath))
            throw new InvalidOperationException("The original profile catalogue changed; read it again.");
    }

    /// <summary>Explicitly saves a reviewed original subset in device preferences; never opens profiles, posts money or creates paid facts.</summary>
    public bool SaveWorkChoice(ProfileCatalogueReview review, IEnumerable<string> selectedIds)
    {
        ArgumentNullException.ThrowIfNull(selectedIds);
        lock (CatalogueGate)
        {
            ValidateWorkCore(review, true);
            if (!review.CanChoose) throw new InvalidOperationException("No bounded device profile choice is required.");
            var chosen = LocalProfileWorkPolicy.Resolve(review.Access.DeviceScope!, review.Access.Context!, review.Profiles.Select(p => p.Id), selectedIds);
            if (chosen.RequiresSelection) throw new ArgumentException("Choose only original profiles within device capacity.", nameof(selectedIds));
            var state = ReadCatalogue(preferences);
            var value = string.Join(';', chosen.SelectedIds.Select(id => id.Length == 0 ? "~" : id));
            var next = state with { Path = EncodeField(review.CataloguePath), Scope = review.Access.DeviceScope!.Id.ToString("N"), Choice = EncodeField(value), ChoiceRevision = Guid.NewGuid().ToString("N") };
            if (state.Path == next.Path && state.Scope == next.Scope && state.Choice == next.Choice && review.ValidChoice) return false;
            PublishCatalogue(next); Changed?.Invoke(this, EventArgs.Empty); return true;
        }
    }

    /// <summary>Creates one new original and joins a free chosen device slot in the same preference publication.</summary>
    public LocalProfile CreateReviewed(ProfileCatalogueReview review, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (location.IsProtected)
            throw new InvalidOperationException("Protected profiles require asynchronous verified preparation.");
        lock (CatalogueGate)
        {
            var profile = new LocalProfile(Guid.NewGuid().ToString("N"), Clean(name));
            return PublishNewProfile(review, profile);
        }
    }

    /// <summary>Prepares a new database under drained maintenance, then rechecks the reviewed catalogue before publication.</summary>
    public async Task<LocalProfile> CreateReviewedAsync(ProfileCatalogueReview review, string name, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name); ValidateWorkReview(review);
        if (!review.CanCreate) throw new CommercialWriteRejectedException(CommercialFeature.LocalProfiles, QuotaKind.LocalProfiles,
            review.Maximum ?? 0, review.SelectedIds.Count, 1);
        var profile = new LocalProfile(Guid.NewGuid().ToString("N"), Clean(name));
        await runtime.RunExclusiveAsync(async () =>
        {
            using var owner = await location.Access.AcquireExclusiveAsync(ct);
            ValidateWorkReview(review); ct.ThrowIfCancellationRequested();
            await runtime.PrepareNewDatabaseAsync(location, profile.Id, PathOf(MainPath(), profile.Id), owner, ct);
            ct.ThrowIfCancellationRequested();
            lock (CatalogueGate) PublishNewProfile(review, profile);
        });
        return profile;
    }

    private LocalProfile PublishNewProfile(ProfileCatalogueReview review, LocalProfile profile)
    {
        ValidateWorkCore(review, true);
        if (!review.CanCreate)
            throw new CommercialWriteRejectedException(CommercialFeature.LocalProfiles, QuotaKind.LocalProfiles,
                review.Maximum ?? 0, review.SelectedIds.Count, 1);
        var state = ReadCatalogue(preferences) with { List = EncodeProfiles(review.Profiles.Where(p => !p.IsMain).Append(profile)) };
        if (review.Maximum is not null)
            state = state with { Path = EncodeField(review.CataloguePath), Scope = review.Access.DeviceScope!.Id.ToString("N"),
                Choice = EncodeField(string.Join(';', review.SelectedIds.Append(profile.Id).Order(StringComparer.Ordinal).Select(id => id.Length == 0 ? "~" : id))), ChoiceRevision = Guid.NewGuid().ToString("N") };
        PublishCatalogue(state); Changed?.Invoke(this, EventArgs.Empty); return profile;
    }

    /// <summary>Corrects only the reviewed original name; same-id replacements and retired prompts require a fresh read.</summary>
    public void RenameReviewed(ProfileCatalogueReview review, LocalProfile profile, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        lock (CatalogueGate)
        {
            ValidateWorkCore(review, true); RequireReviewedProfile(review, profile);
            var state = ReadCatalogue(preferences);
            state = profile.IsMain ? state with { MainName = Clean(name) }
                : state with { List = EncodeProfiles(review.Profiles.Where(p => !p.IsMain).Select(p => p.Id == profile.Id ? p with { Name = Clean(name) } : p)) };
            PublishCatalogue(state); Changed?.Invoke(this, EventArgs.Empty);
        }
    }

    private static void RequireReviewedProfile(ProfileCatalogueReview review, LocalProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        if (!review.Profiles.Contains(profile)) throw new ArgumentException("The original profile is not in this review.", nameof(profile));
    }

    private static (LocalProfileAvailability? Availability, bool Valid) ResolveCatalogueWork(CatalogueState state,
        IReadOnlyList<LocalProfile> originals, ProfileWorkAccess access, string path)
    {
        var valid = true; string[]? selected = null;
        if (state.Scope != "-")
        {
            try
            {
                valid = access.Enforced && DecodeField(state.Path) == path && Guid.ParseExact(state.Scope, "N") == access.DeviceScope!.Id
                    && Guid.TryParseExact(state.ChoiceRevision, "N", out var revision) && revision != Guid.Empty;
                selected = (DecodeField(state.Choice) ?? throw new FormatException("Missing profile choice.")).Split(';', StringSplitOptions.RemoveEmptyEntries).Select(id => id == "~" ? "" : id).ToArray();
                foreach (var id in selected) LocalProfileWorkPolicy.ValidateId(id);
            }
            catch (Exception ex) when (ex is FormatException or ArgumentException) { valid = false; selected = null; }
        }
        else if (state.Path != "-" || state.Choice != "-") valid = false;
        return (access.Enforced ? LocalProfileWorkPolicy.Resolve(access.DeviceScope!, access.Context!, originals.Select(p => p.Id), selected) : null, valid);
    }

    /// <summary>Captures exact original file activity without consulting the newly current profile or opening any database.</summary>
    internal static PersonalProfileWork CaptureFileWork(ISettingsStore preferences, string mainPath, string databasePath,
        ProfileWorkAccess access)
    {
        mainPath = Path.GetFullPath(mainPath); databasePath = Path.GetFullPath(databasePath);
        if (!access.Enforced || access.CataloguePath != mainPath)
            throw new InvalidOperationException("The verified device facts belong to another catalogue.");
        lock (CatalogueGates.GetOrAdd(mainPath, _ => new()))
        {
            var packet = preferences.Get(CatalogueKey); var legacyList = preferences.Get(ListKey); var legacyName = preferences.Get(MainNameKey);
            LocalProfile? original = null; var needsChoice = true; var canStart = false;
            try
            {
                var state = ReadCatalogue(packet, legacyName, legacyList); var originals = CatalogueProfiles(state);
                original = originals.SingleOrDefault(profile => PathOf(mainPath, profile.Id) == databasePath);
                var (availability, valid) = ResolveCatalogueWork(state, originals, access, mainPath);
                needsChoice = original is null || availability!.Maximum is not null && (!valid || availability.RequiresSelection);
                canStart = original is not null && !needsChoice && availability!.IsSelected(original.Id);
            }
            catch (Exception ex) when (ex is FormatException or ArgumentException or InvalidOperationException)
            {
                // Corrupt or missing catalogue metadata cannot invent an original identity or new activity.
                // The independently verified actual-file context still permits owned history, backup and repair.
                original = null; needsChoice = true; canStart = false;
            }
            if (packet != preferences.Get(CatalogueKey) || legacyList != preferences.Get(ListKey) || legacyName != preferences.Get(MainNameKey))
                throw new InvalidOperationException("The original catalogue changed during file activity capture.");
            // Length-prefixed nullable inputs prevent delimiter collisions without serializing provider objects.
            // The stamp is process/device-local: it is not a purchase, authorization token or portable setting.
            using var hash = System.Security.Cryptography.IncrementalHash.CreateHash(System.Security.Cryptography.HashAlgorithmName.SHA256);
            Span<byte> length = stackalloc byte[4];
            foreach (var value in new[] { mainPath, databasePath, original?.Id, packet, legacyList, legacyName,
                access.DeviceScope!.Kind.ToString(), access.DeviceScope.Id.ToString("N"), access.Context!.PersonalPlan.ToString(),
                access.Context.Scope.Kind.ToString(), access.Context.Scope.Id.ToString("N"), access.Context.SharedAccess?.ToString(),
                access.Context.ProfileWork?.ToString() })
            {
                System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(length, value is null ? -1 : Encoding.UTF8.GetByteCount(value));
                hash.AppendData(length); if (value is not null) hash.AppendData(Encoding.UTF8.GetBytes(value));
            }
            return new(original?.Id, canStart, needsChoice,
                Convert.ToHexStringLower(hash.GetHashAndReset()));
        }
    }

    private sealed record CatalogueState(string? MainName, string? List, string Path = "-", string Scope = "-", string Choice = "-", string ChoiceRevision = "-");
    private static CatalogueState ReadCatalogue(ISettingsStore source) =>
        ReadCatalogue(source.Get(CatalogueKey), source.Get(MainNameKey), source.Get(ListKey));
    private static CatalogueState ReadCatalogue(string? raw, string? mainName, string? list)
    {
        if (raw is null) return new(mainName, list);
        var fields = raw.Split('|');
        if (fields.Length != 8 || fields[0] != "1")
            throw new InvalidOperationException("Invalid original profile catalogue packet.");
        return new(DecodeField(fields[1]), DecodeField(fields[2]), fields[3], fields[4], fields[5], fields[7]);
    }
    private void PublishCatalogue(CatalogueState state)
    {
        // One preference Set publishes names, all original ids and device choice together. Legacy keys stay untouched;
        // startup, backup-set resolution and every catalogue reader use this packet after explicit first mutation.
        preferences.Set(CatalogueKey, string.Join('|', "1", EncodeField(state.MainName), EncodeField(state.List), state.Path,
            state.Scope, state.Choice, Guid.NewGuid().ToString("N"), state.ChoiceRevision));
    }
    private static string EncodeField(string? value) => value is null ? "-" : "=" + Convert.ToBase64String(Encoding.UTF8.GetBytes(value));
    private static string? DecodeField(string value) => value == "-" ? null : value.StartsWith('=')
        ? Encoding.UTF8.GetString(Convert.FromBase64String(value[1..])) : throw new FormatException("Invalid catalogue field.");
    private static string EncodeProfiles(IEnumerable<LocalProfile> profiles) => string.Join(';', profiles.Select(p => $"{p.Id}:{Convert.ToBase64String(Encoding.UTF8.GetBytes(p.Name ?? string.Empty))}"));
    private static IReadOnlyList<LocalProfile> CatalogueProfiles(CatalogueState state) =>
        [new LocalProfile(string.Empty, state.MainName), .. ParseOthers(state.List)];
}
