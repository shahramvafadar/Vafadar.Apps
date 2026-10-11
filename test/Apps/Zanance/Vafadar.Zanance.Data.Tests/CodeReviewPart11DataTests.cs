using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Data;
using Vafadar.Testing;

namespace Vafadar.Zanance.Data.Tests;

/// <summary>Regression tests of the code review, part 11: settings and data.</summary>
public sealed class CodeReviewPart11DataTests : IDisposable
{
    private readonly TemporaryDirectory _directory = new();
    private readonly ServiceProvider _services;
    private readonly ZananceStore _store;

    public CodeReviewPart11DataTests()
    {
        _services = new ServiceCollection().AddZananceData(_directory.Combine("zanance.db")).BuildServiceProvider();
        _services.MigrateLocalDatabase<ZananceDbContext>();
        _store = _services.GetRequiredService<ZananceStore>();
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose()
    {
        _services.Dispose();
        SqliteTestPools.Clear(_directory);
        _directory.Dispose();
    }

    [Fact]
    public async Task Settings_changed_at_the_same_moment_are_all_kept()
    {
        // CR11: each switch on the settings page read the row, changed one value and wrote the whole row back; two at
        // once restored each other's old value. Updates now run one after the other.
        await _store.GetSettingsAsync(Ct);

        await Task.WhenAll(
            Task.Run(() => _store.UpdateSettingsAsync(s => s.MonthStartDay = 25, Ct), Ct),
            Task.Run(() => _store.UpdateSettingsAsync(s => s.ReportCurrencyCode = "USD", Ct), Ct),
            Task.Run(() => _store.UpdateSettingsAsync(s => s.RateFreshnessDays = 7, Ct), Ct),
            Task.Run(() => _store.UpdateSettingsAsync(s => s.ValuationCurrencyEnabled = true, Ct), Ct));

        var settings = await _store.GetSettingsAsync(Ct);
        Assert.Equal(25, settings.MonthStartDay);
        Assert.Equal("USD", settings.ReportCurrencyCode);
        Assert.Equal(7, settings.RateFreshnessDays);
        Assert.True(settings.ValuationCurrencyEnabled);
    }
}
