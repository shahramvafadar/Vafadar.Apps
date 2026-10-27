using Vafadar.Zanance.App;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Protects the exact observed original startup migration without excusing unrelated settings changes.</summary>
[Trait("AT", "AT-574")]
public sealed class LoadedColdStartupSettingsTests
{
    private static readonly string[] Columns = ["Id", "DisplayUnits", "UpdatedAt", "MonthStartDay"];
    private static readonly DateTimeOffset Now = new(2026, 10, 27, 4, 0, 0, TimeSpan.Zero);
    private const string Before = "[1,null,0,1]";
    private static string After(string units, long ticks, int monthStart = 1) =>
        System.Text.Json.JsonSerializer.Serialize(new object[] { 1, units, ticks, monthStart });

    [Fact]
    public void Only_original_null_to_captured_empty_legacy_units_and_original_save_time_are_admitted() =>
        Assert.True(LoadedColdStartPolicy.IsOriginalDisplayUnitStartup(Columns, Before,
            After("", Now.AddHours(-1).UtcTicks), "", Now));

    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)]
    public void Other_settings_foreign_units_and_unbound_save_times_remain_refused(int variant)
    {
        var after = variant switch
        {
            0 => After("", Now.AddHours(-1).UtcTicks, 2),
            1 => After("IRR|foreign|1", Now.AddHours(-1).UtcTicks),
            2 => After("", Now.AddTicks(1).UtcTicks),
            _ => After("", -1)
        };
        Assert.False(LoadedColdStartPolicy.IsOriginalDisplayUnitStartup(Columns, Before, after, "", Now));
    }

    [Fact]
    public void Existing_profile_units_cannot_be_replaced_by_the_startup_exception() =>
        Assert.False(LoadedColdStartPolicy.IsOriginalDisplayUnitStartup(Columns,
            """[1,"IRR|Toman|1",0,1]""", After("", Now.AddHours(-1).UtcTicks), "", Now));

    [Fact]
    public void Formatted_timestamp_text_cannot_replace_the_actual_integer_storage_contract() =>
        Assert.False(LoadedColdStartPolicy.IsOriginalDisplayUnitStartup(Columns, Before,
            """[1,"","2026-10-27 03:00:00",1]""", "", Now));
}
