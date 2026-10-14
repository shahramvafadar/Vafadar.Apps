using System.Runtime.Versioning;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Vafadar.Testing;

namespace Vafadar.Data.Tests;

/// <summary>Checks actual Windows DPAPI files and native synchronization on independently owned directories.</summary>
[SupportedOSPlatform("windows")]
public sealed class WindowsGenerationStorageTests
{
    public static bool IsWindows => OperatingSystem.IsWindows();
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private static DatabaseKeyBinding Binding() => new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
    private static string KeyPath(string root, DatabaseKeyBinding binding) => Path.Combine(root, $"{binding.ProfileId:N}-{binding.GenerationId:N}.key");

    [Fact(Skip = "Requires actual Windows native storage.", SkipUnless = nameof(IsWindows)), Trait("AT", "AT-226")]
    public async Task Local_application_data_namespace_binds_actual_owned_root_and_reopens_exact_key()
    {
        var parent = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Vafadar.NativeStorage.Tests");
        Directory.CreateDirectory(parent);
        var root = Path.Combine(parent, Guid.NewGuid().ToString("N"));
        WindowsPrivateStorage.CreateNewDirectory(parent, root);
        try
        {
            var binding = Binding(); var store = new WindowsGenerationKeyStore(root, () => { });
            await store.PrepareAsync(binding, Ct);
            var first = await store.ReadAsync(binding, Ct);
            try
            {
                var second = await new WindowsGenerationKeyStore(root, () => { }).ReadAsync(binding, Ct);
                try { Assert.Equal(first, second); }
                finally { CryptographicOperations.ZeroMemory(second); }
                var durability = new WindowsGenerationFileDurability(root);
                await durability.SynchronizeFileAsync(KeyPath(root, binding), Ct);
                await durability.SynchronizeDirectoryAsync(Ct);
            }
            finally { CryptographicOperations.ZeroMemory(first); }
        }
        finally
        {
            // Delete only the exact independently created GUID fixture, never its shared parent or app storage.
            if (Path.GetDirectoryName(root) != parent || !Guid.TryParseExact(Path.GetFileName(root), "N", out _))
                throw new IOException("Fixture cleanup root differs.");
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact(Skip = "Requires actual Windows native storage.", SkipUnless = nameof(IsWindows)), Trait("AT", "AT-226")]
    public async Task Durability_refuses_replaced_directory_even_when_its_logical_path_is_unchanged()
    {
        using var folder = new TemporaryDirectory();
        var root = folder.Combine("captured"); Directory.CreateDirectory(root);
        var durability = new WindowsGenerationFileDurability(root);
        Directory.Move(root, folder.Combine("retained")); Directory.CreateDirectory(root);
        Assert.Throws<IOException>(() => durability.AvailableBytes);
        await Assert.ThrowsAsync<IOException>(() => durability.SynchronizeDirectoryAsync(Ct));
        Assert.True(Directory.Exists(folder.Combine("retained")));
    }

    [Fact(Skip = "Requires actual Windows native storage.", SkipUnless = nameof(IsWindows)), Trait("AT", "AT-226")]
    public async Task Prepared_envelope_reopens_and_repeat_preparation_preserves_exact_existing_file()
    {
        using var folder = new TemporaryDirectory(); var binding = Binding();
        var store = new WindowsGenerationKeyStore(folder.Path, () => { });
        await store.PrepareAsync(binding, Ct);
        var before = await File.ReadAllBytesAsync(KeyPath(folder.Path, binding), Ct);
        var first = await store.ReadAsync(binding, Ct);
        try
        {
            await store.PrepareAsync(binding, Ct);
            var second = await new WindowsGenerationKeyStore(folder.Path, () => { }).ReadAsync(binding, Ct);
            try { Assert.Equal(32, first.Length); Assert.Equal(first, second); }
            finally { CryptographicOperations.ZeroMemory(second); }
            Assert.Equal(before, await File.ReadAllBytesAsync(KeyPath(folder.Path, binding), Ct));
        }
        finally { CryptographicOperations.ZeroMemory(first); }
    }

    [Fact(Skip = "Requires actual Windows native storage.", SkipUnless = nameof(IsWindows))]
    public async Task Missing_read_and_foreign_installation_never_prepare_replacement_envelopes()
    {
        using var folder = new TemporaryDirectory(); var binding = Binding(); var store = new WindowsGenerationKeyStore(folder.Path, () => { });
        await Assert.ThrowsAsync<IOException>(() => store.ReadAsync(binding, Ct));
        Assert.Empty(Directory.EnumerateFiles(folder.Path));
        await store.PrepareAsync(binding, Ct);
        var path = KeyPath(folder.Path, binding); var before = await File.ReadAllBytesAsync(path, Ct);
        var foreign = new DatabaseKeyBinding(binding.ProfileId, Guid.NewGuid(), binding.GenerationId);
        await Assert.ThrowsAsync<CryptographicException>(() => store.PrepareAsync(foreign, Ct));
        Assert.Equal(before, await File.ReadAllBytesAsync(path, Ct));
    }

    [Theory(Skip = "Requires actual Windows native storage.", SkipUnless = nameof(IsWindows))]
    [InlineData(0)] [InlineData(1)] [InlineData(4097)]
    public async Task Damaged_or_oversized_existing_envelope_is_preserved_and_refused(int length)
    {
        using var folder = new TemporaryDirectory(); var binding = Binding(); var path = KeyPath(folder.Path, binding);
        var content = new byte[length]; await File.WriteAllBytesAsync(path, content, Ct);
        var store = new WindowsGenerationKeyStore(folder.Path, () => { });
        await Assert.ThrowsAsync<CryptographicException>(() => store.PrepareAsync(binding, Ct));
        Assert.Equal(content, await File.ReadAllBytesAsync(path, Ct));
    }

    [Fact(Skip = "Requires actual Windows native storage.", SkipUnless = nameof(IsWindows))]
    public async Task Retired_access_does_not_create_key_files()
    {
        using var folder = new TemporaryDirectory();
        var store = new WindowsGenerationKeyStore(folder.Path, () => throw new InvalidOperationException("Retired fixture."));
        await Assert.ThrowsAsync<InvalidOperationException>(() => store.PrepareAsync(Binding(), Ct));
        Assert.Empty(Directory.EnumerateFiles(folder.Path));
    }

    [Fact(Skip = "Requires actual Windows native storage.", SkipUnless = nameof(IsWindows))]
    public async Task Native_flush_succeeds_only_for_captured_regular_files_and_directory()
    {
        using var folder = new TemporaryDirectory(); using var foreign = new TemporaryDirectory();
        var file = folder.Combine("fictitious.bin"); await File.WriteAllBytesAsync(file, [1, 2, 3], Ct);
        var effects = new WindowsGenerationFileDurability(folder.Path);
        Assert.True(effects.AvailableBytes > 0);
        await effects.SynchronizeFileAsync(file, Ct); await effects.SynchronizeDirectoryAsync(Ct);
        var elsewhere = foreign.Combine("foreign.bin"); await File.WriteAllBytesAsync(elsewhere, [4], Ct);
        await Assert.ThrowsAsync<IOException>(() => effects.SynchronizeFileAsync(elsewhere, Ct));
        await Assert.ThrowsAsync<IOException>(() => effects.SynchronizeFileAsync(folder.Path, Ct));
        Assert.Equal(new byte[] { 4 }, await File.ReadAllBytesAsync(elsewhere, Ct));
    }

    [Fact(Skip = "Requires actual Windows native storage.", SkipUnless = nameof(IsWindows))]
    public void Relative_or_network_directories_are_not_private_storage_authority()
    {
        Assert.Throws<ArgumentException>(() => new WindowsGenerationKeyStore("relative", () => { }));
        Assert.Throws<ArgumentException>(() => new WindowsGenerationFileDurability("relative"));
        Assert.Throws<IOException>(() => new WindowsGenerationFileDurability(@"\\server\share\private"));
    }

    [Fact(Skip = "Requires actual Windows native storage.", SkipUnless = nameof(IsWindows))]
    public async Task Hard_linked_key_is_refused_without_replacement_or_changes_to_the_original()
    {
        using var folder = new TemporaryDirectory(); var binding = Binding();
        var store = new WindowsGenerationKeyStore(folder.Path, () => { }); await store.PrepareAsync(binding, Ct);
        var path = KeyPath(folder.Path, binding); var before = await File.ReadAllBytesAsync(path, Ct);
        Assert.True(CreateHardLinkW(folder.Combine("fictitious-peer.key"), path, IntPtr.Zero));
        await Assert.ThrowsAsync<IOException>(() => store.ReadAsync(binding, Ct));
        await Assert.ThrowsAsync<IOException>(() => store.PrepareAsync(binding, Ct));
        await Assert.ThrowsAsync<IOException>(() => new WindowsGenerationFileDurability(folder.Path).SynchronizeFileAsync(path, Ct));
        Assert.Equal(before, await File.ReadAllBytesAsync(path, Ct));
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CreateHardLinkW(string name, string existing, IntPtr security);
}
