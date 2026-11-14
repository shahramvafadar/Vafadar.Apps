using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Testing;

namespace Vafadar.Data.Tests;

/// <summary>Cancellation during native creation or constructor callbacks must retire an unaccepted factory result.</summary>
public sealed class FactoryCreationCancellationTests
{
    /// <summary>Both normal and maintenance creation observe cancellation before handing a context to the caller.</summary>
    [Theory]
    [InlineData("creator", false)]
    [InlineData("creator", true)]
    [InlineData("constructor", false)]
    [InlineData("constructor", true)]
    public async Task Cancelled_async_factory_does_not_hand_back_a_context(string stage, bool maintenance)
    {
        using var directory = new TemporaryDirectory();
        var path = directory.Combine("never-opened.db");
        using var cancellation = new CancellationTokenSource();
        var state = new ConstructionState { OnConstruct = stage == "constructor" ? cancellation.Cancel : null };
        using var services = new ServiceCollection().AddSingleton(state)
            .AddLocalDatabase<CancelledContext>(path, protectionRequired: true).BuildServiceProvider();
        var location = services.GetRequiredService<LocalDatabaseLocation<CancelledContext>>();
        DatabaseAccessGate.ExclusiveLease? owner = await location.Access.AcquireExclusiveAsync(TestContext.Current.CancellationToken);
        try
        {
            location.MoveToProtected(path, () =>
            {
                state.Connection = new ClosedConnection(path, state);
                if (stage == "creator") cancellation.Cancel();
                return state.Connection;
            }, "4.19.0", owner);
            if (!maintenance) { owner.Dispose(); owner = null; }
            var factory = services.GetRequiredService<ILocalDbContextFactory<CancelledContext>>();
            CancelledContext? returned = null; Exception? failure = null;
            try
            {
                returned = owner is null ? await factory.CreateDbContextAsync(cancellation.Token)
                    : await factory.CreateDbContextAsync(owner, cancellation.Token);
            }
            catch (Exception exception) { failure = exception; }
            var disposedBeforeCleanup = state.Disposed;
            returned?.Dispose();
            if (owner is not null) location.Access.RequireDrainedOwner(owner);
            else { using var nextOwner = location.Access.AcquireExclusive(); nextOwner.Validate(); }
            var refusal = Assert.IsType<OperationCanceledException>(failure);
            Assert.Equal(cancellation.Token, refusal.CancellationToken);
            Assert.Null(returned);
            Assert.Equal(stage == "creator" ? 0 : 1, state.Constructed);
            Assert.Equal(stage == "creator" ? 0 : 1, disposedBeforeCleanup);
            Assert.True(state.Connection is { Disposals: > 0 });
            Assert.Equal(0, state.OpenAttempts);
            Assert.False(File.Exists(path));
        }
        finally { owner?.Dispose(); }
    }

    /// <summary>A creation token does not become a cancellation token for an already accepted context lifetime.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Cancellation_after_handoff_does_not_revoke_an_accepted_context(bool maintenance)
    {
        using var directory = new TemporaryDirectory();
        var path = directory.Combine("never-opened.db");
        using var cancellation = new CancellationTokenSource();
        var state = new ConstructionState();
        using var services = new ServiceCollection().AddSingleton(state)
            .AddLocalDatabase<CancelledContext>(path, protectionRequired: true).BuildServiceProvider();
        var location = services.GetRequiredService<LocalDatabaseLocation<CancelledContext>>();
        DatabaseAccessGate.ExclusiveLease? owner = await location.Access.AcquireExclusiveAsync(TestContext.Current.CancellationToken);
        try
        {
            location.MoveToProtected(path, () => state.Connection = new ClosedConnection(path, state), "4.19.0", owner);
            if (!maintenance) { owner.Dispose(); owner = null; }
            var factory = services.GetRequiredService<ILocalDbContextFactory<CancelledContext>>();
            using (var context = owner is null ? await factory.CreateDbContextAsync(cancellation.Token)
                : await factory.CreateDbContextAsync(owner, cancellation.Token))
            {
                cancellation.Cancel();
                ProtectedSqliteAccess.Require(state.Connection!);
                Assert.Equal(1, state.Constructed);
                Assert.Equal(0, state.Disposed);
                Assert.Equal(0, state.OpenAttempts);
                Assert.False(File.Exists(path));
            }
            if (owner is not null) location.Access.RequireDrainedOwner(owner);
            else { using var nextOwner = location.Access.AcquireExclusive(); nextOwner.Validate(); }
        }
        finally { owner?.Dispose(); }
    }

    private sealed class ConstructionState
    {
        public Action? OnConstruct;
        public ClosedConnection? Connection;
        public int Constructed;
        public int Disposed;
        public int OpenAttempts;
    }

    /// <summary>Runs only the constructor callback; neither EF services nor a native database is opened.</summary>
    private sealed class CancelledContext : LocalDbContext
    {
        private readonly ConstructionState state;
        private bool disposed;
        public CancelledContext(DbContextOptions<CancelledContext> options, ConstructionState state) : base(options)
        { this.state = state; state.Constructed++; state.OnConstruct?.Invoke(); }
        public override void Dispose()
        {
            if (!disposed) { disposed = true; state.Disposed++; }
            base.Dispose();
        }
    }

    /// <summary>Refuses any storage access; the public marker is never used as a cryptographic key.</summary>
    private sealed class ClosedConnection(string path, ConstructionState state) : SqliteConnection(new SqliteConnectionStringBuilder
    { DataSource = path, Mode = SqliteOpenMode.ReadWrite, Pooling = false, Password = nameof(FactoryCreationCancellationTests) }.ToString())
    {
        public int Disposals { get; private set; }
        public override void Open() { state.OpenAttempts++; throw new InvalidOperationException("Unexpected storage access."); }
        public override Task OpenAsync(CancellationToken cancellationToken)
        { state.OpenAttempts++; return Task.FromException(new InvalidOperationException("Unexpected storage access.")); }
        protected override void Dispose(bool disposing) { if (disposing) Disposals++; base.Dispose(disposing); }
    }
}
