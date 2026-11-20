using Vafadar.Zanance.Core.Analysis;
using Vafadar.Zanance.Core.Forecasts;

namespace Vafadar.Zanance.Core.Tests;

/// <summary>Checks disclosed forecast summaries against a complete synthetic event list.</summary>
public sealed class AiForecastContractTests
{
    /// <summary>Only a summary supported by all of its events can be disclosed as a complete path.</summary>
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public void Complete_forecast_rejects_inconsistent_path(int mutation)
    {
        var start = new DateOnly(2026, 11, 20);
        var second = start.AddDays(1);
        var third = start.AddDays(2);
        var baseline = new AiForecastPath(700, 100, -800, third, second, -300, 1, 6,
        [
            new(third.AddDays(1), 900, ForecastSource.Plan, false, false, false, false),
            new(second, -1700, ForecastSource.Plan, false, false, false, false),
            new(second, 700, ForecastSource.FutureEntry, false, false, false, false),
            new(third, -500, ForecastSource.Plan, false, false, false, false),
            new(second, null, ForecastSource.Plan, false, false, false, false),
            new(third.AddDays(1), null, ForecastSource.Plan, false, false, false, false),
        ]) with { UnknownCount = 2 };
        var scenario = baseline with { Events = [.. baseline.Events.Take(5),
            new(third.AddDays(4), null, ForecastSource.Plan, false, true, true, false)], UnknownCount = 1 };
        var period = new AiPeriod(start, third.AddDays(1), 1, false, [new("EUR", 0, 0, 0, 0)], 0);
        var packet = new AiPayload(4, AiPurpose.PeriodComparison, "en", period, null, Forecast: new("EUR", baseline, scenario));
        AiProtocol.Validate(packet);
        // Missing rows may explain both the undisclosed cash flow and the undisclosed unknown amounts.
        AiProtocol.Validate(packet with { Forecast = new("EUR", baseline with { Events = [baseline.Events[0]] }, null) });

        var inconsistent = mutation switch
        {
            1 => baseline with { Closing = 150 },
            2 => baseline with { Minimum = -900 },
            3 => baseline with { FirstShortfallBalance = -350 },
            4 => baseline with { UnknownCount = 3 },
            _ => baseline with { FirstShortfallOn = third, FirstShortfallBalance = -800 },
        };
        Assert.Throws<ArgumentException>(() => AiProtocol.Validate(packet with { Forecast = new("EUR", inconsistent, scenario) }));
    }
}
