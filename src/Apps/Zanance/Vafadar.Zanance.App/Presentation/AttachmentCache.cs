using Vafadar.Backup;
using Vafadar.Zanance.App.Features.DataFiles;
using Vafadar.Zanance.Core;

namespace Vafadar.Zanance.App.Presentation;

/// <summary>Retires only original application cache outputs on startup and after explicit data deletion.</summary>
internal static class AttachmentCache
{
    /// <summary>Gets the private FileProvider directory used for transient camera originals.</summary>
    internal const string CameraDirectoryName = "camera";

    /// <summary>Gets the cache subdirectory the Android file provider allows for report exports.</summary>
    internal const string ReportDirectoryName = "report-export";

    /// <summary>Gets the private staging directory for accountant exports.</summary>
    internal const string AccountantExportDirectoryName = "accountant-export-v1";

    /// <summary>Recognizes an original generated accountant ZIP name.</summary>
    internal static bool IsAccountantExportName(string name) =>
        name.StartsWith("zanance-accountant-", StringComparison.Ordinal) && name.EndsWith(".zip", StringComparison.Ordinal)
        && Guid.TryParseExact(name[19..^4], "N", out _);

    /// <summary>Expires old shares independently of profile selection or the recipient's private copies.</summary>
    internal static void ClearExpiredDataShares(string cacheRoot, DateTimeOffset now)
    {
        var cutoff = now.UtcDateTime.AddHours(-24);
        var csvRoot = Path.Combine(cacheRoot, CsvSharePreparation.DirectoryName);
        var zipRoot = Path.Combine(cacheRoot, AccountantExportDirectoryName);
        ClearShares(zipRoot, cutoff, IsAccountantExportName);
        try
        {
            if (!IsOrdinaryDirectory(csvRoot)) return;
            foreach (var child in Directory.EnumerateDirectories(csvRoot))
                if (Guid.TryParseExact(Path.GetFileName(child), "N", out _))
                    ClearShares(child, cutoff, CsvSharePreparation.IsOwnedFileName, removeEmptyDirectory: true);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // The next cleanup can retry an unavailable cache directory.
        }
    }

    /// <summary>Retires renderer staging files left by a previous Android process before new reads can start.</summary>
    internal static void ClearInterruptedPdfRenders(string cacheRoot) =>
        ClearShares(cacheRoot, null, name =>
            name.Length == 43 && name.StartsWith("render-", StringComparison.Ordinal)
            && name.EndsWith(".pdf", StringComparison.Ordinal)
            && Guid.TryParseExact(name.AsSpan(7, 32), "N", out var id)
            && name.AsSpan(7, 32).SequenceEqual(id.ToString("N")));

    private static void ClearShares(string directory, DateTime? cutoff, Func<string, bool> ownsName, bool removeEmptyDirectory = false)
    {
        try
        {
            if (!IsOrdinaryDirectory(directory)) return;
            var removed = false;
            foreach (var file in Directory.EnumerateFiles(directory))
            {
                if (!ownsName(Path.GetFileName(file))) continue;
                try
                {
                    if ((File.GetAttributes(file) & FileAttributes.ReparsePoint) == 0
                        && (cutoff is null || File.GetLastWriteTimeUtc(file) < cutoff.Value))
                    {
                        File.Delete(file);
                        removed = true;
                    }
                }
                catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
                {
                    // An unavailable file must not prevent cleanup of other eligible copies.
                }
            }
            // Do not remove an empty directory where a new export may still be starting its write.
            if (removeEmptyDirectory && removed && !Directory.EnumerateFileSystemEntries(directory).Any())
                Directory.Delete(directory);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // Retain this directory when the cache cannot be safely enumerated.
        }
    }

    // MAUI 10.0.120 stages copies of private files under this internal or legacy external cache directory.
    private const string BackupShareDirectoryName = "2203693cc04e0be7f4f024d5f9499e13";

