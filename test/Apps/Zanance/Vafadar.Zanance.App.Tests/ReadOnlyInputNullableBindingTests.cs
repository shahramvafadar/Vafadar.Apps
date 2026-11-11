using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Features.Goals;
using Vafadar.Zanance.App.Features.Holdings;
using Vafadar.Zanance.App.Features.Sync;
using Vafadar.Zanance.Core.Goals;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Sync;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Original first/return reads must tolerate incomplete native drafts without transport or financial effects.</summary>
[Collection("Entry validation units")]
public sealed class ReadOnlyInputNullableBindingTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    /// <summary>Missing connection inputs use defaults only before the first read and only when both are absent.</summary>
    [Theory]
    [InlineData("device")]
    [InlineData("address")]
    [InlineData("fingerprint")]
    [InlineData("both")]
    [InlineData("return-device")]
    [InlineData("empty")]
    [InlineData("return-empty")]
    [InlineData("partial-address")]
    [InlineData("partial-fingerprint")]
    public async Task Original_sync_read_handles_missing_drafts_without_opening_transport(string scenario)
    {
        using var f = new FlowFixture(); await f.Store.GetSettingsAsync(Ct);
        using var environment = new PersonalSyncFlowTests.EnvironmentPort();
        var model = new PersonalSyncViewModel(f.Services.GetRequiredService<PersonalSyncStore>(),
            f.Services.GetRequiredService<PersonalSyncCoordinator>(), environment, f.Platform,
            f.Translator, new DateFormatter(f.Localization), f.Localization);
        var before = await GoalChooserFlowTests.SnapshotAsync(f);
        try
        {
            if (scenario.StartsWith("return", StringComparison.Ordinal))
            { await model.ActivateAsync(); Assert.True(model.IsReady); model.Deactivate(); }
            switch (scenario)
            {
                case "device": case "return-device": model.DeviceName = null!; break;
                case "address": model.Address = null!; break;
                case "fingerprint": model.Fingerprint = null!; break;
                case "both": model.Address = null!; model.Fingerprint = null!; break;
                case "partial-address": model.Address = null!; model.Fingerprint = "retained public draft"; break;
                case "partial-fingerprint": model.Address = "retained public draft"; model.Fingerprint = null!; break;
                case "empty": case "return-empty": model.DeviceName = string.Empty; break;
                default: throw new InvalidOperationException();
            }
            await model.ActivateAsync();
            Assert.True(model.IsReady); Assert.True(model.CanInteract); Assert.Null(model.Error);
            Assert.Equal(f.Translator["Sync_DefaultDevice"], model.DeviceName);
            var partial = scenario.StartsWith("partial", StringComparison.Ordinal);
            Assert.Equal(partial ? 0 : 1, environment.Reads);
            if (scenario == "partial-address") { Assert.Null(model.Address); Assert.Equal("retained public draft", model.Fingerprint); }
            else if (scenario == "partial-fingerprint") { Assert.Equal("retained public draft", model.Address); Assert.Null(model.Fingerprint); }
            else { Assert.Equal("https://127.0.0.1:5048/", model.Address); Assert.Equal(environment.Fingerprint, model.Fingerprint); }
            Assert.Equal(0, environment.Opens); Assert.Equal(0, environment.RememberedPeers); Assert.Equal(0, environment.ConnectionsChanged);
            Assert.False(model.HasPendingOperation); Assert.Empty(f.Platform.ConfirmationDialogs);
            Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
        }
        finally { model.Deactivate(); }
    }

    /// <summary>Clearing a read-only goal amount cannot turn an unchanged actual source reload into a failed read.</summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task Original_goal_detail_reload_keeps_cleared_amount_without_read_failure(string? raw)
    {
        using var f = new FlowFixture(); await f.Store.GetSettingsAsync(Ct);
        var goal = new Goal { Name = "Read-only original goal", Type = GoalType.Earmark, CurrencyCode = "EUR", TargetAmount = 1000 };
        await using (var db = await f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct))
        { db.Goals.Add(goal); await db.SaveChangesAsync(Ct); }
        var dates = new DateFormatter(f.Localization); var holdings = f.Services.GetRequiredService<HoldingStore>();
        var presenter = new GoalPresenter(f.Translator, dates, f.Localization, holdings, new HoldingText(f.Translator, f.Localization, dates));
        var model = new GoalDetailViewModel(f.Store, f.Services.GetRequiredService<GoalStore>(), f.Services.GetRequiredService<PlanStore>(),
            holdings, presenter, f.Translator, dates, f.Localization, f.Time, f.Platform);
        try
        {
            model.ApplyQueryAttributes(new Dictionary<string, object> { ["id"] = goal.Id }); await model.LoadAsync();
            Assert.True(model.CanView); Assert.False(model.NotFound); var before = await GoalChooserFlowTests.SnapshotAsync(f);
            model.AmountText = raw!; await model.LoadAsync();
            Assert.Equal(raw, model.AmountText); Assert.True(model.CanView); Assert.False(model.HasReadFailed);
            Assert.False(model.CanAllocate); Assert.Empty(f.Platform.ConfirmationDialogs);
            Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
        }
        finally { model.Retire(); }
    }
}
