using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Features.Analysis;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Analysis;
using Vafadar.Zanance.Core.Budgets;
using Vafadar.Zanance.Core.Categories;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Analysis;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Bounded independent synthetic cases for interpretation, scope and stale consent.</summary>
public sealed class AiQuestionTests
{
    [Theory]
    [InlineData("Why did EUR spending rise?", "Which EUR plans contribute to next month's shortfall?")]
    [InlineData("چرا هزینه‌های EUR افزایش یافت؟", "کدام برنامه‌های EUR باعث کسری ماه بعد می‌شوند؟")]
    [InlineData("Warum sind die EUR-Ausgaben gestiegen?", "Welche EUR-Pläne tragen zum Fehlbetrag im nächsten Monat bei?")]
    [InlineData("¿Por qué subió mi gasto en EUR?", "¿Qué pagos causan el déficit en EUR el próximo mes?")]
    [InlineData("Pourquoi mes dépenses en EUR ont-elles augmenté ?", "Quels paiements en EUR causent le déficit du mois prochain ?")]
    [InlineData("Perché sono aumentate le mie spese in EUR?", "Quali pagamenti in EUR causano un deficit il prossimo mese?")]
    public void Selected_question_vocabulary_recognizes_intents_in_the_six_app_languages(string spending, string forecast)
    {
        Assert.Equal(new AiQuestion(AiQuestionIntent.SpendingChange, "EUR", false), AiQuestionParser.Interpret(spending));
        Assert.Equal(new AiQuestion(AiQuestionIntent.PlanShortfall, "EUR", true), AiQuestionParser.Interpret(forecast));
    }

    [Fact]
    public async Task Selected_question_refuses_mixed_scope_and_source_changes_without_writing()
    {
        var ct = TestContext.Current.CancellationToken;
        using var f = new FlowFixture();
        var store = f.Services.GetRequiredService<AiSnapshotStore>();
        var runtime = new LocalRuntime();
        var account = new Account { Name = "Fictitious EUR", CurrencyCode = "EUR", OpeningDate = new(2026, 1, 1) };
        var dollars = new Account { Name = "Fictitious USD", CurrencyCode = "USD", OpeningDate = account.OpeningDate };
        var category = new Category { Name = "Fictitious category name", Kind = CategoryKind.Expense };
        await using (var db = await f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(ct))
        {
            db.AddRange(account, dollars, category);
            db.Add(new LedgerEntry { AccountId = account.Id, Date = new(2026, 10, 12),
                Kind = EntryKind.Expense, Amount = 2300, Review = ReviewState.Confirmed, CategoryId = category.Id });
            await db.SaveChangesAsync(ct);
        }
        var before = await GoalChooserFlowTests.SnapshotAsync(f);
        Assert.Null(AiQuestionParser.Interpret("Why did EUR USD spending rise?"));
        Assert.Null(AiQuestionParser.Interpret("Why did EUR spending rise and which plans contributed to the shortfall?"));
        Assert.Null(AiQuestionParser.Interpret("Why did spending rise last month?"));
        Assert.Null(AiQuestionParser.Interpret("Pay the scheduled plan"));
        var vm = new AiAnalysisViewModel(store, f.Translator, f.Localization, new DateFormatter(f.Localization), f.Time, f.Platform, runtime);
        await vm.LoadAsync();
        foreach (var option in vm.Accounts) option.Selected = true;
        vm.QuestionText = "Why did EUR spending rise?";
        await vm.ApplyQuestionCommand.ExecuteAsync(null); await vm.ReviewAsync();
        Assert.Null(vm.Disclosure);
        Assert.Equal(f.Translator["AiQuestion_Scope"], vm.QuestionStatus);
        vm.Accounts.Single(option => option.Id == dollars.Id).Selected = false;
        vm.From = new(2026, 10, 1); vm.To = new(2026, 10, 31);
        await vm.ReviewAsync();
        Assert.NotNull(vm.Disclosure);
        Assert.Contains(category.Name!, vm.Summary, StringComparison.Ordinal);
        Assert.DoesNotContain(category.Name!, vm.Disclosure, StringComparison.Ordinal);
        var file = (await store.AccountsAsync(ct)).File;
        var reviewed = await store.ReviewAsync(new(file, [account.Id], vm.From, vm.To, true, true, "en",
            Question: new(AiQuestionIntent.SpendingChange, "EUR", false)), ct);
        Assert.Equal(new AiCategoryChange(2300, 0), Assert.Single(reviewed.Payload.Report!.Categories!));
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));

        await using (var db = await f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(ct))
            await db.Categories.Where(item => item.Id == category.Id).ExecuteUpdateAsync(change => change.SetProperty(item => item.Name, "Different label"), ct);
        await Assert.ThrowsAsync<InvalidOperationException>(() => store.CheckAsync(reviewed, ct));
        vm.QuestionText = "Pay the plan";
        await vm.ReviewAsync();
        Assert.Null(vm.Disclosure);
    }

    [Fact]
    public async Task Selected_question_resolves_the_original_custom_financial_month()
    {
        var ct = TestContext.Current.CancellationToken;
        using var f = new FlowFixture();
        await f.Store.UpdateSettingsAsync(settings => settings.MonthStartDay = 15, ct);
        var store = f.Services.GetRequiredService<AiSnapshotStore>();
        var file = (await store.AccountsAsync(ct)).File;
        var result = await store.UpcomingMonthAsync(file, new(2026, 10, 4), PeriodCalendar.Gregorian, ct);
        Assert.Equal((new DateOnly(2026, 10, 15), new DateOnly(2026, 11, 14)), result);
    }

    private sealed class LocalRuntime : IAiRuntime
    {
        public bool NetworkAvailable => false;
        public AiUiAccess Capture(string file) => new(() => { }, CancellationToken.None);
        public Task<bool> ConfirmDisclosureAsync() => throw new InvalidOperationException();
        public Task ShareAsync(string exact) => throw new InvalidOperationException();
        public Task<IAiRemote> OpenAsync(string file, CancellationToken ct) => throw new InvalidOperationException();
    }
}
