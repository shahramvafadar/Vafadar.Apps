using Microsoft.Extensions.DependencyInjection;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Features.Analysis;
using Vafadar.Zanance.Data.Analysis;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Original local analysis ownership across native disclosure, row replacement and departure.</summary>
public sealed partial class AiAnalysisFlowTests
{
    [Fact]
    public async Task Changed_actual_manual_disclosure_language_cannot_share_old_native_consent()
    {
        using var f = new FlowFixture(); var runtime = new Runtime { Network = false };
        var vm = await Model(f, runtime); await Review(vm);
        f.Platform.PendingConfirmation = () =>
        {
            f.Localization.SetLanguage(f.Localization.SupportedLanguages.Single(x => x.CultureName == "de"));
            return Task.FromResult(true);
        };
        await vm.ShareAsync();
        Assert.Equal(0, runtime.Shares); Assert.Equal(0, runtime.Opens); vm.Retire();
    }

    [Fact]
    public async Task Replaced_original_account_row_cannot_invalidate_current_analysis_review()
    {
        using var f = new FlowFixture(); var runtime = new Runtime { Network = false };
        var vm = await Model(f, runtime); await Review(vm); var oldRow = Assert.Single(vm.Accounts);
        await vm.LoadAsync(); await Review(vm); var original = vm.Disclosure;
        Assert.NotSame(oldRow, Assert.Single(vm.Accounts)); oldRow.Selected = false;
        Assert.Equal(original, vm.Disclosure); Assert.True(vm.CanShare); vm.Retire();
    }

    [Fact]
    public async Task Retired_original_analysis_form_cannot_open_identity_from_direct_command()
    {
        using var f = new FlowFixture(); var runtime = new Runtime { Network = false };
        var vm = await Model(f, runtime); vm.Retire(); await vm.IdentityCommand.ExecuteAsync(null);
        Assert.Empty(f.Platform.Routes); Assert.Equal(0, runtime.Opens);
    }
    [Fact]
    public async Task Independent_manual_review_blocks_direct_reentry_when_shared_busy_is_cleared()
    {
        using var f = new FlowFixture(); var runtime = new Runtime { Network = false };
        var vm = await Model(f, runtime); await Review(vm);
        var pending = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        f.Platform.PendingConfirmation = () => pending.Task;
        var first = vm.ShareAsync(); vm.IsBusy = false; var second = vm.ShareAsync();
        var dialogs = f.Platform.ConfirmationDialogs.Count;
        pending.SetResult(false); await first; await second;
        Assert.Equal(1, dialogs); Assert.Equal(0, runtime.Shares); vm.Retire();
    }

    [Fact]
    public async Task Returned_original_analysis_result_survives_followup_private_record_read_failure()
    {
        using var f = new FlowFixture(); var runtime = new Runtime();
        var vm = await Model(f, runtime); await Review(vm); await vm.CheckProviderAsync(); await vm.QuoteAsync();
        runtime.Remote.AfterSubmission = () => runtime.Remote.FailRecordRead = true;
        f.Platform.Confirmations.Enqueue(true); await vm.SendAsync();
        Assert.Equal(1, runtime.Remote.Submissions); Assert.Equal(2, runtime.Remote.RecordReads);
        Assert.Equal(runtime.Remote.Record!.Result!.Text, vm.ResultText);
        Assert.Equal(f.Translator.Format("Ai_ResultUnits", "3"), vm.ResultStatus);
        Assert.NotNull(vm.ResultSnapshot); Assert.False(vm.CanSend); vm.Retire();
    }
    [Theory]
    [InlineData(false)][InlineData(true)]
    public async Task Replaced_actual_root_cannot_accept_original_analysis_native_consent(bool permanent)
    {
        using var f = new FlowFixture(); var runtime = new Runtime { Network = false };
        var vm = await Model(f, runtime); await Review(vm);
        f.Platform.PendingConfirmation = () =>
        {
            if (permanent) vm.RetireRoot(); else vm.BindRootOwner(() => false);
            return Task.FromResult(true);
        };
        await vm.ShareAsync(); Assert.Equal(0, runtime.Shares); Assert.False(vm.CanShare);
        if (permanent) { vm.BindRootOwner(() => true); await vm.LoadAsync(); Assert.False(vm.Loaded); }
        vm.Retire();
    }

