using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Features.Home;

/// <summary>Owns only one covered Home publication, including its original final validation.</summary>
public interface IHomeReadOperation : IAsyncDisposable
{
    /// <summary>Gets the complete original snapshot, with its file and work authority.</summary>
    HomeReadSnapshot Snapshot { get; }

    /// <summary>Validates current persisted facts and original inputs before publication.</summary>
    Task ValidateAsync(CancellationToken cancellationToken = default);
}

/// <summary>Begins a bounded publication against the store belonging to this actual Home page.</summary>
public interface IHomeReadOperationSource
{
    /// <summary>Captures the requested original period and file; the caller must dispose the operation.</summary>
    Task<IHomeReadOperation> BeginAsync(DateOnly today, int periodIndex, CancellationToken cancellationToken = default);
}

/// <summary>Retains one operation-local protected connection, with separate read and validation transactions.</summary>
public sealed class HomeReadOperationSource(ZananceStore store) : IHomeReadOperationSource
{
    /// <inheritdoc />
    public async Task<IHomeReadOperation> BeginAsync(DateOnly today, int periodIndex, CancellationToken cancellationToken = default)
        => new Operation(await store.BeginHomeReadSessionAsync(today, periodIndex, cancellationToken));

    /// <summary>Adapts the actual store lease without introducing another connection or key cache.</summary>
    private sealed class Operation(HomeReadSession session) : IHomeReadOperation
    {
        /// <inheritdoc />
        public HomeReadSnapshot Snapshot => session.Snapshot;
        /// <inheritdoc />
        public Task ValidateAsync(CancellationToken cancellationToken = default) => session.ValidateAsync(cancellationToken);
        /// <inheritdoc />
        public ValueTask DisposeAsync() => session.DisposeAsync();
    }
}

/// <summary>Disposes each superseded read before retrying, so pending maintenance cannot deadlock on the old lease.</summary>
internal static class HomePublicationReadLoop
{
    /// <summary>Preserves one original file and day while period changes retire only the current projection.</summary>
    internal static async Task RunAsync(IHomeReadOperationSource source, DateOnly today, Func<int> period,
        Action<string> requireOriginalFile, Func<IHomeReadOperation, Task<bool>> project)
    {
        while (true)
        {
            await using var operation = await source.BeginAsync(today, period());
            requireOriginalFile(operation.Snapshot.DatabasePath);
            if (operation.Snapshot.PeriodIndex != period()) continue;
            if (await project(operation)) return;
        }
    }
}
