using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Features.Holdings;
using Vafadar.Zanance.Core.Holdings;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Tests;

/// <summary>AT-421: original complete event-form loading and routed draft identity.</summary>
[Trait("AT", "AT-421")]
public sealed class HoldingEventPublicationFlowTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task A_different_original_type_receives_a_blank_financial_draft_instead_of_the_previous_types_values()
    {
        using var f = new FlowFixture(); var (model, store, _, _) = await HoldingLocationCreationFlowTests.ReadyAsync(f);
        var next = new AssetType { Name = "Next original item", Kind = AssetKind.CountableItem, Dimension = AssetDimension.Count, PriceCurrencyCode = "USD" };
        await store.SaveTypeAsync(next, Ct); model.AmountText = "77"; model.FeeText = "4"; model.SecondAmountText = "80"; model.ReasonText = "Old reason";
        await model.ApplyQueryAsync(new Dictionary<string, object> { ["type"] = next.Id, ["kind"] = "Opening" });
        Assert.Equal(next.Name, model.TypeName); Assert.Empty(model.QuantityText); Assert.Empty(model.BasisText);
        Assert.Empty(model.Note); Assert.Empty(model.AmountText); Assert.Empty(model.FeeText); Assert.Empty(model.SecondAmountText); Assert.Empty(model.ReasonText);
        Assert.Empty(await store.GetEventsAsync(cancellationToken: Ct)); Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
    }

    [Fact]
    public async Task A_missing_original_event_never_initializes_settings_or_creates_a_default_place()
    {
        using var f = new FlowFixture(); var store = f.Services.GetRequiredService<HoldingStore>();
        var type = new AssetType { Name = "Original missing-event type", Kind = AssetKind.CountableItem, Dimension = AssetDimension.Count, PriceCurrencyCode = "EUR" };
        await store.SaveTypeAsync(type, Ct);
        var model = new AssetEventEditorViewModel(store, f.Store, new HoldingText(f.Translator, f.Localization, new DateFormatter(f.Localization)),
            f.Translator, f.Localization, f.Time, f.Platform);
        try { await model.ApplyQueryAsync(new Dictionary<string, object> { ["id"] = Guid.NewGuid() }); }
        catch (HoldingEventReviewChangedException) { /* The baseline throws; either outcome must preserve the actual original rows. */ }
        await using var db = await f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct);
        Assert.Empty(await db.AssetLocations.ToListAsync(Ct)); Assert.Empty(await db.Settings.ToListAsync(Ct));
        Assert.False(model.CanSave); Assert.Empty(await db.AssetEvents.ToListAsync(Ct));
    }

    [Fact]
    public async Task An_original_read_failure_is_a_closed_form_outcome_instead_of_an_escaping_query_exception()
    {
        using var f = new FlowFixture(); var (model, _, type, factory) = await HoldingLocationCreationFlowTests.ReadyAsync(f);
        factory.Before = _ => throw new IOException("Original event source unavailable");
        var error = await Record.ExceptionAsync(() => model.ApplyQueryAsync(new Dictionary<string, object> { ["type"] = type.Id, ["kind"] = "Opening" }));
        Assert.Null(error); Assert.False(model.CanSave); Assert.Equal("2", model.QuantityText); Assert.Equal("50", model.BasisText);
    }
}
