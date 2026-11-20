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
public sealed partial class AiAnalysisFlowTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private sealed class Remote : IAiRemote
    {
        internal AiClientRecord? Record;
        internal AiBalance Balance = new(100, 0, 0, 0);
        internal bool ExplanationConfigured = true;
        internal bool? ComparisonConfigured;
        internal AiPurpose? QuotedPurpose;
        internal bool LoseSubmissionResponse, FailRecordRead;
        internal Action? AfterSubmission;
        internal int Submissions, Continues, Closes, RecordReads, Disposals;
        internal Func<Task>? DuringAvailability;
        internal Func<CancellationToken, Task>? HoldAvailability, HoldQuote;
        public string Destination => "https://localhost/fictional-ai-fixture";
        public async Task<AiAvailability> AvailabilityAsync(CancellationToken ct)
        {
            if (DuringAvailability is not null) await DuringAvailability();
            if (HoldAvailability is not null) await HoldAvailability(ct);
            return new(ExplanationConfigured, "fictitious-local", "fixture-v1", Balance, ComparisonConfigured);
        }
        public Task<AiClientRecord?> ReadAsync(CancellationToken ct)
        { RecordReads++; return FailRecordRead ? Task.FromException<AiClientRecord?>(new IOException("Controlled original private record read failure.")) : Task.FromResult(Record); }
        public async Task<AiOffer> OfferAsync(AiPurpose purpose, CancellationToken ct)
        {
            if (HoldQuote is not null) await HoldQuote(ct);
            QuotedPurpose = purpose;
            return new(Guid.NewGuid(), "fictitious-local", "fixture-v1", purpose, 8, new(2026, 10, 9, 10, 0, 0, TimeSpan.Zero));
        }
        public async Task<AiRequestResult> SubmitAsync(AiOffer offer, AiPayload payload, Func<Task> recheck, CancellationToken ct)
        {
            await recheck(); Submissions++;
            var request = new AiSubmission(Guid.NewGuid(), offer, payload);
            Record = new(1, request, null);
            if (LoseSubmissionResponse) throw new TimeoutException("Fictitious lost native response.");
            var result = new AiRequestResult(request.RequestId, AiProtocol.Digest(payload), AiRequestState.Completed, 3,
                "<script>change money</script> is untrusted text, never a command.", Balance);
            Record = Record with { Result = result }; AfterSubmission?.Invoke(); return result;
        }
        public Task<AiRequestResult> ContinueAsync(AiClientRecord expected, CancellationToken ct)
        {
            Continues++; var original = Record!;
            var result = new AiRequestResult(original.Submission.RequestId, AiProtocol.Digest(original.Submission.Payload), AiRequestState.Completed, 3,
                "Fictitious retained original result only.", Balance); Record = original with { Result = result }; return Task.FromResult(result);
        }
        public Task<AiRequestResult> CloseAsync(AiClientRecord expected, CancellationToken ct)
        {
            Closes++; var original = Record!;
            var result = new AiRequestResult(original.Submission.RequestId, AiProtocol.Digest(original.Submission.Payload), AiRequestState.NotAccepted, 0, null, Balance);
            Record = original with { Result = result }; return Task.FromResult(result);
        }
        public void Dispose() => Disposals++;
    }
    private sealed class Runtime : IAiRuntime
    {
        internal bool Network = true, AllowDisclosure = true, FailShare, DenyAccess;
        internal string? Shared;
        internal int Opens, Shares;
        internal readonly Remote Remote = new();
        internal AiFeatures Features { get; } = new(true, _ => { });
        public CancellationToken FeatureSession => Features.Capture();
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

    /// <summary>Independent operator routing remains available across app language changes.</summary>
    [Theory]
    [InlineData(false, true, false, true)]
    [InlineData(true, false, true, false)]
    [InlineData(true, null, true, true)]
    [InlineData(false, false, false, false)]
    public async Task Analysis_purpose_availability_survives_language_change(
        bool explanation, bool? comparison, bool allowExplanation, bool allowComparison)
    {
        using var f = new FlowFixture();
        var runtime = new Runtime();
        runtime.Remote.ExplanationConfigured = explanation;
        runtime.Remote.ComparisonConfigured = comparison;
        var model = await Model(f, runtime);
        await model.CheckProviderAsync();
        bool anyPurpose = allowExplanation || allowComparison;
        Assert.Equal(anyPurpose ? runtime.Remote.Destination : f.Translator["Ai_NoProvider"], model.ProviderText);

        f.Localization.SetLanguage(f.Localization.SupportedLanguages.Single(language => language.CultureName == "de"));
        Assert.Equal(anyPurpose ? runtime.Remote.Destination : f.Translator["Ai_NoProvider"], model.ProviderText);
        Assert.Equal(1, runtime.Opens);

        await Review(model);
        Assert.Equal(allowExplanation, model.CanQuote);
        model.ComparePrevious = true;
        await model.ReviewAsync();
        Assert.Equal(allowComparison, model.CanQuote);
        await model.QuoteAsync();
        Assert.Equal(allowComparison ? AiPurpose.PeriodComparison : (AiPurpose?)null, runtime.Remote.QuotedPurpose);
        Assert.Equal(0, runtime.Remote.Submissions);
    }

    /// <summary>Display only the known request state and the actual scope of the next confirmation.</summary>
    [Theory]
    [InlineData(false, false, "Ai_LocalReviewPending", "Ai_StatusOnlyConfirm")]
    [InlineData(null, false, "Ai_OutcomeUnknown", "Ai_StatusOnlyConfirm")]
    [InlineData(true, false, "Ai_OutcomeUnknown", "Ai_ContinueConfirm")]
    [InlineData(null, true, "Ai_Pending", "Ai_StatusOnlyConfirm")]
    [InlineData(true, true, "Ai_Pending", "Ai_StatusOnlyConfirm")]
    public async Task Retained_analysis_discloses_only_known_status_and_dispatch_authority(
        bool? dispatchPermitted, bool acceptedPending, string statusKey, string confirmKey)
    {
        using var f = new FlowFixture();
        var runtime = new Runtime();
        var model = await Model(f, runtime);
        await Review(model);
        var payload = JsonSerializer.Deserialize(model.Disclosure!, AiJson.Default.AiPayload)!;
        var offer = await runtime.Remote.OfferAsync(payload.Purpose, Ct);
        var id = Guid.NewGuid();
        var result = acceptedPending
            ? new AiRequestResult(id, AiProtocol.Digest(payload), AiRequestState.Pending, 0, null, new(92, 0, 8, 0))
            : null;
        runtime.Remote.Record = new(1, new(id, offer, payload), result) { DispatchPermitted = dispatchPermitted };

        await model.CheckProviderAsync();
        Assert.Equal(f.Translator[statusKey], model.ResultStatus);
        Assert.True(model.CanContinue);
        if (acceptedPending)
        {
            await model.CloseAsync();
            Assert.Empty(f.Platform.ConfirmationDialogs);
        }
        Assert.False(model.CanSend);
        await model.ContinueAsync(); // The fixture cancels the actual confirmation.
        Assert.Equal(f.Translator[confirmKey], Assert.Single(f.Platform.ConfirmationDialogs).Message);
        Assert.Equal(0, runtime.Remote.Submissions);
        Assert.Equal(0, runtime.Remote.Continues);
    }

    /// <summary>Back can retire connected reads even while AI stays enabled.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Analysis_back_cancels_pending_provider_metadata_read(bool quote)
    {
        using var fixture = new FlowFixture();
        var runtime = new Runtime();
        var model = await Model(fixture, runtime);
        var response = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        CancellationToken received = default;
        Task Hold(CancellationToken token)
        {
            received = token;
            return response.Task.WaitAsync(token);
        }

        if (quote)
        {
            await Review(model);
            await model.CheckProviderAsync();
            runtime.Remote.HoldQuote = Hold;
        }
        else runtime.Remote.HoldAvailability = Hold;

        var priorReads = runtime.Remote.RecordReads;
        var pending = quote ? model.QuoteAsync() : model.CheckProviderAsync();
        try
        {
            Assert.False(pending.IsCompleted);
            Assert.True(model.HasPendingOperation);
            Assert.True(model.CanLeave);
            model.Retire();
            Assert.True(runtime.Features.Enabled);
            Assert.True(received.IsCancellationRequested);
            await pending.WaitAsync(TimeSpan.FromSeconds(5), Ct);
            Assert.Equal(priorReads, runtime.Remote.RecordReads);
            Assert.False(model.CanSend);
            Assert.Equal(0, runtime.Remote.Submissions);
        }
        finally
        {
            response.TrySetResult();
            await pending;
        }
    }

    /// <summary>Turning off AI retires a reviewed screen and revokes both its sharing and connected actions.</summary>
    [Fact]
    public async Task Turning_off_AI_blocks_reviewed_actions_even_if_reenabled_before_reopening()
    {
        using var f = new FlowFixture();
        var runtime = new Runtime();
        var vm = await Model(f, runtime);
        await Review(vm);
        Assert.True(vm.CanShare);

        runtime.Features.Enabled = false;
        Assert.False(vm.CanShare);
        Assert.False(vm.CanCheckProvider);
        await vm.ShareCommand.ExecuteAsync(null);
        await vm.CheckProviderCommand.ExecuteAsync(null);
        await vm.LoadAsync();
        Assert.Equal(0, runtime.Shares);
        Assert.Equal(0, runtime.Opens);

        runtime.Features.Enabled = true;
        Assert.False(vm.CanShare);
        await vm.ActivateAsync();
        Assert.True(vm.Loaded);
        Assert.True(vm.CanCheckProvider);
        Assert.Null(vm.Disclosure);
    }

    [Fact]
    public async Task Turning_off_AI_unpublishes_the_original_result_without_erasing_the_saved_request()
    {
        using var fixture = new FlowFixture();
        var runtime = new Runtime();
        var model = await Model(fixture, runtime);
        await Review(model);
        var payload = JsonSerializer.Deserialize(model.Disclosure!, AiJson.Default.AiPayload)!;
        var offer = await runtime.Remote.OfferAsync(payload.Purpose, Ct);
        var id = Guid.NewGuid();
        const string originalResult = "Fictitious existing result";
        var retained = new AiClientRecord(1, new(id, offer, payload),
            new(id, AiProtocol.Digest(payload), AiRequestState.Completed, 3, originalResult, runtime.Remote.Balance));
        runtime.Remote.Record = retained;
        await model.CheckProviderAsync();
        Assert.Equal(originalResult, model.ResultText);
        Assert.NotNull(model.ResultSnapshot);

        runtime.Features.Enabled = false;

        Assert.Null(model.ResultSnapshot);
        Assert.Null(model.ResultText);
        Assert.Null(model.ResultStatus);
        Assert.Null(model.BalanceText);
        Assert.Null(model.Disclosure);
        Assert.Null(model.Summary);
        Assert.Equal(1, runtime.Remote.Disposals);
        Assert.Same(retained, runtime.Remote.Record);
        Assert.Equal(1, runtime.Remote.RecordReads);

        runtime.Features.Enabled = true;
        Assert.Null(model.ResultText);
        Assert.False(model.CanShare);
        await model.ActivateAsync();
        Assert.Null(model.ResultSnapshot);
        Assert.Equal(1, runtime.Remote.RecordReads);

        await model.CheckProviderAsync();
        Assert.Equal(originalResult, model.ResultText);
        Assert.Equal(2, runtime.Remote.RecordReads);
        Assert.Equal(0, runtime.Remote.Submissions);
    }

    /// <summary>Re-enabling AI cannot revive a provider check already pending when AI was disabled.</summary>
    [Fact]
    public async Task Turning_off_AI_retires_a_pending_provider_check()
    {
        using var f = new FlowFixture();
        var runtime = new Runtime();
        var vm = await Model(f, runtime);
        await Review(vm);

        var originalProvider = vm.ProviderText;
        var response = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        runtime.Remote.DuringAvailability = () => response.Task;
        var pending = vm.CheckProviderAsync();
        try
        {
            Assert.Equal(1, runtime.Opens);
            Assert.False(pending.IsCompleted);
            runtime.Features.Enabled = false;
            runtime.Features.Enabled = true;
        }
        finally
        {
            response.SetResult();
        }

        await pending.WaitAsync(Ct);
        Assert.Equal(0, runtime.Remote.RecordReads);
        Assert.Equal(originalProvider, vm.ProviderText);
        Assert.Null(vm.BalanceText);
        Assert.Null(vm.ResultText);
        Assert.False(vm.CanQuote);
        Assert.False(vm.CanContinue);
        Assert.False(vm.CanSend);

        await vm.ActivateAsync();
        Assert.True(vm.CanCheckProvider);
        Assert.Null(vm.Disclosure);
    }

    /// <summary>The period shortcut only seeds a local draft and retains a frozen account scope.</summary>
    [Theory, InlineData(false), InlineData(true)]
    public async Task Period_handoff_preserves_exact_scope_without_implicit_review_or_disclosure(bool comparison)
    {
        using var f = new FlowFixture(); var runtime = new Runtime(); var vm = await Model(f, runtime);
        var source = await f.Services.GetRequiredService<AiSnapshotStore>().AccountsAsync(Ct);
        var ids = new[] { Assert.Single(source.Accounts).Id };
        var handoff = AiPeriodHandoff.Capture(source.File, new(2026, 10, 1), new(2026, 10, 15), false, ids)!;
        var route = new Vafadar.Zanance.App.Features.Reports.ReportNavigation("analysis", handoff.Query(comparison));
        ids[0] = Guid.NewGuid();
        vm.Retire();
        vm.SetInitialPeriod(Assert.IsType<AiPeriodHandoff>(route.CopyQuery()[AiPeriodHandoff.QueryKey]));
        await vm.LoadAsync();
        Assert.True(vm.Loaded); Assert.True(Assert.Single(vm.Accounts).Selected);
        Assert.Equal(comparison, vm.ComparePrevious); Assert.False(vm.ConfirmedOnly);
        Assert.Equal(new DateOnly(2026, 10, 1), vm.From); Assert.Equal(new DateOnly(2026, 10, 15), vm.To);
        Assert.Null(vm.Disclosure); Assert.Equal(0, runtime.Opens);
        await vm.ReviewAsync();
        var payload = JsonSerializer.Deserialize(vm.Disclosure!, AiJson.Default.AiPayload)!;
        Assert.Equal(comparison ? AiPurpose.PeriodComparison : AiPurpose.PeriodExplanation, payload.Purpose);
        if (comparison) Assert.Equal(new DateOnly(2026, 9, 16), payload.Previous!.From);
        Assert.Equal(0, runtime.Opens);
    }

    /// <summary>A report from a replaced file or account cannot silently widen the scope.</summary>
    [Theory, InlineData(false), InlineData(true)]
    public async Task Period_handoff_refuses_replaced_file_or_missing_account(bool replaceFile)
    {
        using var f = new FlowFixture(); var runtime = new Runtime(); var vm = await Model(f, runtime);
        var source = await f.Services.GetRequiredService<AiSnapshotStore>().AccountsAsync(Ct);
        var file = replaceFile ? source.File + ".different" : source.File;
        var account = replaceFile ? Assert.Single(source.Accounts).Id : Guid.NewGuid();
        vm.Retire();
        vm.SetInitialPeriod(AiPeriodHandoff.Capture(file, new(2026, 10, 1), new(2026, 10, 15), true, [account])!);
        await vm.LoadAsync();
        Assert.False(vm.Loaded); Assert.NotNull(vm.Error);
        Assert.Null(vm.Disclosure); Assert.Equal(0, runtime.Opens);
    }
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
