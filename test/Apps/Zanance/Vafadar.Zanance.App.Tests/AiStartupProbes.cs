using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Vafadar.Localization;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Features.Analysis;
using Vafadar.Zanance.App.Interaction;
using Vafadar.Zanance.Core.Analysis;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Analysis;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Portable original-model preparation from caller-owned metadata, without a ledger entry, real provider or authentication answer.</summary>
internal static class AiStartupPreparation
{
    internal static async Task<AiStartupPrepared> CreateAsync(string operation, IDbContextFactory<ZananceDbContext> original,
        ICommercialWriteAccessSource access, Translator text, ILocalizationService locale, TimeProvider clock, IAppInteraction port)
    {
        var factory = new AiStartupSourceProbe(original); var runtime = new AiStartupRuntimeProbe(clock);
        var model = new AiAnalysisViewModel(new AiSnapshotStore(factory, access), text, locale, new DateFormatter(locale), clock, port, runtime);
        if (operation != "load")
        {
            await model.LoadAsync(); if (!model.Loaded || model.Accounts.Count != 1) throw new InvalidDataException("One original metadata account required.");
            if (operation != "provider") model.Accounts[0].Selected = true;
            if (operation is "share" or "quote" or "send" or "continue" or "close")
            { await model.ReviewAsync(); if (model.Disclosure is null) throw new InvalidDataException("Original zero-activity review required."); }
            if (operation is "continue" or "close")
            {
                var payload = JsonSerializer.Deserialize(model.Disclosure!, AiJson.Default.AiPayload) ?? throw new InvalidDataException();
                var terms = await runtime.Remote.OfferAsync(payload.Purpose, CancellationToken.None);
                // A declared fictitious pending input, never an accepted request, credit reservation or private intent write.
                runtime.Remote.Record = new(1, new(Guid.NewGuid(), terms, payload), null);
            }
            if (operation is "quote" or "send" or "continue" or "close") await model.CheckProviderAsync();
            if (operation == "send") await model.QuoteAsync();
        }
        var ready = operation switch
        {
            "load" => !model.HasPendingOperation, "review" => model.Loaded,
            "provider" => model.CanCheckProvider, "share" => model.CanShare, "quote" => model.CanQuote,
            "send" => model.CanSend, "continue" or "close" => model.CanContinue, _ => false
        };
        if (!ready) throw new InvalidDataException("Original operation preparation refused.");
        runtime.Reset(true); factory.Calls = 0; factory.Refuse = true; return new(model, factory, runtime);
    }

    internal static Task InvokeAsync(AiAnalysisViewModel model, string operation) => operation switch
    {
        "load" => model.LoadAsync(), "review" => model.ReviewAsync(), "provider" => model.CheckProviderAsync(),
        "share" => model.ShareAsync(), "quote" => model.QuoteAsync(), "send" => model.SendAsync(),
        "continue" => model.ContinueAsync(), "close" => model.CloseAsync(), _ => throw new ArgumentOutOfRangeException(nameof(operation))
    };
}

internal sealed record AiStartupPrepared(AiAnalysisViewModel Model, AiStartupSourceProbe Factory, AiStartupRuntimeProbe Runtime);

/// <summary>Counts actual source-factory entry and refuses before a connection/read while armed.</summary>
internal sealed class AiStartupSourceProbe(IDbContextFactory<ZananceDbContext> original) : IDbContextFactory<ZananceDbContext>
{
    internal bool Refuse; internal int Calls;
    public ZananceDbContext CreateDbContext()
    { Calls++; if (Refuse) throw new IOException("D958 original source boundary"); return original.CreateDbContext(); }
    public Task<ZananceDbContext> CreateDbContextAsync(CancellationToken ct = default)
    { Calls++; return Refuse ? Task.FromException<ZananceDbContext>(new IOException("D958 original source boundary")) : original.CreateDbContextAsync(ct); }
}

