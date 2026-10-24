using System.Diagnostics;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.Performance;

/// <summary>Separates the original complete Home reader and final validator without replaying financial writes.</summary>
internal static class HomeReadStages
{
    /// <summary>Records first and one subsequent observation on the same retained original protected generation.</summary>
    internal static async Task<object> MeasureAsync(ZananceStore store, int expectedEntries, DateOnly today,
        CancellationToken cancellationToken)
    {
        var observations = new List<object>();
        string? firstPacket = null;
        for (var observation = 0; observation < 2; observation++)
        {
            // Keep serialization and the preservation oracle outside both operation timers. These are
            // store boundaries, not Home view-model projection, native rendering or cold-start timing.
            var clock = Stopwatch.StartNew();
            var packet = await store.GetHomeSnapshotAsync(today, 0, cancellationToken);
            var readMilliseconds = clock.Elapsed.TotalMilliseconds;
            clock.Restart();
            await store.ValidateHomeSnapshotAsync(packet, cancellationToken);
            var validationMilliseconds = clock.Elapsed.TotalMilliseconds;
            clock.Stop();
            if (packet.Entries.Count != expectedEntries)
                throw new IOException("The original complete Home entry count changed during stage diagnosis.");
            var captured = HomeReadWorkload.Capture(packet);
            if (firstPacket is not null && captured != firstPacket)
                throw new IOException("The complete Home financial or work packet changed during stage diagnosis.");
            firstPacket = captured;
            observations.Add(new
            {
                Observation = observation == 0 ? "FirstForThisShapeInThisProcess" : "SubsequentForThisShapeInThisProcess",
                CompleteReadMilliseconds = readMilliseconds,
                FinalValidationMilliseconds = validationMilliseconds,
                packet.Entries.Count,
                Accounts = packet.QuickResources.Accounts.Count,
                Plans = packet.Plans.Schedules.Count,
                CompletePacketMatchesFirst = true,
            });
        }
        return new { Observations = observations, FinancialWritesDispatched = 0,
            UiProjectionMeasured = false, NativeRenderingMeasured = false, ColdStartMeasured = false };
    }
}
