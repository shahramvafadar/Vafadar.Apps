using System.Runtime.Versioning;
using System.Security.AccessControl;
using System.Security.Principal;
using Vafadar.Testing;

namespace Vafadar.Data.Tests;

/// <summary>Checks actual no-replace directory creation and inherited private access on owned Windows fixtures.</summary>
[SupportedOSPlatform("windows")]
public sealed class WindowsPrivateDirectoryTests
{
    /// <summary>Requires real Windows native directory and ACL effects.</summary>
    public static bool IsWindows => OperatingSystem.IsWindows();

    [Fact(Skip = "Requires actual Windows native storage.", SkipUnless = nameof(IsWindows)), Trait("AT", "AT-226")]
    public void New_staging_root_has_protected_owner_only_inheritable_access()
    {
        using var folder = new TemporaryDirectory(); var child = folder.Combine("staging");
        WindowsPrivateStorage.CreateNewDirectory(folder.Path, child);
        var security = new DirectoryInfo(child).GetAccessControl();
        Assert.True(security.AreAccessRulesProtected);
        using var identity = WindowsIdentity.GetCurrent();
        var rules = security.GetAccessRules(true, true, typeof(SecurityIdentifier)).Cast<FileSystemAccessRule>().ToArray();
        Assert.Single(rules); Assert.Equal(identity.User, rules[0].IdentityReference);
        Assert.Equal(AccessControlType.Allow, rules[0].AccessControlType);
        Assert.True(rules[0].InheritanceFlags.HasFlag(InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit));
    }

    [Fact(Skip = "Requires actual Windows native storage.", SkipUnless = nameof(IsWindows)), Trait("AT", "AT-226")]
    public void Existing_staging_name_is_refused_and_its_bytes_remain_intact()
    {
        using var folder = new TemporaryDirectory(); var child = folder.Combine("staging");
        Directory.CreateDirectory(child); var retained = Path.Combine(child, "retained.txt");
        File.WriteAllText(retained, "fictitious retained data");
        Assert.Throws<IOException>(() => WindowsPrivateStorage.CreateNewDirectory(folder.Path, child));
        Assert.Equal("fictitious retained data", File.ReadAllText(retained));
    }

    [Fact(Skip = "Requires actual Windows native storage.", SkipUnless = nameof(IsWindows)), Trait("AT", "AT-226")]
    public void Staging_creation_rejects_a_path_outside_the_captured_parent()
    {
        using var folder = new TemporaryDirectory(); using var foreign = new TemporaryDirectory();
        var child = foreign.Combine("staging");
        Assert.Throws<IOException>(() => WindowsPrivateStorage.CreateNewDirectory(folder.Path, child));
        Assert.False(Directory.Exists(child));
    }
}
