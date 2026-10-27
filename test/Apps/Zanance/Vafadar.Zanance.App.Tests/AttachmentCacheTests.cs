using Vafadar.Zanance.App.Presentation;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Actual cache cleanup against disposable original-format filesystem outputs, without native or financial effects.</summary>
[Trait("AT", "AT-171")]
public sealed class AttachmentCacheTests
{
    /// <summary>A terminated capture cannot run its finally block; the next startup removes its raw original.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Abandoned_original_camera_file_is_retired_at_the_next_cleanup(bool empty)
    {
        using var directory = new OwnedDirectory();
        var camera = Directory.CreateDirectory(Path.Combine(directory.Root, AttachmentCache.CameraDirectoryName)).FullName;
        var raw = Path.Combine(camera, $"{Guid.NewGuid():N}.jpg");
        File.WriteAllBytes(raw, empty ? [] : [0xFF, 0xD8, 0x45, 0x58, 0x49, 0x46, 0xFF, 0xD9]);
        AttachmentCache.Clear(directory.Root);
        Assert.False(File.Exists(raw));
    }

    /// <summary>Cleanup owns only the camera adapter's opaque JPEG names, not arbitrary or nested files.</summary>
    [Fact]
    public void Camera_cleanup_preserves_unrelated_names_nested_files_and_outside_data()
    {
        using var directory = new OwnedDirectory();
        var camera = Directory.CreateDirectory(Path.Combine(directory.Root, AttachmentCache.CameraDirectoryName)).FullName;
        var nested = Directory.CreateDirectory(Path.Combine(camera, "unrelated")).FullName;
        string[] paths = [Path.Combine(camera, "owner-photo.jpg"), Path.Combine(camera, $"{Guid.NewGuid():N}.pdf"),
            Path.Combine(nested, $"{Guid.NewGuid():N}.jpg"), Path.Combine(directory.Root, $"{Guid.NewGuid():N}.jpg")];
        foreach (var path in paths) File.WriteAllBytes(path, [7, 3, 9]);
        AttachmentCache.Clear(directory.Root);
        foreach (var path in paths) Assert.Equal(new byte[] { 7, 3, 9 }, File.ReadAllBytes(path));
    }

    /// <summary>Original viewer/export retirement and preservation of other root files remain unchanged.</summary>
    [Fact]
    public void Original_viewer_and_export_cleanup_keeps_other_cache_files()
    {
        using var directory = new OwnedDirectory();
        var viewer = Directory.CreateDirectory(Path.Combine(directory.Root, "attachments")).FullName;
        File.WriteAllBytes(Path.Combine(viewer, "owned.pdf"), [1]);
        string[] outputs = ["zanance-export-owned.csv", "zanance-holdings-owned.csv", "zanance-report-owned.pdf"];
        foreach (var name in outputs) File.WriteAllBytes(Path.Combine(directory.Root, name), [2]);
        var other = Path.Combine(directory.Root, "unrelated.csv"); File.WriteAllBytes(other, [4, 6]);
        AttachmentCache.Clear(directory.Root);
        Assert.False(Directory.Exists(viewer));
        foreach (var name in outputs) Assert.False(File.Exists(Path.Combine(directory.Root, name)));
        Assert.Equal(new byte[] { 4, 6 }, File.ReadAllBytes(other));
        AttachmentCache.Clear(directory.Root);
        Assert.Equal(new byte[] { 4, 6 }, File.ReadAllBytes(other));
    }

    private sealed class OwnedDirectory : IDisposable
    {
        internal string Root { get; } = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "Zanance-CameraCache-" + Guid.NewGuid().ToString("N"))).FullName;
        public void Dispose() => Directory.Delete(Root, recursive: true);
    }
}
