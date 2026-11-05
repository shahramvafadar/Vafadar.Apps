using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Features.Holdings;
using Vafadar.Zanance.Core.Holdings;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Actual unsaved holding drafts use chronological per-location quantities and retain overflow refusal.</summary>
public sealed class HoldingChronologicalPreviewFlowTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    /// <summary>Valid correction and transfer drafts publish exact effects; a true total overflow has no guessed preview.</summary>
    [Theory]
    [InlineData("correction")]
    [InlineData("move")]
    [InlineData("overflow")]
    public async Task Original_draft_preview_preserves_source_and_checked_location_effects(string kind)
    {
        using var f = new FlowFixture();
        f.Time.Now = new(2026, 11, 6, 9, 0, 0, TimeSpan.Zero);
        var type = new AssetType(Id(1)) { Name = "Chronological preview", Kind = AssetKind.CountableItem,
            Dimension = AssetDimension.Count, Divisible = true, CountUnitName = "tokens", PriceCurrencyCode = "EUR" };
        var places = new[] { new AssetLocation(Id(2)) { Name = "Fictitious vault" }, new AssetLocation(Id(3)) { Name = "Fictitious box" } };
        var events = new[]
        {
            new AssetEvent(Id(4)) { AssetTypeId = type.Id, LocationId = Id(2), Kind = AssetEventKind.Opening, Quantity = long.MaxValue, Date = new(2026, 11, 1) },
            new AssetEvent(Id(5)) { AssetTypeId = type.Id, LocationId = Id(2), Kind = AssetEventKind.GiftReceived, Quantity = long.MaxValue, Date = new(2026, 11, 3) },
            new AssetEvent(Id(6)) { AssetTypeId = type.Id, LocationId = Id(2), Kind = AssetEventKind.Outflow, Quantity = long.MaxValue, Date = new(2026, 11, 2) },
            new AssetEvent(Id(7)) { AssetTypeId = type.Id, LocationId = Id(2), ToLocationId = Id(3), Kind = AssetEventKind.LocationTransfer, Quantity = 1000, Date = new(2026, 11, 4) },
        };
        Assert.Null(HoldingsLedger.FindConflict(events));
        await using (var db = await f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct))
        {
            db.AssetTypes.Add(type); db.AssetLocations.AddRange(places); db.AssetEvents.AddRange(events);
            await db.SaveChangesAsync(Ct);
        }

        var before = await GoalChooserFlowTests.SnapshotAsync(f);
        var vm = new AssetEventEditorViewModel(f.Services.GetRequiredService<HoldingStore>(), f.Store,
            new HoldingText(f.Translator, f.Localization, new DateFormatter(f.Localization)), f.Translator, f.Localization, f.Time, f.Platform);
        var query = new Dictionary<string, object> { ["type"] = type.Id, ["kind"] = "Correction" };
        if (kind == "move") query["id"] = Id(7);
        await vm.ApplyQueryAsync(query);
        vm.Location = vm.Locations.Single(p => p.Id == Id(2));
        if (kind == "move") vm.ToLocation = vm.Locations.Single(p => p.Id == Id(3));
        vm.Date = kind == "move" ? new(2026, 11, 4) : new(2026, 11, 5);
        vm.DirectionIndex = kind == "overflow" ? 0 : 1;
        vm.QuantityText = kind == "move" ? "2.000" : "0.001";
        vm.ReasonText = "Fictitious preview only"; vm.Note = "Retained draft";

        Assert.True(vm.Loading.IsReady); Assert.True(vm.CanEdit); Assert.True(vm.IsDirty);
        Assert.False(vm.HasMoney); Assert.Null(vm.Error);
        Assert.Equal(kind == "move" ? "2.000" : "0.001", vm.QuantityText);
        Assert.Equal("Fictitious preview only", vm.ReasonText); Assert.Equal("Retained draft", vm.Note);
        if (kind == "overflow") Assert.Null(vm.EffectText);
        else
        {
            Assert.NotNull(vm.EffectText);
            Assert.Contains(kind == "move" ? "9,223,372,036,854,773.807" : "9,223,372,036,854,774.806", vm.EffectText, StringComparison.Ordinal);
            Assert.Contains("Fictitious vault", vm.EffectText, StringComparison.Ordinal);
            if (kind == "move") { Assert.Contains("2 tokens", vm.EffectText, StringComparison.Ordinal); Assert.Contains("Fictitious box", vm.EffectText, StringComparison.Ordinal); }
        }

        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
        Assert.Empty(f.Platform.Routes); Assert.Empty(f.Platform.Failures); Assert.Empty(f.Platform.Alerts);
        Assert.Empty(await f.Store.GetAccountsAsync(cancellationToken: Ct));
        Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
    }

    private static Guid Id(int number) => Guid.Parse("d8700000-0000-4000-8000-" + number.ToString("D12", System.Globalization.CultureInfo.InvariantCulture));
}
