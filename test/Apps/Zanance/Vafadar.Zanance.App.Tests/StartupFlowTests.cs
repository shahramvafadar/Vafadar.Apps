using Microsoft.Extensions.DependencyInjection;
using Vafadar.Data;
using Vafadar.Zanance.App.Startup;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Exercises the original startup retry state and storage-host guard without fake UI controls.</summary>
public sealed class StartupFlowTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact, Trait("AT", "AT-238")]
    public async Task Pending_backup_recovery_excludes_startup_and_other_recovery_until_its_explicit_result()
    {
        var held = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var calls = 0; var codes = 0;
        var vm = new StartupViewModel(() => ++calls == 1 ? Task.FromException(new IOException("Fictitious missing key")) : Task.CompletedTask,
            () => { codes++; return Task.FromResult(true); }, () => held.Task);
        await vm.InitializeAsync(); Assert.True(vm.BackupRecoveryAvailable);
        var restore = vm.RestoreBackupCommand.ExecuteAsync(null);
        await vm.InitializeAsync(); await vm.RecoverCommand.ExecuteAsync(null);
        Assert.Equal(1, calls); Assert.Equal(0, codes); Assert.True(vm.IsBusy);
        held.SetResult(true); await restore;
        Assert.Equal(2, calls); Assert.False(vm.HasFailed); Assert.False(vm.IsBusy);
    }

    [Fact, Trait("AT", "AT-222")]
    public async Task Pending_startup_is_covered_and_cannot_start_another_attempt_or_repeat_completed_preparation()
    {
        var held = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously); var calls = 0;
        var vm = new StartupViewModel(async () => { calls++; await held.Task; });
        Assert.True(vm.IsBusy); Assert.False(vm.HasFailed);
        var pending = vm.InitializeAsync(); await vm.InitializeAsync();
        Assert.True(vm.IsBusy); Assert.Equal(1, calls);
        held.SetResult(); await pending; await vm.InitializeAsync();
        Assert.False(vm.IsBusy); Assert.False(vm.HasFailed); Assert.Equal(1, calls);
    }

    [Fact, Trait("AT", "AT-222")]
    public async Task Failed_preparation_keeps_generic_retry_state_and_only_explicit_retry_prepares_again()
    {
        var calls = 0;
        var vm = new StartupViewModel(() => ++calls == 1 ? Task.FromException(new IOException("Fictitious startup failure")) : Task.CompletedTask);
        await vm.InitializeAsync(); Assert.True(vm.HasFailed); Assert.False(vm.IsBusy); Assert.Equal(1, calls);
        await vm.InitializeCommand.ExecuteAsync(null);
        Assert.False(vm.HasFailed); Assert.False(vm.IsBusy); Assert.Equal(2, calls);
    }

    [Fact, Trait("AT", "AT-238")]
    public async Task Recovery_is_available_only_after_failure_and_success_retries_original_preparation_once()
    {
        var calls = 0; var recoveries = 0;
        var vm = new StartupViewModel(() => ++calls == 1 ? Task.FromException(new IOException("Missing fictitious key")) : Task.CompletedTask,
            () => { recoveries++; return Task.FromResult(true); });
        Assert.True(vm.RecoveryAvailable);
        await vm.RecoverCommand.ExecuteAsync(null); Assert.Equal(0, recoveries);
        await vm.InitializeAsync(); Assert.True(vm.HasFailed);
        await vm.RecoverCommand.ExecuteAsync(null);
        Assert.Equal(1, recoveries); Assert.Equal(2, calls); Assert.False(vm.HasFailed);
        await vm.RecoverCommand.ExecuteAsync(null); Assert.Equal(1, recoveries);
    }

    [Theory, InlineData(false), InlineData(true), Trait("AT", "AT-238")]
    public async Task Cancelled_or_failed_recovery_keeps_startup_covered_without_repeating_preparation(bool fail)
    {
        var calls = 0;
        var vm = new StartupViewModel(() => { calls++; return Task.FromException(new IOException("Fictitious startup failure")); },
            () => fail ? Task.FromException<bool>(new IOException("Fictitious recovery failure")) : Task.FromResult(false));
        await vm.InitializeAsync(); await vm.RecoverCommand.ExecuteAsync(null);
        Assert.True(vm.HasFailed); Assert.False(vm.IsBusy); Assert.Equal(1, calls);
    }

    [Fact, Trait("AT", "AT-222")]
    public async Task Configured_protected_session_never_falls_back_to_plaintext_startup_migration()
    {
        using var f = new FlowFixture(); var location = f.Services.GetRequiredService<LocalDatabaseLocation<ZananceDbContext>>();
        using var services = new ServiceCollection().AddSingleton(location)
            .AddSingleton(new SqlCipherProfileSession<ZananceDbContext>(location, (_, _, _) => throw new IOException("Unexpected native resolution")))
            .BuildServiceProvider();
        var startup = new LocalDatabaseStartup(services);
        await Assert.ThrowsAsync<InvalidOperationException>(() => startup.PrepareAsync(Ct));
        Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
    }
    [Fact, Trait("AT", "AT-222")]
    public async Task Legacy_database_waits_for_the_actual_context_lease_before_rename_and_migration()
    {
        using var directory = new Vafadar.Testing.TemporaryDirectory();
        var legacy = directory.Combine("finance.db"); var current = directory.Combine("zanance.db");
        using (var old = new ServiceCollection().AddZananceData(legacy).BuildServiceProvider())
        {
            old.MigrateLocalDatabase<ZananceDbContext>();
            await old.GetRequiredService<ZananceStore>().SaveAccountAsync(new Vafadar.Zanance.Core.Accounts.Account
            { Name = "Fictitious legacy account", CurrencyCode = "EUR", OpeningDate = new(2026, 1, 1), OpeningBalance = -321 }, cancellationToken: Ct);
        }
        Vafadar.Testing.SqliteTestPools.Clear(directory);
        using var services = new ServiceCollection().AddZananceData(current).BuildServiceProvider();
        var factory = services.GetRequiredService<Microsoft.EntityFrameworkCore.IDbContextFactory<ZananceDbContext>>();
        var held = await factory.CreateDbContextAsync(Ct);
        var pending = new LocalDatabaseStartup(services).PrepareAsync(Ct);
        Assert.False(pending.IsCompleted); Assert.True(File.Exists(legacy)); Assert.False(File.Exists(current));
        await held.DisposeAsync(); await pending;
        Assert.False(File.Exists(legacy)); Assert.True(File.Exists(current));
        var account = Assert.Single(await services.GetRequiredService<ZananceStore>().GetAccountsAsync(cancellationToken: Ct));
        Assert.Equal(-321, account.OpeningBalance); Assert.Equal("Fictitious legacy account", account.Name);
        await new LocalDatabaseStartup(services).PrepareAsync(Ct);
        Vafadar.Testing.SqliteTestPools.Clear(directory);
    }

    [Theory, Trait("AT", "AT-222")]
    [InlineData("zanance.db-wal")]
    [InlineData("finance.db.generation.sqlite")]
    [InlineData("finance.db.fixture.cipher.db")]
    public async Task Legacy_rename_refuses_conflicting_or_protected_artifacts_before_moving_any_file(string conflictingName)
    {
        using var directory = new Vafadar.Testing.TemporaryDirectory();
        var legacy = directory.Combine("finance.db"); var current = directory.Combine("zanance.db");
        await File.WriteAllTextAsync(legacy, "fictitious original", Ct);
        await File.WriteAllTextAsync(directory.Combine(conflictingName), "fictitious conflict", Ct);
        using var services = new ServiceCollection().AddZananceData(current).BuildServiceProvider();
        await Assert.ThrowsAsync<IOException>(() => new LocalDatabaseStartup(services).PrepareAsync(Ct));
        Assert.Equal("fictitious original", await File.ReadAllTextAsync(legacy, Ct));
        Assert.Equal("fictitious conflict", await File.ReadAllTextAsync(directory.Combine(conflictingName), Ct));
        Assert.False(File.Exists(current));
    }

    [Fact, Trait("AT", "AT-222")]
    public async Task Protection_required_before_publication_refuses_plaintext_startup_without_renaming_legacy_data()
    {
        using var directory = new Vafadar.Testing.TemporaryDirectory();
        var legacy = directory.Combine("finance.db"); var current = directory.Combine("zanance.db");
        await File.WriteAllTextAsync(legacy, "fictitious original", Ct);
        using var services = new ServiceCollection().AddZananceData(current, protectionRequired: true).BuildServiceProvider();
        await Assert.ThrowsAsync<InvalidOperationException>(() => new LocalDatabaseStartup(services).PrepareAsync(Ct));
        Assert.Equal("fictitious original", await File.ReadAllTextAsync(legacy, Ct)); Assert.False(File.Exists(current));
    }

}
