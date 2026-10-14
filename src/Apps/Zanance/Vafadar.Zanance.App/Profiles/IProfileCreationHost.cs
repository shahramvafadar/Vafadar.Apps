using Vafadar.Data;

namespace Vafadar.Zanance.App.Profiles;

/// <summary>Retains an explicit Add request before native preparation and resumes its exact identity on startup.</summary>
public interface IProfileCreationHost
{
    /// <summary>Records the reviewed device request, prepares its native files and persists catalogue publication.</summary>
    Task CreateReviewedAsync(ProfileService profiles, ProfileCatalogueReview review, LocalProfile profile,
        DatabaseAccessGate.ExclusiveLease owner, CancellationToken ct);
    /// <summary>Continues existing intent before startup captures all profiles; absence never authorizes creation.</summary>
    Task ResumePendingAsync(ProfileService profiles, DatabaseAccessGate.ExclusiveLease owner, CancellationToken ct);
}
