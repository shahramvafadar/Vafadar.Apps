using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Features.Sync;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Budgets;
using Vafadar.Zanance.Core.Categories;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Sync;
using Vafadar.Zanance.Core.Plans;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Core.Holdings;
using Vafadar.Zanance.Core.Goals;
using Vafadar.Zanance.App.Features.Holdings;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Sync;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Actual original review formatting and SQLite reference guards, without synthetic native controls.</summary>
[Trait("AT", "AT-292")]
public sealed class PersonalSyncReviewTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private sealed record ReviewFixture(Account Account, LedgerEntry Entry, PersonalSyncConflictReview Review, SyncRecord AccountRecord);

    private static async Task<ReviewFixture> SeedAsync(FlowFixture f)
    {
        var account = new Account { Name = "Fictitious KWD cash", CurrencyCode = "KWD", OpeningDate = new(2026, 10, 15) };
        await f.Store.SaveAccountAsync(account, Ct);
        var sync = f.Services.GetRequiredService<PersonalSyncStore>();
        await sync.ConnectAsync(await sync.ReviewAsync(Ct), new("https://127.0.0.1:5048/", new('1', 64)),
            "fictitious-review-owner", Guid.NewGuid(), new(Guid.NewGuid(), "Fictitious review copy", SyncSchema.Hash, 0, false), Ct);
        var entry = new LedgerEntry { AccountId = account.Id, Amount = 1400, Kind = EntryKind.Expense,
            Date = new(2026, 10, 15), Title = "Fictitious purchase", Note = "Fictitious note" };
        Assert.True((await f.Store.SaveEntryAsync(entry, Ct)).Succeeded);
        var local = await ReadCapturedAsync(f, "Entries", entry.Id); var owner = await sync.ReviewAsync(Ct);
        var accountRow = await ReadCapturedAsync(f, "Accounts", account.Id);
        var current = local with { Version = 5, Values = Change(local.Values!.Value, "Amount", JsonValue.Create(1300L)) };
        var conflict = new SyncConflict(new(local.Record, 3, local.Values), null, current, ["Amount"], "CompetingChanges");
        var payload = JsonSerializer.Serialize(conflict, PersonalSyncJsonContext.Default.SyncConflict);
        var operation = Guid.NewGuid(); var digest = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(payload)));
        long sequence;
        await using (var db = await f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct))
        {
            await db.Database.OpenConnectionAsync(Ct); await using var command = db.Database.GetDbConnection().CreateCommand();
            command.CommandText = "SELECT MAX(Sequence) FROM SyncOutbox"; sequence = Convert.ToInt64(await command.ExecuteScalarAsync(Ct));
            // These are fictional received journal facts, not a duplicated financial writer or native widget.
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO SyncClientRound(Id,OperationId,RemoteId,Cutoff,Phase,PartCount,SentParts,ContentHash,ThroughCursor,ReadCursor) VALUES(1,{operation},NULL,{sequence},1,1,1,{new string('1', 64)},NULL,0)", Ct);
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO SyncConflicts(TableName,RecordKey,Payload,LocalSequence,UseProposal) VALUES({local.Record.Table},{local.Record.Key},{payload},{sequence},NULL)", Ct);
        }
        var review = new PersonalSyncConflictReview(owner, operation, 1, sequence, digest, conflict, [local, accountRow]);
        return new(account, entry, review, accountRow);
    }

    private static async Task<SyncRecord> ReadCapturedAsync(FlowFixture f, string table, Guid id)
        => await ReadCapturedKeyAsync(f, table, JsonSerializer.Serialize(new[] { id.ToString("D").ToUpperInvariant() }));

    private static async Task<SyncRecord> ReadCapturedKeyAsync(FlowFixture f, string table, string identity)
    {
        var key = new SyncRecordKey(table, identity);
        await using var db = await f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct);
        await db.Database.OpenConnectionAsync(Ct); await using var command = db.Database.GetDbConnection().CreateCommand();
        command.CommandText = "SELECT Body FROM SyncOutbox WHERE TableName=$table AND RecordKey=$key ORDER BY Sequence DESC LIMIT 1";
        var tableParameter = command.CreateParameter(); tableParameter.ParameterName = "$table"; tableParameter.Value = table; command.Parameters.Add(tableParameter);
        var keyParameter = command.CreateParameter(); keyParameter.ParameterName = "$key"; keyParameter.Value = key.Key; command.Parameters.Add(keyParameter);
        var body = Assert.IsType<string>(await command.ExecuteScalarAsync(Ct)); using var value = JsonDocument.Parse(body);
        return new(key, 0, value.RootElement.Clone());
    }

    private static JsonElement Change(JsonElement source, string property, JsonNode? value)
    {
        var body = JsonNode.Parse(source.GetRawText())!.AsObject(); body[property] = value;
        return JsonSerializer.SerializeToElement(body);
    }
    private static PersonalSyncReviewPresenter Presenter(FlowFixture f) => new(f.Translator, new DateFormatter(f.Localization), f.Localization);

    [Fact]
    public async Task Initialization_reconnection_discards_only_unacknowledged_round_metadata_and_preserves_local_changes()
    {
        using var f = new FlowFixture(); var seeded = await SeedAsync(f); var sync = f.Services.GetRequiredService<PersonalSyncStore>();
        var connection = await sync.ReviewAsync(Ct); var before = await sync.StatusAsync(Ct);
        async Task<List<(long Sequence, string Table, string Key, string? Body)>> QueueRows()
        {
            await using var context = await f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct);
            await context.Database.OpenConnectionAsync(Ct); await using var query = context.Database.GetDbConnection().CreateCommand();
            query.CommandText = "SELECT Sequence,TableName,RecordKey,Body FROM SyncOutbox ORDER BY Sequence";
            await using var rows = await query.ExecuteReaderAsync(Ct); var result = new List<(long, string, string, string?)>();
            while (await rows.ReadAsync(Ct)) result.Add((rows.GetInt64(0), rows.GetString(1), rows.GetString(2), rows.IsDBNull(3) ? null : rows.GetString(3)));
            return result;
        }
        var previousRows = await QueueRows();
        var remote = new SyncProfile(connection.RemoteProfile!.Value, "Fictitious recovered initialization", SyncSchema.Hash, 0, true, connection.DeviceId!.Value);
        await sync.ConnectAsync(connection, new("https://127.0.0.1:5048/", new('1', 64)), connection.IdentityId!, connection.DeviceId.Value, remote, Ct,
            restartInitialization: true);
        Assert.Empty(await sync.ConflictsAsync(Ct)); Assert.True((await sync.StatusAsync(Ct)).PendingChanges >= before.PendingChanges);
        // Reconnection legitimately appends a full reconciliation. Every old nonce/body/tombstone must survive unchanged.
        var currentRows = await QueueRows(); Assert.All(previousRows, row => Assert.Contains(row, currentRows));
        Assert.Equal(1400, Assert.Single(await f.Store.GetEntriesAsync(cancellationToken: Ct)).Amount);
        await using var db = await f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct);
        await db.Database.OpenConnectionAsync(Ct); await using var command = db.Database.GetDbConnection().CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM SyncClientRound";
        Assert.Equal(0L, Convert.ToInt64(await command.ExecuteScalarAsync(Ct)));
    }

    [Fact]
    public async Task Initialization_reconnection_cannot_discard_an_already_acknowledged_local_round()
    {
        using var f = new FlowFixture(); await SeedAsync(f); var sync = f.Services.GetRequiredService<PersonalSyncStore>();
        await using (var db = await f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct))
            await db.Database.ExecuteSqlRawAsync("UPDATE SyncClientRound SET Phase=2,ThroughCursor=7,ReadCursor=7", Ct);
        var connection = await sync.ReviewAsync(Ct); var before = await sync.StatusAsync(Ct);
        var remote = new SyncProfile(connection.RemoteProfile!.Value, "Fictitious unsafe reset", SyncSchema.Hash, 0, true, connection.DeviceId!.Value);
        await Assert.ThrowsAsync<InvalidOperationException>(() => sync.ConnectAsync(connection, new("https://127.0.0.1:5048/", new('1', 64)),
            connection.IdentityId!, connection.DeviceId.Value, remote, Ct, restartInitialization: true));
        Assert.Equal(2, Assert.Single(await sync.ConflictsAsync(Ct)).Phase);
        Assert.Equal(before.PendingChanges, (await sync.StatusAsync(Ct)).PendingChanges);
        Assert.Equal(1400, Assert.Single(await f.Store.GetEntriesAsync(cancellationToken: Ct)).Amount);
    }

    [Theory]
    [InlineData(1250)]
    [InlineData(-1250)]
    public async Task Goal_allocation_review_preserves_signed_account_currency_and_goal_identity(long amount)
    {
        using var f = new FlowFixture(); var seeded = await SeedAsync(f);
        var goal = new Goal { Name = "Fictitious earmarked saving", Type = GoalType.Earmark, CurrencyCode = "KWD", TargetAmount = 10000 };
        var store = f.Services.GetRequiredService<GoalStore>(); await store.SaveGoalAsync(goal, Ct);
        var allocation = new GoalAllocation { GoalId = goal.Id, AccountId = seeded.Account.Id, Amount = amount, Date = new(2026, 10, 15) };
        await store.AddAllocationAsync(allocation, Ct);
        var captured = await ReadCapturedAsync(f, "GoalAllocations", allocation.Id); var goalRow = await ReadCapturedAsync(f, "Goals", goal.Id);
        var remote = captured with { Version = 7, Values = Change(captured.Values!.Value, "Amount", JsonValue.Create(amount / 2)) };
        var review = seeded.Review with { Conflict = new(new(captured.Record, 6, captured.Values), null, remote, ["Amount"], "CompetingChanges"),
            LocalContext = [captured, goalRow, seeded.AccountRecord] };
        var row = Presenter(f).Create(review, []); Assert.True(row.CanChooseLocal); Assert.True(row.CanChooseRemote);
        var expected = Vafadar.Zanance.Core.Money.MoneyText.Format(amount, "KWD", f.Localization.CurrentCulture);
        Assert.Contains(row.Versions[0].Fields, field => field.Caption == f.Translator["Entry_Amount"] && field.Value == expected);
        Assert.Contains(row.Versions[0].Fields, field => field.Caption == f.Translator["Goal_Details"] && field.Value.StartsWith(goal.Name, StringComparison.Ordinal) && field.Value.Contains("KWD", StringComparison.Ordinal));
        Assert.Equal(1400, Assert.Single(await f.Store.GetEntriesAsync(cancellationToken: Ct)).Amount);
    }

    [Fact]
    public async Task Contribution_review_prepares_the_nested_holding_type_and_refuses_its_later_rename()
    {
        using var f = new FlowFixture(); var seeded = await SeedAsync(f);
        var type = new AssetType { Name = "Fictitious nested holding", Dimension = AssetDimension.Mass, PriceCurrencyCode = "KWD" };
        var holdings = f.Services.GetRequiredService<HoldingStore>(); Assert.True(await holdings.SaveTypeAsync(type, Ct));
        var goal = new Goal { Name = "Fictitious nested quantity goal", Type = GoalType.HoldingQuantity, AssetTypeId = type.Id, CurrencyCode = "KWD", TargetAmount = 50000 };
        var plan = new ContributionPlan { GoalId = goal.Id, Amount = 1000, Rule = new() { Start = new(2026, 10, 15) } };
        await f.Services.GetRequiredService<GoalStore>().SaveGoalWithContributionPlanAsync(goal, plan, Ct);
        var captured = await ReadCapturedAsync(f, "ContributionPlans", plan.Id);
        var conflict = new SyncConflict(new(captured.Record, 6, captured.Values), null,
            captured with { Version = 7, Values = Change(captured.Values!.Value, "Amount", JsonValue.Create(2000L)) }, ["Amount"], "CompetingChanges");
        var payload = JsonSerializer.Serialize(conflict, PersonalSyncJsonContext.Default.SyncConflict);
        await using (var db = await f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct))
        {
            await db.Database.ExecuteSqlRawAsync("DELETE FROM SyncConflicts", Ct);
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO SyncConflicts(TableName,RecordKey,Payload,LocalSequence,UseProposal) SELECT {captured.Record.Table},{captured.Record.Key},{payload},MAX(Sequence),NULL FROM SyncOutbox WHERE TableName={captured.Record.Table} AND RecordKey={captured.Record.Key}", Ct);
        }
        var sync = f.Services.GetRequiredService<PersonalSyncStore>(); var prepared = await sync.PrepareReviewsAsync(await sync.ConflictsAsync(Ct), Ct);
        var review = Assert.Single(prepared.Reviews);
        Assert.Contains(review.LocalContext!, row => row.Record.Table == "AssetTypes" && row.Values!.Value.GetProperty("Name").GetString() == type.Name);
        Assert.True(Presenter(f).Create(review, prepared.RemoteRecords).CanChooseLocal);
        type.Name = "Fictitious later holding caption"; Assert.True(await holdings.SaveTypeAsync(type, Ct));
        await Assert.ThrowsAsync<InvalidOperationException>(() => sync.ResolveAsync(review, true, Ct));
        Assert.Single(await sync.ConflictsAsync(Ct));
    }

    [Theory]
    [InlineData(GoalType.Earmark, AssetDimension.Mass)]
    [InlineData(GoalType.AccountBalance, AssetDimension.Mass)]
    [InlineData(GoalType.HoldingQuantity, AssetDimension.Mass)]
    [InlineData(GoalType.HoldingQuantity, AssetDimension.Count)]
    public async Task Goal_review_keeps_money_and_holding_quantity_units_distinct_in_target_and_contribution(GoalType kind, AssetDimension dimension)
    {
        using var f = new FlowFixture(); var seeded = await SeedAsync(f);
        var type = new AssetType { Name = "Fictitious goal holding", Dimension = dimension, Divisible = true, PriceCurrencyCode = "KWD" };
        var holdings = f.Services.GetRequiredService<HoldingStore>(); Assert.True(await holdings.SaveTypeAsync(type, Ct));
        var goal = new Goal { Name = "Fictitious reviewed goal", Type = kind, CurrencyCode = "KWD", TargetAmount = 12345,
            AccountId = kind == GoalType.AccountBalance ? seeded.Account.Id : null, AssetTypeId = kind == GoalType.HoldingQuantity ? type.Id : null,
            TargetDate = new(2027, 10, 15), Priority = GoalPriority.High, HomePin = 1, Protect = kind == GoalType.Earmark };
        var plan = new ContributionPlan { GoalId = goal.Id, Amount = 1250, Method = ContributionMethod.FixedAmount,
            AssumedPricePerUnitMilli = kind == GoalType.HoldingQuantity ? 3456123 : null,
            Rule = new() { Frequency = Frequency.Monthly, Start = new(2026, 10, 15) }, ReminderEnabled = true };
        await f.Services.GetRequiredService<GoalStore>().SaveGoalWithContributionPlanAsync(goal, plan, Ct);
        var goalRow = await ReadCapturedAsync(f, "Goals", goal.Id); var planRow = await ReadCapturedAsync(f, "ContributionPlans", plan.Id);
        var typeRow = await ReadCapturedAsync(f, "AssetTypes", type.Id);
        PersonalSyncConflictReview Review(SyncRecord record, string field, long next) => seeded.Review with
        { Conflict = new(new(record.Record, 6, record.Values), null, record with { Version = 7, Values = Change(record.Values!.Value, field, JsonValue.Create(next)) },
            [field], "CompetingChanges"), LocalContext = new[] { record, goalRow, typeRow, seeded.AccountRecord }.DistinctBy(row => row.Record).ToArray() };
        var target = Presenter(f).Create(Review(goalRow, "TargetAmount", 23456), []);
        var contribution = Presenter(f).Create(Review(planRow, "Amount", 2250), []);
        Assert.True(target.CanChooseLocal); Assert.True(target.CanChooseRemote);
        Assert.True(contribution.CanChooseLocal); Assert.True(contribution.CanChooseRemote);
        var expected = kind == GoalType.HoldingQuantity
            ? new HoldingText(f.Translator, f.Localization, new DateFormatter(f.Localization)).Quantity(1250, type)
            : Vafadar.Zanance.Core.Money.MoneyText.Format(1250, "KWD", f.Localization.CurrentCulture);
        Assert.Contains(contribution.Versions[0].Fields, field => field.Caption == f.Translator["Entry_Amount"] && field.Value == expected);
        if (kind == GoalType.HoldingQuantity)
        {
            Assert.Contains(contribution.Versions[0].Fields, field => field.Caption == f.Translator["Sync_Field_AssumedPrice"] && field.Value.Contains("3.456123", StringComparison.Ordinal));
            var missing = Presenter(f).Create(Review(planRow, "Amount", 2250) with { LocalContext = [planRow, goalRow] }, []);
            Assert.False(missing.CanChooseLocal); Assert.False(missing.CanChooseRemote);
        }
    }

    [Fact]
    public async Task Remote_account_caption_and_currency_come_from_its_authenticated_context_instead_of_local_edits()
    {
        using var f = new FlowFixture(); var seeded = await SeedAsync(f);
        var localAccount = seeded.AccountRecord with { Values = Change(seeded.AccountRecord.Values!.Value, "Name", JsonValue.Create("Fictitious local account name")) };
        var remoteAccount = seeded.AccountRecord with { Version = 9, Values = Change(seeded.AccountRecord.Values!.Value, "Name", JsonValue.Create("Fictitious authenticated account name")) };
        var review = seeded.Review with { Conflict = seeded.Review.Conflict with { Context = [remoteAccount] },
            LocalContext = [new(seeded.Review.Conflict.Proposal.Record, 0, seeded.Review.Conflict.Proposal.Values), localAccount] };
        var row = Presenter(f).Create(review, []);
        Assert.True(row.CanChooseLocal); Assert.True(row.CanChooseRemote);
        Assert.Contains(row.Versions[0].Fields, field => field.Value.StartsWith("Fictitious local account name", StringComparison.Ordinal));
        Assert.Contains(row.Versions[1].Fields, field => field.Value.StartsWith("Fictitious authenticated account name", StringComparison.Ordinal));
        Assert.DoesNotContain(row.Versions[1].Fields, field => field.Value.StartsWith("Fictitious local account name", StringComparison.Ordinal));
        var missing = Presenter(f).Create(review with { Conflict = review.Conflict with { Context = [] } }, []);
        Assert.False(missing.CanChooseLocal); Assert.False(missing.CanChooseRemote);
        Assert.DoesNotContain(missing.Versions[1].Fields, field => field.Value.Contains("KWD", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(AssetDimension.Mass)]
    [InlineData(AssetDimension.Count)]
    public async Task Holding_review_preserves_original_quantity_purity_weight_and_subminor_price_precision(AssetDimension dimension)
    {
        using var f = new FlowFixture(); var seeded = await SeedAsync(f);
        var store = f.Services.GetRequiredService<HoldingStore>();
        var type = new AssetType { Name = "Fictitious reviewed gold", Dimension = dimension,
            Kind = dimension == AssetDimension.Mass ? AssetKind.PreciousMetal : AssetKind.CoinOrBar,
            Metal = Metal.Gold, PurityPer10000 = 7500, UnitWeightMg = 8123, CountUnitName = "fictitious coin", Divisible = true, PriceCurrencyCode = "KWD" };
        Assert.True(await store.SaveTypeAsync(type, Ct));
        var location = new AssetLocation { Name = "Fictitious reviewed safe" }; await store.SaveLocationAsync(location, Ct);
        var entry = new AssetEvent { AssetTypeId = type.Id, LocationId = location.Id, Kind = AssetEventKind.Opening,
            Date = new(2026, 10, 15), Quantity = 1250, BasisAmount = 3456 };
        Assert.True((await store.SaveEventAsync(entry, [], Ct)).Succeeded);
        var valuation = new AssetValuation { AssetTypeId = type.Id, CurrencyCode = "KWD", Date = entry.Date, PricePerUnitMilli = 3456123 };
        await store.SaveValuationAsync(valuation, Ct);
        var typeRow = await ReadCapturedAsync(f, "AssetTypes", type.Id); var locationRow = await ReadCapturedAsync(f, "AssetLocations", location.Id);
        var eventRow = await ReadCapturedAsync(f, "AssetEvents", entry.Id); var priceRow = await ReadCapturedAsync(f, "AssetValuations", valuation.Id);
        PersonalSyncConflictReview Review(SyncRecord record, string field, long next) => seeded.Review with
        {
            Conflict = new(new(record.Record, 6, record.Values), null, record with { Version = 7, Values = Change(record.Values!.Value, field, JsonValue.Create(next)) },
                [field], "CompetingChanges"), LocalContext = new[] { record, typeRow, locationRow }.DistinctBy(row => row.Record).ToArray(),
        };
        var holding = new HoldingText(f.Translator, f.Localization, new DateFormatter(f.Localization));
        var quantityReview = Review(eventRow, "Quantity", 2250); var quantity = Presenter(f).Create(quantityReview, []);
        Assert.True(quantity.CanChooseLocal); Assert.True(quantity.CanChooseRemote);
        Assert.Contains(quantity.Versions[0].Fields, field => field.Caption == f.Translator["AssetEvent_Quantity"] && field.Value == holding.Quantity(1250, type));
        Assert.Contains(quantity.Versions[0].Fields, field => field.Value == location.Name);
        Assert.Contains(quantity.Versions[0].Fields, field => field.Value.Contains("3.456", StringComparison.Ordinal) && field.Value.Contains("KWD", StringComparison.Ordinal));
        var price = Presenter(f).Create(Review(priceRow, "PricePerUnitMilli", 3456124), []);
        Assert.True(price.CanChooseLocal); Assert.True(price.CanChooseRemote);
        Assert.Contains(price.Versions[0].Fields, field => field.Value.Contains("3.456123", StringComparison.Ordinal) && field.Value.Contains(holding.PerUnit(type), StringComparison.Ordinal));
        Assert.Contains(price.Versions[1].Fields, field => field.Value.Contains("3.456124", StringComparison.Ordinal));
        var typeReview = Presenter(f).Create(Review(typeRow, "PurityPer10000", 9999), []);
        Assert.True(typeReview.CanChooseLocal);
        Assert.Contains(typeReview.Versions[0].Fields, field => field.Caption == f.Translator["AssetType_Purity"] && field.Value == holding.Purity(type));
        var missing = Presenter(f).Create(quantityReview with { LocalContext = [eventRow, locationRow] }, []);
        Assert.False(missing.CanChooseLocal); Assert.False(missing.CanChooseRemote);
    }

    [Theory]
    [InlineData(Frequency.Monthly, PeriodCalendar.Gregorian, EndKind.AfterCount)]
    [InlineData(Frequency.Yearly, PeriodCalendar.Persian, EndKind.OnDate)]
    [InlineData(Frequency.Once, PeriodCalendar.Gregorian, EndKind.Never)]
    public async Task Plan_review_uses_original_recurrence_and_both_actual_transfer_currencies(Frequency frequency, PeriodCalendar calendar, EndKind end)
    {
        using var f = new FlowFixture(); var seeded = await SeedAsync(f);
        var destination = new Account { Name = "Fictitious JPY destination", CurrencyCode = "JPY", OpeningDate = new(2026, 1, 1) };
        await f.Store.SaveAccountAsync(destination, Ct);
        var plan = new Schedule { Name = "Fictitious transfer plan", AccountId = seeded.Account.Id, ToAccountId = destination.Id,
            Kind = EntryKind.Transfer, Amount = 3456, ToAmount = 500, AmountMode = AmountMode.Fixed,
            Rule = new() { Frequency = frequency, Start = new(2026, 1, 31), Calendar = calendar, End = end,
                Count = end == EndKind.AfterCount ? 3 : null, EndDate = end == EndKind.OnDate ? new(2027, 12, 31) : null,
                MissingDay = MissingDayPolicy.Skip, WeekendDays = (1 << (int)DayOfWeek.Saturday) | (1 << (int)DayOfWeek.Sunday),
                WeekendShift = WeekendShift.Before, HolidayRegion = "DE" },
            ReminderEnabled = true, ReminderOnDueDate = true, ReminderDaysBefore = 2, ReminderTime = new(9, 30),
            AutoPost = true, AutoPostFrom = new(2026, 10, 16), ContractProvider = "Fictitious provider", ContractRenews = true };
        await f.Services.GetRequiredService<PlanStore>().SaveScheduleAsync(plan, Ct);
        var captured = await ReadCapturedAsync(f, "Schedules", plan.Id); var destinationRow = await ReadCapturedAsync(f, "Accounts", destination.Id);
        var current = captured with { Version = 7, Values = Change(captured.Values!.Value, "ToAmount", JsonValue.Create(600L)) };
        var review = seeded.Review with { Conflict = new(new(captured.Record, 6, captured.Values), null, current, ["ToAmount"], "CompetingChanges"),
            LocalContext = [captured, seeded.AccountRecord, destinationRow] };
        var row = Presenter(f).Create(review, []);
        Assert.True(row.CanChooseLocal); Assert.True(row.CanChooseRemote);
        var fields = row.Versions[0].Fields;
        Assert.Contains(fields, field => field.Value.Contains("3.456", StringComparison.Ordinal) && field.Value.Contains("KWD", StringComparison.Ordinal));
        Assert.Contains(fields, field => field.Value.Contains("500", StringComparison.Ordinal) && field.Value.Contains("JPY", StringComparison.Ordinal));
        Assert.Contains(row.Versions[1].Fields, field => field.Value.Contains("600", StringComparison.Ordinal) && field.Value.Contains("JPY", StringComparison.Ordinal));
        var expected = new PlanText(f.Translator, new DateFormatter(f.Localization), f.Localization).Rule(plan.Rule);
        Assert.Contains(fields, field => field.Caption == f.Translator["Plan_Repeat"] && field.Value == expected);
        Assert.Contains(fields, field => field.Caption == f.Translator["Plan_ReminderTime"] && field.Value.Contains("9", StringComparison.Ordinal));
        Assert.Contains(fields, field => field.Caption == f.Translator["Plan_AutoPost"] && field.Value == f.Translator["Sync_Yes"]);
        var missing = Presenter(f).Create(review with { LocalContext = [captured, seeded.AccountRecord] }, []);
        Assert.False(missing.CanChooseLocal); Assert.False(missing.CanChooseRemote);
    }

    [Theory]
    [InlineData(BudgetMethod.Limits, BudgetRollover.None)]
    [InlineData(BudgetMethod.Envelopes, BudgetRollover.Surplus)]
    [InlineData(BudgetMethod.Flex, BudgetRollover.SurplusAndDeficit)]
    public async Task Budget_review_shows_actual_currency_scope_and_method_without_internal_identifiers(BudgetMethod method, BudgetRollover rollover)
    {
        using var f = new FlowFixture(); var seeded = await SeedAsync(f);
        var budget = new Budget { Year = 2026, Month = 10, CurrencyCode = "KWD", TotalLimit = 3456,
            AccountIds = [seeded.Account.Id], Method = method, Rollover = rollover };
        await f.Store.SaveBudgetAsync(budget, Ct);
        var captured = await ReadCapturedAsync(f, "Budgets", budget.Id);
        var current = captured with { Version = 7, Values = Change(captured.Values!.Value, "TotalLimit", JsonValue.Create(4567L)) };
        var conflict = new SyncConflict(new(captured.Record, 6, captured.Values), null, current, ["TotalLimit"], "CompetingChanges");
        var review = seeded.Review with { Conflict = conflict, LocalContext = [captured, seeded.AccountRecord] };
        var row = Presenter(f).Create(review, []);
        Assert.True(row.CanChooseLocal); Assert.True(row.CanChooseRemote);
        var local = row.Versions[0].Fields;
        Assert.Contains(local, field => field.Value.Contains("3.456", StringComparison.Ordinal) && field.Value.Contains("KWD", StringComparison.Ordinal));
        Assert.Contains(row.Versions[1].Fields, field => field.Value.Contains("4.567", StringComparison.Ordinal));
        Assert.Contains(local, field => field.Caption == f.Translator["Budget_Accounts"] && field.Value.Contains(seeded.Account.Name, StringComparison.Ordinal));
        Assert.Contains(local, field => field.Caption == f.Translator["Budget_Method"] && field.Value == f.Translator["Budget_Method" + method]);
        Assert.DoesNotContain(local, field => field.Value.Contains(seeded.Account.Id.ToString("D"), StringComparison.OrdinalIgnoreCase));
        var incomplete = Presenter(f).Create(review with { LocalContext = [captured] }, []);
        Assert.False(incomplete.CanChooseLocal); Assert.False(incomplete.CanChooseRemote);
    }

    [Fact]
    public async Task Budget_empty_scope_explains_all_eligible_accounts_and_distinguishes_zero_from_no_limit()
    {
        using var f = new FlowFixture(); var seeded = await SeedAsync(f);
        var budget = new Budget { Year = 2026, Month = 10, CurrencyCode = "KWD", TotalLimit = 3456 };
        await f.Store.SaveBudgetAsync(budget, Ct);
        var captured = await ReadCapturedAsync(f, "Budgets", budget.Id);
        var current = captured with { Version = 7, Values = Change(captured.Values!.Value, "TotalLimit", JsonValue.Create(0L)) };
        var review = seeded.Review with { Conflict = new(new(captured.Record, 6, captured.Values), null, current, ["TotalLimit"], "CompetingChanges"),
            LocalContext = [captured] };
        var row = Presenter(f).Create(review, []);
        Assert.True(row.CanChooseRemote);
        Assert.Contains(row.Versions[0].Fields, field => field.Value == f.Translator["Sync_AllBudgetAccounts"]);
        Assert.Contains(row.Versions[1].Fields, field => field.Value.Contains("0.000", StringComparison.Ordinal) && field.Value.Contains("KWD", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Budget_category_limit_uses_its_parent_budget_currency_and_actual_category_name(bool defaultCategory)
    {
        using var f = new FlowFixture(); var seeded = await SeedAsync(f);
        var category = new Category { Name = defaultCategory ? null : "Fictitious household supplies", SystemKey = defaultCategory ? "Food" : null, Kind = CategoryKind.Expense };
        await f.Store.SaveCategoryAsync(category, Ct);
        var budget = new Budget { Year = 2026, Month = 10, CurrencyCode = "KWD", AccountIds = [seeded.Account.Id],
            CategoryLimits = [new() { CategoryId = category.Id, Limit = 2345 }] };
        await f.Store.SaveBudgetAsync(budget, Ct);
        var table = SyncSchema.Manifest.Tables.Single(row => row.Name == "BudgetCategoryLimits");
        var ids = table.PrimaryKey.Select(name => (name == "BudgetId" ? budget.Id : category.Id).ToString("D").ToUpperInvariant()).ToArray();
        var captured = await ReadCapturedKeyAsync(f, table.Name, JsonSerializer.Serialize(ids));
        var parent = await ReadCapturedAsync(f, "Budgets", budget.Id); var categoryRow = await ReadCapturedAsync(f, "Categories", category.Id);
        var current = captured with { Version = 7, Values = Change(captured.Values!.Value, "Limit", JsonValue.Create(0L)) };
        var review = seeded.Review with { Conflict = new(new(captured.Record, 6, captured.Values), null, current, ["Limit"], "CompetingChanges"),
            LocalContext = [captured, parent, categoryRow, seeded.AccountRecord] };
        var row = Presenter(f).Create(review, []);
        Assert.True(row.CanChooseLocal); Assert.True(row.CanChooseRemote);
        Assert.Contains(row.Versions[0].Fields, field => field.Value.Contains("2.345", StringComparison.Ordinal) && field.Value.Contains("KWD", StringComparison.Ordinal));
        Assert.Contains(row.Versions[0].Fields, field => field.Value == (defaultCategory ? f.Translator["Category_Food"] : category.Name));
        Assert.Contains(row.Versions[0].Fields, field => field.Caption == f.Translator["Budget_Title"] && field.Value.Contains("2026", StringComparison.Ordinal));
        var missing = Presenter(f).Create(review with { LocalContext = [captured, categoryRow] }, []);
        Assert.False(missing.CanChooseLocal); Assert.False(missing.CanChooseRemote);
    }

    [Fact]
    public async Task Budget_preparation_reads_array_references_and_refuses_consent_after_a_displayed_account_rename()
    {
        using var f = new FlowFixture(); var seeded = await SeedAsync(f);
        var budget = new Budget { Year = 2026, Month = 10, CurrencyCode = "KWD", TotalLimit = 3456, AccountIds = [seeded.Account.Id] };
        await f.Store.SaveBudgetAsync(budget, Ct);
        var captured = await ReadCapturedAsync(f, "Budgets", budget.Id);
        var current = captured with { Version = 7, Values = Change(captured.Values!.Value, "TotalLimit", JsonValue.Create(4567L)) };
        var conflict = new SyncConflict(new(captured.Record, 6, captured.Values), null, current, ["TotalLimit"], "CompetingChanges");
        var payload = JsonSerializer.Serialize(conflict, PersonalSyncJsonContext.Default.SyncConflict);
        await using (var db = await f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct))
        {
            // Replace only fictional received review facts; financial rows still come from the original store.
            await db.Database.ExecuteSqlRawAsync("DELETE FROM SyncConflicts", Ct);
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO SyncConflicts(TableName,RecordKey,Payload,LocalSequence,UseProposal) SELECT {captured.Record.Table},{captured.Record.Key},{payload},MAX(Sequence),NULL FROM SyncOutbox WHERE TableName={captured.Record.Table} AND RecordKey={captured.Record.Key}", Ct);
        }
        var sync = f.Services.GetRequiredService<PersonalSyncStore>();
        var prepared = await sync.PrepareReviewsAsync(await sync.ConflictsAsync(Ct), Ct);
        var review = Assert.Single(prepared.Reviews);
        Assert.Contains(review.LocalContext!, row => row.Record == seeded.AccountRecord.Record);
        Assert.True(Presenter(f).Create(review, prepared.RemoteRecords).CanChooseLocal);
        seeded.Account.Name = "Fictitious changed budget scope name"; await f.Store.SaveAccountAsync(seeded.Account, Ct);
        await Assert.ThrowsAsync<InvalidOperationException>(() => sync.ResolveAsync(review, true, Ct));
        Assert.Single(await sync.ConflictsAsync(Ct));
    }

    [Fact]
    public async Task Amount_comparison_preserves_the_actual_three_decimal_currency_and_hides_internal_identity()
    {
        using var f = new FlowFixture(); var seeded = await SeedAsync(f); var row = Presenter(f).Create(seeded.Review, []);
        Assert.True(row.CanChooseLocal); Assert.True(row.CanChooseRemote);
        var local = Assert.Single(row.Versions[0].Fields, field => field.Caption == f.Translator["Entry_Amount"]);
        var remote = Assert.Single(row.Versions[1].Fields, field => field.Caption == f.Translator["Entry_Amount"]);
        Assert.Contains("1.400", local.Value); Assert.Contains("KWD", local.Value);
        Assert.Contains("1.300", remote.Value); Assert.Contains("KWD", remote.Value);
        Assert.Contains('\u2066', local.Value); Assert.Contains('\u200e', local.Value);
        var all = string.Join(" ", row.Versions.SelectMany(version => version.Fields).Select(field => field.Value));
        Assert.DoesNotContain(seeded.Entry.Id.ToString("D"), all, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(seeded.Account.Id.ToString("D"), all, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Missing_account_currency_does_not_become_a_guessed_two_decimal_EUR_amount()
    {
        using var f = new FlowFixture(); var seeded = await SeedAsync(f);
        var missing = seeded.Review with { LocalContext = seeded.Review.LocalContext!.Where(row => row.Record.Table != "Accounts").ToArray() };
        var row = Presenter(f).Create(missing, []);
        Assert.False(row.CanChooseLocal); Assert.False(row.CanChooseRemote); Assert.Equal(f.Translator["Sync_ReviewIncomplete"], row.Hint);
        Assert.DoesNotContain(row.Versions.SelectMany(version => version.Fields), field => field.Value.Contains("EUR", StringComparison.Ordinal));
    }

    [Fact]
    public async Task A_changed_group_relationship_is_not_hidden_behind_an_enabled_whole_record_choice()
    {
        using var f = new FlowFixture(); var seeded = await SeedAsync(f);
        var local = Change(seeded.Review.Conflict.Proposal.Values!.Value, "GroupId", JsonValue.Create(Guid.NewGuid().ToString("D").ToUpperInvariant()));
        var conflict = seeded.Review.Conflict with { Proposal = seeded.Review.Conflict.Proposal with { Values = local } };
        var review = seeded.Review with { Conflict = conflict, LocalContext = [new(conflict.Proposal.Record, 0, local), seeded.AccountRecord] };
        var row = Presenter(f).Create(review, []);
        Assert.False(row.CanChooseLocal); Assert.False(row.CanChooseRemote); Assert.Equal(f.Translator["Sync_ReviewIncomplete"], row.Hint);
    }

    [Fact]
    public async Task Duplicate_review_shows_the_existing_payment_separately_and_cannot_create_a_second_full_payment()
    {
        using var f = new FlowFixture(); var seeded = await SeedAsync(f);
        var otherId = Guid.NewGuid().ToString("D").ToUpperInvariant();
        var existing = seeded.Review.Conflict.Current! with { Record = new("Entries", JsonSerializer.Serialize(new[] { otherId })),
            Values = Change(seeded.Review.Conflict.Current.Values!.Value, "Id", JsonValue.Create(otherId)) };
        var review = seeded.Review with { Conflict = seeded.Review.Conflict with
        { Current = null, RelatedRecord = existing, ExistingSettlement = existing.Record, Reason = "OccurrenceAlreadySettled" } };
        var row = Presenter(f).Create(review, [existing]);
        Assert.False(row.CanChooseLocal); Assert.True(row.CanChooseRemote); Assert.Equal(3, row.Versions.Count);
        Assert.Equal(f.Translator["Sync_KeepExisting"], row.RemoteAction);
        Assert.Contains(row.Versions[2].Fields, field => field.Caption == f.Translator["Entry_Amount"] && field.Value.Contains("1.300", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Preparing_real_reference_names_binds_consent_to_the_actual_unchanged_local_relationships()
    {
        using var f = new FlowFixture(); var seeded = await SeedAsync(f); var sync = f.Services.GetRequiredService<PersonalSyncStore>();
        var prepared = await sync.PrepareReviewsAsync(await sync.ConflictsAsync(Ct), Ct);
        var review = Assert.Single(prepared.Reviews);
        Assert.Contains(review.LocalContext!, row => row.Record.Table == "Accounts" && row.Values!.Value.GetProperty("Name").GetString() == seeded.Account.Name);
        seeded.Account.Name = "Fictitious renamed account"; await f.Store.SaveAccountAsync(seeded.Account, Ct);
        await Assert.ThrowsAsync<InvalidOperationException>(() => sync.ResolveAsync(review, false, Ct));
        Assert.Single(await sync.ConflictsAsync(Ct)); Assert.Equal(1400, Assert.Single(await f.Store.GetEntriesAsync(cancellationToken: Ct)).Amount);
    }

    [Fact]
    public async Task Stable_prepared_choice_records_only_the_decision_and_does_not_write_the_selected_money_early()
    {
        using var f = new FlowFixture(); await SeedAsync(f); var sync = f.Services.GetRequiredService<PersonalSyncStore>();
        var prepared = await sync.PrepareReviewsAsync(await sync.ConflictsAsync(Ct), Ct);
        await sync.ResolveAsync(Assert.Single(prepared.Reviews), false, Ct);
        Assert.Empty(await sync.ConflictsAsync(Ct)); Assert.Equal(1400, Assert.Single(await f.Store.GetEntriesAsync(cancellationToken: Ct)).Amount);
    }

    [Theory]
    [InlineData("en")]
    [InlineData("fa")]
    [InlineData("de")]
    public async Task Linked_group_review_displays_every_original_event_payment_and_fee_as_one_version_choice(string language)
    {
        using var f = new FlowFixture(); var seeded = await SeedAsync(f);
        f.Localization.SetLanguage(f.Localization.SupportedLanguages.First(item => item.CultureName == language));
        var holdings = f.Services.GetRequiredService<HoldingStore>();
        var type = new AssetType { Name = "Fictitious grouped gold", Dimension = AssetDimension.Mass, PriceCurrencyCode = "KWD" };
        Assert.True(await holdings.SaveTypeAsync(type, Ct)); var location = new AssetLocation { Name = "Fictitious grouped safe" };
        await holdings.SaveLocationAsync(location, Ct);
        var purchase = new AssetEvent { AssetTypeId = type.Id, LocationId = location.Id, Kind = AssetEventKind.Purchase,
            Date = new(2026, 10, 15), Quantity = 1000, BasisAmount = 3456 };
        var payment = new LedgerEntry { AccountId = seeded.Account.Id, Date = purchase.Date, Kind = EntryKind.AssetPurchase, Amount = 3456 };
        var feesCategory = new Category { Name = "Fictitious group fees", Kind = CategoryKind.Expense };
        await f.Store.SaveCategoryAsync(feesCategory, Ct);
        var fee = new LedgerEntry { AccountId = seeded.Account.Id, Date = purchase.Date, Kind = EntryKind.Expense, Amount = 10,
            CategoryId = feesCategory.Id };
        Assert.True((await holdings.SaveEventAsync(purchase, [payment, fee], Ct)).Succeeded);
        var rows = new[] { await ReadCapturedAsync(f, "AssetEvents", purchase.Id), await ReadCapturedAsync(f, "Entries", payment.Id),
            await ReadCapturedAsync(f, "Entries", fee.Id) };
        var dependencies = new[] { seeded.AccountRecord, await ReadCapturedAsync(f, "AssetTypes", type.Id),
            await ReadCapturedAsync(f, "AssetLocations", location.Id), await ReadCapturedAsync(f, "Categories", fee.CategoryId!.Value) };
        var remote = rows.Select(row => row with { Version = 7, Values = Change(row.Values!.Value,
            row.Record.Table == "AssetEvents" ? "BasisAmount" : "Amount", JsonValue.Create(row.Record == rows[2].Record ? 20 : 4567)) }).ToArray();
        var reviews = rows.Select(row => seeded.Review with { Conflict = new(new(row.Record, 6, row.Values), null,
            remote.Single(item => item.Record == row.Record), ["GroupId"], "LinkedGroupReview", Context: remote.Concat(dependencies).ToArray(),
            LinkedGroup: purchase.GroupId), LocalContext = rows.Concat(dependencies).ToArray() }).ToArray();
        var root = reviews[0] with { GroupMembers = reviews.Skip(1).ToArray() };
        var item = Presenter(f).Create(root, remote.Concat(dependencies).ToArray());
        Assert.True(item.CanChooseLocal); Assert.True(item.CanChooseRemote); Assert.Equal(6, item.Versions.Count);
        Assert.Equal(f.Translator["Sync_LinkedGroup"], item.Title);
        Assert.All(item.Versions, version => Assert.Contains(version.Fields, field => field.Caption == f.Translator["Sync_LinkedGroup"] &&
            field.Value == f.Translator["Sync_GroupMember"]));
        Assert.DoesNotContain(item.Versions.SelectMany(version => version.Fields), field => field.Value.Contains(purchase.GroupId!.Value.ToString(), StringComparison.OrdinalIgnoreCase));
        var laterFee = remote[2] with { Record = new("Entries", JsonSerializer.Serialize(new[] { Guid.NewGuid().ToString("D").ToUpperInvariant() })) };
        var extraContext = remote.Concat(dependencies).Append(laterFee).ToArray();
        var withRemoteFee = root with { Conflict = root.Conflict with { Context = extraContext },
            GroupMembers = root.GroupMembers!.Select(member => member with { Conflict = member.Conflict with { Context = extraContext } }).ToArray() };
        var expanded = Presenter(f).Create(withRemoteFee, extraContext);
        Assert.False(expanded.CanChooseLocal); Assert.True(expanded.CanChooseRemote); Assert.Equal(7, expanded.Versions.Count);
    }

    [Theory]
    [InlineData("en")]
    [InlineData("fa")]
    [InlineData("de")]
    public async Task Portable_record_reviews_preserve_original_money_named_scope_settings_and_daily_forecast(string language)
    {
        using var f = new FlowFixture(); var seeded = await SeedAsync(f);
        f.Localization.SetLanguage(f.Localization.SupportedLanguages.First(item => item.CultureName == language));
        var target = new Account { Name = "Fictitious JPY destination", CurrencyCode = "JPY", OpeningDate = new(2026, 10, 1) };
        await f.Store.SaveAccountAsync(target, Ct);
        var parent = new Category { Name = "Fictitious parent", Kind = CategoryKind.Expense }; await f.Store.SaveCategoryAsync(parent, Ct);
        var child = new Category { Name = "Fictitious child", ParentId = parent.Id, Kind = CategoryKind.Expense,
            SpendingType = SpendingType.Fixed, IsEssential = true, Icon = "Home", Color = "#112233" }; await f.Store.SaveCategoryAsync(child, Ct);
        var rule = new CategoryRule { CategoryId = child.Id, Kind = CategoryKind.Expense, Match = "Fictitious shop" }; await f.Store.SaveCategoryRuleAsync(rule, Ct);
        var template = new EntryTemplate { Name = "Fictitious transfer template", Kind = EntryKind.Transfer,
            AccountId = seeded.Account.Id, ToAccountId = target.Id, Amount = 1250, Icon = "Home" }; await f.Store.SaveTemplateAsync(template, Ct);
        var filter = new SavedFilter { Name = "Fictitious named filter", AccountId = seeded.Account.Id, CategoryIds = [child.Id],
            CategoryName = child.Name, From = new(2026, 10, 1), To = new(2026, 10, 15), Kind = 1, Period = 2, InTotalsOnly = true,
            UnreviewedOnly = true, Search = "#fictitious" }; await f.Store.SaveSavedFilterAsync(filter, Ct);
        var rate = new Vafadar.Zanance.Core.Rates.ExchangeRate { FromCurrencyCode = "KWD", ToCurrencyCode = "JPY", Date = new(2026, 10, 15),
            Rate = 123.456789m, IsEstimate = true }; await f.Store.SaveRateAsync(rate, Ct);
        var settings = await f.Store.GetSettingsAsync(Ct); settings.DefaultAccountId = seeded.Account.Id; settings.ReportCurrencyCode = "JPY";
        settings.DefaultCurrencyCode = "KWD"; settings.Mode = Vafadar.Zanance.Core.Settings.ExperienceMode.Advanced;
        settings.BudgetCalendar = PeriodCalendar.Persian; settings.EssentialEstimate = 1250; settings.EssentialEstimateCurrency = "KWD";
        settings.EssentialEstimatePeriod = Vafadar.Zanance.Core.Settings.EstimatePeriod.Week; settings.RateFreshnessDays = 0;
        settings.DisplayUnits = "KWD|Fictitious unit|2"; settings.HomeLayout = "Budget,-Accounts,Recent";
        settings.ReviewProgress = "2026-09:Unreviewed,Plans"; await f.Store.SaveSettingsAsync(settings, Ct);
        var forecast = new Vafadar.Zanance.Core.Forecasts.ForecastSnapshot { Name = "Fictitious immutable forecast", CurrencyCode = "KWD",
            AccountIds = seeded.Account.Id.ToString("D"), BaseDate = new(2026, 10, 15), Horizon = new(2026, 10, 17),
            Path = "1250,-1250," + long.MinValue.ToString(System.Globalization.CultureInfo.InvariantCulture), Minimum = long.MinValue,
            MinimumDate = new(2026, 10, 17), UnknownCount = 2, Assumptions = "Fictitious unchanged assumptions", AppVersion = "0.1" };
        await f.Store.SaveForecastSnapshotAsync(forecast, Ct);
        var records = new[] { seeded.AccountRecord, await ReadCapturedAsync(f, "Accounts", target.Id), await ReadCapturedAsync(f, "Categories", parent.Id),
            await ReadCapturedAsync(f, "Categories", child.Id), await ReadCapturedAsync(f, "CategoryRules", rule.Id),
            await ReadCapturedAsync(f, "Templates", template.Id), await ReadCapturedAsync(f, "SavedFilters", filter.Id),
            await ReadCapturedAsync(f, "ExchangeRates", rate.Id), await ReadCapturedKeyAsync(f, "Settings", "[]"),
            await ReadCapturedAsync(f, "ForecastSnapshots", forecast.Id) };
        var presenter = Presenter(f);
        PersonalSyncConflictItem Item(string table)
        {
            var record = table == "Categories" ? records.Single(row => row.Record == new SyncRecordKey(table,
                JsonSerializer.Serialize(new[] { child.Id.ToString("D").ToUpperInvariant() }))) : records.Single(row => row.Record.Table == table);
            var field = table switch { "CategoryRules" => "Match", "ExchangeRates" => "Rate", "Settings" => "EssentialEstimate", _ => "Name" };
            var value = field == "EssentialEstimate" ? JsonValue.Create(0) : JsonValue.Create(field == "Rate" ? "130.000001" : "Fictitious changed value");
            var remote = record with { Version = 7, Values = Change(record.Values!.Value, field, value) };
            var context = records.Where(row => row.Record != record.Record).Append(remote).ToArray();
            return presenter.Create(seeded.Review with { Conflict = new(new(record.Record, 6, record.Values), null, remote, [field],
                "CompetingChanges", Context: context), LocalContext = records }, context);
        }
        foreach (var table in new[] { "Categories", "CategoryRules", "Templates", "SavedFilters", "ExchangeRates", "Settings", "ForecastSnapshots" })
        {
            var item = Item(table); Assert.True(item.CanChooseLocal, table + ": local"); Assert.True(item.CanChooseRemote, table + ": service");
            Assert.DoesNotContain(item.Versions.SelectMany(version => version.Fields), field => field.Value.Contains(seeded.Account.Id.ToString(), StringComparison.OrdinalIgnoreCase));
        }
        Assert.Contains(Item("Templates").Versions[0].Fields, field => field.Value.Contains("KWD", StringComparison.Ordinal) && field.Value.Contains("1", StringComparison.Ordinal));
        Assert.Contains(Item("Templates").Versions[0].Fields, field => field.Value.Contains(target.Name, StringComparison.Ordinal));
        Assert.Contains(Item("SavedFilters").Versions[0].Fields, field => field.Value.Contains(child.Name!, StringComparison.Ordinal));
        Assert.Contains(Item("ExchangeRates").Versions[0].Fields, field => field.Value.Contains("KWD", StringComparison.Ordinal) && field.Value.Contains("JPY", StringComparison.Ordinal));
        Assert.Contains(Item("Settings").Versions[0].Fields, field => field.Value.Contains("Fictitious unit", StringComparison.Ordinal));
        Assert.Contains(Item("Settings").Versions[1].Fields, field => field.Caption == f.Translator["Settings_Essential"] &&
            field.Value == Vafadar.Zanance.Core.Money.MoneyText.Format(0, "KWD", f.Localization.CurrentCulture));
        var path = Assert.Single(Item("ForecastSnapshots").Versions[0].Fields, field => field.Caption == f.Translator["Forecast_Path"]);
        Assert.Equal(3, path.Value.Split(Environment.NewLine).Length);
        Assert.Contains(Vafadar.Zanance.Core.Money.MoneyText.Format(long.MinValue, "KWD", f.Localization.CurrentCulture), path.Value);
    }

    [Theory]
    [InlineData("DisplayUnits", "KWD|Fictitious unit|0")]
    [InlineData("DisplayUnits", "KWD|First unit|2;KWD|Second unit|3")]
    [InlineData("HomeLayout", "Budget,UnknownSection")]
    [InlineData("ReviewProgress", "2026-09:UnknownStep")]
    public async Task Malformed_portable_setting_changes_cannot_authorize_a_hidden_value(string field, string changed)
    {
        using var f = new FlowFixture(); var seeded = await SeedAsync(f);
        await f.Store.SaveSettingsAsync(await f.Store.GetSettingsAsync(Ct), Ct);
        var settings = await ReadCapturedKeyAsync(f, "Settings", "[]");
        var remote = settings with { Version = 7, Values = Change(settings.Values!.Value, field, JsonValue.Create(changed)) };
        var item = Presenter(f).Create(seeded.Review with { Conflict = new(new(settings.Record, 6, settings.Values), null, remote,
            [field], "CompetingChanges", Context: [seeded.AccountRecord]), LocalContext = [settings, seeded.AccountRecord] }, []);
        Assert.False(item.CanChooseLocal); Assert.False(item.CanChooseRemote);
    }

    [Fact]
    public async Task Original_empty_saved_forecast_keeps_empty_scope_and_path_without_claiming_all_accounts()
    {
        using var f = new FlowFixture(); var seeded = await SeedAsync(f);
        var snapshot = new Vafadar.Zanance.Core.Forecasts.ForecastSnapshot { Name = "Fictitious empty forecast", CurrencyCode = "KWD",
            AccountIds = "", Path = "", BaseDate = new(2026, 10, 15), Horizon = new(2026, 10, 15), MinimumDate = new(2026, 10, 15) };
        await f.Store.SaveForecastSnapshotAsync(snapshot, Ct);
        var record = await ReadCapturedAsync(f, "ForecastSnapshots", snapshot.Id);
        var remote = record with { Version = 7, Values = Change(record.Values!.Value, "Name", JsonValue.Create("Fictitious renamed empty forecast")) };
        var item = Presenter(f).Create(seeded.Review with { Conflict = new(new(record.Record, 6, record.Values), null, remote,
            ["Name"], "CompetingChanges", Context: []), LocalContext = [record] }, []);
        Assert.True(item.CanChooseLocal); Assert.True(item.CanChooseRemote);
        Assert.Contains(item.Versions[0].Fields, field => field.Caption == f.Translator["Forecast_Path"] && field.Value == f.Translator["Sync_ReviewEmpty"]);
        Assert.DoesNotContain(item.Versions[0].Fields, field => field.Value == f.Translator["Sync_AllBudgetAccounts"]);
    }

    [Theory]
    [InlineData("Settings")]
    [InlineData("ForecastSnapshots")]
    public async Task Prepared_default_account_and_csv_forecast_scope_require_unchanged_original_account_names(string table)
    {
        using var f = new FlowFixture(); var seeded = await SeedAsync(f);
        SyncRecord record;
        if (table == "Settings")
        {
            var settings = await f.Store.GetSettingsAsync(Ct); settings.DefaultAccountId = seeded.Account.Id;
            await f.Store.SaveSettingsAsync(settings, Ct); record = await ReadCapturedKeyAsync(f, table, "[]");
        }
        else
        {
            var saved = new Vafadar.Zanance.Core.Forecasts.ForecastSnapshot { Name = "Fictitious referenced forecast", CurrencyCode = "KWD",
                AccountIds = seeded.Account.Id.ToString("D"), Path = "1250", BaseDate = new(2026, 10, 15), Horizon = new(2026, 10, 15),
                Minimum = 1250, MinimumDate = new(2026, 10, 15) };
            await f.Store.SaveForecastSnapshotAsync(saved, Ct); record = await ReadCapturedAsync(f, table, saved.Id);
        }
        var conflict = new SyncConflict(new(record.Record, 6, record.Values), null, record with { Version = 7 }, [], "CompetingChanges", Context: []);
        var payload = JsonSerializer.Serialize(conflict, PersonalSyncJsonContext.Default.SyncConflict);
        await using (var db = await f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct))
        {
            await db.Database.OpenConnectionAsync(Ct); await using var query = db.Database.GetDbConnection().CreateCommand();
            query.CommandText = "SELECT MAX(Sequence) FROM SyncOutbox WHERE TableName=$table AND RecordKey=$key";
            var t = query.CreateParameter(); t.ParameterName = "$table"; t.Value = table; query.Parameters.Add(t);
            var k = query.CreateParameter(); k.ParameterName = "$key"; k.Value = record.Record.Key; query.Parameters.Add(k);
            var boundary = Convert.ToInt64(await query.ExecuteScalarAsync(Ct));
            await db.Database.ExecuteSqlRawAsync("DELETE FROM SyncConflicts", Ct);
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO SyncConflicts(TableName,RecordKey,Payload,LocalSequence,UseProposal) VALUES({table},{record.Record.Key},{payload},{boundary},NULL)", Ct);
        }
        var sync = f.Services.GetRequiredService<PersonalSyncStore>();
        var prepared = await sync.PrepareReviewsAsync(await sync.ConflictsAsync(Ct), Ct); var review = Assert.Single(prepared.Reviews);
        Assert.Contains(review.LocalContext!, row => row.Record == seeded.AccountRecord.Record && row.Values?.GetProperty("Name").GetString() == seeded.Account.Name);
        seeded.Account.Name = "Fictitious renamed consent account"; await f.Store.SaveAccountAsync(seeded.Account, Ct);
        await Assert.ThrowsAsync<InvalidOperationException>(() => sync.ResolveAsync(review, false, Ct));
        Assert.Single(await sync.ConflictsAsync(Ct));
        var after = table == "Settings" ? await ReadCapturedKeyAsync(f, table, "[]") : await ReadCapturedKeyAsync(f, table, record.Record.Key);
        Assert.True(JsonElement.DeepEquals(record.Values!.Value, after.Values!.Value));
    }

    private static async Task<(EntryAttachment Attachment, byte[] Remote)> SeedAttachmentReviewAsync(FlowFixture f)
    {
        var seeded = await SeedAsync(f);
        var attachment = new EntryAttachment { EntryId = seeded.Entry.Id, FileName = "fictitious-local.pdf", ContentType = "application/pdf",
            Data = Encoding.UTF8.GetBytes("Fictitious local opaque content; native rendering is an explicit effect port.") };
        var remote = Encoding.UTF8.GetBytes("Fictitious remote opaque content; no actual PDF rendering is claimed here.");
        await f.Store.AddAttachmentAsync(attachment, Ct);
        var captured = await ReadCapturedAsync(f, "Attachments", attachment.Id);
        JsonElement Manifest(byte[] bytes) => JsonSerializer.SerializeToElement(new { sha256 = Convert.ToHexStringLower(SHA256.HashData(bytes)), length = bytes.Length });
        var local = Change(captured.Values!.Value, "Data", JsonSerializer.SerializeToNode(Manifest(attachment.Data)));
        var other = Change(Change(local, "Data", JsonSerializer.SerializeToNode(Manifest(remote))), "FileName", JsonValue.Create("fictitious-service.pdf"));
        var conflict = new SyncConflict(new(captured.Record, 6, local), null, new(captured.Record, 7, other), ["Data"], "CompetingChanges",
            Context: [seeded.AccountRecord, await ReadCapturedAsync(f, "Entries", seeded.Entry.Id)]);
        var payload = JsonSerializer.Serialize(conflict, PersonalSyncJsonContext.Default.SyncConflict);
        await using var db = await f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct);
        await db.Database.OpenConnectionAsync(Ct); await using var query = db.Database.GetDbConnection().CreateCommand();
        query.CommandText = "SELECT MAX(Sequence) FROM SyncOutbox WHERE TableName='Attachments'";
        var boundary = Convert.ToInt64(await query.ExecuteScalarAsync(Ct));
        await db.Database.ExecuteSqlRawAsync("DELETE FROM SyncConflicts", Ct);
        await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO SyncConflicts(TableName,RecordKey,Payload,LocalSequence,UseProposal) VALUES('Attachments',{captured.Record.Key},{payload},{boundary},NULL)", Ct);
        foreach (var bytes in new[] { attachment.Data, remote })
        {
            var hash = Convert.ToHexStringLower(SHA256.HashData(bytes));
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO SyncBlobTransfers(Hash,Length,Manifest) VALUES({hash},{bytes.Length},NULL)", Ct);
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO SyncBlobParts(Hash,Part,Data) VALUES({hash},0,{bytes})", Ct);
        }
        return (attachment, remote);
    }

    private static Vafadar.Zanance.App.Features.Sync.PersonalSyncViewModel AttachmentFlow(FlowFixture f, PersonalSyncFlowTests.EnvironmentPort port) =>
        new(f.Services.GetRequiredService<PersonalSyncStore>(), f.Services.GetRequiredService<PersonalSyncCoordinator>(), port, f.Platform,
            f.Translator, new DateFormatter(f.Localization), f.Localization);

    [Fact]
    public async Task Attachment_preview_navigation_and_close_do_not_select_or_replace_the_original_file()
    {
        using var f = new FlowFixture(); var seeded = await SeedAttachmentReviewAsync(f);
        using var port = new PersonalSyncFlowTests.EnvironmentPort(); var frames = new List<PersonalSyncRenderedPage>();
        port.RenderPage = (content, index, ct) =>
        {
            ct.ThrowIfCancellationRequested(); var count = content.Data.SequenceEqual(seeded.Remote) ? 2 : 1;
            Assert.InRange(index, 0, count - 1);
            var frame = new PersonalSyncRenderedPage(count, [(byte)(index + 1), 21, 22], 400, 800); frames.Add(frame); return Task.FromResult(frame);
        };
        var vm = AttachmentFlow(f, port); await vm.ActivateAsync(); var review = Assert.Single(vm.Conflicts);
        Assert.True(Assert.Single(vm.ReviewItems).CanChooseLocal); Assert.True(Assert.Single(vm.ReviewItems).CanChooseRemote);
        await vm.PreviewLocalCommand.ExecuteAsync(review); Assert.True(vm.HasAttachmentPreview);
        Assert.True(Assert.Single(vm.ReviewItems).CanChooseRemote);
        var width = vm.AttachmentImageWidth; vm.PreviewZoomInCommand.Execute(null); Assert.True(vm.AttachmentImageWidth > width);
        await vm.PreviewRemoteCommand.ExecuteAsync(review); Assert.True(vm.CanPreviewNext); Assert.False(vm.CanPreviewPrevious);
        Assert.True(Assert.Single(vm.ReviewItems).CanChooseLocal);
        await vm.PreviewNextCommand.ExecuteAsync(null); Assert.True(vm.CanPreviewPrevious); Assert.False(vm.CanPreviewNext);
        Assert.True(Assert.Single(vm.ReviewItems).CanChooseLocal); Assert.True(Assert.Single(vm.ReviewItems).CanChooseRemote);
        vm.CloseAttachmentPreviewCommand.Execute(null); Assert.False(vm.HasAttachmentPreview);
        Assert.All(frames, frame => Assert.All(frame.Data, value => Assert.Equal(0, value)));
        Assert.Equal(seeded.Attachment.Data, (await f.Store.GetAttachmentAsync(seeded.Attachment.Id, Ct))!.Data);
        Assert.Single(await f.Services.GetRequiredService<PersonalSyncStore>().ConflictsAsync(Ct));
        f.Platform.Confirmations.Enqueue(false); await vm.KeepRemoteCommand.ExecuteAsync(review);
        Assert.Single(await f.Services.GetRequiredService<PersonalSyncStore>().ConflictsAsync(Ct));
        vm.Deactivate();
    }

    [Fact]
    public async Task Attachment_render_completion_after_retirement_cannot_publish_an_image_or_enable_a_choice()
    {
        using var f = new FlowFixture(); var seeded = await SeedAttachmentReviewAsync(f);
        using var port = new PersonalSyncFlowTests.EnvironmentPort();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var deferred = new TaskCompletionSource<PersonalSyncRenderedPage>(TaskCreationOptions.RunContinuationsAsynchronously);
        port.RenderPage = (_, _, _) => { entered.SetResult(); return deferred.Task; };
        var vm = AttachmentFlow(f, port); await vm.ActivateAsync(); var rendering = vm.PreviewRemoteCommand.ExecuteAsync(Assert.Single(vm.Conflicts));
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(10), Ct); vm.Deactivate();
        var frame = new PersonalSyncRenderedPage(1, [51, 52, 53], 400, 800); deferred.SetResult(frame); await rendering;
        Assert.False(vm.HasAttachmentPreview); Assert.All(frame.Data, value => Assert.Equal(0, value));
        Assert.False(vm.CanInteract);
        Assert.Equal(seeded.Attachment.Data, (await f.Store.GetAttachmentAsync(seeded.Attachment.Id, Ct))!.Data);
    }

    [Fact]
    public async Task Attachment_edit_during_native_render_refuses_the_completed_preview_without_consuming_the_conflict()
    {
        using var f = new FlowFixture(); var seeded = await SeedAttachmentReviewAsync(f);
        using var port = new PersonalSyncFlowTests.EnvironmentPort(); PersonalSyncRenderedPage? frame = null;
        port.RenderPage = async (_, _, ct) =>
        {
            await using var db = await f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(ct);
            var file = await db.Attachments.SingleAsync(row => row.Id == seeded.Attachment.Id, ct); file.FileName = "fictitious-edited-during-preview.pdf";
            await db.SaveChangesAsync(ct); frame = new(1, [61, 62, 63], 400, 800); return frame;
        };
        var vm = AttachmentFlow(f, port); await vm.ActivateAsync(); await vm.PreviewLocalCommand.ExecuteAsync(Assert.Single(vm.Conflicts));
        Assert.False(vm.HasAttachmentPreview); Assert.NotNull(vm.Error); Assert.NotNull(frame); Assert.All(frame.Data, value => Assert.Equal(0, value));
        var store = f.Services.GetRequiredService<PersonalSyncStore>();
        await Assert.ThrowsAsync<InvalidOperationException>(() => store.ResolveAsync(Assert.Single(vm.Conflicts), true, Ct));
        await vm.ReloadCommand.ExecuteAsync(null); Assert.False(Assert.Single(vm.ReviewItems).CanChooseLocal);
        Assert.Single(await store.ConflictsAsync(Ct));
        Assert.Equal("fictitious-edited-during-preview.pdf", (await f.Store.GetAttachmentAsync(seeded.Attachment.Id, Ct))!.FileName);
        vm.Deactivate();
    }

    [Fact]
    public async Task Unsupported_native_attachment_preview_preserves_an_explicit_complete_file_choice()
    {
        using var f = new FlowFixture(); var seeded = await SeedAttachmentReviewAsync(f);
        using var port = new PersonalSyncFlowTests.EnvironmentPort();
        port.RenderPage = (_, _, _) => Task.FromException<PersonalSyncRenderedPage>(new InvalidDataException("Fictitious unsupported or locked file."));
        var vm = AttachmentFlow(f, port); await vm.ActivateAsync(); var review = Assert.Single(vm.Conflicts);
        await vm.PreviewLocalCommand.ExecuteAsync(review); Assert.NotNull(vm.Error); Assert.False(vm.HasAttachmentPreview);
        Assert.True(Assert.Single(vm.ReviewItems).CanChooseLocal); Assert.True(Assert.Single(vm.ReviewItems).CanChooseRemote);
        f.Platform.Confirmations.Enqueue(true); await vm.KeepLocalCommand.ExecuteAsync(review);
        Assert.Empty(await f.Services.GetRequiredService<PersonalSyncStore>().ConflictsAsync(Ct));
        Assert.Equal(seeded.Attachment.Data, (await f.Store.GetAttachmentAsync(seeded.Attachment.Id, Ct))!.Data);
        Assert.Equal(1400, Assert.Single(await f.Store.GetEntriesAsync(cancellationToken: Ct)).Amount);
        vm.Deactivate();
    }
}
