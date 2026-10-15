using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Vafadar.Zanance.Core.Budgets;
using Vafadar.Zanance.Core.Categories;
using Vafadar.Zanance.Core.Goals;
using Vafadar.Zanance.Core.Rates;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;
using Vafadar.Zanance.Api;
using Vafadar.Zanance.Api.Sync;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Sync;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Sync;

namespace Vafadar.Zanance.Sync.Tests;

public sealed partial class PersonalSyncAcceptanceTests
{
    /// <summary>Actual SQLite capture and PostgreSQL/TLS commits reject inconsistent planning without partial publication.</summary>
    [Fact]
    public async Task Planning_domain_refusals_preserve_original_rows_and_publish_only_a_valid_complete_graph()
    {
        var connection = Environment.GetEnvironmentVariable("ZANANCE_SYNC_TEST_DB") ?? throw new InvalidOperationException("An isolated service database is required.");
        var root = Environment.GetEnvironmentVariable("ZANANCE_SYNC_TEST_ROOT") ?? throw new InvalidOperationException("The retained fictional service root is required.");
        await PrepareStandaloneFixtureAsync(connection, root);
        var database = new NpgsqlConnectionStringBuilder(connection); Assert.True(database.Host is "127.0.0.1" or "localhost");
        Assert.StartsWith("zanance_identity_sync_", database.Database); database.Username = "zanance_runtime";
        var scenario = Guid.NewGuid().ToString("N"); var proof = Path.Combine(root, "planning-" + scenario + ".txt");
        void Checkpoint(string name) => File.AppendAllText(proof, name + Environment.NewLine);
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Integration", ApplicationName = typeof(ServiceHost).Assembly.FullName });
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Service"] = database.ConnectionString, ["Service:Root"] = Path.Combine(root, "service"),
        });
        builder.AddZananceService(); builder.Logging.ClearProviders(); builder.WebHost.ConfigureKestrel(options => ServiceHost.ListenLocalTls(options, 0));
        // Only this fixture drives explicit worker passes to observe the exact live publication barrier.
        builder.Services.Remove(builder.Services.Single(item => item.ServiceType == typeof(IHostedService) && item.ImplementationType == typeof(PrivateMaintenance)));
        await using var app = builder.Build(); app.UseZananceService(); app.MapZananceService(); await app.StartAsync(Ct);
        try
        {
            var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
            var peer = new LocalSyncPeer(address, await File.ReadAllTextAsync(Path.Combine(root, "service", "local-tls.fingerprint"), Ct));
            var email = "planning-" + scenario + "@example.invalid"; var password = Convert.ToBase64String(RandomNumberGenerator.GetBytes(36)) + "aA9!";
            using (var enrollment = LocalSyncHttp.CreateClient(peer, TimeProvider.System))
            {
                using var registration = await enrollment.PostAsJsonAsync("identity/register", new IdentityInput(email, password), Ct); Assert.Equal(HttpStatusCode.Accepted, registration.StatusCode);
                await using var scope = app.Services.CreateAsyncScope(); var service = scope.ServiceProvider.GetRequiredService<ServiceDbContext>();
                var user = await service.Users.SingleAsync(value => value.Email == email, Ct);
                var challenge = await service.Challenges.AsNoTracking().SingleAsync(value => value.UserId == user.Id && value.Purpose == "confirm", Ct);
                var mail = JsonSerializer.Deserialize<PrivateMail>(scope.ServiceProvider.GetRequiredService<IDataProtectionProvider>().CreateProtector("identity-mail-v1").Unprotect(challenge.ProtectedMail))!;
                using var confirmation = await enrollment.PostAsJsonAsync("identity/complete", new ChallengeInput(challenge.Id, mail.Token, null), Ct); Assert.Equal(HttpStatusCode.NoContent, confirmation.StatusCode);
            }
            using var client = new Client(Path.Combine(root, "client-planning-" + scenario + ".sqlite"), peer);
            var owner = await client.Transport.SignInAsync(email, password, Ct);
            var device = await client.Transport.RegisterDeviceAsync("Fictitious planning device", Ct);
            var profile = await client.Transport.CreateProfileAsync(new(device.Id, Guid.NewGuid(), "Fictitious planning validation", SyncSchema.Hash), Ct);
            var euro = new Account { Name = "Fictitious euro account", CurrencyCode = "EUR", OpeningDate = new(2026, 10, 15) };
            var dollar = new Account { Name = "Fictitious dollar account", CurrencyCode = "USD", OpeningDate = new(2026, 10, 15) };
            await client.Finances.SaveAccountAsync(euro, Ct); await client.Finances.SaveAccountAsync(dollar, Ct);
            await client.Finances.GetSettingsAsync(Ct);
            var earmark = new Goal { Name = "Fictitious earmark", CurrencyCode = "EUR", TargetAmount = 5000 };
            var balance = new Goal { Name = "Fictitious balance", CurrencyCode = "EUR", TargetAmount = 10000,
                Type = GoalType.AccountBalance, AccountId = euro.Id };
            var category = new Category { Name = "Fictitious expense", Kind = CategoryKind.Expense };
            var budget = new Budget { CurrencyCode = "EUR", Year = 2026, Month = 10, TotalLimit = 0,
                CategoryLimits = [new() { CategoryId = category.Id, Limit = 0 }] };
            var plan = new ContributionPlan { GoalId = earmark.Id };
            var funding = new GoalAllocation { GoalId = earmark.Id, AccountId = euro.Id, Amount = -5, Date = new(2026, 10, 15) };
            var rate = new ExchangeRate { FromCurrencyCode = "EUR", ToCurrencyCode = "USD", Rate = 1.1m, Date = new(2026, 10, 15) };
            await using (var local = await client.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct))
            {
                local.AddRange(earmark, balance, category, budget, plan, funding, rate); await local.SaveChangesAsync(Ct);
            }
            await client.Sync.ConnectAsync(await client.Sync.ReviewAsync(Ct), peer, owner.Id, device.Id, profile, Ct);
            var captured = new Dictionary<SyncRecordKey, SyncProposal>();
            await using (var local = await client.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct))
            {
                await local.Database.OpenConnectionAsync(Ct); await using var command = local.Database.GetDbConnection().CreateCommand();
                command.CommandText = "SELECT TableName,RecordKey,Body FROM SyncOutbox WHERE Body IS NOT NULL ORDER BY Sequence";
                await using var reader = await command.ExecuteReaderAsync(Ct);
                while (await reader.ReadAsync(Ct))
                {
                    var key = new SyncRecordKey(reader.GetString(0), reader.GetString(1));
                    captured[key] = new(key, 0, JsonSerializer.Deserialize<JsonElement>(reader.GetString(2)));
                }
            }
            var baseline = captured.Values.ToArray();
            SyncRecord[] Graph(IEnumerable<SyncProposal> proposals) => proposals.Select(value => new SyncRecord(value.Record, 0, value.Values)).ToArray();
            Assert.Empty(SyncFinancialValidation.Validate([], Graph(baseline)));
            Checkpoint("original-capture-accepts-negative-earmark-release-zero-budget-and-empty-contribution-scaffolding");
            var goalKey = baseline.Single(value => value.Record.Table == "Goals" && value.Values!.Value.GetProperty("Id").GetGuid() == earmark.Id).Record;
            var balanceKey = baseline.Single(value => value.Record.Table == "Goals" && value.Values!.Value.GetProperty("Id").GetGuid() == balance.Id).Record;
            var cases = new (SyncRecordKey Key, string Field, JsonNode? Value, string Reason)[]
            {
                (goalKey, "Name", JsonValue.Create(" "), "GoalNameRequired"),
                (goalKey, "TargetAmount", JsonValue.Create(0L), "GoalTargetMustBePositive"),
                (balanceKey, "CurrencyCode", JsonValue.Create("USD"), "GoalAccountCurrencyMismatch"),
                (Key("Budgets"), "TotalLimit", JsonValue.Create(-1L), "BudgetLimitMustBeNonnegative"),
                (Key("Budgets"), "Year", JsonValue.Create(0), "InvalidBudgetPeriod"),
                (Key("Budgets"), "Month", JsonValue.Create(0), "InvalidBudgetPeriod"),
                (Key("Budgets"), "Month", JsonValue.Create(13), "InvalidBudgetPeriod"),
                (Key("BudgetCategoryLimits"), "Limit", JsonValue.Create(-1L), "BudgetLimitMustBeNonnegative"),
                (Key("ContributionPlans"), "Amount", JsonValue.Create(0L), "ContributionMustBePositive"),
                (Key("ContributionPlans"), "Percent", JsonValue.Create("101"), "ContributionPercentOutOfRange"),
                (Key("Settings"), "MonthStartDay", JsonValue.Create(0), "InvalidFinancialMonthStart"),
                (Key("Settings"), "DefaultCurrencyCode", JsonValue.Create("ZZZ"), "InvalidCurrency"),
                (Key("Settings"), "EssentialEstimate", JsonValue.Create(-1L), "EssentialEstimateMustBeNonnegative"),
                (Key("ExchangeRates"), "Rate", JsonValue.Create("-1"), "ExchangeRateMustBePositive"),
                (Key("GoalAllocations"), "Amount", JsonValue.Create(0L), "GoalAllocationMustBeNonzero"),
                (Key("GoalAllocations"), "AccountId", JsonValue.Create(dollar.Id.ToString("D").ToUpperInvariant()), "GoalAllocationCurrencyMismatch"),
            };
            foreach (var item in cases)
            {
                var candidate = baseline.Select(value => value.Record == item.Key ? Change(value, item.Field, item.Value) : value).ToArray();
                await Refused(candidate, item.Reason);
            }
            var duplicate = baseline.Select(value => value.Record != goalKey ? value :
                Change(Change(Change(value, "Type", JsonValue.Create((int)GoalType.AccountBalance)), "AccountId", JsonValue.Create(euro.Id.ToString("D").ToUpperInvariant())),
                    "State", JsonValue.Create((int)GoalState.Paused))).ToArray();
            await Refused(duplicate, "AccountAlreadyHasBalanceGoal");
            var weekly = baseline.Select(value => value.Record != Key("Budgets") ? value :
                Change(Change(Change(Change(value, "Period", JsonValue.Create((int)BudgetPeriod.Week)),
                    "Year", JsonValue.Create(0)), "Month", JsonValue.Create(0)), "PeriodStart", JsonValue.Create("2026-10-15"))).ToArray();
            Assert.Empty(SyncFinancialValidation.Validate([], Graph(weekly)));
            await Refused(weekly.Select(value => value.Record != Key("Budgets") ? value :
                Change(value, "PeriodStart", JsonValue.Create("9999-12-31"))).ToArray(), "InvalidBudgetPeriod");
            var lastFinancialMonth = baseline.Select(value => value.Record == Key("Budgets") ?
                Change(Change(value, "Year", JsonValue.Create(9999)), "Month", JsonValue.Create(12)) :
                value.Record == Key("Settings") ? Change(value, "MonthStartDay", JsonValue.Create(25)) : value).ToArray();
            await Refused(lastFinancialMonth, "InvalidBudgetPeriod");
            Checkpoint("nineteen-original-domain-refusals-leave-service-cursor-events-and-records-empty");
            var valid = await Submit(baseline); Assert.True(valid.Result.Committed); Assert.Empty(valid.Result.Conflicts);
            Assert.True(valid.Result.Cursor > 0);
            using var receiver = new Client(Path.Combine(root, "client-planning-receiver-" + scenario + ".sqlite"), peer);
            await receiver.Transport.SignInAsync(email, password, Ct);
            var receivingDevice = await receiver.Transport.RegisterDeviceAsync("Fictitious planning receiver", Ct);
            await receiver.Sync.ConnectAsync(await receiver.Sync.ReviewAsync(Ct), peer, owner.Id, receivingDevice.Id,
                profile with { Initializing = false, Cursor = valid.Result.Cursor }, Ct, receiveFirstRun: true);
            Assert.True((await receiver.Coordinator.SynchronizeAsync(receiver.Transport, Ct)).Applied);
            await using (var received = await receiver.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct))
            {
                Assert.Equal(2, await received.Goals.CountAsync(Ct));
                Assert.Equal(-5, (await received.GoalAllocations.SingleAsync(Ct)).Amount);
                Assert.Equal(0, (await received.Budgets.SingleAsync(Ct)).TotalLimit);
                Assert.Null((await received.ContributionPlans.SingleAsync(Ct)).Amount);
                Assert.Equal(1.1m, (await received.ExchangeRates.SingleAsync(Ct)).Rate);
            }
            Checkpoint("complete-valid-original-graph-published-and-received-through-original-coordinator");
            Assert.Equal(5000, (await client.Services.GetRequiredService<GoalStore>().GetGoalsAsync(cancellationToken: Ct)).Single(value => value.Id == earmark.Id).TargetAmount);
            Checkpoint("source-financial-values-remain-unchanged");

            SyncRecordKey Key(string table) => baseline.Single(value => value.Record.Table == table).Record;
            static SyncProposal Change(SyncProposal proposal, string field, JsonNode? value)
            {
                var row = JsonNode.Parse(proposal.Values!.Value.GetRawText())!.AsObject(); row[field] = value?.DeepClone();
                return proposal with { Values = JsonSerializer.SerializeToElement(row) };
            }
            async Task<(SyncRoundHandle Round, SyncCommitResult Result)> Submit(SyncProposal[] proposals)
            {
                var round = await client.Transport.OpenAsync(profile.Id, new(device.Id, Guid.NewGuid(), SyncSchema.Hash), Ct);
                var part = new SyncRoundPart(0, proposals); await client.Transport.PutPartAsync(profile.Id, device.Id, round.Id, part, Ct);
                var result = await client.Transport.CommitAsync(profile.Id, device.Id, round.Id,
                    new(1, proposals.Length, SyncProtocolDigest.Round([SyncProtocolDigest.Part(part)]), []), Ct);
                return (round, result);
            }
            async Task Refused(SyncProposal[] proposals, string reason)
            {
                Assert.Contains(SyncFinancialValidation.Validate([], Graph(proposals)), issue => issue.Reason == reason);
                var response = await Submit(proposals); Assert.False(response.Result.Committed);
                Assert.Contains(response.Result.Conflicts, value => value.Reason == reason); Assert.Equal(0, response.Result.Cursor);
                await using var scope = app.Services.CreateAsyncScope(); var service = scope.ServiceProvider.GetRequiredService<ServiceDbContext>();
                Assert.False(await service.Set<SyncRecordRow>().AnyAsync(row => row.SpaceId == profile.Id, Ct));
                Assert.False(await service.Set<SyncEventRow>().AnyAsync(row => row.SpaceId == profile.Id, Ct));
                await client.Transport.AbandonAsync(profile.Id, device.Id, response.Round.Id, Ct);
            }
        }
        finally { await app.StopAsync(CancellationToken.None); }
    }
}
