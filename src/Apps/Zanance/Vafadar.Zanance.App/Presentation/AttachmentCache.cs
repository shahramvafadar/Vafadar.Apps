namespace Vafadar.Zanance.App.Presentation;

/// <summary>Retires only original application cache outputs on startup and after explicit data deletion.</summary>
internal static class AttachmentCache
{
    /// <summary>Gets the private FileProvider directory used for transient camera originals.</summary>
    internal const string CameraDirectoryName = "camera";

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
