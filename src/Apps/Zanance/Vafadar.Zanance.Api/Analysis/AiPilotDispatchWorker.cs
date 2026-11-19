using Npgsql;

namespace Vafadar.Zanance.Api.Analysis;

/// <summary>Continues only the isolated pilot's durable, never-claimed creates after service restart.</summary>
internal sealed class AiPilotDispatchWorker(AiRequestStore store, AiPilotOpenAiNotices notices, ILogger<AiPilotDispatchWorker> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(5);

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var ticks = new PeriodicTimer(Interval);
        while (await ticks.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await notices.DrainAsync(stoppingToken);
                await store.DrainPilotDispatchesAsync(stoppingToken);
            }
            catch (Exception error) when (error is NpgsqlException or IOException or TimeoutException)
            {
                logger.LogWarning("The synthetic analysis queue could not be read or claimed.");
            }
        }
    }
}
