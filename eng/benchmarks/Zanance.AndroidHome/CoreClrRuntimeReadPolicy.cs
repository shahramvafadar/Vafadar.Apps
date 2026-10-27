namespace Vafadar.Zanance.App;

/// <summary>Bounds the separate experimental runtime's source port; no shipping or retained caller is eligible.</summary>
internal static class CoreClrRuntimeReadPolicy
{
    /// <summary>The fresh package owning only this fictitious cohort.</summary>
    internal const string Package = "pro.vafadar.zanance.coreclrruntimereview";
    /// <summary>The sole shell-protected observation authority.</summary>
    internal const string Authority = Package + ".runtimeproof";
    /// <summary>The actual new UID observed at its first installation, never an inferred original Main UID.</summary>
    internal const int OwnerUid = 10399;

    /// <summary>Admits only a fixed source checkpoint and a traversal-free unique run identifier.</summary>
    internal static bool IsRequest(string run, string stage) => run.Length == 32
        && run.All(c => c is >= '0' and <= '9' or >= 'a' and <= 'f')
        && stage is "seal" or "opening" or "expense" or "income" or "destination" or "transfer" or "refund"
            or "en-light" or "en-dark" or "fa-light" or "fa-dark" or "de-light" or "de-dark" or "cold";

    /// <summary>Checks ownership before any dependency/service/storage access, including a cold provider start.</summary>
    internal static bool IsOwnedShellRequest(string package, int uid, int callerUid, int api, string model,
        bool coreClr, string run, string stage) => package == Package && uid == OwnerUid && callerUid == 2000
        && api == 36 && model == "SM-A176B" && coreClr && IsRequest(run, stage);

    /// <summary>Never lets a pending/covered/locked/foreign-profile observation trigger source preparation or reads.</summary>
    internal static Task<T> ReadOnlyWhenReadyAsync<T>(bool originalStartupReady, bool originalWindowFocused,
        bool unlocked, bool onlyOriginalMain, Func<Task<T>> read)
    {
        if (!originalStartupReady || !originalWindowFocused || !unlocked || !onlyOriginalMain)
            throw new InvalidOperationException("Original owned unlocked ready Main is required before source observation.");
        return read();
    }
}
