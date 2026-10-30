using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Zanance.App.Features.Settings;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Tests;

/// <summary>D-648: original settings file and complete source freshness across actual native availability waits.</summary>
public sealed class SettingsOriginalSourceBoundaryTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private static async Task SeedAsync(FlowFixture f, string name)
    {
        var settings = await f.Store.GetSettingsAsync(Ct); settings.DefaultCurrencyCode = "EUR"; await f.Store.SaveSettingsAsync(settings, Ct);
        await f.Store.SaveAccountAsync(new Account { Name = name, CurrencyCode = "EUR", OpeningBalance = 0, OpeningDate = new(2026, 10, 1) }, Ct);
    }
    private sealed class ChangingFactory(IDbContextFactory<ZananceDbContext> first, IDbContextFactory<ZananceDbContext> later) : IDbContextFactory<ZananceDbContext>
    {
        private int _calls;
        public ZananceDbContext CreateDbContext() => (++_calls == 1 ? first : later).CreateDbContext();
        public Task<ZananceDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
            => (++_calls == 1 ? first : later).CreateDbContextAsync(cancellationToken);
    }
    /// <summary>A read may refuse a changed actual file, but never combine original settings with another profile's accounts.</summary>
    [Fact]
    public async Task Original_settings_packet_cannot_mix_a_later_actual_profile_file()
    {
        using var first = new FlowFixture(); using var later = new FlowFixture(); await SeedAsync(first, "D648 original account"); await SeedAsync(later, "D648 other profile");
        var factory = new ChangingFactory(first.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>(), later.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>());
        var store = new ZananceStore(factory, first.Time, first.Services.GetRequiredService<ICommercialWriteAccessSource>()); SettingsSnapshot? result = null;
        var error = await Record.ExceptionAsync(async () => result = await SettingsSnapshot.ReadAsync(store, first.Localization, first.Time, () => Task.FromResult(false), false, () => Task.FromResult(false), Ct));
        if (error is not null) Assert.IsType<InvalidOperationException>(error);
        else Assert.Equal("D648 original account", Assert.Single(Assert.IsType<SettingsSnapshot>(result).DefaultAccounts).Name);
    }
    /// <summary>Completing a native availability read cannot publish original settings replaced during that actual wait.</summary>
    [Fact]
    public async Task Original_settings_changed_during_native_availability_are_not_published_as_current()
    {
        using var f = new FlowFixture(); await SeedAsync(f, "D648 original account"); var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var held = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously); SettingsSnapshot? result = null;
        var reading = Record.ExceptionAsync(async () => result = await SettingsSnapshot.ReadAsync(f.Store, f.Localization, f.Time,
            () => { entered.TrySetResult(); return held.Task; }, false, () => Task.FromResult(false), Ct));
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(10), Ct); var settings = await f.Store.GetSettingsAsync(Ct); settings.DefaultCurrencyCode = "USD"; await f.Store.SaveSettingsAsync(settings, Ct);
        held.SetResult(false); var error = await reading;
        if (error is not null) Assert.IsType<InvalidOperationException>(error);
        else Assert.Equal("USD", Assert.IsType<SettingsSnapshot>(result).Settings.DefaultCurrencyCode);
    }
}
