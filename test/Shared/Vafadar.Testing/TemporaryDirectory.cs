namespace Vafadar.Testing;

/// <summary>A uniquely named directory under the system temp folder, deleted on dispose.</summary>
public sealed class TemporaryDirectory : IDisposable
{
    /// <summary>Creates the directory.</summary>
    public TemporaryDirectory()
    {
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "vafadar-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path);
    }

    /// <summary>Gets the full path of the directory.</summary>
    public string Path { get; }

    /// <summary>Combines the directory path with a relative path.</summary>
    public string Combine(string relativePath) => System.IO.Path.Combine(Path, relativePath);

    /// <inheritdoc />
    public void Dispose()
    {
        // Files may still be locked for a moment on Windows (e.g. SQLite connections closing).
        for (var attempt = 0; attempt < 5; attempt++)
        {
            try
            {
                if (Directory.Exists(Path))
                {
                    Directory.Delete(Path, recursive: true);
                }

                return;
            }
            catch (IOException) when (attempt < 4)
            {
                Thread.Sleep(100);
            }
            catch (UnauthorizedAccessException) when (attempt < 4)
            {
                Thread.Sleep(100);
            }
        }
    }
}
