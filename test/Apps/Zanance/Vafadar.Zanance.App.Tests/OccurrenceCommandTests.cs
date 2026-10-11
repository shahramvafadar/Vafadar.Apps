using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Zanance.App.Features.Plans;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Plans;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Tests;

/// <summary>AT-138: actual occurrence commands, isolated SQLite and explicit native interaction ports.</summary>
[Trait("AT", "AT-138")]
public sealed class OccurrenceCommandTests
{
    private static readonly DateOnly Day = new(2026, 10, 9);
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Theory]
    [InlineData("full"), InlineData("partial"), InlineData("link"), InlineData("skip")]
    [InlineData("change"), InlineData("unskip"), InlineData("unsettle")]
    public async Task Actual_SQL_failure_shows_feedback_without_publication_or_draft_replacement_then_retry_succeeds(string action)
    {
        using var f = new FlowFixture(); var seed = await SeedAsync(f, action);
        var commands = new OccurrenceCommands(seed.Plans, f.Translator, f.Platform);
        await SqlAsync(f, "CREATE TRIGGER FailInsert BEFORE INSERT ON OccurrenceStates BEGIN SELECT RAISE(ABORT, 'owned failure'); END");
        await SqlAsync(f, "CREATE TRIGGER FailUpdate BEFORE UPDATE ON OccurrenceStates BEGIN SELECT RAISE(ABORT, 'owned failure'); END");
        var before = await SnapshotAsync(f); var published = 0; var events = 0;
        seed.Plans.Changed += (_, _) => events++;
        // The actual UI only replaces its draft in this successful-publication continuation.
        var draft = (Amount: "12.34", Date: Day.AddDays(-1), Due: Day.AddDays(3), Note: "Unsaved owned note");
        var original = draft;
        Task Completed() { published++; draft = default; return Task.CompletedTask; }
        f.Platform.Confirmations.Enqueue(true);
        Assert.False(await ActAsync(commands, seed, action, Completed));
        Assert.Equal(before, await SnapshotAsync(f)); Assert.Equal(original, draft);
        Assert.Equal(0, published); Assert.Equal(0, events); Assert.Single(f.Platform.Failures); Assert.Empty(f.Platform.Routes);
        await SqlAsync(f, "DROP TRIGGER FailInsert"); await SqlAsync(f, "DROP TRIGGER FailUpdate");
        f.Platform.Confirmations.Enqueue(true);
        Assert.True(await ActAsync(commands, seed, action, Completed));
        Assert.Equal(1, published); Assert.True(events > 0); Assert.NotEqual(before, await SnapshotAsync(f));
        Assert.Single(f.Platform.Failures);
    }

    [Theory]
    [InlineData("en", "link"), InlineData("fa", "link"), InlineData("de", "link")]
    [InlineData("es", "link"), InlineData("fr", "link"), InlineData("it", "link")]
    [InlineData("en", "unsettle"), InlineData("fa", "unsettle"), InlineData("de", "unsettle")]
    [InlineData("es", "unsettle"), InlineData("fr", "unsettle"), InlineData("it", "unsettle")]
    public async Task Cancelled_translated_consent_preserves_all_rows_and_never_publishes(string language, string action)
    {
        using var f = new FlowFixture(); f.Localization.SetLanguage(f.Localization.SupportedLanguages.First(l => l.CultureName == language));
        var seed = await SeedAsync(f, action); var before = await SnapshotAsync(f); var publications = 0;
        Assert.False(await ActAsync(new(seed.Plans, f.Translator, f.Platform), seed, action,
            () => { publications++; return Task.CompletedTask; }));
        Assert.Equal(before, await SnapshotAsync(f)); Assert.Equal(0, publications); Assert.Empty(f.Platform.Failures);
        var dialog = Assert.Single(f.Platform.ConfirmationDialogs);
        var key = action == "link" ? "Occurrence_Link" : "Occurrence_Undo";
        Assert.Equal(f.Translator[key], dialog.Title); Assert.Equal(f.Translator[action == "link" ? key : "Occurrence_ReopenConfirm"], dialog.Accept);
        Assert.Equal(f.Translator["Common_Cancel"], dialog.Cancel); Assert.NotEqual(dialog.Accept, dialog.Cancel);
    }

    [Fact]
    public async Task Consent_gate_prevents_a_different_command_from_writing_until_cancelled()
    {
        using var f = new FlowFixture(); var seed = await SeedAsync(f, "link"); var before = await SnapshotAsync(f);
        var answer = new TaskCompletionSource<bool>(); f.Platform.PendingConfirmation = () => answer.Task;
        var commands = new OccurrenceCommands(seed.Plans, f.Translator, f.Platform);
        var pending = ActAsync(commands, seed, "link", () => Task.CompletedTask);
        Assert.False(await ActAsync(commands, seed, "skip", () => Task.CompletedTask));
        Assert.Equal(before, await SnapshotAsync(f)); Assert.Single(f.Platform.ConfirmationDialogs);
        answer.SetResult(false); Assert.False(await pending);
        Assert.True(await ActAsync(commands, seed, "skip", () => Task.CompletedTask));
    }

    [Fact]
    public async Task Failure_dialog_holds_the_gate_and_releases_it_after_dismissal()
    {
        using var f = new FlowFixture(); var seed = await SeedAsync(f, "skip");
        await SqlAsync(f, "CREATE TRIGGER FailInsert BEFORE INSERT ON OccurrenceStates BEGIN SELECT RAISE(ABORT, 'owned failure'); END");
        var dismissed = new TaskCompletionSource(); f.Platform.PendingFailure = _ => dismissed.Task;
        var commands = new OccurrenceCommands(seed.Plans, f.Translator, f.Platform);
        var failed = ActAsync(commands, seed, "skip", () => Task.CompletedTask);
        Assert.False(await ActAsync(commands, seed, "full", () => Task.CompletedTask));
        Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
        dismissed.SetResult(); Assert.False(await failed);
        await SqlAsync(f, "DROP TRIGGER FailInsert");
        Assert.True(await ActAsync(commands, seed, "skip", () => Task.CompletedTask));
    }

    [Theory, InlineData("en"), InlineData("fa"), InlineData("de"), InlineData("es"), InlineData("fr"), InlineData("it")]
    public async Task Post_commit_refresh_failure_is_reported_as_saved_and_does_not_replay_the_write(string language)
    {
        using var f = new FlowFixture(); f.Localization.SetLanguage(f.Localization.SupportedLanguages.First(l => l.CultureName == language));
        var seed = await SeedAsync(f, "full"); var commands = new OccurrenceCommands(seed.Plans, f.Translator, f.Platform);
        Assert.True(await ActAsync(commands, seed, "full", () => throw new InvalidOperationException("Owned refresh failure")));
        Assert.Single(await f.Store.GetEntriesAsync(cancellationToken: Ct));
        Assert.Equal(OccurrenceStatus.Settled, Assert.Single(await seed.Plans.GetStatesAsync(seed.Plan.Id, Ct)).Status);
        Assert.Empty(f.Platform.Failures); var alert = Assert.Single(f.Platform.Alerts);
        Assert.Equal((f.Translator["Error_Title"], f.Translator["Occurrence_SavedRefreshFailed"]), alert);
    }

    [Fact]
    public async Task Publication_gate_stays_closed_until_the_committed_screen_finishes_refreshing()
    {
        using var f = new FlowFixture(); var seed = await SeedAsync(f, "partial");
        var refreshed = new TaskCompletionSource(); var entered = new TaskCompletionSource();
        var commands = new OccurrenceCommands(seed.Plans, f.Translator, f.Platform);
        var saving = ActAsync(commands, seed, "partial", () => { entered.SetResult(); return refreshed.Task; });
        await entered.Task;
        Assert.False(await ActAsync(commands, seed, "partial", () => Task.CompletedTask));
        Assert.Single(await f.Store.GetEntriesAsync(cancellationToken: Ct)); refreshed.SetResult(); Assert.True(await saving);
    }

    [Fact]
    public async Task Actual_ledger_rejection_uses_translated_inline_feedback_without_publishing_or_showing_an_unexpected_failure()
    {
        using var f = new FlowFixture(); var seed = await SeedAsync(f, "full");
        await SqlAsync(f, "UPDATE Accounts SET IsArchived=1");
        var before = await SnapshotAsync(f); string? error = null; var publications = 0;
        Assert.False(await new OccurrenceCommands(seed.Plans, f.Translator, f.Platform).PayAsync(seed.Occurrence,
            1234, Day, false, text => error = text, () => { publications++; return Task.CompletedTask; }));
        Assert.Equal(f.Translator["LedgerError_AccountArchived"], error); Assert.Empty(f.Platform.Failures);
        Assert.Equal(0, publications); Assert.Equal(before, await SnapshotAsync(f));
    }

    private sealed record Seed(PlanStore Plans, Schedule Plan, Occurrence Occurrence, LedgerEntry Candidate);
    private static async Task<Seed> SeedAsync(FlowFixture f, string action)
    {
        var account = new Account { Name = "Owned fictitious cash", Type = AccountType.Cash, CurrencyCode = "EUR", OpeningDate = Day.AddDays(-30) };
        Assert.True(await f.Store.SaveAccountAsync(account, Ct));
        var plans = f.Services.GetRequiredService<PlanStore>();
        var plan = new Schedule { Name = "Owned bill", AccountId = account.Id, Kind = EntryKind.Expense, Amount = 1000,
            Rule = new() { Frequency = Frequency.Once, Start = Day } };
        await plans.SaveScheduleAsync(plan, Ct);
        var occurrence = Occurrences.Between(plan, [], Day, Day, Day).Single();
        var candidate = new LedgerEntry { Kind = EntryKind.Expense, AccountId = account.Id, Amount = 950, Date = Day, Title = "Owned candidate" };
        if (action == "link") Assert.True((await f.Store.SaveEntryAsync(candidate, Ct)).Succeeded);
        if (action == "unskip") await plans.SkipAsync(occurrence, Ct);
        if (action == "unsettle") Assert.True((await plans.SettleAsync(occurrence,
            Occurrences.CreateEntry(occurrence, 1000, Day, ReviewState.Confirmed), Ct)).Succeeded);
        occurrence = Occurrences.Between(plan, await plans.GetStatesAsync(plan.Id, Ct), Day, Day, Day).Single();
        return new(plans, plan, occurrence, candidate);
    }

    private static Task<bool> ActAsync(OccurrenceCommands commands, Seed seed, string action, Func<Task> completed) => action switch
    {
        "full" => commands.PayAsync(seed.Occurrence, 1234, Day.AddDays(-1), false, _ => Assert.Fail("Unexpected ledger validation"), completed),
        "partial" => commands.PayAsync(seed.Occurrence, 400, Day.AddDays(-1), true, _ => Assert.Fail("Unexpected ledger validation"), completed),
        "link" => commands.LinkAsync(seed.Occurrence, seed.Candidate.Id, "Owned candidate", "EUR 9.50", completed),
        "skip" => commands.SkipAsync(seed.Occurrence, completed),
        "change" => commands.ChangeAsync(seed.Occurrence, Day.AddDays(3), 1234, "Unsaved owned note", completed),
        "unskip" or "unsettle" => commands.UndoAsync(seed.Occurrence, completed),
        _ => throw new ArgumentOutOfRangeException(nameof(action))
    };

    private static async Task SqlAsync(FlowFixture f, string sql)
    {
        await using var db = await f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct);
        await db.Database.ExecuteSqlRawAsync(sql, Ct);
    }
    private static async Task<string> SnapshotAsync(FlowFixture f)
    {
        await using var db = await f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct);
        await db.Database.OpenConnectionAsync(Ct); var connection = db.Database.GetDbConnection(); var tables = new List<string>();
        using (var command = connection.CreateCommand())
        {
            command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%' ORDER BY name";
            using var reader = await command.ExecuteReaderAsync(Ct); while (await reader.ReadAsync(Ct)) tables.Add(reader.GetString(0));
        }
        Assert.Equal(24, tables.Count); var all = new Dictionary<string, List<string>>();
        foreach (var table in tables)
        {
            using var command = connection.CreateCommand(); command.CommandText = "SELECT * FROM \"" + table + "\"";
            using var reader = await command.ExecuteReaderAsync(Ct); var rows = new List<string>();
            while (await reader.ReadAsync(Ct))
            {
                var values = new object?[reader.FieldCount];
                for (var i = 0; i < values.Length; i++) values[i] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                rows.Add(JsonSerializer.Serialize(values));
            }
            rows.Sort(StringComparer.Ordinal); all[table] = rows;
        }
        return JsonSerializer.Serialize(all);
    }
}
