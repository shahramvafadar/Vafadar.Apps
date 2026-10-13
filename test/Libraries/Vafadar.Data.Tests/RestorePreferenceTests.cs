using Vafadar.Core.Settings;
using Vafadar.Testing;

namespace Vafadar.Data.Tests;

/// <summary>Exercises allowlist capture and replay; native encrypted persistence is verified independently.</summary>
public sealed class RestorePreferenceTests : IDisposable
{
    private readonly TemporaryDirectory directory = new();
    public void Dispose() => directory.Dispose();

    [Fact, Trait("AT", "AT-201")]
    public void Capture_preserves_absent_keys_and_excludes_imported_device_security()
    {
        var settings = new InMemorySettingsStore(); settings.Set("language", "fa"); settings.Set("culture", "fa-IR");
        settings.Set("device.pin", "original-device-state");
        var packet = Capsule().Capture("""{"language":"en","device.pin":"foreign"}"""u8.ToArray(), settings);
        Assert.Equal("fa", settings.Get("language")); Assert.Equal("en", packet.Restored["language"]);
        Assert.Equal("fa-IR", packet.Restored["culture"]); Assert.False(packet.Restored.ContainsKey("device.pin"));
        Capsule().Apply(packet, settings, () => { }, TestContext.Current.CancellationToken);
        Assert.Equal("original-device-state", settings.Get("device.pin"));
    }

    [Fact, Trait("AT", "AT-201")]
    public void Missing_preference_source_preserves_all_choices_and_explicit_null_removes_only_allowed_key()
    {
        var settings = new InMemorySettingsStore(); settings.Set("language", "fa"); settings.Set("culture", "de-DE");
        var capsule = Capsule(); var absent = capsule.Capture(null, settings);
        Assert.Equal(absent.Original, absent.Restored);
        var packet = capsule.Capture("""{"language":null}"""u8.ToArray(), settings);
        capsule.Apply(packet, settings, () => { }, TestContext.Current.CancellationToken); Assert.Null(settings.Get("language")); Assert.Equal("de-DE", settings.Get("culture"));
    }

    [Fact, Trait("AT", "AT-201")]
    public void Malformed_or_oversized_preferences_are_rejected_without_writes()
    {
        var settings = new InMemorySettingsStore(); settings.Set("language", "fa");
        Assert.Throws<System.Text.Json.JsonException>(() => Capsule().Capture("bad"u8.ToArray(), settings));
        Assert.Throws<InvalidDataException>(() => Capsule().Capture(new byte[1024 * 1024 + 1], settings));
        Assert.Equal("fa", settings.Get("language"));
    }

    [Fact, Trait("AT", "AT-201")]
    public void Foreign_allowlist_packet_is_rejected_before_writing()
    {
        var settings = new InMemorySettingsStore();
        var packet = new RestorePreferencePacket(new() { ["device.pin"] = null }, new() { ["device.pin"] = "foreign" });
        Assert.Throws<InvalidDataException>(() => Capsule().Apply(packet, settings, () => { }, TestContext.Current.CancellationToken)); Assert.Null(settings.Get("device.pin"));
    }

    [Fact, Trait("AT", "AT-201")]
    public void Partial_preference_failure_is_replayed_from_same_owned_packet()
    {
        var store = new InterruptedStore(); var capsule = Capsule();
        var packet = capsule.Capture("""{"language":"en","culture":"de-DE"}"""u8.ToArray(), store);
        Assert.Throws<IOException>(() => capsule.Apply(packet, store, () => { }, TestContext.Current.CancellationToken));
        Assert.Equal("en", store.Get("language")); Assert.Null(store.Get("culture"));
        store.Fail = false; capsule.Apply(packet, store, () => { }, TestContext.Current.CancellationToken); Assert.Equal("de-DE", store.Get("culture"));
    }

    private SqlCipherRestorePreferences Capsule() => new(Path.Combine(directory.Path, "preferences.cipher.db"),
        new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()), new UnusedKeys(), new UnusedDurability(), "4.19.0", ["language", "culture"]);

    private sealed class InterruptedStore : ISettingsStore
    {
        private readonly InMemorySettingsStore inner = new();
        public bool Fail { get; set; } = true;
        public string? Get(string key) => inner.Get(key);
        public void Remove(string key) => inner.Remove(key);
        public void Set(string key, string? value) { if (Fail && key == "culture") throw new IOException("Fictitious write failure."); inner.Set(key, value); }
    }
    private sealed class UnusedKeys : IDatabaseGenerationKeyStore
    {
        public Task PrepareAsync(DatabaseKeyBinding binding, CancellationToken ct) => throw new InvalidOperationException("Unexpected key effect.");
        public Task<byte[]> ReadAsync(DatabaseKeyBinding binding, CancellationToken ct) => throw new InvalidOperationException("Unexpected key effect.");
    }
    private sealed class UnusedDurability : IDatabaseGenerationFileDurability
    {
        public long AvailableBytes => throw new InvalidOperationException("Unexpected disk effect.");
        public Task SynchronizeFileAsync(string path, CancellationToken ct) => throw new InvalidOperationException("Unexpected disk effect.");
        public Task SynchronizeDirectoryAsync(CancellationToken ct) => throw new InvalidOperationException("Unexpected disk effect.");
    }
}
