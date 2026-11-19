using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.Api;
using Vafadar.Zanance.Api.Analysis;
using Vafadar.Zanance.Api.Sync;
using Vafadar.Zanance.App.Features.Analysis;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Analysis;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Categories;
using Vafadar.Zanance.Core.Plans;
using Vafadar.Zanance.Core.Sync;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Analysis;
using Vafadar.Zanance.Data.Sync;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Opt-in application flow using the original ViewModel, private journal client and real pinned service.</summary>
public sealed class AiLiveApplicationFlowTests
{
    /// <summary>Runs distinct synthetic report and comparison journeys against the two prepared providers.</summary>
    [Theory]
    [InlineData(false, "gemini-3.5-flash-lite")]
    [InlineData(true, "gpt-6.2-sol")]
    public Task Original_analysis_commands_reach_the_selected_live_provider(bool comparison, string model) =>
        RunAsync(comparison, model, false);

    /// <summary>Qualifies both newly disclosed report detail scopes with two distinct synthetic provider requests.</summary>
    [Theory]
    [InlineData(true, "gpt-6.2-sol")]
    [InlineData(false, "gemini-3.5-flash-lite")]
    public Task Detailed_reports_reach_the_selected_live_provider(bool comparison, string model) =>
        RunAsync(comparison, model, true);

    /// <summary>Qualifies the original synthetic budget-limit explanation.</summary>
    [Fact]
    public Task Budget_limit_funding_reaches_the_selected_live_provider() =>
        RunAsync(false, "gpt-6-luna", true, AiFundingKind.BudgetLimit);

    /// <summary>Qualifies the rounded historical suggestion through the selected model.</summary>
    [Fact]
    public Task Budget_suggestion_funding_reaches_the_selected_live_provider() =>
        RunAsync(false, "gemini-3.5-flash-lite", true, AiFundingKind.BudgetSuggestion);

    /// <summary>Qualifies the funding gap and local contribution alternative.</summary>
    [Fact]
    public Task Goal_gap_funding_reaches_the_selected_live_provider() =>
        RunAsync(false, "gemini-3.5-flash-lite", true, AiFundingKind.Goal);

    /// <summary>Checks exact funding arithmetic and rejects inputs outside the reviewed funding contract without a provider.</summary>
    [Fact]
    public void Funding_contract_keeps_original_amounts_and_rejects_inconsistent_facts()
    {
        var original = AiPilotProvider.FundingFixtures[AiFundingKind.BudgetLimit];
        var limit = AiFundingFacts.Limit("EUR", 1000, -200, 300, -1200);
        Assert.Equal(500, limit.Difference);
        Assert.Equal(1500, limit.Alternative());
        var packet = original with { Funding = limit };
        AiProtocol.Validate(packet);
        Assert.Throws<ArgumentException>(() => AiProtocol.Validate(packet with { Funding = limit with { Difference = 501 } }));
        Assert.Throws<ArgumentException>(() => AiProtocol.Validate(packet with { Funding = limit with { Currency = "USD" } }));
        Assert.False(AiPilotProvider.IsSynthetic(AiPurpose.PeriodExplanation, AiProtocol.Digest(packet)));

        var noDates = AiFundingFacts.Goal("EUR", 10000, -500, 10000, 0, null, null, 0);
        Assert.Null(noDates.Alternative());
        AiProtocol.Validate(original with { Funding = noDates });
    }

    /// <summary>Checks the original calculator's separate crossing and minimum, scenario flags and unknown balance limits.</summary>
    [Fact]
    public void Forecast_contract_keeps_first_shortfall_and_scenario_unknowns()
    {
        var packet = AiPilotProvider.ForecastFixture;
        var forecast = packet.Forecast!;
        Assert.Equal(new DateOnly(2026, 11, 5), forecast.Baseline.FirstShortfallOn);
        Assert.Equal(-5000, forecast.Baseline.FirstShortfallBalance);
        Assert.Equal(new DateOnly(2026, 11, 10), forecast.Baseline.MinimumOn);
        Assert.Equal(-11000, forecast.Baseline.Minimum);
        Assert.Equal(18000, forecast.Baseline.Closing);
        var scenario = forecast.Scenario!;
        Assert.Null(scenario.FirstShortfallOn);
        Assert.Equal(24000, scenario.Closing);
        Assert.Equal(1, scenario.UnknownCount);
        Assert.Contains(scenario.Events, item => item.Moved && item.On.Day == 13);
        Assert.Contains(scenario.Events, item => item.Excluded && item.Effect == -6000);
        Assert.Throws<ArgumentException>(() => AiProtocol.Validate(packet with
        { Forecast = forecast with { Scenario = scenario with { UnknownCount = 0 } } }));
        Assert.False(AiPilotProvider.IsSynthetic(packet.Purpose, AiProtocol.Digest(packet with
        { Forecast = forecast with { Scenario = scenario with { Closing = 24001 } } })));
    }