    /// <summary>Removes only staged portable-backup copies owned by the profile whose reset was accepted.</summary>
    internal static void ClearBackupShares(string cacheRoot, string profileId)
    {
        if (profileId.Length != 0 && !Guid.TryParseExact(profileId, "N", out _)) return;
        var expectedSet = profileId.Length == 0 ? ZananceApp.AppId : $"{ZananceApp.AppId}~{profileId}";
        VisitBackupShareDirectories(cacheRoot, child => ClearShares(child, null,
            name => BackupFileName.TryParse(name, out var set, out _) && set == expectedSet));
    }

    /// <summary>Expires old Android staging copies without touching saved backups or newly offered shares.</summary>
    internal static void ClearExpiredBackupShares(string cacheRoot, DateTimeOffset now)
    {
        var cutoff = now.UtcDateTime.AddHours(-24);
        VisitBackupShareDirectories(cacheRoot, child =>
        {
            // A fresh staging directory can contain an older file copied with its original timestamp.
            if (Directory.GetLastWriteTimeUtc(child) < cutoff)
                ClearShares(child, cutoff, IsZananceBackupName, removeEmptyDirectory: true);
        });
    }

    private static bool IsZananceBackupName(string name)
    {
        if (!BackupFileName.TryParse(name, out var set, out _)) return false;
        var prefix = ZananceApp.AppId + "~";
        return set == ZananceApp.AppId
            || (set.StartsWith(prefix, StringComparison.Ordinal) && Guid.TryParseExact(set[prefix.Length..], "N", out _));
    }

    private static void VisitBackupShareDirectories(string cacheRoot, Action<string> visit)
    {
        var staging = Path.Combine(cacheRoot, BackupShareDirectoryName);
        try
        {
            if (!IsOrdinaryDirectory(staging)) return;
            foreach (var child in Directory.EnumerateDirectories(staging))
            {
                try
                {
                    // The SDK creates an opaque child directory for each offered share.
                    if (Guid.TryParseExact(Path.GetFileName(child), "N", out _) && IsOrdinaryDirectory(child))
                        visit(child);
                }
                catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
                {
                    // One unavailable share must not prevent retirement of other owned copies.
                }
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // This cache may be inaccessible even after the accepted profile deletion.
        }
    }

    private static bool IsOrdinaryDirectory(string path) =>
        Directory.Exists(path) && (File.GetAttributes(path) & FileAttributes.ReparsePoint) == 0;

    // These original export patterns are local temporary copies, never the user's chosen destination.
    private static readonly string[] SharedFilePatterns = ["zanance-export-*.csv", "zanance-holdings-*.csv", "zanance-report-*.pdf"];

    /// <summary>Removes temporary outputs under the original platform-provided application cache root.</summary>
    internal static void Clear(string cacheRoot)
    {
        try
        {
            var attachments = Path.Combine(cacheRoot, "attachments");
            if (Directory.Exists(attachments)) Directory.Delete(attachments, recursive: true);
            foreach (var file in SharedFilePatterns.SelectMany(pattern => Directory.EnumerateFiles(cacheRoot, pattern)))
                File.Delete(file);

            var reports = Path.Combine(cacheRoot, ReportDirectoryName);
            if (Directory.Exists(reports) && (File.GetAttributes(reports) & FileAttributes.ReparsePoint) == 0)
            {
                foreach (var file in Directory.EnumerateFiles(reports, "zanance-report-*.pdf"))
                    if ((File.GetAttributes(file) & FileAttributes.ReparsePoint) == 0)
                        File.Delete(file);
            }

            // Android can terminate the owner while the external camera is writing. Its finally block
            // cannot run then (D-596); a new startup retires only that adapter's original opaque JPEGs.
            var camera = Path.Combine(cacheRoot, CameraDirectoryName);
            if (Directory.Exists(camera) && (File.GetAttributes(camera) & FileAttributes.ReparsePoint) == 0)
            {
                foreach (var file in Directory.EnumerateFiles(camera, "*", SearchOption.TopDirectoryOnly))
                {
                    if (Path.GetExtension(file).Equals(".jpg", StringComparison.OrdinalIgnoreCase)
                        && Guid.TryParseExact(Path.GetFileNameWithoutExtension(file), "N", out _)
                        && (File.GetAttributes(file) & FileAttributes.ReparsePoint) == 0)
                        File.Delete(file);
                }
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // A temporary file still open in another app remains retryable on a later startup.
        }
    }
}
