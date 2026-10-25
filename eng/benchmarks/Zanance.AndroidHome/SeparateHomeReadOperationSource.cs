using Vafadar.Zanance.App.Features.Home;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App;

/// <summary>Independent comparison adapter for the original two-context APIs; no copied query or projection algorithm.</summary>
internal sealed class SeparateHomeReadOperationSource(ZananceStore store) : IHomeReadOperationSource
{
    /// <inheritdoc />
    public async Task<IHomeReadOperation> BeginAsync(DateOnly today, int periodIndex, CancellationToken cancellationToken = default)
        => new Operation(store, await store.GetHomeSnapshotAsync(today, periodIndex, cancellationToken));

    /// <summary>The original public APIs close their own contexts, so the comparison has no retained connection.</summary>
    private sealed class Operation(ZananceStore store, HomeReadSnapshot snapshot) : IHomeReadOperation
    {
        /// <inheritdoc />
        public HomeReadSnapshot Snapshot => snapshot;
        /// <inheritdoc />
        public Task ValidateAsync(CancellationToken cancellationToken = default) => store.ValidateHomeSnapshotAsync(snapshot, cancellationToken);
        /// <inheritdoc />
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
