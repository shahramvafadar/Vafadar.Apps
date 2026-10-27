using System.Text.Json;
using Vafadar.Core.Settings;

namespace Vafadar.Data.Tests;

/// <summary>Exercises original package preference admission before any native recovery effects.</summary>
[Trait("AT", "AT-575")]
public sealed class OptionalRestorePreferenceSourceTests
{
    [Fact]
    public void Database_only_package_preserves_existing_choices_and_absent_keys()
    {
        var settings = new InMemorySettingsStore(); settings.Set("language", "fa");
        settings.Set("device.pin", "fictitious-device-only");
        var entries = new Dictionary<string, ReadOnlyMemory<byte>> { ["database.sqlite"] = new byte[] { 1, 2, 3 } };
        var packet = SqlCipherRestorePreferences.CapturePackagePreferences(entries, settings, ["language", "culture"]);
        Assert.Equal(packet.Original, packet.Restored); Assert.Equal("fa", packet.Restored["language"]);
        Assert.Null(packet.Restored["culture"]); Assert.False(packet.Restored.ContainsKey("device.pin"));
        Assert.Equal("fictitious-device-only", settings.Get("device.pin")); Assert.Single(entries);
    }

    [Theory]
    [InlineData("")]
    [InlineData("invalid")]
    public void Present_empty_or_malformed_source_remains_rejected_without_changing_preferences(string payload)
    {
        var settings = new InMemorySettingsStore(); settings.Set("language", "fa");
        var entries = new Dictionary<string, ReadOnlyMemory<byte>> { ["display-settings.json"] = System.Text.Encoding.UTF8.GetBytes(payload) };
        Assert.Throws<JsonException>(() => SqlCipherRestorePreferences.CapturePackagePreferences(entries, settings, ["language"]));
        Assert.Equal("fa", settings.Get("language"));
    }

    [Fact]
    public void Present_source_retains_explicit_null_and_excludes_foreign_device_security()
    {
        var settings = new InMemorySettingsStore(); settings.Set("language", "fa"); settings.Set("culture", "de-DE");
        var entries = new Dictionary<string, ReadOnlyMemory<byte>>
        {
            ["display-settings.json"] = """{"language":null,"device.pin":"foreign"}"""u8.ToArray()
        };
        var packet = SqlCipherRestorePreferences.CapturePackagePreferences(entries, settings, ["language", "culture"]);
        Assert.Null(packet.Restored["language"]); Assert.Equal("de-DE", packet.Restored["culture"]);
        Assert.False(packet.Restored.ContainsKey("device.pin")); Assert.Equal("fa", settings.Get("language"));
    }
}
