using Microsoft.Extensions.DependencyInjection;
using Vafadar.Data;
using Vafadar.Zanance.App.Startup;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Exercises the original startup retry state and storage-host guard without fake UI controls.</summary>
public sealed class StartupFlowTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

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
}
