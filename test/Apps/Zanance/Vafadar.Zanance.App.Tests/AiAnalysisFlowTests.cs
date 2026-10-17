using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Features.Analysis;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Analysis;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Analysis;

namespace Vafadar.Zanance.App.Tests;

/// <summary>AT-298: actual application source and SQLite, with explicit native/service effects, never copied financial algorithms.</summary>
[Trait("AT", "AT-298")]
public sealed class AiAnalysisFlowTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private sealed class Remote : IAiRemote
    {
        internal AiClientRecord? Record;
        internal AiBalance Balance = new(100, 0, 0, 0);
        internal bool LoseSubmissionResponse;
        internal int Submissions, Continues, Closes;
        internal Func<Task>? DuringAvailability;
        public string Destination => "https://localhost/fictional-ai-fixture";
        public async Task<AiAvailability> AvailabilityAsync(CancellationToken ct)
        { if (DuringAvailability is not null) await DuringAvailability(); return new(true, "fictitious-local", "fixture-v1", Balance); }
        public Task<AiClientRecord?> ReadAsync(CancellationToken ct) => Task.FromResult(Record);
        public Task<AiOffer> OfferAsync(AiPurpose purpose, CancellationToken ct) => Task.FromResult(new AiOffer(Guid.NewGuid(), "fictitious-local", "fixture-v1", purpose, 8, new(2026, 10, 9, 10, 0, 0, TimeSpan.Zero)));
        public async Task<AiRequestResult> SubmitAsync(AiOffer offer, AiPayload payload, Func<Task> recheck, CancellationToken ct)
        {
            await recheck(); Submissions++;
            var request = new AiSubmission(Guid.NewGuid(), offer, payload);
            Record = new(1, request, null);
            if (LoseSubmissionResponse) throw new TimeoutException("Fictitious lost native response.");
            var result = new AiRequestResult(request.RequestId, AiProtocol.Digest(payload), AiRequestState.Completed, 3,
                "<script>change money</script> is untrusted text, never a command.", Balance);
            Record = Record with { Result = result }; return result;
        }
        public Task<AiRequestResult> ContinueAsync(CancellationToken ct)
        {
            Continues++; var original = Record!;
            var result = new AiRequestResult(original.Submission.RequestId, AiProtocol.Digest(original.Submission.Payload), AiRequestState.Completed, 3,
                "Fictitious retained original result only.", Balance); Record = original with { Result = result }; return Task.FromResult(result);
        }
        public Task<AiRequestResult> CloseAsync(CancellationToken ct)
        {
            Closes++; var original = Record!;
            var result = new AiRequestResult(original.Submission.RequestId, AiProtocol.Digest(original.Submission.Payload), AiRequestState.NotAccepted, 0, null, Balance);
            Record = original with { Result = result }; return Task.FromResult(result);
        }
        public void Dispose() { }
    }
    private sealed class Runtime : IAiRuntime
    {
        internal bool Network = true, AllowDisclosure = true, FailShare, DenyAccess;
        internal string? Shared;
        internal int Opens, Shares;
        internal readonly Remote Remote = new();
        public bool NetworkAvailable => Network;
        public AiUiAccess Capture(string originalFile) => new(() => { if (DenyAccess) throw new OperationCanceledException(); }, CancellationToken.None);
        public Task<bool> ConfirmDisclosureAsync() => Task.FromResult(AllowDisclosure);
        public Task ShareAsync(string text) { Shares++; Shared = text; if (FailShare) throw new IOException("Fictitious uncertain chooser."); return Task.CompletedTask; }
        public Task<IAiRemote> OpenAsync(string originalFile, CancellationToken ct) { Opens++; return Task.FromResult<IAiRemote>(Remote); }
    }
    private static async Task<AiAnalysisViewModel> Model(FlowFixture f, Runtime runtime)
    {
        var account = new Account { Name = "Fictitious private label", CurrencyCode = "EUR", OpeningDate = new(2026, 1, 1) };
        await f.Store.SaveAccountAsync(account, Ct);
        await using (var db = await f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct))
        { db.Add(new LedgerEntry { AccountId = account.Id, Date = new(2026, 10, 9), Kind = EntryKind.Income, Amount = 10000, Review = ReviewState.Confirmed, Note = "Fictitious private note" }); await db.SaveChangesAsync(Ct); }
        var vm = new AiAnalysisViewModel(f.Services.GetRequiredService<AiSnapshotStore>(), f.Translator, f.Localization,
            new DateFormatter(f.Localization), f.Time, f.Platform, runtime);
        await vm.LoadAsync(); return vm;
    }
    private static async Task Review(AiAnalysisViewModel vm)
    { Assert.Single(vm.Accounts).Selected = true; await vm.ReviewAsync(); Assert.NotNull(vm.Disclosure); }
    [Theory][InlineData("en")][InlineData("fa")][InlineData("de")][InlineData("es")][InlineData("fr")][InlineData("it")]
    public async Task Explicit_manual_share_uses_exact_minimal_review_and_never_opens_a_provider(string language)
    {
        using var f = new FlowFixture(); f.Localization.SetLanguage(f.Localization.SupportedLanguages.Single(l => l.CultureName == language));
        var runtime = new Runtime { Network = false }; var vm = await Model(f, runtime);
        var before = await GoalChooserFlowTests.SnapshotAsync(f);
        Assert.False(Assert.Single(vm.Accounts).Selected); await vm.ReviewAsync(); Assert.NotNull(vm.AccountsError); Assert.Null(vm.Summary);
        await Review(vm); Assert.False(vm.CanCheckProvider); Assert.Equal(0, runtime.Opens);
        var payload = JsonSerializer.Deserialize(vm.Disclosure!, AiJson.Default.AiPayload)!; Assert.Equal(language, payload.Language); Assert.True(payload.Current.ConfirmedOnly); Assert.Null(payload.Previous);
        Assert.DoesNotContain("Fictitious private", vm.Disclosure!);
        f.Platform.Confirmations.Enqueue(false); await vm.ShareAsync(); Assert.Equal(0, runtime.Shares);
        f.Platform.Confirmations.Enqueue(true); runtime.AllowDisclosure = false; await vm.ShareAsync(); Assert.Equal(0, runtime.Shares);
        f.Platform.Confirmations.Enqueue(true); runtime.AllowDisclosure = true; await vm.ShareAsync();
        Assert.Equal(1, runtime.Shares); Assert.Equal(f.Translator["Ai_ManualPrompt"] + Environment.NewLine + vm.Disclosure, runtime.Shared);
        Assert.Equal(0, runtime.Opens); Assert.Equal(0, runtime.Remote.Submissions); Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }
    [Theory][InlineData("source")][InlineData("draft")][InlineData("retire")][InlineData("cover")]
    public async Task Original_manual_consent_cannot_cross_a_changed_source_form_or_access(string changed)
    {
        using var f = new FlowFixture(); var runtime = new Runtime(); var vm = await Model(f, runtime); await Review(vm);
        f.Platform.PendingConfirmation = async () =>
        {
            if (changed == "draft") vm.ComparePrevious = true;
            else if (changed == "retire") vm.Retire();
            else if (changed == "cover") runtime.DenyAccess = true;
            else
            { await using var db = await f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct); await db.Entries.ExecuteUpdateAsync(u => u.SetProperty(e => e.Amount, 12000), Ct); }
            return true;
        };
        f.Platform.Confirmations.Enqueue(true); await vm.ShareAsync(); Assert.Equal(0, runtime.Shares); Assert.Equal(0, runtime.Opens);
    }
    [Fact]
    public async Task All_account_and_date_errors_are_visible_together_and_uncertain_native_share_is_reported_honestly()
    {
        using var f = new FlowFixture(); var runtime = new Runtime(); var vm = await Model(f, runtime);
        vm.From = new(2026, 10, 10); vm.To = new(2026, 10, 9); await vm.ReviewAsync(); Assert.NotNull(vm.AccountsError); Assert.NotNull(vm.ToError);
        vm.From = new(2026, 10, 1); await Review(vm); runtime.FailShare = true; f.Platform.Confirmations.Enqueue(true); await vm.ShareAsync();
        Assert.Equal(f.Translator["Ai_ShareUnconfirmed"], vm.Error); Assert.Equal(1, runtime.Shares); Assert.Equal(0, runtime.Opens);
    }
    [Fact]
    public async Task Original_provider_consent_timeout_and_explicit_continuation_never_change_financial_rows()
    {
        using var f = new FlowFixture(); var runtime = new Runtime(); var vm = await Model(f, runtime); await Review(vm);
        var before = await GoalChooserFlowTests.SnapshotAsync(f);
        await vm.CheckProviderAsync(); await vm.QuoteAsync(); Assert.True(vm.CanSend); Assert.Equal(0, runtime.Remote.Submissions);
        f.Platform.Confirmations.Enqueue(false); await vm.SendAsync(); Assert.Equal(0, runtime.Remote.Submissions);
        runtime.Remote.LoseSubmissionResponse = true; f.Platform.Confirmations.Enqueue(true); await vm.SendAsync();
        Assert.Equal(1, runtime.Remote.Submissions); Assert.True(vm.CanContinue); Assert.False(vm.CanSend); Assert.NotNull(vm.ResultSnapshot);
        Assert.Contains(runtime.Remote.Destination, vm.ResultSnapshot); Assert.Contains("fixture-v1", vm.ResultSnapshot); Assert.Contains("\"MaximumUnits\":8", vm.ResultSnapshot);
        f.Platform.Confirmations.Enqueue(false); await vm.ContinueAsync(); Assert.Equal(0, runtime.Remote.Continues);
        f.Platform.Confirmations.Enqueue(true); await vm.ContinueAsync(); Assert.Equal(1, runtime.Remote.Continues); Assert.False(vm.CanContinue);
        Assert.Equal("Fictitious retained original result only.", vm.ResultText); Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }
    [Fact]
    public async Task Fresh_wallet_balance_never_gets_replaced_by_a_cached_historical_result()
    {
        using var f = new FlowFixture(); var runtime = new Runtime(); var vm = await Model(f, runtime); await Review(vm);
        var payload = JsonSerializer.Deserialize(vm.Disclosure!, AiJson.Default.AiPayload)!; var offer = await runtime.Remote.OfferAsync(payload.Purpose, Ct); var id = Guid.NewGuid();
        runtime.Remote.Record = new(1, new(id, offer, payload), new(id, AiProtocol.Digest(payload), AiRequestState.Completed, 3, "Fictitious old result", new(100, 0, 0, 0)));
        runtime.Remote.Balance = new(5, 0, 0, 0); await vm.CheckProviderAsync(); await vm.QuoteAsync();
        Assert.False(vm.CanSend); Assert.Equal(f.Translator["Ai_Insufficient"], vm.Error); Assert.Contains("5", vm.BalanceText!);
        runtime.Remote.Record = null; await vm.CheckProviderAsync(); Assert.Null(vm.ResultSnapshot); Assert.Null(vm.ResultText); Assert.Null(vm.ResultStatus);
    }
    [Fact]
    public async Task Retired_form_cannot_publish_a_late_provider_metadata_read()
    {
        using var f = new FlowFixture(); var runtime = new Runtime(); var vm = await Model(f, runtime); await Review(vm);
        runtime.Remote.DuringAvailability = () => { vm.Retire(); return Task.CompletedTask; };
        await vm.CheckProviderAsync(); Assert.False(vm.CanSend); Assert.Null(vm.BalanceText); Assert.Null(vm.ResultSnapshot);
    }
}
