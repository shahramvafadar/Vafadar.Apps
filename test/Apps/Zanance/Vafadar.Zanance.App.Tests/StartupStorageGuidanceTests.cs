using Vafadar.Data;
using Vafadar.Zanance.App.Startup;

namespace Vafadar.Zanance.App.Tests;

/// <summary>AT-222: a known storage preflight refusal stays covered without suggesting credential recovery.</summary>
[Trait("AT", "AT-222")]
public sealed class StartupStorageGuidanceTests
{
    [Fact]
    public async Task Known_storage_refusal_does_not_offer_key_or_backup_recovery()
    {
        var vm = new StartupViewModel(() => Task.FromException(new InsufficientDatabaseStorageException()),
            () => Task.FromResult(false), () => Task.FromResult(false));

        await vm.InitializeAsync();

        Assert.True(vm.HasFailed);
        Assert.False(vm.IsBusy);
        Assert.False(vm.RecoveryAvailable);
        Assert.False(vm.BackupRecoveryAvailable);
    }

    [Fact]
    public async Task Retained_recovery_commands_cannot_run_after_a_known_storage_refusal()
    {
        var calls = 0;
        Task<bool> Recover() { calls++; return Task.FromResult(false); }
        var vm = new StartupViewModel(() => Task.FromException(new InsufficientDatabaseStorageException()), Recover, Recover);
        var keyCommand = vm.RecoverCommand;
        var backupCommand = vm.RestoreBackupCommand;
        await vm.InitializeAsync();

        await keyCommand.ExecuteAsync(null);
        await backupCommand.ExecuteAsync(null);

        Assert.Equal(0, calls);
        Assert.True(vm.HasFailed);
    }

    [Fact]
    public async Task Identical_untyped_error_text_does_not_classify_storage_or_hide_recovery()
    {
        var vm = new StartupViewModel(() => Task.FromException(new IOException(
            new InsufficientDatabaseStorageException().Message)), () => Task.FromResult(false), () => Task.FromResult(false));
        await vm.InitializeAsync();
        Assert.True(vm.ShowGenericFailure);
        Assert.False(vm.ShowStorageFailure);
        Assert.True(vm.RecoveryAvailable);
        Assert.True(vm.BackupRecoveryAvailable);
    }

    [Fact]
    public async Task Explicit_retry_success_clears_storage_guidance_without_calling_recovery()
    {
        var attempts = 0;
        var recoveries = 0;
        var vm = new StartupViewModel(() => ++attempts == 1
            ? Task.FromException(new InsufficientDatabaseStorageException()) : Task.CompletedTask,
            () => { recoveries++; return Task.FromResult(false); });
        await vm.InitializeAsync();
        await vm.InitializeCommand.ExecuteAsync(null);
        Assert.Equal(2, attempts);
        Assert.Equal(0, recoveries);
        Assert.False(vm.HasFailed);
        Assert.False(vm.HasInsufficientStorage);
        Assert.False(vm.IsBusy);
        Assert.False(vm.ShowStorageFailure);
    }

    [Fact]
    public async Task New_generic_failure_replaces_storage_reason_and_notifies_recovery_bindings()
    {
        var attempts = 0;
        var vm = new StartupViewModel(() => Task.FromException(++attempts == 1
            ? new InsufficientDatabaseStorageException() : new IOException("Different failure.")),
            () => Task.FromResult(false), () => Task.FromResult(false));
        await vm.InitializeAsync();
        var changes = new List<string?>();
        vm.PropertyChanged += (_, e) => changes.Add(e.PropertyName);
        await vm.InitializeCommand.ExecuteAsync(null);
        Assert.True(vm.ShowGenericFailure);
        Assert.False(vm.ShowStorageFailure);
        Assert.True(vm.RecoveryAvailable);
        Assert.True(vm.BackupRecoveryAvailable);
        Assert.Contains(nameof(vm.RecoveryAvailable), changes);
        Assert.Contains(nameof(vm.BackupRecoveryAvailable), changes);
        Assert.Contains(nameof(vm.ShowStorageFailure), changes);
    }

    [Fact]
    public async Task Space_refusal_during_recovery_blocks_further_recovery_until_explicit_retry()
    {
        var calls = 0;
        var vm = new StartupViewModel(() => Task.FromException(new IOException("Initial failure.")),
            () => { calls++; return Task.FromException<bool>(new InsufficientDatabaseStorageException()); },
            () => { calls++; return Task.FromResult(false); });
        await vm.InitializeAsync();
        await vm.RecoverCommand.ExecuteAsync(null);
        await vm.RestoreBackupCommand.ExecuteAsync(null);
        Assert.Equal(1, calls);
        Assert.True(vm.ShowStorageFailure);
        Assert.False(vm.ShowGenericFailure);
        Assert.False(vm.RecoveryAvailable);
        Assert.False(vm.BackupRecoveryAvailable);
    }
}
