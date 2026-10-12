using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Features.Goals;
using Vafadar.Zanance.App.Features.Holdings;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Commerce;
using Vafadar.Zanance.Core.Goals;
using Vafadar.Zanance.Core.Money;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Tests;

/// <summary>AT-162: structured original write feedback uses real resources, real SQLite and actual retained editor drafts.</summary>
[Trait("AT", "AT-162")]
public sealed class CommercialFeedbackFlowTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private sealed class Access(string path) : ICommercialWriteAccessSource
    {
        public CommercialWriteAccess Capture(string databasePath) => new(path,
            new(ProductPlan.Free, new(EntitlementScopeKind.PersonalProfile, Guid.Parse("79797979-3434-4545-6767-898989898989"))));
    }
    [Theory]
    [InlineData("en")][InlineData("fa")][InlineData("de")][InlineData("es")][InlineData("fr")][InlineData("it")]
    public async Task Actual_filter_quota_rejection_keeps_complete_query_raw_name_and_rows_without_claiming_commit(string language)
    {
        using var f = new FlowFixture();
        f.Localization.SetLanguage(f.Localization.SupportedLanguages.First(item => item.CultureName == language));
        DigitPreferences.Apply(f.Localization);
        await f.Store.SaveSavedFilterAsync(new Vafadar.Zanance.Core.Ledger.SavedFilter { Name = "Fictitious existing query", Search = "#original" }, Ct);
        var store = new ZananceStore(f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>(), f.Time, new Access(f.Directory.Combine("zanance.db")));
        var actions = new Vafadar.Zanance.App.Features.Commerce.QuickResourceActions(store, f.Translator, f.Platform);
        await actions.LoadAsync();
        var draft = new Vafadar.Zanance.Core.Ledger.SavedFilter { Name = "Complete retained query", Search = "#complete raw query", Period = 2,
            From = new(2026, 10, 1), To = new(2026, 10, 31), CategoryIds = [Guid.NewGuid()], InTotalsOnly = true, UnreviewedOnly = true };
        var raw = System.Text.Json.JsonSerializer.Serialize(draft);
        var before = await GoalChooserFlowTests.SnapshotAsync(f);
        f.Platform.Choices.Enqueue(f.Translator["Filter_SaveCurrent"]); f.Platform.Inputs.Enqueue("  Complete unsaved filter name  ");
        await actions.FilterMenuAsync(draft, () => actions.LoadAsync());
        var rejection = Assert.IsType<CommercialWriteRejectedException>(Assert.Single(f.Platform.Failures));
        Assert.Equal(QuotaKind.SavedFilters, rejection.Quota); Assert.Equal(1, rejection.Maximum);
        Assert.Equal(CommercialFailureMessage.Describe(rejection, f.Translator, draftKept: true), actions.Feedback);
        Assert.Equal("  Complete unsaved filter name  ", actions.FilterNameDraft);
        Assert.Equal(raw, System.Text.Json.JsonSerializer.Serialize(draft));
        Assert.Null(actions.Review); Assert.False(actions.IsBusy); Assert.Empty(f.Platform.Routes);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
        Assert.Equal("  Complete unsaved filter name  ", actions.FilterNameDraft);
        f.Platform.Choices.Enqueue(f.Translator["Filter_SaveCurrent"]); f.Platform.Inputs.Enqueue(null);
        await actions.FilterMenuAsync(draft, () => actions.LoadAsync());
        Assert.Equal("  Complete unsaved filter name  ", f.Platform.PromptInitialValues.Last());
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f)); Assert.Single(f.Platform.Failures);
    }
    [Theory]
    [InlineData("en")][InlineData("fa")][InlineData("de")][InlineData("es")][InlineData("fr")][InlineData("it")]
    public void All_original_permission_and_resource_reasons_translate_without_enum_ids_purchase_claims_or_a_false_no_save_promise(string language)
    {
        using var f = new FlowFixture(); f.Localization.SetLanguage(f.Localization.SupportedLanguages.First(item => item.CultureName == language));
        DigitPreferences.Apply(f.Localization);
        foreach (var permission in Enum.GetValues<FeaturePermission>().Where(item => item != FeaturePermission.Allowed))
        {
            var error = new CommercialWriteRejectedException(CommercialFeature.AdvancedGoals, permission);
            var message = CommercialFailureMessage.Describe(error, f.Translator);
            Assert.NotNull(message); Assert.DoesNotContain(permission.ToString(), message);
            Assert.DoesNotContain(f.Translator["Commercial_DraftKept"], message);
            Assert.Equal(message + "\n\n" + f.Translator["Commercial_DraftKept"], CommercialFailureMessage.Describe(error, f.Translator, true));
        }
        foreach (var kind in Enum.GetValues<QuotaKind>())
        {
            var quota = new CommercialWriteRejectedException(CommercialFeature.BasicGoals, kind, 1, int.MaxValue, int.MaxValue);
            var message = CommercialFailureMessage.Describe(quota, f.Translator)!;
            Assert.DoesNotContain("Commercial_", message); Assert.Contains("\u2066\u200E", message);
            Assert.Contains(NativeDigits.Apply(4294967294L.ToString("N0", f.Translator.Culture))!, message);
            var originalId = Guid.NewGuid(); var selection = CommercialFailureMessage.Describe(
                new CommercialWriteRejectedException(CommercialFeature.BasicGoals, kind, originalId, true), f.Translator)!;
            Assert.DoesNotContain(originalId.ToString(), selection); Assert.DoesNotContain("Commercial_", selection);
        }
    }
    [Fact]
    public void Unknown_failures_and_malformed_structured_values_keep_the_original_fallback_without_leaking_exception_text()
    {
        using var f = new FlowFixture();
        var unknown = new InvalidOperationException("Fictitious internal file and provider details");
        Assert.Null(CommercialFailureMessage.Describe(unknown, f.Translator, true));
        Assert.Equal(f.Translator["Goal_SaveFailed"], CommercialFailureMessage.OrFallback(unknown, f.Translator, "Goal_SaveFailed", true));
        Assert.Null(CommercialFailureMessage.Describe(new CommercialWriteRejectedException(CommercialFeature.BasicGoals,
            (QuotaKind)999, 1, 2, 1), f.Translator));
        Assert.Null(CommercialFailureMessage.Describe(new CommercialWriteRejectedException(CommercialFeature.BasicGoals,
            FeaturePermission.Allowed), f.Translator));
    }
    [Fact]
    public async Task A_structured_rejection_after_the_first_real_commit_keeps_saved_refresh_feedback_and_cannot_resubmit_the_write()
    {
        using var f = new FlowFixture();
        var store = new ZananceStore(f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>(), f.Time, new Access(f.Directory.Combine("zanance.db")));
        var actions = new Vafadar.Zanance.App.Features.Commerce.QuickResourceActions(store, f.Translator, f.Platform);
        await actions.LoadAsync(); var reloads = 0;
        f.Platform.Choices.Enqueue(f.Translator["Filter_SaveCurrent"]); f.Platform.Inputs.Enqueue("Fictitious committed query");
        var draft = new Vafadar.Zanance.Core.Ledger.SavedFilter { Name = "Retained form query", Search = "#actual saved query" };
        await actions.FilterMenuAsync(draft, async () =>
        {
            reloads++;
            await store.SaveSavedFilterAsync(new Vafadar.Zanance.Core.Ledger.SavedFilter { Name = "Fictitious rejected second query" }, Ct);
        });
        Assert.Equal(f.Translator["Occurrence_SavedRefreshFailed"], actions.Feedback);
        Assert.DoesNotContain(f.Translator["Commercial_DraftKept"], actions.Feedback!);
        Assert.Empty(f.Platform.Failures); Assert.Single(f.Platform.Alerts); Assert.Null(actions.Review);
        Assert.Equal("#actual saved query", Assert.Single(await store.GetSavedFiltersAsync(Ct)).Search);
        f.Platform.Choices.Enqueue(null);
        await actions.FilterMenuAsync(draft, () => Task.CompletedTask); Assert.Equal(1, reloads);
        Assert.Single(await store.GetSavedFiltersAsync(Ct));
    }
    [Fact]
    public async Task Identical_original_filter_ids_in_another_file_never_share_the_draft_file_identity()
    {
        using var first = new FlowFixture();
        var original = new Vafadar.Zanance.Core.Ledger.SavedFilter { Name = "Fictitious same original", Search = "#original" };
        await first.Store.SaveSavedFilterAsync(original, Ct);
        var one = await first.Store.GetQuickResourceReviewAsync(cancellationToken: Ct);
        using var second = new FlowFixture();
        var other = new Vafadar.Zanance.Core.Ledger.SavedFilter { Name = original.Name, Search = original.Search };
        await second.Store.SaveSavedFilterAsync(other, Ct);
        await using (var db = await second.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct))
            await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE SavedFilters SET Id={original.Id} WHERE Id={other.Id}", Ct);
        var two = await second.Store.GetQuickResourceReviewAsync(cancellationToken: Ct);
        Assert.Equal(one.Filters.Single().Id, two.Filters.Single().Id);
        Assert.False(one.BelongsToSameFile(two)); Assert.False(two.BelongsToSameFile(one));
        Assert.True(one.BelongsToSameFile(await first.Store.GetQuickResourceReviewAsync(cancellationToken: Ct)));
    }
}