/// <summary>No HTTP, identity or native adapter exists here; counters refuse attempted startup effects rather than manufacturing consent.</summary>
internal sealed class AiStartupRuntimeProbe(TimeProvider clock) : IAiRuntime
{
    internal readonly AiStartupRemoteProbe Remote = new(clock);
    internal bool Refuse, DenyAccess; internal int Captures, Validations, Authorizations, Opens, Shares;
    internal Func<Task>? DuringOpen;
    internal int BoundaryCalls => Captures + Validations + Authorizations + Opens + Shares + Remote.Calls;
    public bool NetworkAvailable => true;
    internal void Reset(bool refuse) { Captures = Validations = Authorizations = Opens = Shares = 0; Remote.Reset(); Refuse = Remote.Refuse = refuse; }
    public AiUiAccess Capture(string originalFile)
    { Captures++; if (Refuse) throw new IOException("D958 local access capture refused"); return new(() => { Validations++; if (DenyAccess) throw new OperationCanceledException("D958 captured access denied"); }, CancellationToken.None); }
    public Task<bool> ConfirmDisclosureAsync()
    { Authorizations++; return Task.FromException<bool>(new InvalidOperationException("D958 no genuine authentication requested or answered")); }
    public Task ShareAsync(string exactText) { Shares++; return Task.FromException(new InvalidOperationException("D958 no recipient or share")); }
    public async Task<IAiRemote> OpenAsync(string originalFile, CancellationToken ct)
    { Opens++; if (Refuse) throw new IOException("D958 identity/provider entry refused"); if (DuringOpen is { } effect) await effect(); return Remote; }
}

/// <summary>Metadata-only fictitious quantities and declared pending input; primary work is always refused, never billed or persisted.</summary>
internal sealed class AiStartupRemoteProbe(TimeProvider clock) : IAiRemote
{
    internal bool Refuse; internal int Calls; internal AiClientRecord? Record;
    internal int AvailabilityReads, RecordReads; internal Func<Task>? DuringAvailability;
    internal void Reset() { Calls = AvailabilityReads = RecordReads = 0; }
    private static AiBalance Balance => new(0, 100, 0, 0);
    public string Destination => "https://localhost/unused-startup-fixture";
    public async Task<AiAvailability> AvailabilityAsync(CancellationToken ct)
    { Calls++; AvailabilityReads++; if (Refuse) throw new IOException("D958 metadata refused"); if (DuringAvailability is { } effect) await effect(); return new(true, "fictitious-startup", "fixture-only", Balance); }
    public Task<AiClientRecord?> ReadAsync(CancellationToken ct)
    { Calls++; RecordReads++; return Refuse ? Task.FromException<AiClientRecord?>(new IOException("D958 private-record boundary refused")) : Task.FromResult(Record); }
    public Task<AiOffer> OfferAsync(AiPurpose purpose, CancellationToken ct)
    { Calls++; return Refuse ? Task.FromException<AiOffer>(new IOException("D958 quote entry refused")) : Task.FromResult(new AiOffer(Guid.NewGuid(), "fictitious-startup", "fixture-only", purpose, 8, clock.GetUtcNow().AddHours(1))); }
    public Task<AiRequestResult> SubmitAsync(AiOffer offer, AiPayload payload, Func<Task> recheck, CancellationToken ct)
    { Calls++; return Task.FromException<AiRequestResult>(new InvalidOperationException("D958 no submission, intent write, reservation or model run")); }
    public Task<AiRequestResult> ContinueAsync(CancellationToken ct)
    { Calls++; return Task.FromException<AiRequestResult>(new InvalidOperationException("D958 no genuine continuation")); }
    public Task<AiRequestResult> CloseAsync(CancellationToken ct)
    { Calls++; return Task.FromException<AiRequestResult>(new InvalidOperationException("D958 no genuine closure")); }
    public void Dispose() { }
}
