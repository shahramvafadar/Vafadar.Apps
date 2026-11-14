using System.Runtime.CompilerServices;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Data;
using Vafadar.Testing;

namespace Vafadar.Zanance.Data.Tests;

/// <summary>The original closed household callback must not root a retired protected context through shared infrastructure.</summary>
public sealed class ProtectedContextServiceLifetimeTests
{
    /// <summary>Releases the complete original context/connection graph with the application factory and publisher still alive.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Disposed_original_context_with_closed_household_callback_is_collectible(bool asynchronous)
    {
        using var directory = new TemporaryDirectory();
        var path = directory.Combine("never-opened.db");
        using var services = new ServiceCollection().AddLocalDatabase<ZananceDbContext>(path, protectionRequired: true).BuildServiceProvider();
        var location = services.GetRequiredService<LocalDatabaseLocation<ZananceDbContext>>();
        using (var owner = await location.Access.AcquireExclusiveAsync(TestContext.Current.CancellationToken))
        {
            // This public test label is not a key; the original closed creator never opens storage in this contract.
            location.MoveToProtected(path, () => new SqliteConnection(new SqliteConnectionStringBuilder
            {
                DataSource = path, Mode = SqliteOpenMode.ReadWrite, Pooling = false, Password = nameof(ProtectedContextServiceLifetimeTests),
            }.ToString()), "4.19.0", owner);
        }
        var observed = await CreateAndDisposeAsync(services.GetRequiredService<ILocalDbContextFactory<ZananceDbContext>>(), asynchronous);
        await Task.Yield();
        for (var attempt = 0; attempt < 3 && (observed.Context.IsAlive || observed.Connection.IsAlive); attempt++)
        {
            GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
            await Task.Yield();
        }
        Assert.False(observed.Context.IsAlive);
        Assert.False(observed.Connection.IsAlive);
        Assert.False(File.Exists(path));
        using var finalOwner = location.Access.AcquireExclusive();
        finalOwner.Validate();
        GC.KeepAlive(services);
        GC.KeepAlive(location);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async Task<Lifetime> CreateAndDisposeAsync(ILocalDbContextFactory<ZananceDbContext> factory, bool asynchronous)
    {
        var context = asynchronous ? await factory.CreateDbContextAsync(TestContext.Current.CancellationToken) : factory.CreateDbContext();
        try
        {
            var connection = context.Database.GetDbConnection();
            Assert.Equal(System.Data.ConnectionState.Closed, connection.State);
            Assert.NotEmpty(context.Model.GetEntityTypes());
            return new(new WeakReference(context), new WeakReference(connection));
        }
        finally
        {
            if (asynchronous) await context.DisposeAsync();
            else context.Dispose();
        }
    }

    private sealed record Lifetime(WeakReference Context, WeakReference Connection);
}
