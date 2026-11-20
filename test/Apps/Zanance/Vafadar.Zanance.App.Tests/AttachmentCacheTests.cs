using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Core.Ledger;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Actual cache cleanup against disposable original-format filesystem outputs, without native or financial effects.</summary>
[Trait("AT", "AT-171")]
public sealed class AttachmentCacheTests
{
    [Fact]
    public async Task A_viewer_that_refuses_the_file_leaves_no_unneeded_private_copy()
    {
        using var directory = new OwnedDirectory();
        var attachment = new EntryAttachment { FileName = "Synthetic.pdf", ContentType = "application/pdf", Data = [1, 2, 3] };

        var accepted = await AttachmentFiles.StageForViewerAsync(directory.Root, attachment, async path =>
        {
            Assert.Equal(attachment.Data, await File.ReadAllBytesAsync(path, TestContext.Current.CancellationToken));
            return false;
        }, mayOpen: null);

        Assert.False(accepted);
        Assert.Empty(Directory.EnumerateFileSystemEntries(directory.Root));
        Assert.Equal(new byte[] { 1, 2, 3 }, attachment.Data);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task A_viewer_that_has_or_may_have_accepted_keeps_its_verified_copy(bool interrupted)
    {
        using var directory = new OwnedDirectory();
        var attachment = new EntryAttachment { FileName = "Synthetic.pdf", ContentType = "application/pdf", Data = [7, 4] };
        string? retained = null;
        var approved = false;
        var work = AttachmentFiles.StageForViewerAsync(directory.Root, attachment, async path =>
        {
            Assert.True(approved);
            Assert.Equal(attachment.Data, await File.ReadAllBytesAsync(path, TestContext.Current.CancellationToken));
            retained = path;
            if (interrupted) throw new IOException("The native handoff did not report an outcome.");
            return true;
        }, () => Task.FromResult(approved = true));

        if (interrupted) await Assert.ThrowsAsync<IOException>(() => work);
        else Assert.True(await work);
        Assert.Equal(retained, Assert.Single(Directory.EnumerateFiles(directory.Root)));
        Assert.Equal(new byte[] { 7, 4 }, attachment.Data);
    }

    /// <summary>A new Android process removes only its renderer's abandoned cache files.</summary>
    [Fact]
    public void Interrupted_pdf_cleanup_preserves_other_files_and_nested_directories()
    {
        using var directory = new OwnedDirectory();
        const string id = "3ea174e30e5b490d9bae5e809f426cc2";
        var abandoned = Path.Combine(directory.Root, $"render-{id}.pdf");
        var partial = Path.Combine(directory.Root, $"render-{Guid.NewGuid():N}.pdf");
        var nested = Directory.CreateDirectory(Path.Combine(directory.Root, "nested")).FullName;
        string[] untouched = [Path.Combine(directory.Root, "render-4BA174E30E5B490D9BAE5E809F426CC2.pdf"),
            Path.Combine(directory.Root, "render-owner.pdf"), Path.Combine(directory.Root, $"render-{id}.bak"),
            Path.Combine(nested, $"render-{id}.pdf")];
        File.WriteAllBytes(abandoned, [1, 2]);
        File.WriteAllBytes(partial, []);
        foreach (var file in untouched) File.WriteAllBytes(file, [7]);

        AttachmentCache.ClearInterruptedPdfRenders(directory.Root);

        Assert.False(File.Exists(abandoned));
        Assert.False(File.Exists(partial));
        foreach (var file in untouched) Assert.Equal(new byte[] { 7 }, File.ReadAllBytes(file));
    }

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

    /// <summary>Only report outputs in the new provider directory are cleaned up.</summary>
    [Fact]
    public void Cleanup_retires_report_exports_without_touching_unrelated_files()
    {
        using var directory = new OwnedDirectory();
        var reports = Directory.CreateDirectory(Path.Combine(directory.Root, AttachmentCache.ReportDirectoryName)).FullName;
        var report = Path.Combine(reports, "zanance-report-test.pdf");
        var unrelated = Path.Combine(reports, "unrelated.pdf");
        File.WriteAllBytes(report, [1]);
        File.WriteAllBytes(unrelated, [2]);

        AttachmentCache.Clear(directory.Root);

        Assert.False(File.Exists(report));
        Assert.Equal(new byte[] { 2 }, File.ReadAllBytes(unrelated));
    }

    /// <summary>Profile reset removes only that profile's staged portable backups.</summary>
    [Theory]
    [InlineData("")]
    [InlineData("176785fa415a4e0586e0911be87a13f2")]
    public void Backup_share_cleanup_owns_only_the_accepted_profile(string profileId)
    {
        using var directory = new OwnedDirectory();
        const string stagingName = "2203693cc04e0be7f4f024d5f9499e13";
        const string mainBackup = "pro.vafadar.zanance_20261120T121314Z.vbak";
        const string secondBackup = "pro.vafadar.zanance~176785fa415a4e0586e0911be87a13f2_20261120T121314Z.vbak";
        var staging = Path.Combine(directory.Root, stagingName);
        var main = Directory.CreateDirectory(Path.Combine(staging, Guid.NewGuid().ToString("N"))).FullName;
        var second = Directory.CreateDirectory(Path.Combine(staging, Guid.NewGuid().ToString("N"))).FullName;
        var unrelated = Path.Combine(second, "user-selected.vbak");
        var outside = Path.Combine(directory.Root, mainBackup);
        var mainPath = Path.Combine(main, mainBackup);
        var secondPath = Path.Combine(second, secondBackup);
        var sameNameOutsideOpaqueFolder = Path.Combine(staging, mainBackup);
        foreach (var path in new[] { mainPath, secondPath, unrelated, outside, sameNameOutsideOpaqueFolder })
            File.WriteAllBytes(path, [9]);

        AttachmentCache.ClearBackupShares(directory.Root, profileId);

        Assert.Equal(profileId.Length != 0, File.Exists(mainPath));
        Assert.Equal(profileId.Length == 0, File.Exists(secondPath));
        Assert.True(Directory.Exists(main));
        Assert.Equal(new byte[] { 9 }, File.ReadAllBytes(unrelated));
        Assert.Equal(new byte[] { 9 }, File.ReadAllBytes(outside));
        Assert.Equal(new byte[] { 9 }, File.ReadAllBytes(sameNameOutsideOpaqueFolder));
    }

    /// <summary>Startup pruning is limited to expired outputs in the two original staging layouts.</summary>
    [Fact]
    public void Expired_data_shares_are_pruned_without_removing_recent_or_foreign_files()
    {
        using var directory = new OwnedDirectory();
        var now = new DateTimeOffset(2026, 11, 20, 12, 0, 0, TimeSpan.Zero);
        var csv = Directory.CreateDirectory(Path.Combine(directory.Root, "csv-export-v1")).FullName;
        var zip = Directory.CreateDirectory(Path.Combine(directory.Root, "accountant-export-v1")).FullName;
        var expiredCsvDirectory = Directory.CreateDirectory(Path.Combine(csv, Guid.NewGuid().ToString("N"))).FullName;
        var freshCsvDirectory = Directory.CreateDirectory(Path.Combine(csv, Guid.NewGuid().ToString("N"))).FullName;
        var foreignDirectory = Directory.CreateDirectory(Path.Combine(csv, "outside-sharing-layout")).FullName;
        var expiredCsv = Write(expiredCsvDirectory, "zanance-export-older.csv", -25);
        var expiredZip = Write(zip, $"zanance-accountant-{Guid.NewGuid():N}.zip", -25);
        var freshCsv = Write(freshCsvDirectory, "zanance-export-recent.csv", -1);
        var exactCutoffZip = Write(zip, $"zanance-accountant-{Guid.NewGuid():N}.zip", -24);
        var foreignZip = Write(zip, "zanance-accountant-unrelated.zip", -25);
        var foreignCsv = Write(foreignDirectory, "zanance-export-unrelated.csv", -25);
        var unrelatedName = Write(freshCsvDirectory, "other.csv", -25);

        AttachmentCache.ClearExpiredDataShares(directory.Root, now);

        Assert.False(File.Exists(expiredCsv));
        Assert.False(Directory.Exists(expiredCsvDirectory));
        Assert.False(File.Exists(expiredZip));
        foreach (var retained in new[] { freshCsv, exactCutoffZip, foreignZip, foreignCsv, unrelatedName })
            Assert.Equal(new byte[] { 7 }, File.ReadAllBytes(retained));

        string Write(string folder, string name, int offsetHours)
        {
            var path = Path.Combine(folder, name);
            File.WriteAllBytes(path, [7]);
            File.SetLastWriteTimeUtc(path, now.UtcDateTime.AddHours(offsetHours));
            return path;
        }
    }

    /// <summary>Both the backup copy and its staging directory must age past the retention cutoff.</summary>
    [Fact]
    public void Expired_backup_shares_keep_recent_staging_and_original_backups()
    {
        using var directory = new OwnedDirectory();
        var now = new DateTimeOffset(2026, 11, 20, 12, 0, 0, TimeSpan.Zero);
        var staging = Directory.CreateDirectory(Path.Combine(directory.Root, "2203693cc04e0be7f4f024d5f9499e13")).FullName;
        const string main = "pro.vafadar.zanance_20261117T121314Z.vbak";
        const string secondary = "pro.vafadar.zanance~176785fa415a4e0586e0911be87a13f2_20261117T121314Z.vbak";
        var expiredMain = Write(staging, main, -25, -25);
        var expiredSecondary = Write(staging, secondary, -25, -25);
        var newlySharedOldBackup = Write(staging, main, -1, -25);
        var exactCutoff = Write(staging, main, -25, -24);
        var foreign = Write(staging, "pro.vafadar.other_20261117T121314Z.vbak", -25, -25);
        var invalidProfile = Write(staging, "pro.vafadar.zanance~invalid_20261117T121314Z.vbak", -25, -25);
        var original = Write(directory.Root, main, -25, -25, "backups");
        var outsideStaging = Write(staging, main, -25, -25, "not-an-opaque-share");

        AttachmentCache.ClearExpiredBackupShares(directory.Root, now);

        Assert.False(Directory.Exists(Path.GetDirectoryName(expiredMain)));
        Assert.False(Directory.Exists(Path.GetDirectoryName(expiredSecondary)));
        foreach (var retained in new[] { newlySharedOldBackup, exactCutoff, foreign, invalidProfile, original, outsideStaging })
            Assert.Equal(new byte[] { 7 }, File.ReadAllBytes(retained));

        string Write(string root, string name, int folderAge, int fileAge, string? folder = null)
        {
            var child = Directory.CreateDirectory(Path.Combine(root, folder ?? Guid.NewGuid().ToString("N"))).FullName;
            var path = Path.Combine(child, name);
            File.WriteAllBytes(path, [7]);
            File.SetLastWriteTimeUtc(path, now.UtcDateTime.AddHours(fileAge));
            Directory.SetLastWriteTimeUtc(child, now.UtcDateTime.AddHours(folderAge));
            return path;
        }
    }

    private sealed class OwnedDirectory : IDisposable
    {
        internal string Root { get; } = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "Zanance-CameraCache-" + Guid.NewGuid().ToString("N"))).FullName;
        public void Dispose() => Directory.Delete(Root, recursive: true);
    }
}
