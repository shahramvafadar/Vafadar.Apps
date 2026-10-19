using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Features.Home;

/// <summary>Retains one covered Home refresh and its actual file through bounded, classified source supersession.</summary>
internal sealed class HomeRefreshSession
{
    private string? _file;

    /// <summary>Rejects a different profile even when a preceding same-file source change allowed another read.</summary>
    internal void RequireOriginalFile(string file)
    {
        _file ??= file;
        if (!string.Equals(_file, file, StringComparison.Ordinal))
            throw new InvalidOperationException("The original Home refresh belongs to another opened file.");
    }

    /// <summary>Retries only same-file data supersession, without uncovering intermediate output or unbounded spinning.</summary>
    internal static async Task RunAsync(Func<HomeRefreshSession, Task> read, Action requireCurrent)
    {
        var session = new HomeRefreshSession();
        for (var attempt = 0; ; attempt++)
        {
            requireCurrent();
            try { await read(session); return; }
            catch (HomeSnapshotSupersededException) when (attempt < 2)
            {
                // D-398: a normal concurrent source edit retires only this read, never its file or access guards.
                requireCurrent();
            }
        }
    }
}