    /// <summary>Qualifies the new forecast comparison with actual application disclosure and the chosen live adapter.</summary>
    [Theory]
    [InlineData("gpt-6.2-sol")]
    [InlineData("gemini-4-hydrogen")]
    public Task Forecast_scenario_reaches_the_selected_live_provider(string model) =>
        RunAsync(true, model, true, projection: true);

    /// <summary>Qualifies the first shortfall explanation without a scenario.</summary>
    [Fact]
    public Task Forecast_baseline_reaches_the_selected_live_provider() =>
        RunAsync(true, "gpt-6.2-sol", true, projection: true, baselineOnly: true);

    /// <summary>Exercises distinct locally interpreted questions through the existing app and pinned service.</summary>
    [Theory]
    [InlineData(AiQuestionIntent.SpendingChange, "gpt-6.2-sol")]
    [InlineData(AiQuestionIntent.PlanShortfall, "gemini-4-hydrogen")]
    public Task Selected_question_reaches_the_selected_live_provider(AiQuestionIntent question, string model) =>
        RunAsync(true, model, false, question: question);

    private static async Task RunAsync(bool comparison, string model, bool detailed, AiFundingKind? funding = null,
        bool projection = false, bool baselineOnly = false, AiQuestionIntent? question = null)
    {
        var ct = TestContext.Current.CancellationToken;
        var connection = Environment.GetEnvironmentVariable("ZANANCE_AI_APP_TEST_CONNECTION");
        if (connection is null) { Assert.Skip("Explicit isolated synthetic live application fixture required."); return; }
        var settings = new NpgsqlConnectionStringBuilder(connection);
        Assert.Equal(("127.0.0.1", 55479, "zanance_ai_provider_pilot"), (settings.Host, settings.Port, settings.Database));
        var root = Environment.GetEnvironmentVariable("ZANANCE_AI_APP_TEST_ROOT")!;
        Assert.Equal("Zanance synthetic app analysis", await File.ReadAllTextAsync(Path.Combine(root, "fixture.marker"), ct));
        var lane = Path.Combine(root, question?.ToString() ?? (projection ? (baselineOnly ? "baseline" : model) : funding?.ToString() ?? (detailed ? (comparison ? "categories" : "plans") : (comparison ? "comparison" : "report"))));
        Directory.CreateDirectory(lane);
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Integration", ApplicationName = typeof(ServiceHost).Assembly.FullName });
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Service"] = connection, ["Service:Root"] = Path.Combine(lane, "service"),
            ["Analysis:LocalQualification"] = "true", ["Analysis:ProviderPilot:Enabled"] = "true",
        });
        builder.AddZananceService();
        builder.Logging.ClearProviders(); builder.Services.RemoveAll<IHostedService>();
        await using var server = builder.Build();
        await ServiceHost.MigrateAsync(server.Services);
        var pilot = server.Services.GetRequiredService<AiPilotProvider>();
        var serviceStore = server.Services.GetRequiredService<AiRequestStore>();
        var factory = server.Services.GetRequiredService<IDbContextFactory<ServiceDbContext>>();
        var purpose = comparison ? AiPurpose.PeriodComparison : AiPurpose.PeriodExplanation;
        var scopeKey = question?.ToString() ?? (projection ? (baselineOnly ? "forecast-baseline" : "forecast") : funding is AiFundingKind.BudgetSuggestion or AiFundingKind.Goal ? $"{funding}-qualified" : funding?.ToString() ?? "period");
        var email = $"synthetic-{model}-{scopeKey}@example.invalid";
        const string fixturePassword = "Fictional-App-Analysis-9!";
        await using (var scope = server.Services.CreateAsyncScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ServiceUser>>();
            var user = new ServiceUser { Email = email, UserName = email, EmailConfirmed = true };
            Assert.True((await users.CreateAsync(user, fixturePassword)).Succeeded);
            await serviceStore.GrantAsync(user.Id, $"synthetic-app-{model}-{scopeKey}", false, 1, ct);
            await using var db = await factory.CreateDbContextAsync(ct);
            await pilot.SelectModelAsync(db, comparison ? AiPurpose.PeriodExplanation : AiPurpose.PeriodComparison, "off", "app-pilot", ct);
            await pilot.SelectModelAsync(db, purpose, model, "app-pilot", ct);
        }
        server.UseZananceService(); server.MapZananceService();
        await server.StartAsync(ct);
        var address = server.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
        var peer = new LocalSyncPeer(address, server.Services.GetRequiredService<LocalSyncCertificate>().Fingerprint);
        var tokens = new SessionStore();
        using var login = new PersonalSyncTransport(peer, tokens);
        var identity = await login.SignInAsync(email, fixturePassword, ct);
        using var local = new FlowFixture();
        // The app derives an equally long preceding period, October 2–31, from this November selection.
        var fixture = question is { } asked ? asked == AiQuestionIntent.SpendingChange
            ? AiPilotProvider.SpendingQuestionFixture : AiPilotProvider.PlanQuestionFixture : projection ? (baselineOnly ? AiPilotProvider.ForecastBaselineFixture : AiPilotProvider.ForecastFixture) : funding is { } kind ? AiPilotProvider.FundingFixtures[kind] : detailed ? (comparison ? AiPilotProvider.CategoryFixture : AiPilotProvider.PlanFixture)
            : (comparison ? AiPilotProvider.ComparisonFixture : AiPilotProvider.ReportFixture);
        var localAccounts = new List<Guid>();
        await using (var db = await local.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(ct))
        {
            var category = question == AiQuestionIntent.SpendingChange
                ? new Category { Name = "Fictitious private cost category", Kind = CategoryKind.Expense } : null;
            if (category is not null) db.Add(category);
            foreach (var totals in fixture.Current.Totals)
            {
                var account = new Account { Name = "Fictitious private account " + totals.CurrencyCode, CurrencyCode = totals.CurrencyCode, OpeningDate = new(2026, 1, 1), OpeningBalance = question == AiQuestionIntent.PlanShortfall ? 5000 : projection ? 10000 : 0 };
                db.Add(account);
                localAccounts.Add(account.Id);
                if (question == AiQuestionIntent.PlanShortfall)
                {
                    AddPlan(account.Id, new(2026, 10, 23), EntryKind.Expense, 1000);
                    AddPlan(account.Id, new(2026, 10, 25), EntryKind.Expense, null);
                    AddPlan(account.Id, new(2026, 11, 4), EntryKind.Expense, 9000);
                    AddPlan(account.Id, new(2026, 11, 10), EntryKind.Income, 9000);
                }
                AddPeriod(account.Id, fixture.Current, totals);
                if (fixture.Previous is { } prior) AddPeriod(account.Id, prior, prior.Totals.Single(t => t.CurrencyCode == totals.CurrencyCode));
            }
            await db.SaveChangesAsync(ct);

            void AddPlan(Guid account, DateOnly due, EntryKind kind, long? amount) => db.Add(new Schedule
            {
                AccountId = account, Name = "Fictitious private due plan", Kind = kind, Amount = amount,
                AmountMode = amount.HasValue ? AmountMode.Fixed : AmountMode.Unknown,
                Rule = new() { Frequency = Frequency.Once, Start = due },
            });

            void AddPeriod(Guid account, AiPeriod period, PeriodTotals totals)
            {
                foreach (var (kind, amount) in new[] { (EntryKind.Income, totals.Income), (EntryKind.IncomeReversal, totals.IncomeReversals),
                             (EntryKind.Expense, totals.GrossExpense), (EntryKind.Refund, totals.Refunds) })
                    if (amount != 0) db.Add(new LedgerEntry { AccountId = account, Date = period.To, Kind = kind, Amount = amount,
                        Review = ReviewState.Confirmed, CategoryId = category?.Id, Note = "Fictitious note never sent" });
            }
        }

        var before = await GoalChooserFlowTests.SnapshotAsync(local);
        var journal = new Journal(lane, new EphemeralDataProtectionProvider().CreateProtector("synthetic-app-analysis"));
        var vm = new AiAnalysisViewModel(local.Services.GetRequiredService<AiSnapshotStore>(), local.Translator, local.Localization,
            new DateFormatter(local.Localization), question is null ? TimeProvider.System : new FixedTime { Now = new(2026, 10, 20, 9, 0, 0, TimeSpan.Zero) }, local.Platform, new Runtime(peer, tokens, journal, identity.Id));
        var reportSourceCurrent = true;
        var draftsOpened = 0;
        if (detailed)
        {
            var store = local.Services.GetRequiredService<AiSnapshotStore>();
            var scope = await store.AccountsAsync(ct);
            var packet = AiPeriodHandoff.Capture(scope.File, fixture.Current.From, fixture.Current.To, fixture.Current.ConfirmedOnly, localAccounts)!;
            var reportRows = fixture.Forecast is { } forecast ? forecast.Baseline.Events.Count + (forecast.Scenario?.Events.Count ?? 0)
                : fixture.Report?.Categories?.Count ?? fixture.Report?.Plans?.Count ?? 1;
            vm.SetInitialPeriod(packet with
            {
                ComparePrevious = comparison && !projection,
                CompareFrom = fixture.Previous?.From,
                CompareTo = fixture.Previous?.To,
                Report = fixture.Report,
                Funding = fixture.Funding,
                Forecast = fixture.Forecast,
                OpenDraftAsync = funding.HasValue ? () => { draftsOpened++; return Task.CompletedTask; } : null,
                ReportLabels = Enumerable.Range(1, reportRows).Select(i => $"Fictitious private label {i}").ToArray(),
                ValidateReportAsync = () => reportSourceCurrent ? Task.CompletedTask
                    : Task.FromException(new InvalidOperationException("Synthetic retired report source.")),
            });
        }

        await vm.LoadAsync();
        Assert.Equal(fixture.Current.AccountCount, vm.Accounts.Count);
        if (question is { } askedQuestion)
        {
            foreach (var account in vm.Accounts) account.Selected = true;
            vm.QuestionText = askedQuestion == AiQuestionIntent.SpendingChange
                ? "Why did my EUR spending rise? Fictitious private marker"
                : "Which EUR plans contribute to next month's shortfall? Fictitious private marker";
            await vm.ApplyQuestionCommand.ExecuteAsync(null);
            Assert.NotNull(vm.QuestionStatus);
            if (askedQuestion == AiQuestionIntent.SpendingChange) { vm.From = fixture.Current.From; vm.To = fixture.Current.To; }
            Assert.Equal((fixture.Current.From, fixture.Current.To), (vm.From, vm.To));
        }
        else if (!detailed)
        {
            foreach (var account in vm.Accounts) account.Selected = true;
            vm.From = fixture.Current.From; vm.To = fixture.Current.To; vm.ComparePrevious = comparison;
        }
        else
        {
            Assert.False(vm.CanEditScope);
            reportSourceCurrent = false;
            await vm.ReviewAsync();
            Assert.Null(vm.Disclosure);
            reportSourceCurrent = true;
        }

        await vm.ReviewAsync(); Assert.NotNull(vm.Disclosure);
        if (question is not null)
        {
            Assert.Contains("Fictitious private", vm.Summary, StringComparison.Ordinal);
            var reviewed = System.Text.Json.JsonSerializer.Deserialize(vm.Disclosure, AiJson.Default.AiPayload)!;
            Assert.Equal(AiProtocol.Digest(fixture), AiProtocol.Digest(reviewed));
        }
        if (detailed)
        {
            Assert.Contains("Fictitious private label 1", vm.Summary, StringComparison.Ordinal);
            var payload = System.Text.Json.JsonSerializer.Deserialize(vm.Disclosure, AiJson.Default.AiPayload)!;
            Assert.Equal(AiProtocol.Digest(fixture), AiProtocol.Digest(payload));
            if (funding.HasValue)
            {
                Assert.True(vm.CanOpenFundingDraft);
                reportSourceCurrent = false;
                await vm.OpenFundingDraftAsync();
                Assert.Equal(0, draftsOpened);
                reportSourceCurrent = true;
                await vm.OpenFundingDraftAsync();
                Assert.Equal(1, draftsOpened);
            }
        }
        Assert.DoesNotContain("Fictitious", vm.Disclosure, StringComparison.Ordinal);
        await vm.CheckProviderAsync(); Assert.True(vm.CanQuote, vm.Error);
        await vm.QuoteAsync(); Assert.True(vm.CanSend, vm.Error); Assert.Contains(model, vm.ProviderText, StringComparison.Ordinal);
        local.Platform.Confirmations.Enqueue(false);
        await vm.SendAsync();
        Assert.False(journal.Written);
        local.Platform.Confirmations.Enqueue(true);
        await vm.SendAsync();
        Assert.True(journal.Written); Assert.NotNull(vm.ResultSnapshot);

        await using (var db = await factory.CreateDbContextAsync(ct))
            await pilot.SelectModelAsync(db, purpose, "off", "app-pilot", ct);
        for (var attempt = 0; vm.CanContinue && attempt < 30; attempt++)
        {
            await Task.Delay(TimeSpan.FromSeconds(4), ct);
            local.Platform.Confirmations.Enqueue(true);
            await vm.ContinueAsync();
        }
        Assert.False(vm.CanContinue, vm.Error);
        Assert.False(string.IsNullOrWhiteSpace(vm.ResultText), vm.Error);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(local));
        await using var verify = await factory.CreateDbContextAsync(ct);
        var request = await verify.Set<AiRequestRow>().SingleAsync(r => r.UserId == identity.Id, ct);
        Assert.Equal(AiRequestState.Completed, request.State);
        Assert.Equal(1L, request.ChargedUnits);
        Assert.True(request.VendorInputTokens > 0 && request.VendorOutputTokens > 0);
        Assert.NotNull(request.VendorOperationId);
        await File.WriteAllTextAsync(Path.Combine(lane, "usage.txt"), $"{model}|{request.VendorInputTokens}|{request.VendorOutputTokens}", ct);
        vm.Retire();
    }

    private sealed class Runtime(LocalSyncPeer peer, IPersonalSyncTokenStore tokens, IAiPrivateStorage journal, string user) : IAiRuntime
    {
        public bool NetworkAvailable => true;
        public AiUiAccess Capture(string file) => new(() => { }, CancellationToken.None);
        public Task<bool> ConfirmDisclosureAsync() => Task.FromResult(true);
        public Task ShareAsync(string exactText) => throw new InvalidOperationException("No share belongs to the connected pilot.");
        public Task<IAiRemote> OpenAsync(string file, CancellationToken ct) =>
            Task.FromResult<IAiRemote>(new AiConnectedRemote(new PersonalSyncTransport(peer, tokens), journal, user, file, Capture(file)));
    }

    private sealed class SessionStore : IPersonalSyncTokenStore
    {
        private PersonalSyncSession? session;
        public Task<PersonalSyncSession?> ReadAsync(string peer, CancellationToken ct) => Task.FromResult(session);
        public Task WriteAsync(string peer, PersonalSyncSession value, CancellationToken ct) { session = value; return Task.CompletedTask; }
        public Task RemoveAsync(string peer, CancellationToken ct) { session = null; return Task.CompletedTask; }
    }

    private sealed class Journal(string root, IDataProtector protection) : IAiPrivateStorage
    {
        internal bool Written { get; private set; }
        public async Task<string?> ReadAsync(string scope, CancellationToken ct)
        {
            var path = Path.Combine(root, scope + ".protected");
            return File.Exists(path) ? protection.Unprotect(await File.ReadAllTextAsync(path, ct)) : null;
        }
        public async Task WriteAsync(string scope, string value, CancellationToken ct)
        {
            var path = Path.Combine(root, scope + ".protected");
            await File.WriteAllTextAsync(path, protection.Protect(value), ct);
            Assert.Equal(value, await ReadAsync(scope, ct));
            Written = true;
        }
    }
}
