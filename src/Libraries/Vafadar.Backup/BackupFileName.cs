using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Vafadar.Backup;

/// <summary>
/// Builds and parses backup file names: <c>{appId}_{yyyyMMdd'T'HHmmss'Z'}.vbak</c>,
/// e.g. <c>pro.vafadar.finance_20260925T143000Z.vbak</c>.
/// </summary>
/// <remarks>
/// The app id prefix lets several apps share one storage location (for example, all apps registered in the same
/// Google Cloud project share one Drive app data folder).
/// </remarks>
public static class BackupFileName
{
    /// <summary>The file extension of backup packages.</summary>
    public const string Extension = ".vbak";

    private const string TimestampFormat = "yyyyMMdd'T'HHmmss'Z'";

    /// <summary>Creates the file name for a backup of <paramref name="appId"/> taken at <paramref name="createdAt"/>.</summary>
    public static string Create(string appId, DateTimeOffset createdAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(appId);

        if (appId.IndexOfAny(['_', '/', '\\']) >= 0)
        {
            throw new ArgumentException("The app id must not contain '_', '/' or '\\'.", nameof(appId));
        }

        return $"{appId}_{createdAt.UtcDateTime.ToString(TimestampFormat, CultureInfo.InvariantCulture)}{Extension}";
    }

    /// <summary>Parses a file name created by <see cref="Create"/>.</summary>
    public static bool TryParse(string? fileName, [NotNullWhen(true)] out string? appId, out DateTimeOffset createdAt)
    {
        appId = null;
        createdAt = default;

        if (fileName is null || !fileName.EndsWith(Extension, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var stem = fileName[..^Extension.Length];
        var separator = stem.LastIndexOf('_');
        if (separator <= 0)
        {
            return false;
        }

        if (!DateTime.TryParseExact(
                stem[(separator + 1)..],
                TimestampFormat,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal,
                out var timestamp))
        {
            return false;
        }

        appId = stem[..separator];
        createdAt = new DateTimeOffset(timestamp, TimeSpan.Zero);
        return true;
    }
}
