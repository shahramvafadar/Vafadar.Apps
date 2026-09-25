namespace Vafadar.Testing;

/// <summary>Locates files in the repository from a running test.</summary>
public static class RepositoryPaths
{
    private const string SolutionFileName = "Vafadar.Apps.slnx";

    private static readonly Lazy<string> RootPath = new(FindRoot);

    /// <summary>Gets the repository root (the folder containing <c>Vafadar.Apps.slnx</c>).</summary>
    public static string Root => RootPath.Value;

    /// <summary>Combines the repository root with a relative path.</summary>
    public static string Combine(params string[] relativePath) => Path.Combine([Root, .. relativePath]);

    private static string FindRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, SolutionFileName)))
            {
                return directory.FullName;
            }
        }

        throw new InvalidOperationException($"Could not find {SolutionFileName} above {AppContext.BaseDirectory}.");
    }
}
