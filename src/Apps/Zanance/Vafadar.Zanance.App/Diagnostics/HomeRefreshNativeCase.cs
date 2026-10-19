#if (DEBUG && WINDOWS) || ZANANCE_INDEPENDENT_HOME_REVIEW
using System.ComponentModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Data;
using Vafadar.Zanance.App.Features.Home;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Diagnostics;

/// <summary>Original Home publication with one deterministic real same-file edit in an independent native fixture.</summary>
internal static class HomeRefreshNativeCase
{
    private sealed class EditingFactory(IDbContextFactory<ZananceDbContext> original, Func<Task> edit) : IDbContextFactory<ZananceDbContext>
    {
        internal int Created;
        internal bool EditSawCover;
        internal HomeViewModel? Home;
        public ZananceDbContext CreateDbContext() => throw new InvalidOperationException("Use the original asynchronous Home reader.");
        public async Task<ZananceDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
        {
            if (++Created == 2)
            {
                EditSawCover = Home is { Loading.IsLoading: true, Loading.IsReady: false, Loading.HasFailed: false };
                await edit();
            }
            return await original.CreateDbContextAsync(cancellationToken);
        }
    }

    /// <summary>Exercises actual VM/page loading and source refusal; caller supplies only native attach/observe/return effects.</summary>
    internal static async Task<object> RunAsync(IServiceProvider services, string file, Func<HomePage, Task> show,
        Func<HomePage, HomeViewModel, Task> observe, Func<HomePage, Task> close, CancellationToken ct, bool addAccount = false)
    {
        if (File.Exists(file)) throw new IOException("Never reseed an existing native Home refresh fixture.");
        using var fixture = new ServiceCollection().AddZananceData(file).BuildServiceProvider();
        await Task.Run(() => fixture.MigrateLocalDatabase<ZananceDbContext>(), ct);
        var original = fixture.GetRequiredService<IDbContextFactory<ZananceDbContext>>();
        var seed = fixture.GetRequiredService<ZananceStore>();
        var account = new Account { Name = "Original Home source", CurrencyCode = "EUR", Type = AccountType.Checking,
            OpeningDate = DateOnly.FromDateTime(services.GetRequiredService<TimeProvider>().GetLocalNow().DateTime), OpeningBalance = 1234 };
        await seed.SaveAccountAsync(account, ct);
        var factory = new EditingFactory(original, async () =>
        {
            await using var writer = await original.CreateDbContextAsync(ct);
            (await writer.Accounts.SingleAsync(ct)).Name = "Current Home source";
            if (addAccount) writer.Accounts.Add(new Account { Name = "Concurrent Home account", CurrencyCode = "EUR",
                OpeningDate = account.OpeningDate });
            await writer.SaveChangesAsync(ct);
        });
        var store = new ZananceStore(factory, services.GetRequiredService<TimeProvider>(), fixture.GetRequiredService<ICommercialWriteAccessSource>());
        var home = ActivatorUtilities.CreateInstance<HomeViewModel>(services, store); factory.Home = home;
        var page = new HomePage(home); var publications = 0; var premature = false;
        void Presented() { publications++; if (!home.Loading.IsLoading || home.Loading.IsReady) premature = true; }
        void CoverChanged(object? sender, PropertyChangedEventArgs args)
        { if (home.Loading.IsReady && publications != 1) premature = true; }
        home.PresentingSnapshot += Presented; home.Loading.PropertyChanged += CoverChanged;
        try
        {
            await show(page);
            for (var i = 0; !home.Loading.HasLoaded && !home.Loading.HasFailed && i < 600; i++) await Task.Delay(50, ct);
            await page.PendingPublication;
            if (!home.Loading.IsReady || home.Loading.HasFailed || publications != 1 || premature || !factory.EditSawCover
                || factory.Created != 4 || home.Accounts.Count != (addAccount ? 2 : 1) || home.Accounts.Single(a => a.Id == account.Id).Name != "Current Home source")
                throw new IOException("Original Home did not retain one covered, current-file publication through supersession.");
            await observe(page, home);
            var storedAccounts = await seed.GetAccountsAsync(cancellationToken: ct);
            var stored = storedAccounts.Single(a => a.Id == account.Id);
            if (storedAccounts.Count != (addAccount ? 2 : 1) || storedAccounts.Where(a => a.Id != account.Id).Any(a => a.OpeningBalance != 0))
                throw new IOException("Concurrent Home references changed financial values.");
            if (stored.Id != account.Id || stored.Name != "Current Home source" || stored.OpeningBalance != 1234
                || (await seed.GetEntriesAsync(cancellationToken: ct)).Count != 0)
                throw new IOException("The Home refresh changed original financial values.");
            await using var db = await original.CreateDbContextAsync(ct);
            if (await db.Settings.AnyAsync(ct)) throw new IOException("Home refresh wrote default preferences.");
            return new { OriginalActualHomePage = true, OriginalCoveredEdit = factory.EditSawCover,
                ReadContexts = factory.Created, Publications = publications, AccountCount = storedAccounts.Count, NoPrematureUncover = !premature,
                CurrentAccountName = stored.Name, OpeningBalanceMinor = stored.OpeningBalance,
                LedgerRows = 0, NoDefaultSettingsWrite = true, SeparateFictitiousPlainSqliteFixture = true };
        }
        finally
        {
            home.PresentingSnapshot -= Presented; home.Loading.PropertyChanged -= CoverChanged;
            page.RetirePublication(); await close(page);
        }
    }
}
#endif
