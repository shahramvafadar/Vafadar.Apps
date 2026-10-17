using System.Security.Cryptography;
using Vafadar.Backup.Security;
using Vafadar.Backup.Storage;
using Vafadar.Testing;

namespace Vafadar.Backup.Tests.Security;

/// <summary>Checks original opaque discovery boundaries without interpreting candidates as authenticated packages.</summary>
public sealed class DeviceSafetyCopyDiscoveryTests
{
    [Fact, Trait("AT", "AT-312")]
    public async Task Only_current_profile_installation_safety_cohort_is_listed_without_changing_any_file()
    {
        using var directory = new TemporaryDirectory(); var storage = new LocalFolderBackupStorage(directory.Path);
        var profile = Guid.NewGuid(); var installation = Guid.NewGuid();
        var own = await AddAsync(storage, profile, installation);
        await AddAsync(storage, Guid.NewGuid(), installation); await AddAsync(storage, profile, Guid.NewGuid());
        await AddAsync(storage, profile, installation, "automatic-fictional.vbs");
        var original = Directory.GetFiles(directory.Path).ToDictionary(path => Path.GetFileName(path)!, File.ReadAllBytes);
        var files = await DeviceSafetyCopyDiscovery.ListAsync(storage, profile, installation, () => { }, TestContext.Current.CancellationToken);
        Assert.Equal(own, Assert.Single(files));
        foreach (var pair in original) Assert.Equal(pair.Value, File.ReadAllBytes(Path.Combine(directory.Path, pair.Key!)));
    }

    [Theory, Trait("AT", "AT-312")]
    [InlineData("partial")]
    [InlineData("oversized")]
    [InlineData("wrong-magic")]
    [InlineData("empty-generation")]
    public async Task Invalid_or_unbounded_headers_are_not_presented(string kind)
    {
        using var directory = new TemporaryDirectory(); var storage = new LocalFolderBackupStorage(directory.Path);
        var profile = Guid.NewGuid(); var installation = Guid.NewGuid(); var file = await AddAsync(storage, profile, installation);
        var path = Path.Combine(directory.Path, file.FileName); var bytes = File.ReadAllBytes(path);
        if (kind == "partial") File.WriteAllBytes(path, bytes[..51]);
        else if (kind == "oversized") { using var stream = File.OpenWrite(path); stream.SetLength(DeviceSafetyEncryption.MaximumPackageBytes + 81L); }
        else { if (kind == "wrong-magic") bytes[0] ^= 1; else Array.Clear(bytes, 36, 16); File.WriteAllBytes(path, bytes); }
        Assert.Empty(await DeviceSafetyCopyDiscovery.ListAsync(storage, profile, installation, () => { }, TestContext.Current.CancellationToken));
    }

    [Fact, Trait("AT", "AT-312")]
    public async Task Forged_matching_header_is_only_a_candidate_and_full_authentication_still_refuses_it()
    {
        using var directory = new TemporaryDirectory(); var storage = new LocalFolderBackupStorage(directory.Path);
        var profile = Guid.NewGuid(); var installation = Guid.NewGuid(); var file = await AddAsync(storage, profile, installation);
        var path = Path.Combine(directory.Path, file.FileName); var bytes = File.ReadAllBytes(path); bytes[^1] ^= 1; File.WriteAllBytes(path, bytes);
        Assert.Single(await DeviceSafetyCopyDiscovery.ListAsync(storage, profile, installation, () => { }, TestContext.Current.CancellationToken));
        Assert.ThrowsAny<CryptographicException>(() => DeviceSafetyEncryption.Open(bytes, new byte[32]));
    }

    [Fact, Trait("AT", "AT-312")]
    public async Task Changed_captured_authority_stops_publication_without_modifying_retained_candidates()
    {
        using var directory = new TemporaryDirectory(); var storage = new LocalFolderBackupStorage(directory.Path);
        var profile = Guid.NewGuid(); var installation = Guid.NewGuid(); await AddAsync(storage, profile, installation);
        var checks = 0;
        await Assert.ThrowsAsync<IOException>(() => DeviceSafetyCopyDiscovery.ListAsync(storage, profile, installation,
            () => { if (++checks == 4) throw new IOException("Fictitious retired profile."); }, TestContext.Current.CancellationToken));
        Assert.Single(await storage.ListAsync(TestContext.Current.CancellationToken));
    }

    private static async Task<BackupFileInfo> AddAsync(IBackupStorage storage, Guid profile, Guid installation, string? name = null)
    {
        var identity = new byte[48]; profile.TryWriteBytes(identity.AsSpan(0, 16), bigEndian: true, out _);
        installation.TryWriteBytes(identity.AsSpan(16, 16), bigEndian: true, out _);
        Guid.NewGuid().TryWriteBytes(identity.AsSpan(32, 16), bigEndian: true, out _);
        using var content = new MemoryStream(DeviceSafetyEncryption.Protect("fictitious package"u8, new byte[32], identity));
        return await storage.UploadAsync(name ?? $"safety-{Guid.NewGuid():N}.vbs", content, TestContext.Current.CancellationToken);
    }
}
