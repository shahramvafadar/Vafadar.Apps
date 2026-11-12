using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Testing;

namespace Vafadar.Data.Tests;

/// <summary>Factory completion must still belong to the exact live publication and maintenance owner.</summary>
public sealed class FactoryReturnAuthorityTests
{
    /// <summary>A connection creator may synchronously retire its maintenance turn before returning a closed connection.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task Owner_retired_inside_creator_refuses_before_context_construction(bool asynchronous) =>
        VerifyAsync("creator-owner", asynchronous);

    /// <summary>A context constructor cannot hand back a result after retiring its maintenance turn.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task Owner_retired_inside_constructor_disposes_the_unpublished_context(bool asynchronous) =>
        VerifyAsync("constructor-owner", asynchronous);

    /// <summary>A same-path replacement during construction must not return the earlier publication's context.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task Publication_retired_inside_constructor_disposes_the_unpublished_context(bool asynchronous) =>
        VerifyAsync("constructor-publication", asynchronous);

    /// <summary>The shared completion boundary also refuses a redirected ordinary unprotected context.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Unprotected_publication_retired_inside_constructor_disposes_the_unpublished_context(bool asynchronous)
    {
        using var directory = new TemporaryDirectory();
        var path = directory.Combine("first-never-opened.db");
        var destination = directory.Combine("second-never-opened.db");
        var state = new ConstructionState();
        using var services = new ServiceCollection().AddSingleton(state).AddLocalDatabase<ReturnContext>(path).BuildServiceProvider();
        var location = services.GetRequiredService<LocalDatabaseLocation<ReturnContext>>();
        var factory = services.GetRequiredService<ILocalDbContextFactory<ReturnContext>>();
        state.OnConstruct = () => location.MoveTo(destination);
        ReturnContext? returned = null; Exception? failure = null;
        try { returned = asynchronous ? await factory.CreateDbContextAsync(TestContext.Current.CancellationToken) : factory.CreateDbContext(); }
        catch (Exception exception) { failure = exception; }
        var disposedBeforeCleanup = state.ContextsDisposed;
        returned?.Dispose(); state.OnConstruct = null;
        using var nextOwner = location.Access.AcquireExclusive(); nextOwner.Validate();
        var refusal = Assert.IsType<InvalidOperationException>(failure);
        Assert.Equal("Captured database publication retired.", refusal.Message);
        Assert.Null(returned); Assert.Equal(1, state.Constructed); Assert.Equal(1, disposedBeforeCleanup);
        Assert.False(File.Exists(path)); Assert.False(File.Exists(destination));
    }

    private static async Task VerifyAsync(string stage, bool asynchronous)
    {
        using var directory = new TemporaryDirectory();
        var path = directory.Combine("never-opened.db");
        var state = new ConstructionState();
        using var services = new ServiceCollection().AddSingleton(state)
            .AddLocalDatabase<ReturnContext>(path, protectionRequired: true).BuildServiceProvider();
        var location = services.GetRequiredService<LocalDatabaseLocation<ReturnContext>>();
        var factory = services.GetRequiredService<ILocalDbContextFactory<ReturnContext>>();
        var owner = stage == "constructor-publication" ? null : await location.Access.AcquireExclusiveAsync(TestContext.Current.CancellationToken);
        var connections = new List<ClosedConnection>();
        ClosedConnection CreateConnection()
        {
            var connection = new ClosedConnection(path, state); connections.Add(connection);
            if (stage == "creator-owner") owner!.Dispose();
            return connection;
        }
        location.MoveToProtected(path, CreateConnection, "4.19.0", owner);
        if (stage == "constructor-owner") state.OnConstruct = () => owner!.Dispose();
        if (stage == "constructor-publication") state.OnConstruct = () =>
            location.MoveToProtected(path, () => new ClosedConnection(path, state), "4.19.0");
        ReturnContext? returned = null; Exception? failure = null;
        try
        {
            returned = asynchronous
                ? owner is null ? await factory.CreateDbContextAsync(TestContext.Current.CancellationToken)
                    : await factory.CreateDbContextAsync(owner, TestContext.Current.CancellationToken)
                : owner is null ? factory.CreateDbContext() : factory.CreateDbContext(owner);
        }
        catch (Exception exception) { failure = exception; }
        var disposedBeforeCleanup = state.ContextsDisposed;
        var connectionsDisposedBeforeCleanup = connections.All(connection => connection.Disposals > 0);
        // The baseline intentionally returns an invalid result. Clean it without turning that return into acceptance.
        returned?.Dispose(); state.OnConstruct = null; owner?.Dispose();
        foreach (var connection in connections) connection.Dispose();
        using var nextOwner = location.Access.AcquireExclusive(); nextOwner.Validate();
        var refusal = Assert.IsType<InvalidOperationException>(failure);
        Assert.Equal(stage == "constructor-publication" ? "Captured database publication retired."
            : "Database maintenance ownership retired or belongs to another factory.", refusal.Message);
        Assert.Null(returned);
        Assert.Equal(stage == "creator-owner" ? 0 : 1, state.Constructed);
        Assert.Equal(stage == "creator-owner" ? 0 : 1, disposedBeforeCleanup);
        Assert.True(connectionsDisposedBeforeCleanup);
        Assert.Equal(0, state.OpenAttempts);
        Assert.False(File.Exists(path));
    }

    /// <summary>Only counters and a deterministic retirement callback; no source, credential or SQL state.</summary>
    private sealed class ConstructionState
    {
        public Action? OnConstruct { get; set; }
        public int Constructed;
        public int ContextsDisposed;
        public int OpenAttempts;
    }

    /// <summary>Exercises the actual generic factory's constructor handoff without opening a database.</summary>
    private sealed class ReturnContext : LocalDbContext
    {
        private readonly ConstructionState state;
        private bool disposed;

        public ReturnContext(DbContextOptions<ReturnContext> options, ConstructionState state) : base(options)
        { this.state = state; state.Constructed++; state.OnConstruct?.Invoke(); }

        public override void Dispose()
        {
            if (!disposed) { disposed = true; state.ContextsDisposed++; }
            base.Dispose();
        }
    }

    /// <summary>Fictitious closed connection; any attempted native access is an explicit test failure.</summary>
    private sealed class ClosedConnection : SqliteConnection
    {
        private readonly ConstructionState state;

        public ClosedConnection(string path, ConstructionState state) : base(new SqliteConnectionStringBuilder
        { DataSource = path, Mode = SqliteOpenMode.ReadWrite, Pooling = false, Password = Convert.ToBase64String(new byte[32]) }.ToString()) => this.state = state;

        public int Disposals { get; private set; }

        public override void Open() { state.OpenAttempts++; throw new InvalidOperationException("Unexpected storage access."); }
        public override Task OpenAsync(CancellationToken cancellationToken)
        { state.OpenAttempts++; return Task.FromException(new InvalidOperationException("Unexpected storage access.")); }

        protected override void Dispose(bool disposing) { if (disposing) Disposals++; base.Dispose(disposing); }
    }
}
