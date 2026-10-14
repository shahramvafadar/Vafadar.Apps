using System.Runtime.Versioning;

namespace Vafadar.Data;

/// <summary>Creates explicitly selected Windows staging roots without replacing existing names or inheriting public access.</summary>
[SupportedOSPlatform("windows")]
public static class WindowsPrivateStorage
{
    /// <summary>Creates one direct child under an existing verified local parent and synchronizes both native directories.</summary>
    public static void CreateNewDirectory(string parent, string path)
    {
        using var directory = new WindowsPrivateFiles(parent);
        directory.CreateNewDirectory(path);
    }
}
