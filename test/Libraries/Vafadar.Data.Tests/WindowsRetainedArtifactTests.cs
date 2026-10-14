using System.Runtime.Versioning;
using Vafadar.Testing;

namespace Vafadar.Data.Tests;

/// <summary>Checks exact native retention removal and authority failures on independently owned Windows files.</summary>
[SupportedOSPlatform("windows")]
public sealed class WindowsRetainedArtifactTests
{
    /// <summary>Requires the real Windows handle and directory durability effects.</summary>
    public static bool IsWindows => OperatingSystem.IsWindows();
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact(Skip = "Requires actual Windows native storage.", SkipUnless = nameof(IsWindows)), Trait("AT", "AT-226")]
    public async Task Authenticated_retention_removes_only_captured_file_and_can_resume_absence()
    {
        using var folder = new TemporaryDirectory();
        var path = folder.Combine("owned.vbs");
        await File.WriteAllBytesAsync(path, [1, 2, 3], Ct);
        var neighbor = folder.Combine("retained.vbs");
        await File.WriteAllBytesAsync(neighbor, [4, 5], Ct);
        var files = new WindowsProfileRetirementFiles(new Dictionary<string, string> { ["safety"] = folder.Path }, () => { });
        var capture = await files.CaptureAsync("safety", "owned.vbs", Ct);
        var checks = 0;
        Task Authority(CancellationToken token) { token.ThrowIfCancellationRequested(); checks++; return Task.CompletedTask; }
        await files.RemoveRetainedArtifactAsync(capture, Authority, Ct);
        Assert.False(File.Exists(path)); Assert.Equal(2, checks);
        await files.RemoveRetainedArtifactAsync(capture, Authority, Ct);
        Assert.Equal(new byte[] { 4, 5 }, await File.ReadAllBytesAsync(neighbor, Ct));
    }

    [Fact(Skip = "Requires actual Windows native storage.", SkipUnless = nameof(IsWindows)), Trait("AT", "AT-226")]
    public async Task Changed_captured_bytes_refuse_retention_without_deleting_file()
    {
        using var folder = new TemporaryDirectory(); var path = folder.Combine("owned.vbs");
        await File.WriteAllBytesAsync(path, [1, 2, 3], Ct);
        var files = new WindowsProfileRetirementFiles(new Dictionary<string, string> { ["safety"] = folder.Path }, () => { });
        var capture = await files.CaptureAsync("safety", "owned.vbs", Ct);
        await File.WriteAllBytesAsync(path, [3, 2, 1], Ct);
        await Assert.ThrowsAsync<IOException>(() => files.RemoveRetainedArtifactAsync(capture, _ => Task.CompletedTask, Ct));
        Assert.Equal(new byte[] { 3, 2, 1 }, await File.ReadAllBytesAsync(path, Ct));
    }

    [Fact(Skip = "Requires actual Windows native storage.", SkipUnless = nameof(IsWindows)), Trait("AT", "AT-226")]
    public async Task Authority_failure_after_hashing_preserves_captured_file()
    {
        using var folder = new TemporaryDirectory(); var path = folder.Combine("owned.vbs");
        await File.WriteAllBytesAsync(path, [1, 2, 3], Ct);
        var files = new WindowsProfileRetirementFiles(new Dictionary<string, string> { ["safety"] = folder.Path }, () => { });
        var capture = await files.CaptureAsync("safety", "owned.vbs", Ct); var checks = 0;
        Task Authority(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            if (++checks == 2) throw new IOException("Retained fixture authority changed.");
            return Task.CompletedTask;
        }
        await Assert.ThrowsAsync<IOException>(() => files.RemoveRetainedArtifactAsync(capture, Authority, Ct));
        Assert.Equal(new byte[] { 1, 2, 3 }, await File.ReadAllBytesAsync(path, Ct));
    }
}
