using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Backup;
using Vafadar.Core.Hosting;
using Vafadar.Core.Settings;
using Vafadar.Data;
using Vafadar.Testing;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Plans;

namespace Vafadar.Zanance.Data.Tests;

public sealed class ZananceBackupTests : IDisposable
{
    private static readonly DateOnly Today = new(2027, 3, 20);
    private readonly TemporaryDirectory _directory = new();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose()
    {
        SqliteTestPools.Clear(_directory);
        _directory.Dispose();
    }

    [Fact]
    [Trait("AT", "AT-57")]
    public async Task Restore_on_a_fresh_install_gives_the_same_balances_plans_and_occurrences()
    {
        await using var original = Install("device-a");
        var store = original.GetRequiredService<ZananceStore>();
        var account = new Account { Name = "Checking", CurrencyCode = "EUR", OpeningBalance = 100_000, OpeningDate = new DateOnly(2027, 1, 1) };
        await store.SaveAccountAsync(account, Ct);
        await store.SaveEntryAsync(new LedgerEntry { Kind = EntryKind.Expense, AccountId = account.Id, Amount = 2_500, Date = new DateOnly(2027, 2, 3) }, Ct);
        var plan = new Schedule
        {
            Name = "Rent",
            AccountId = account.Id,
            Amount = 50_000,
            Rule = new RecurrenceRule { Frequency = Frequency.Monthly, Start = new DateOnly(2027, 1, 5) },
            AutoPost = true,
        };
        await original.GetRequiredService<PlanStore>().SaveScheduleAsync(plan, Ct);
        await original.GetRequiredService<AutoPostProcessor>().RunAsync(Today, Ct);
        var package = await original.GetRequiredService<IBackupService>().CreatePackageAsync("correct horse", Ct);

        await using var fresh = Install("device-b");
        var inspected = await fresh.GetRequiredService<IBackupService>().InspectPackageAsync(package, "correct horse", Ct);
        await fresh.GetRequiredService<IBackupService>().RestorePackageAsync(package, "correct horse", Ct);

        var restored = fresh.GetRequiredService<ZananceStore>();
        var accounts = await restored.GetAccountsAsync(cancellationToken: Ct);
        var entries = await restored.GetEntriesAsync(cancellationToken: Ct);
        Assert.Equal("4", inspected.Summary![ZananceBackupSummary.Entries]);
        Assert.Equal(100_000 - 2_500 - 150_000, LedgerCalculator.Balance(accounts.Single(), entries, Today));
        Assert.Equal(3, (await fresh.GetRequiredService<PlanStore>().GetStatesAsync(plan.Id, Ct)).Count);

        // Automatic posting after the restore does not post settled occurrences again (BAK-11).
        var rerun = await fresh.GetRequiredService<AutoPostProcessor>().RunAsync(Today, Ct);
        Assert.Equal(0, rerun.Posted);
    }

    [Fact]
    [Trait("AT", "AT-56")]
    public async Task A_wrong_password_leaves_the_current_data_unchanged()
    {
        await using var device = Install("device");
        var store = device.GetRequiredService<ZananceStore>();
        var account = new Account { Name = "Checking", CurrencyCode = "EUR", OpeningDate = new DateOnly(2027, 1, 1) };
        await store.SaveAccountAsync(account, Ct);
        var package = await device.GetRequiredService<IBackupService>().CreatePackageAsync("secret", Ct);
        await store.SaveEntryAsync(new LedgerEntry { Kind = EntryKind.Income, AccountId = account.Id, Amount = 1, Date = new DateOnly(2027, 1, 2) }, Ct);

        var error = await Assert.ThrowsAsync<BackupException>(() => device.GetRequiredService<IBackupService>().RestorePackageAsync(package, "wrong", Ct));

        Assert.Equal(BackupError.InvalidPasswordOrCorrupted, error.Error);
        Assert.Single(await store.GetEntriesAsync(cancellationToken: Ct));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("reused-password")]
    [Trait("AT", "AT-57")]
    public async Task First_run_restore_keeps_the_account_and_preferences_without_creating_another_account(string? password)
    {
        await using var original = Install("original");
        var store = original.GetRequiredService<ZananceStore>();
        var account = new Account { Name = "Existing account", CurrencyCode = "EUR", OpeningBalance = 123_456, OpeningDate = Today };
        await store.SaveAccountAsync(account, Ct);
        await store.UpdateSettingsAsync(settings =>
        {
            settings.DefaultAccountId = account.Id;
            settings.DefaultCurrencyCode = "EUR";
            settings.ReportCurrencyCode = "CHF";
            settings.Mode = Core.Settings.ExperienceMode.Advanced;
            // Older and interrupted onboarding backups may have accounts without the completed flag.
            settings.OnboardingCompleted = false;
        }, Ct);
        var backup = original.GetRequiredService<IBackupService>();
        var package = await backup.CreatePackageAsync(password, Ct);
        // Reusing a password on the next backup is supported; no new recovery secret is required.
        var nextPackage = await backup.CreatePackageAsync(password, Ct);
        await backup.InspectPackageAsync(nextPackage, password, Ct);

        await using var fresh = Install("fresh");
        var restored = fresh.GetRequiredService<ZananceStore>();
        Assert.Empty(await restored.GetAccountsAsync(cancellationToken: Ct));
        await fresh.GetRequiredService<IBackupService>().RestorePackageAsync(package, password, Ct);
        await restored.CompleteRestoredOnboardingAsync(Ct);
        await restored.CompleteRestoredOnboardingAsync(Ct);

        var restoredAccount = Assert.Single(await restored.GetAccountsAsync(cancellationToken: Ct));
        Assert.Equal(account.Id, restoredAccount.Id);
        Assert.Equal(123_456, restoredAccount.OpeningBalance);
        var settings = await restored.GetSettingsAsync(Ct);
        Assert.True(settings.OnboardingCompleted);
        Assert.Equal(account.Id, settings.DefaultAccountId);
        Assert.Equal("CHF", settings.ReportCurrencyCode);
        Assert.Equal(Core.Settings.ExperienceMode.Advanced, settings.Mode);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Trait("AT", "AT-01")]
    public async Task An_empty_restored_profile_still_needs_a_first_account(bool previouslyCompleted)
    {
        await using var original = Install("empty-original");
        await original.GetRequiredService<ZananceStore>().UpdateSettingsAsync(settings => settings.OnboardingCompleted = previouslyCompleted, Ct);
        var package = await original.GetRequiredService<IBackupService>().CreatePackageAsync(null, Ct);
        await using var fresh = Install("empty-fresh");
        await fresh.GetRequiredService<IBackupService>().RestorePackageAsync(package, null, Ct);
        var store = fresh.GetRequiredService<ZananceStore>();
        await store.CompleteRestoredOnboardingAsync(Ct);
        Assert.False((await store.GetSettingsAsync(Ct)).OnboardingCompleted);
        Assert.Empty(await store.GetAccountsAsync(cancellationToken: Ct));
    }

    private ServiceProvider Install(string name)
    {
        var services = new ServiceCollection()
            .AddZananceData(_directory.Combine(Path.Combine(name, "zanance.db")))
            .AddVafadarBackup()
            .AddSingleton<IAppEnvironment>(new StaticAppEnvironment("pro.vafadar.zanance", "Zanance", new Version(1, 0, 0), name, "Tests"))
            .AddSingleton<ISettingsStore, InMemorySettingsStore>()
            .BuildServiceProvider();
        services.MigrateLocalDatabase<ZananceDbContext>();
        return services;
    }
}
