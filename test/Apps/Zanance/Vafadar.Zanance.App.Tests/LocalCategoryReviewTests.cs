using Vafadar.Zanance.App.Features.Categories;
using Vafadar.Zanance.Core.Categories;
using Vafadar.Zanance.Core.Receipts;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Data.Importing;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Focused synthetic checks for the fixed local taxonomy, uncertain rules and editable rule proposals.</summary>
public sealed class LocalCategoryReviewTests
{
    [Theory]
    [InlineData("SUPERMARKET, copy")]
    [InlineData("سوپرماركت")]
    [InlineData("Supermarkt")]
    [InlineData("supermercado")]
    [InlineData("supermarché")]
    [InlineData("supermercato")]
    public void Fixed_taxonomy_suggests_the_same_category_from_six_languages(string text)
    {
        var food = new Category { Name = "Fictitious food", SystemKey = "Food", Kind = CategoryKind.Expense };
        Assert.Equal(food.Id, LocalCategoryReview.Evaluate([food], CategoryKind.Expense, text, null).CategoryId);
        Assert.Null(LocalCategoryReview.Evaluate([food], CategoryKind.Income, text, null).CategoryId);
    }

    [Fact]
    public void Rules_and_local_signals_abstain_when_the_original_signal_is_ambiguous()
    {
        var food = new Category { Name = "Fictitious food", SystemKey = "Food", Kind = CategoryKind.Expense };
        var health = new Category { Name = "Fictitious health", SystemKey = "Health", Kind = CategoryKind.Expense };
        var categories = new[] { food, health }.ToDictionary(item => item.Id);
        var rules = new[]
        {
            new CategoryRule { CategoryId = food.Id, Kind = food.Kind, Match = "Blue" },
            new CategoryRule { CategoryId = health.Id, Kind = health.Kind, Match = "Blue" },
        };
        var tie = CategoryRules.Resolve(rules, categories, CategoryKind.Expense, "Blue pharmacy", null);
        Assert.Null(tie.Rule);
        Assert.True(tie.Conflicting);
        Assert.Equal(CategoryReviewReason.ConflictingRules,
            LocalCategoryReview.Evaluate(categories.Values, CategoryKind.Expense, "Blue pharmacy", null, tie.Conflicting).Reason);
        Assert.Equal(CategoryReviewReason.ConflictingSignals,
            LocalCategoryReview.Evaluate(categories.Values, CategoryKind.Expense, "pharmacy supermarket", null).Reason);
        Assert.Equal(CategoryReviewReason.NoMatch,
            LocalCategoryReview.Evaluate(categories.Values, CategoryKind.Expense, "supermarketwork", null).Reason);
        Assert.Equal(CategoryReviewReason.NoMatch,
            LocalCategoryReview.Evaluate(categories.Values, CategoryKind.Expense, "supermarket", new string('a', 513)).Reason);
        Assert.Equal(health.Id, CategoryRules.Resolve([.. rules,
            new CategoryRule { CategoryId = health.Id, Kind = health.Kind, Match = "Blue pharmacy" }],
            categories, CategoryKind.Expense, "Blue pharmacy", null).Rule?.CategoryId);
    }

    [Fact]
    public async Task Category_only_import_rebinding_refuses_tampered_finance_and_own_format()
    {
        var ct = TestContext.Current.CancellationToken;
        using var fixture = new FlowFixture();
        await fixture.Store.EnsureDefaultCategoriesAsync(ct);
        var categories = await fixture.Store.GetCategoriesAsync(ct);
        var target = categories.Single(item => item.SystemKey == "Food");
        var foreign = categories.Single(item => item.SystemKey == "Salary");
        var path = await fixture.Store.CaptureImportFileAsync(ct);
        var generic = await fixture.Store.ReadImportReviewAsync(path, ct, reviewAccess: false, includeCategoryRules: true);
        var owned = await fixture.Store.ReadImportReviewAsync(path, ct, reviewAccess: false);
        var entry = new LedgerEntry { Kind = EntryKind.Expense, Amount = 1234 };
        generic.Bind([entry]); owned.Bind([entry]);
        var tampered = entry.Copy();
        tampered.Amount++;
        Assert.Throws<ImportReviewChangedException>(() => generic.SelectCategory(tampered, target.Id));
        Assert.Throws<ImportReviewChangedException>(() => generic.SelectCategory(entry, foreign.Id));
        Assert.Throws<ImportReviewChangedException>(() => owned.SelectCategory(entry, target.Id));
        Assert.Null(entry.CategoryId);
        Assert.Equal(1234, entry.Amount);
        Assert.Empty(await fixture.Store.GetEntriesAsync(cancellationToken: ct));
    }

    [Fact]
    public async Task Parsed_receipt_merchant_can_prepare_an_editable_rule_without_writing()
    {
        var ct = TestContext.Current.CancellationToken;
        using var fixture = new FlowFixture();
        await fixture.Store.EnsureDefaultCategoriesAsync(ct);
        var category = (await fixture.Store.GetCategoriesAsync(ct)).Single(item => item.SystemKey == "Health");
        var receipt = ReceiptParser.Parse("FICTITIOUS PHARMACY\nTOTAL EUR 12.34", new(2026, 11, 19));
        Assert.NotNull(receipt.Merchant);
        Assert.Equal(category.Id, LocalCategoryReview.Evaluate([category], CategoryKind.Expense, receipt.Merchant, null).CategoryId);
        var before = await GoalChooserFlowTests.SnapshotAsync(fixture);
        var file = await fixture.Store.CaptureImportFileAsync(ct);
        var model = new RulesViewModel(fixture.Store, fixture.Translator, fixture.Localization, fixture.Platform);
        model.SetInitialDraft(new(file, category.Id, receipt.Merchant));
        model.SetActive(true);
        await model.LoadAsync();
        Assert.True(model.CanInteract);
        Assert.Equal(receipt.Merchant, model.MatchText);
        Assert.Equal(category.Id, model.SelectedCategory?.Id);
        model.MatchText += " branch";
        Assert.Empty(await fixture.Store.GetCategoryRulesAsync(ct));
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(fixture));
    }
}
