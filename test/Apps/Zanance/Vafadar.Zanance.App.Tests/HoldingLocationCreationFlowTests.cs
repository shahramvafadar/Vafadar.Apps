using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Features.Holdings;
using Vafadar.Zanance.Core.Holdings;
using Vafadar.Zanance.Core.Settings;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Tests;

/// <summary>AT-420: original new-location prompt, actual-file write and retained financial draft boundaries.</summary>
[Trait("AT", "AT-420")]
public sealed class HoldingLocationCreationFlowTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    internal static async Task<(AssetEventEditorViewModel Model, HoldingStore Store, AssetType Type, Factory Factory)> ReadyAsync(FlowFixture f)
    {
        var store = f.Services.GetRequiredService<HoldingStore>();
        await f.Store.UpdateSettingsAsync(s => s.Mode = ExperienceMode.Advanced, Ct);
        var type = new AssetType { Name = "Original place owner", Kind = AssetKind.CountableItem, Dimension = AssetDimension.Count, PriceCurrencyCode = "EUR" };
        await store.SaveTypeAsync(type, Ct); await store.EnsureDefaultLocationAsync("Original safe", Ct);
        var factory = new Factory(f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>());
        var reviewed = new HoldingStore(factory, f.Services.GetRequiredService<ICommercialWriteAccessSource>());
        var model = new AssetEventEditorViewModel(reviewed, f.Store, new HoldingText(f.Translator, f.Localization, new DateFormatter(f.Localization)),
            f.Translator, f.Localization, f.Time, f.Platform);
        await model.ApplyQueryAsync(new Dictionary<string, object> { ["type"] = type.Id, ["kind"] = "Opening" });
        model.QuantityText = "2"; model.BasisText = "50"; model.Note = "Original unchanged event draft";
        return (model, reviewed, type, factory);
    }

    [Fact]
    public async Task A_new_location_prompt_never_writes_into_a_later_opened_file()
    {
        using var f = new FlowFixture(); using var next = new FlowFixture(); var (model, _, _, factory) = await ReadyAsync(f);
        f.Platform.PendingInput = () =>
        {
            factory.Current = next.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>();
            return Task.FromResult<string?>("Original intended new place");
        };
        await model.AddLocationCommand.ExecuteAsync(null);
        Assert.DoesNotContain(await next.Services.GetRequiredService<HoldingStore>().GetLocationsAsync(Ct), l => l.Name == "Original intended new place");
        Assert.Equal("2", model.QuantityText); Assert.Equal("50", model.BasisText); Assert.Equal("Original unchanged event draft", model.Note);
        Assert.Empty(await next.Store.GetEntriesAsync(cancellationToken: Ct));
    }

    [Fact]
    public async Task Changed_original_type_during_prompt_refuses_the_location_before_persisting_it()
    {
        using var f = new FlowFixture(); var (model, store, type, _) = await ReadyAsync(f);
        f.Platform.PendingInput = async () => { type.Note = "Concurrent original type metadata"; await store.SaveTypeAsync(type, Ct); return "New place after changed source"; };
        await model.AddLocationCommand.ExecuteAsync(null);
        Assert.DoesNotContain(await store.GetLocationsAsync(Ct), l => l.Name == "New place after changed source");
        Assert.Equal("2", model.QuantityText); Assert.Equal("50", model.BasisText); Assert.False(model.CanSave);
        Assert.Empty(await store.GetEventsAsync(cancellationToken: Ct)); Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
    }

    [Fact]
    public async Task Original_prompt_owns_the_form_until_cancel_without_allowing_a_financial_save()
    {
        using var f = new FlowFixture(); var (model, store, _, _) = await ReadyAsync(f);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
        f.Platform.PendingInput = () => { entered.SetResult(); return release.Task; };
        var pending = model.AddLocationCommand.ExecuteAsync(null); await entered.Task.WaitAsync(Ct);
        var saveEnabledDuringPrompt = model.CanSave; release.SetResult(null); await pending;
        Assert.False(saveEnabledDuringPrompt); Assert.True(model.CanSave); Assert.Equal("2", model.QuantityText);
        Assert.Single(await store.GetLocationsAsync(Ct)); Assert.Empty(await store.GetEventsAsync(cancellationToken: Ct));
    }

    internal sealed class Factory(IDbContextFactory<ZananceDbContext> original) : IDbContextFactory<ZananceDbContext>
    {
        internal IDbContextFactory<ZananceDbContext> Current = original;
        internal Func<CancellationToken, Task>? Before { get; set; }
        public ZananceDbContext CreateDbContext() => Current.CreateDbContext();
        public async Task<ZananceDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
        { if (Before is { } before) await before(cancellationToken); return await Current.CreateDbContextAsync(cancellationToken); }
    }
}
