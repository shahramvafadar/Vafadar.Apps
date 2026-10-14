using Vafadar.Data;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Profiles;

/// <summary>Final protected profile retirement under the real drained application owner.</summary>
public interface IProfileRetirementHost
{
    /// <summary>Persists the reviewed request, removes its exact catalogue row and completes native reference cleanup.</summary>
    Task DeleteReviewedAsync(ProfileService profiles, ProfileCatalogueReview review, LocalProfile profile,
        LocalDatabaseLocation<ZananceDbContext> location, DatabaseAccessGate.ExclusiveLease owner, CancellationToken ct);
    /// <summary>Resumes existing pending operations before complete startup profile capture.</summary>
    Task ResumePendingAsync(ProfileService profiles, LocalDatabaseLocation<ZananceDbContext> location,
        DatabaseAccessGate.ExclusiveLease owner, CancellationToken ct);
}
