using Vafadar.Testing;

namespace Vafadar.Data.Tests;

/// <summary>Checks immutable bounded retirement inventory and actual Windows file-object deletion.</summary>
public sealed class ProfileRetirementArtifactTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private static ProfileRetirementCheckpoint Request() => new(Guid.NewGuid().ToString("N"), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

    [Fact, Trait("AT", "AT-233")]
    public async Task Captured_inventory_reopens_exactly_and_cannot_include_later_files()
    {
        using var folder = new TemporaryDirectory(); var request = Request(); var effects = new Effects();
        var journal = new SqliteProfileRetirementJournal(folder.Combine("retirement.sqlite"), request, () => { });
        await journal.CreateNewAsync(effects, Ct);
        await Assert.ThrowsAsync<IOException>(() => journal.ReadArtifactsAsync(Ct));
        var artifact = new ProfileRetirementArtifact("database", "owned.db", "1234", 5, new string('a', 64));
        await journal.CaptureArtifactsAsync([artifact], effects, Ct);
        await journal.CaptureArtifactsAsync([artifact], effects, Ct);
        Assert.Equal(artifact, Assert.Single(await journal.ReadArtifactsAsync(Ct)));
        await Assert.ThrowsAsync<IOException>(() => journal.CaptureArtifactsAsync([], effects, Ct));
        await journal.CompareAndAdvanceAsync(request, effects, Ct);
        await Assert.ThrowsAsync<InvalidOperationException>(() => journal.CaptureArtifactsAsync([artifact], effects, Ct));
        Assert.Equal(artifact, Assert.Single(await journal.ReadArtifactsAsync(Ct)));
    }

    [Theory, InlineData("../owned.db"), InlineData("sub/owned.db"), InlineData("owned:*"), InlineData(".."), Trait("AT", "AT-233")]
    public void Paths_and_wildcards_are_never_native_artifact_names(string name) =>
        Assert.Throws<ArgumentException>(() => new ProfileRetirementArtifact("database", name, "1", 0, new string('a', 64)));

    [Fact, Trait("AT", "AT-233")]
    public async Task Actual_windows_deletion_removes_only_captured_key_envelope_and_retains_foreign_file()
    {
        if (!OperatingSystem.IsWindows()) return;
        using var folder = new TemporaryDirectory(); var request = Request();
        var keys = new WindowsGenerationKeyStore(folder.Path, () => { });
        var binding = new DatabaseKeyBinding(request.ProfileId, request.InstallationId, Guid.NewGuid());
        await keys.PrepareAsync(binding, Ct);
        var name = $"{binding.ProfileId:N}-{binding.GenerationId:N}.key";
        await File.WriteAllTextAsync(folder.Combine("foreign.txt"), "Retain this file.", Ct);
        var native = new WindowsGenerationFileDurability(folder.Path);
        var files = new WindowsProfileRetirementFiles(new Dictionary<string, string> { ["database"] = folder.Path }, () => { });
        var journal = new SqliteProfileRetirementJournal(folder.Combine("retirement.sqlite"), request, () => { });
        await journal.CreateNewAsync(native, Ct);
        await journal.CaptureArtifactsAsync([await files.CaptureAsync("database", name, Ct)], native, Ct);
        await journal.CompareAndAdvanceAsync(request, native, Ct);
        await files.RemoveAsync(request.Advance(), journal, native, Ct);
        await files.RemoveAsync(request.Advance(), journal, native, Ct);
        await Assert.ThrowsAsync<IOException>(() => { if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException(); return keys.ReadAsync(binding, Ct); });
        Assert.Equal("Retain this file.", await File.ReadAllTextAsync(folder.Combine("foreign.txt"), Ct));
        Assert.True(File.Exists(folder.Combine("retirement.sqlite")));
    }

    [Theory, InlineData(false), InlineData(true), Trait("AT", "AT-233")]
    public async Task Changed_content_or_same_content_replacement_is_preserved_before_any_cleanup(bool replacement)
    {
        if (!OperatingSystem.IsWindows()) return;
        using var folder = new TemporaryDirectory(); var request = Request(); var native = new WindowsGenerationFileDurability(folder.Path);
        var files = new WindowsProfileRetirementFiles(new Dictionary<string, string> { ["database"] = folder.Path }, () => { });
        var journal = new SqliteProfileRetirementJournal(folder.Combine("retirement.sqlite"), request, () => { });
        await File.WriteAllTextAsync(folder.Combine("a.bin"), "Original A", Ct);
        await File.WriteAllTextAsync(folder.Combine("b.bin"), "Original B", Ct);
        var first = await files.CaptureAsync("database", "a.bin", Ct); var second = await files.CaptureAsync("database", "b.bin", Ct);
        await journal.CreateNewAsync(native, Ct); await journal.CaptureArtifactsAsync([first, second], native, Ct);
        await journal.CompareAndAdvanceAsync(request, native, Ct);
        if (replacement) File.Move(folder.Combine("b.bin"), folder.Combine("retained-old.bin"));
        await File.WriteAllTextAsync(folder.Combine("b.bin"), replacement ? "Original B" : "Changed B", Ct);
        await Assert.ThrowsAsync<IOException>(() => { if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException(); return files.RemoveAsync(request.Advance(), journal, native, Ct); });
        Assert.Equal("Original A", await File.ReadAllTextAsync(folder.Combine("a.bin"), Ct));
        Assert.Equal(replacement ? "Original B" : "Changed B", await File.ReadAllTextAsync(folder.Combine("b.bin"), Ct));
    }

    [Fact, Trait("AT", "AT-233")]
    public async Task Native_cleanup_cannot_delete_its_own_journal_even_if_mistakenly_captured()
    {
        if (!OperatingSystem.IsWindows()) return;
        using var folder = new TemporaryDirectory(); var request = Request(); var native = new WindowsGenerationFileDurability(folder.Path);
        var files = new WindowsProfileRetirementFiles(new Dictionary<string, string> { ["database"] = folder.Path }, () => { });
        var journal = new SqliteProfileRetirementJournal(folder.Combine("retirement.sqlite"), request, () => { });
        await journal.CreateNewAsync(native, Ct);
        await journal.CaptureArtifactsAsync([await files.CaptureAsync("database", "retirement.sqlite", Ct)], native, Ct);
        await journal.CompareAndAdvanceAsync(request, native, Ct);
        await Assert.ThrowsAsync<IOException>(() => { if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException(); return files.RemoveAsync(request.Advance(), journal, native, Ct); });
        Assert.Equal(request.Advance(), await journal.ReadAsync(Ct));
    }

    [Fact, Trait("AT", "AT-234")]
    public async Task Empty_replacement_root_cannot_prove_that_files_in_the_original_root_were_removed()
    {
        if (!OperatingSystem.IsWindows()) return;
        using var folder = new TemporaryDirectory(); var objects = folder.Combine("objects");
        Directory.CreateDirectory(objects); var request = Request(); var native = new WindowsGenerationFileDurability(folder.Path);
        var files = new WindowsProfileRetirementFiles(new Dictionary<string, string> { ["database"] = objects }, () => { });
        var journal = new SqliteProfileRetirementJournal(folder.Combine("retirement.sqlite"), request, () => { });
        await File.WriteAllTextAsync(Path.Combine(objects, "owned.bin"), "Retain original bytes.", Ct);
        await journal.CreateNewAsync(native, Ct);
        await journal.CaptureArtifactsAsync([await files.CaptureAsync("database", "owned.bin", Ct)], native, Ct);
        await journal.CompareAndAdvanceAsync(request, native, Ct);
        Directory.Move(objects, objects + ".retained"); Directory.CreateDirectory(objects);
        await Assert.ThrowsAsync<IOException>(() => { if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException(); return files.RemoveAsync(request.Advance(), journal, native, Ct); });
        await Assert.ThrowsAsync<IOException>(() => { if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException(); return files.VerifyRemovedAsync(journal, Ct); });
        Assert.Equal("Retain original bytes.", await File.ReadAllTextAsync(Path.Combine(objects + ".retained", "owned.bin"), Ct));
        Assert.Equal(request.Advance(), await journal.ReadAsync(Ct));
    }

    private sealed class Effects : IDatabaseGenerationFileDurability
    {
        public long AvailableBytes => long.MaxValue;
        public Task SynchronizeFileAsync(string path, CancellationToken ct) => Task.CompletedTask;
        public Task SynchronizeDirectoryAsync(CancellationToken ct) => Task.CompletedTask;
    }
}
