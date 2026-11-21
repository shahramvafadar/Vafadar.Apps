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

    /// <summary>Case does not change a distinct currency code, while ordinary words keep their original reading.</summary>
    [Theory]
    [InlineData("Why did usd spending rise?", "USD")]
    [InlineData("Warum sind die eUr-Ausgaben gestiegen?", "EUR")]
    [InlineData("چرا هزینه‌های gbp افزایش یافت؟", "GBP")]
    [InlineData("Why did EUR eur spending rise?", "EUR")]
    [InlineData("Try to explain why spending rose?", null)]
    [InlineData("Why did TRY spending rise?", "TRY")]
    public void Question_currency_codes_are_case_insensitive_for_distinct_words(string input, string? currency)
        => Assert.Equal(new AiQuestion(AiQuestionIntent.SpendingChange, currency, false), AiQuestionParser.Interpret(input));

    /// <summary>Conflicting currencies require clarification even when written in mixed case.</summary>
    [Fact]
    public void Question_currency_codes_refuse_mixed_case_disagreement()
        => Assert.Null(AiQuestionParser.Interpret("Why did EUR usd spending rise?"));

    /// <summary>An explicitly written currency cannot be overwritten by the selected account.</summary>
    [Fact]
    public async Task Question_currency_codes_refuse_a_different_account_currency_at_review()
    {
        var ct = TestContext.Current.CancellationToken;
        using var f = new FlowFixture();
        var store = f.Services.GetRequiredService<AiSnapshotStore>();
        var account = new Account { Name = "Fictitious EUR", CurrencyCode = "EUR", OpeningDate = new(2026, 1, 1) };
        var category = new Category { Name = "Fictitious recurring expense", Kind = CategoryKind.Expense };
        await using (var db = await f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(ct))
        {
            db.AddRange(account, category);
            db.Add(new LedgerEntry { AccountId = account.Id, Date = new(2026, 10, 3), Kind = EntryKind.Expense,
                Amount = 800, Review = ReviewState.Confirmed, CategoryId = category.Id });
            await db.SaveChangesAsync(ct);
        }
        var before = await GoalChooserFlowTests.SnapshotAsync(f);
        var vm = new AiAnalysisViewModel(store, f.Translator, f.Localization, new DateFormatter(f.Localization),
            f.Time, f.Platform, new LocalRuntime());
        await vm.LoadAsync();
        Assert.Single(vm.Accounts).Selected = true;
        vm.QuestionText = "Why did usd spending rise?";
        await vm.ApplyQuestionCommand.ExecuteAsync(null);
        await vm.ReviewAsync();
        Assert.Null(vm.Disclosure);
        Assert.Equal(f.Translator["AiQuestion_Scope"], vm.QuestionStatus);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }

    /// <summary>Do not silently replace a written date or count with the selected scope.</summary>
    [Theory]
    [InlineData("Why did EUR spending rise from 2026-10-01 to 2026-10-15?")]
    [InlineData("چرا هزینه‌های EUR در ۲۹ مهر افزایش یافت؟")]
    [InlineData("Welche EUR-Zahlungen verursachen in 3 Tagen einen Fehlbetrag?")]
    [InlineData("Quels paiements en EUR causent un déficit le １２ novembre ?")]
    public async Task Question_with_an_unhandled_numeric_scope_requires_clarification(string questionText)
    {
        Assert.Null(AiQuestionParser.Interpret(questionText));
        using var f = new FlowFixture();
        var store = f.Services.GetRequiredService<AiSnapshotStore>();
        var before = await GoalChooserFlowTests.SnapshotAsync(f);
        var vm = new AiAnalysisViewModel(store, f.Translator, f.Localization, new DateFormatter(f.Localization),
            f.Time, f.Platform, new LocalRuntime());
        await vm.LoadAsync();
        vm.QuestionText = questionText;
        var dates = (vm.From, vm.To);
        await vm.ApplyQuestionCommand.ExecuteAsync(null);
        await vm.ReviewAsync();
        Assert.Equal(dates, (vm.From, vm.To));
        Assert.Equal(f.Translator["AiQuestion_Clarify"], vm.QuestionStatus);
        Assert.Null(vm.Disclosure);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
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

    /// <summary>The original Interpret action cannot apply a separate question introduced by a binding callback.</summary>
    [Fact]
    public async Task Interpret_command_does_not_adopt_a_new_question_from_its_first_notification()
    {
        using var f = new FlowFixture();
        var store = f.Services.GetRequiredService<AiSnapshotStore>();
        var before = await GoalChooserFlowTests.SnapshotAsync(f);
        var vm = new AiAnalysisViewModel(store, f.Translator, f.Localization, new DateFormatter(f.Localization),
            f.Time, f.Platform, new LocalRuntime());
        await vm.LoadAsync();
        vm.QuestionText = "Why did EUR spending rise?";
        var initialDates = (vm.From, vm.To);
        var initialCompare = vm.ComparePrevious;
        var changed = false;
        vm.PropertyChanged += (_, args) =>
        {
            if (changed || args.PropertyName != nameof(vm.CanEditQuestionComparison)) return;
            changed = true;
            vm.QuestionText = "Which EUR plans contribute to next month's shortfall?";
        };

        await vm.ApplyQuestionCommand.ExecuteAsync(null);

        Assert.True(changed);
        Assert.Contains("next month", vm.QuestionText, StringComparison.Ordinal);
        Assert.Null(vm.QuestionStatus);
        Assert.Equal(initialDates, (vm.From, vm.To));
        Assert.Equal(initialCompare, vm.ComparePrevious);
        Assert.True(vm.CanEditQuestionComparison);
        Assert.False(vm.HasPendingOperation);
        Assert.Null(vm.Disclosure);

        // A separate Interpret action may apply the new question normally.
        await vm.ApplyQuestionCommand.ExecuteAsync(null);
        Assert.Equal(f.Translator.Format("AiQuestion_Currency", f.Translator["AiQuestion_Plans"], "EUR"), vm.QuestionStatus);
        Assert.False(vm.CanEditQuestionComparison);
        Assert.NotEqual(initialDates, (vm.From, vm.To));
        Assert.False(vm.HasPendingOperation);
        Assert.Null(vm.Disclosure);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
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