    [Fact]
    public async Task Original_pending_source_read_cancels_on_retirement_without_another_read()
    {
        using var f = new FlowFixture(); var runtime = new Runtime { Network = false };
        var factory = new AccountDetailOriginalBoundaryTests.ControlledFactory(AccountDetailOriginalBoundaryTests.Factory(f));
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously); var calls = 0;
        factory.Before = async ct => { Interlocked.Increment(ref calls); started.TrySetResult(); await Task.Delay(Timeout.Infinite, ct); };
        var reader = new AiSnapshotStore(factory, f.Services.GetRequiredService<ICommercialWriteAccessSource>());
        var vm = new AiAnalysisViewModel(reader, f.Translator, f.Localization, new DateFormatter(f.Localization), f.Time, f.Platform, runtime);
        var read = vm.LoadAsync(); await started.Task.WaitAsync(TimeSpan.FromSeconds(10), Ct);
        Assert.True(vm.HasPendingOperation); Assert.True(vm.CanLeave);
        await vm.ActivateAsync(); Assert.Equal(1, calls);
        vm.Retire(); await read.WaitAsync(TimeSpan.FromSeconds(10), Ct);
        Assert.False(vm.Loaded); Assert.False(vm.HasPendingOperation); Assert.Null(vm.Error); Assert.Equal(1, calls);
    }

    [Fact]
    public async Task Visible_original_source_failure_preserves_literal_selection_and_requires_explicit_retry()
    {
        using var f = new FlowFixture(); var runtime = new Runtime { Network = false };
        await f.Store.SaveAccountAsync(new Vafadar.Zanance.Core.Accounts.Account { Name = "Fictitious selection", CurrencyCode = "EUR", OpeningDate = new(2026, 1, 1) }, Ct);
        var factory = new AccountDetailOriginalBoundaryTests.ControlledFactory(AccountDetailOriginalBoundaryTests.Factory(f));
        var reader = new AiSnapshotStore(factory, f.Services.GetRequiredService<ICommercialWriteAccessSource>());
        var vm = new AiAnalysisViewModel(reader, f.Translator, f.Localization, new DateFormatter(f.Localization), f.Time, f.Platform, runtime);
        await vm.LoadAsync(); Assert.Single(vm.Accounts).Selected = true; vm.From = new(2026, 9, 1); vm.To = new(2026, 9, 30); vm.ComparePrevious = true;
        var calls = 0; factory.Before = _ => { calls++; throw new IOException("Controlled original source unavailable."); };
        await vm.LoadAsync(); Assert.False(vm.Loaded); Assert.NotNull(vm.Error); Assert.True(Assert.Single(vm.Accounts).Selected);
        await vm.ActivateAsync(); Assert.Equal(1, calls);
        Assert.Equal(new(2026, 9, 1), vm.From); Assert.Equal(new(2026, 9, 30), vm.To); Assert.True(vm.ComparePrevious);
        factory.Before = null; await vm.LoadAsync(); Assert.True(vm.Loaded); Assert.True(Assert.Single(vm.Accounts).Selected);
        Assert.Equal(new(2026, 9, 1), vm.From); Assert.True(vm.ComparePrevious); vm.Retire();
    }
    [Fact]
    public async Task Changed_review_language_before_native_share_requires_a_fresh_original_review()
    {
        using var f = new FlowFixture(); var runtime = new Runtime { Network = false };
        var vm = await Model(f, runtime); await Review(vm);
        f.Localization.SetLanguage(f.Localization.SupportedLanguages.Single(x => x.CultureName == "de"));
        f.Platform.Confirmations.Enqueue(true); await vm.ShareAsync();
        Assert.Equal(0, runtime.Shares); Assert.Equal(0, runtime.Opens); vm.Retire();
    }

    [Fact]
    public async Task Returned_original_result_remains_historical_when_a_newer_date_draft_changes()
    {
        using var f = new FlowFixture(); var runtime = new Runtime();
        var vm = await Model(f, runtime); await Review(vm); await vm.CheckProviderAsync(); await vm.QuoteAsync();
        var original = vm.Disclosure; var newDate = vm.To.AddDays(1);
        runtime.Remote.AfterSubmission = () => vm.To = newDate;
        f.Platform.Confirmations.Enqueue(true); await vm.SendAsync();
        Assert.Equal(newDate, vm.To); Assert.Equal(runtime.Remote.Record!.Result!.Text, vm.ResultText);
        Assert.NotNull(vm.ResultSnapshot); Assert.Contains(original!, vm.ResultSnapshot!);
        Assert.Equal(1, runtime.Remote.Submissions); Assert.False(vm.CanSend); vm.Retire();
    }
    [Fact]
    public void Original_account_disclosure_caption_keeps_its_single_currency_separator()
    {
        var account = new AiAccount(Guid.NewGuid(), "Original account", "EUR", false);
        var row = new AiAccountChoice(account, () => { });
        Assert.Equal("Original account \u00b7 EUR", row.Caption);
    }
    [Theory]
    [InlineData("en")][InlineData("fa")][InlineData("de")][InlineData("es")][InlineData("fr")][InlineData("it")]
    public async Task Visible_original_source_error_refreshes_actual_language_without_read_or_draft_replay(string language)
    {
        using var f = new FlowFixture(); var runtime = new Runtime { Network = false }; var calls = 0;
        var factory = new AccountDetailOriginalBoundaryTests.ControlledFactory(AccountDetailOriginalBoundaryTests.Factory(f))
            { Before = _ => { calls++; throw new IOException("Controlled original unavailable source."); } };
        var vm = new AiAnalysisViewModel(new AiSnapshotStore(factory, f.Services.GetRequiredService<ICommercialWriteAccessSource>()),
            f.Translator, f.Localization, new DateFormatter(f.Localization), f.Time, f.Platform, runtime);
        await vm.LoadAsync(); vm.From = new(2026, 9, 1);
        f.Localization.SetLanguage(f.Localization.SupportedLanguages.Single(x => x.CultureName == language));
        Assert.Equal(f.Translator["Ai_ScopeError"], vm.Error); Assert.False(vm.Loaded);
        Assert.Equal(new(2026, 9, 1), vm.From); Assert.Equal(1, calls); Assert.Equal(0, runtime.Opens); vm.Retire();
    }
}
