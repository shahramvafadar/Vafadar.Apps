using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Testing;

namespace Vafadar.Data.Tests;

/// <summary>Export/material access to a factory connection ends with its owning context, even before publication changes.</summary>
public sealed class ProtectedConnectionLifetimeAuthorityTests
{
    /// <summary>A retained connection reference cannot outlive the exact normal or maintenance context lease.</summary>
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task Disposed_context_revokes_captured_native_access(bool asynchronous, bool maintenance)
    {
        using var directory = new TemporaryDirectory();
        var path = directory.Combine("never-opened.db");
        using var services = new ServiceCollection().AddLocalDatabase<TestDbContext>(path, protectionRequired: true).BuildServiceProvider();
        var location = services.GetRequiredService<LocalDatabaseLocation<TestDbContext>>();
        DatabaseAccessGate.ExclusiveLease? owner = await location.Access.AcquireExclusiveAsync(TestContext.Current.CancellationToken);
        try
        {
            // This public closed-creator label is never used as a key or to open storage.
            location.MoveToProtected(path, () => new SqliteConnection(new SqliteConnectionStringBuilder
            {
                DataSource = path, Mode = SqliteOpenMode.ReadWrite, Pooling = false, Password = nameof(ProtectedConnectionLifetimeAuthorityTests),
            }.ToString()), "4.19.0", owner);
            if (!maintenance) { owner.Dispose(); owner = null; }
            var factory = services.GetRequiredService<ILocalDbContextFactory<TestDbContext>>();
            var context = asynchronous
                ? owner is null ? await factory.CreateDbContextAsync(TestContext.Current.CancellationToken)
                    : await factory.CreateDbContextAsync(owner, TestContext.Current.CancellationToken)
                : owner is null ? factory.CreateDbContext() : factory.CreateDbContext(owner);
            var connection = (SqliteConnection)context.Database.GetDbConnection();
            Assert.Equal(System.Data.ConnectionState.Closed, connection.State);
            ProtectedSqliteAccess.Require(connection);
            if (asynchronous) await context.DisposeAsync(); else context.Dispose();
            // Keep the original publication and maintenance owner live: only this context's lease retired.
            owner?.Validate();
            if (owner is null)
            {
                using var nextOwner = location.Access.AcquireExclusive();
                nextOwner.Validate();
                Assert.Throws<InvalidOperationException>(() => ProtectedSqliteAccess.Require(connection));
            }
            else Assert.Throws<InvalidOperationException>(() => ProtectedSqliteAccess.Require(connection));
            Assert.False(File.Exists(path));
        }
        finally { owner?.Dispose(); }
    }
}
