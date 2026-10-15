namespace Vafadar.Zanance.App.Features.Sync;

/// <summary>Explicit network-enabled local personal edition; offline builds never acquire its permission implicitly.</summary>
public static class PersonalSyncBuild
{
    /// <summary>Gets whether this build explicitly opted into the qualified local personal transport.</summary>
    public static bool IsAvailable =>
#if PERSONAL_SYNC && (ANDROID || WINDOWS)
        true;
#else
        false;
#endif
}
