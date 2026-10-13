using Microsoft.Data.Sqlite;
using System.Security.Cryptography;

namespace Vafadar.Data.Tests;

/// <summary>Actual input/ownership/lease guards; native encrypted import and migration use the Android fixture.</summary>
public sealed class SqlCipherRestoreStagingTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "vafadar-restore-stage-" + Guid.NewGuid().ToString("N"));
    private readonly DatabaseKeyBinding binding = new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
    private readonly Keys keys = new();
    private readonly Durability durability = new();
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private string Candidate => Path.Combine(root, "candidate.db");
    /// <summary>Creates only an independently owned fixture directory.</summary>
    public SqlCipherRestoreStagingTests() => Directory.CreateDirectory(root);
    private Task Prepare(Stream input, Action? access = null, int maximum = 1024 * 1024, CancellationToken? ct = null) =>
        SqlCipherRestoreStaging.PrepareAsync(input, Candidate, binding, keys, durability, (_, _) => Task.CompletedTask,
            access ?? (() => { }), maximum, "4.19.0", ct ?? Ct);

    [Fact, Trait("AT", "AT-198")]
    public async Task Existing_candidate_is_not_overwritten_or_input_consumed()
    {
        File.WriteAllBytes(Candidate, [7]); using var input = new MemoryStream([1, 2, 3]);
        await Assert.ThrowsAsync<IOException>(() => Prepare(input));
        Assert.Equal(0, input.Position); Assert.Equal(new byte[] { 7 }, File.ReadAllBytes(Candidate)); Assert.Equal(0, keys.Prepares);
    }
    [Fact]
    public async Task Existing_sidecar_stops_before_input_or_key_preparation()
    {
        File.WriteAllBytes(Candidate + "-wal", [7]); using var input = new MemoryStream([1, 2, 3]);
        await Assert.ThrowsAsync<IOException>(() => Prepare(input)); Assert.Equal(0, input.Position); Assert.Equal(0, keys.Prepares);
    }
    [Fact]
    public async Task Invalid_portable_image_does_not_prepare_keys_or_create_output()
    {
        using var input = new MemoryStream([1, 2, 3]);
        await Assert.ThrowsAsync<InvalidDataException>(() => Prepare(input)); Assert.Equal(0, keys.Prepares); Assert.False(File.Exists(Candidate));
    }
    [Fact]
    public async Task Bounded_input_refuses_before_platform_key_effects()
    {
        using var input = new MemoryStream(new byte[4096]);
        await Assert.ThrowsAsync<InvalidDataException>(() => Prepare(input, maximum: 100)); Assert.Equal(0, keys.Prepares);
    }
    [Fact]
    public async Task Retired_access_is_checked_before_portable_input_is_read()
    {
        using var input = new MemoryStream([1, 2, 3]);
        await Assert.ThrowsAsync<InvalidOperationException>(() => Prepare(input, () => throw new InvalidOperationException("Retired fictitious lease.")));
        Assert.Equal(0, input.Position); Assert.Equal(0, keys.Prepares);
    }
    [Fact]
    public async Task Cancelled_restore_does_not_consume_input_or_prepare_keys()
    {
        using var input = new MemoryStream([1, 2, 3]); using var cancelled = new CancellationTokenSource(); cancelled.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Prepare(input, ct: cancelled.Token)); Assert.Equal(0, input.Position);
    }
    /// <summary>Removes only this fixture's own files.</summary>
    public void Dispose() => Directory.Delete(root, recursive: true);
    private sealed class Keys : IDatabaseGenerationKeyStore
    {
        internal int Prepares;
        public Task PrepareAsync(DatabaseKeyBinding context, CancellationToken ct) { Prepares++; return Task.CompletedTask; }
        public Task<byte[]> ReadAsync(DatabaseKeyBinding context, CancellationToken ct) => Task.FromResult(RandomNumberGenerator.GetBytes(32));
    }
    private sealed class Durability : IDatabaseGenerationFileDurability
    {
        public long AvailableBytes => 1024L * 1024 * 1024;
        public Task SynchronizeFileAsync(string path, CancellationToken ct) => Task.CompletedTask;
        public Task SynchronizeDirectoryAsync(CancellationToken ct) => Task.CompletedTask;
    }
}
