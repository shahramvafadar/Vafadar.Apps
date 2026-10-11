using Vafadar.Zanance.Core.Commerce;

namespace Vafadar.Zanance.App.Profiles;

/// <summary>Detached complete original device catalogue and reviewed choice; no database or portable security state.</summary>
public sealed class ProfileCatalogueReview
{
    /// <summary>Captures original catalogue/current-file inputs and a detached device work projection.</summary>
    internal ProfileCatalogueReview(string path, string currentPath, string currentId, string? packet, string? legacyList,
        string? legacyName, IReadOnlyList<LocalProfile> profiles, ProfileWorkAccess access, LocalProfileAvailability? availability,
        bool validChoice)
    {
        CataloguePath = path; CurrentPath = currentPath; CurrentId = currentId; Packet = packet; LegacyList = legacyList;
        LegacyName = legacyName; Profiles = Array.AsReadOnly(profiles.ToArray()); Access = access; Availability = availability; ValidChoice = validChoice;
    }
    /// <summary>Gets the original main-file boundary.</summary>
    internal string CataloguePath { get; }
    /// <summary>Gets the original open-file boundary.</summary>
    internal string CurrentPath { get; }
    /// <summary>Gets the original current preference id.</summary>
    internal string CurrentId { get; }
    /// <summary>Gets the original atomic catalogue packet.</summary>
    internal string? Packet { get; }
    /// <summary>Gets the original legacy catalogue input.</summary>
    internal string? LegacyList { get; }
    /// <summary>Gets the original legacy main-name input.</summary>
    internal string? LegacyName { get; }
    /// <summary>Gets the immutable cached device authority.</summary>
    internal ProfileWorkAccess Access { get; }
    /// <summary>Gets the separate original device work projection.</summary>
    internal LocalProfileAvailability? Availability { get; }
    /// <summary>Gets whether stored choice scope and value can be used without repair.</summary>
    internal bool ValidChoice { get; }
    /// <summary>Gets every original main/other profile, with original names and identity spelling.</summary>
    public IReadOnlyList<LocalProfile> Profiles { get; }
    /// <summary>Gets device capacity; inactive registration remains unrestricted.</summary>
    public int? Maximum => Availability?.Maximum;
    /// <summary>Gets whether missing/malformed/foreign or over-capacity selection needs explicit repair.</summary>
    public bool RequiresSelection => Maximum is not null && (!ValidChoice || Availability!.RequiresSelection);
    /// <summary>Gets the currently usable originals without granting financial-account or paid feature rights.</summary>
    public IReadOnlyList<string> SelectedIds => RequiresSelection ? [] : Availability?.SelectedIds ?? Profiles.Select(p => p.Id).ToArray();
    /// <summary>Gets whether explicit bounded choice is currently supported.</summary>
    public bool CanChoose => Maximum is > 0;
    /// <summary>Gets whether a new profile can join a free device slot, including an explicitly empty repaired choice.</summary>
    public bool CanCreate => !RequiresSelection && (Maximum is null || SelectedIds.Count < Maximum);
    /// <summary>Checks original device activity only; all originals remain openable for retained work.</summary>
    public bool CanUse(string id) => Profiles.Any(p => p.Id == id) && SelectedIds.Contains(id, StringComparer.Ordinal);
}
