using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Zanance.App.Features.Plans;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Plans;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Complete original occurrence sources and held native-command ports reject retirement without money replay.</summary>
public sealed class OccurrenceOwnershipSnapshotTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private static readonly DateOnly Day = OccurrenceOriginFailureTests.Day;
    internal static async Task<(Guid Plan, LedgerEntry Entry)> CandidateAsync(FlowFixture f, bool settled = false)
    {
        var ids = await OccurrenceOriginFailureTests.SeedAsync(f, "EUR");
        await using var db = await OccurrenceOriginFailureTests.Factory(f).CreateDbContextAsync(Ct);
        var entry = new LedgerEntry { AccountId = ids.Account, Kind = EntryKind.Expense, Date = Day,
            Amount = 123, Payee = "Fictitious original", Note = "Original note", Tags = ["First", "Second"] };
        if (settled)
        {
            entry.ScheduleId = ids.Plan; entry.OccurrenceDate = Day;
            db.OccurrenceStates.Add(new OccurrenceState { ScheduleId = ids.Plan, OriginalDate = Day,
                Status = OccurrenceStatus.Settled, EntryId = entry.Id });
        }
        db.Entries.Add(entry); await db.SaveChangesAsync(Ct); return (ids.Plan, entry);
    }
    private static PlanStore Plans(FlowFixture f) => f.Services.GetRequiredService<PlanStore>();

    [Fact]
    public async Task A_complete_occurrence_read_uses_one_original_context_and_keeps_every_source_table_equal()
    {
        using var f = new FlowFixture(); var seed = await CandidateAsync(f); var calls = 0;
        var factory = new AccountDetailOriginalBoundaryTests.ControlledFactory(OccurrenceOriginFailureTests.Factory(f))
            { Select = () => { calls++; return OccurrenceOriginFailureTests.Factory(f); } };
        var store = OccurrenceOriginFailureTests.Reader(f, factory).Plans; var before = await GoalChooserFlowTests.SnapshotAsync(f);
        var source = await store.ReadOccurrenceSnapshotAsync(seed.Plan, Day, Day, ct: Ct);
        Assert.Equal(1, calls); Assert.Equal(seed.Entry.Id, Assert.Single(source.Candidates).Id);
        Assert.Equal(["First", "Second"], source.Candidates[0].Tags); Assert.Equal("EUR", Assert.Single(source.Source.Inputs.Accounts).CurrencyCode);
        await store.ValidateOccurrenceSnapshotAsync(source, Ct);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }

    [Theory]
    [InlineData("candidate-note")]
    [InlineData("candidate-tags")]
    [InlineData("plan-name")]
    [InlineData("account-currency")]
    public async Task Mutating_any_original_packet_refuses_the_bound_command_without_a_state_or_money_write(string field)
    {
        using var f = new FlowFixture(); var seed = await CandidateAsync(f);
        var source = await Plans(f).ReadOccurrenceSnapshotAsync(seed.Plan, Day, Day, ct: Ct);
        var before = await GoalChooserFlowTests.SnapshotAsync(f);
        switch (field)
        {
            case "candidate-note": source.Candidates[0].Note = "Replacement"; break;
            case "candidate-tags": source.Candidates[0].Tags[0] = "Replacement"; break;
            case "plan-name": source.Source.Inputs.Schedule!.Name = "Replacement"; break;
            default: source.Source.Inputs.Accounts[0].CurrencyCode = "USD"; break;
        }
        var published = 0; var command = new OccurrenceCommands(Plans(f), f.Translator, f.Platform);
        Assert.False(await command.SkipAsync(source.Occurrence!, () => { published++; return Task.CompletedTask; }, original: source));
        Assert.Equal(0, published); Assert.Single(f.Platform.Failures);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }

    [Fact]
    public async Task A_changed_candidate_row_is_rejected_by_the_actual_original_database_validation()
    {
        using var f = new FlowFixture(); var seed = await CandidateAsync(f);
        var source = await Plans(f).ReadOccurrenceSnapshotAsync(seed.Plan, Day, Day, ct: Ct);
        await using (var db = await OccurrenceOriginFailureTests.Factory(f).CreateDbContextAsync(Ct))
            await db.Entries.Where(e => e.Id == seed.Entry.Id).ExecuteUpdateAsync(e => e.SetProperty(x => x.Note, "Changed persisted note"), Ct);
        var before = await GoalChooserFlowTests.SnapshotAsync(f);
        await Assert.ThrowsAsync<PlanEditorSourceChangedException>(() => Plans(f).ValidateOccurrenceSnapshotAsync(source, Ct));
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }

    [Fact]
    public async Task A_substituted_occurrence_record_cannot_borrow_the_original_read_authority()
    {
        using var f = new FlowFixture(); var seed = await CandidateAsync(f);
        var source = await Plans(f).ReadOccurrenceSnapshotAsync(seed.Plan, Day, Day, ct: Ct);
        var before = await GoalChooserFlowTests.SnapshotAsync(f);
        await Assert.ThrowsAsync<InvalidOperationException>(() => Plans(f).SkipAsync(source.Occurrence! with { DueDate = Day.AddDays(1) }, Ct, original: source));
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }

    [Fact]
    public async Task A_held_original_factory_read_cancels_without_acquiring_a_context_or_following_another_source()
    {
        using var f = new FlowFixture(); var ids = await OccurrenceOriginFailureTests.SeedAsync(f, "EUR");
        using var cancelled = CancellationTokenSource.CreateLinkedTokenSource(Ct); var entered = new TaskCompletionSource(); var never = new TaskCompletionSource(); var contexts = 0;
        var factory = new AccountDetailOriginalBoundaryTests.ControlledFactory(OccurrenceOriginFailureTests.Factory(f))
            { Before = async token => { entered.SetResult(); await never.Task.WaitAsync(token); }, Select = () => { contexts++; return OccurrenceOriginFailureTests.Factory(f); } };
        var before = await GoalChooserFlowTests.SnapshotAsync(f);
        var pending = OccurrenceOriginFailureTests.Reader(f, factory).Plans.ReadOccurrenceSnapshotAsync(ids.Plan, Day, Day, ct: cancelled.Token);
        await entered.Task.WaitAsync(Ct); cancelled.Cancel(); await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending);
        Assert.Equal(0, contexts); Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Retiring_original_link_or_reopen_consent_never_corrects_money_or_opens_failure_on_the_replacement_owner(bool reopen)
    {
        using var f = new FlowFixture(); var seed = await CandidateAsync(f, reopen);
        var source = await Plans(f).ReadOccurrenceSnapshotAsync(seed.Plan, Day, Day, ct: Ct);
        var before = await GoalChooserFlowTests.SnapshotAsync(f); var active = true; var published = 0;
        var answer = new TaskCompletionSource<bool>(); f.Platform.PendingConfirmation = () => answer.Task;
        var command = new OccurrenceCommands(Plans(f), f.Translator, f.Platform);
        Task Completed() { published++; return Task.CompletedTask; }
        var pending = reopen ? command.UndoAsync(source.Occurrence!, Completed, source, () => active, _ => active)
            : command.LinkAsync(source.Occurrence!, seed.Entry.Id, seed.Entry.Payee!, "1.23 EUR", Completed, source, () => active, _ => active);
        Assert.Single(f.Platform.ConfirmationDialogs); active = false; answer.SetResult(true);
        Assert.False(await pending); Assert.Equal(0, published); Assert.Empty(f.Platform.Failures); Assert.Empty(f.Platform.Alerts);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }

    [Fact]
    public async Task Original_owner_retirement_inside_the_state_writer_rolls_back_the_uncommitted_change()
    {
        using var f = new FlowFixture(); var ids = await OccurrenceOriginFailureTests.SeedAsync(f, "EUR");
        var source = await Plans(f).ReadOccurrenceSnapshotAsync(ids.Plan, Day, Day, ct: Ct); var checks = 0;
        var before = await GoalChooserFlowTests.SnapshotAsync(f);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Plans(f).SkipAsync(source.Occurrence!, Ct, original: source, isCurrent: () => ++checks < 4));
        Assert.Equal(4, checks); Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }

    [Fact]
    public async Task One_accepted_nonfinancial_skip_keeps_its_receipt_and_cannot_reuse_the_retired_original_source()
    {
        using var f = new FlowFixture(); var ids = await OccurrenceOriginFailureTests.SeedAsync(f, "EUR");
        var source = await Plans(f).ReadOccurrenceSnapshotAsync(ids.Plan, Day, Day, ct: Ct); var published = 0;
        Plans(f).Changed += (_, _) => throw new InvalidOperationException("Fictitious post-commit notification failure");
        var command = new OccurrenceCommands(Plans(f), f.Translator, f.Platform);
        Assert.True(await command.SkipAsync(source.Occurrence!, () => { published++; return Task.CompletedTask; }, original: source));
        Assert.Equal(1, published); Assert.Single(f.Platform.Alerts); Assert.Empty(f.Platform.Failures);
        var before = await GoalChooserFlowTests.SnapshotAsync(f);
        Assert.False(await command.SkipAsync(source.Occurrence!, () => throw new InvalidOperationException("No accepted publication replay"), original: source));
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f)); Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
    }
    [Theory]
    [InlineData("full")]
    [InlineData("partial")]
    [InlineData("link")]
    [InlineData("skip")]
    [InlineData("change")]
    [InlineData("unskip")]
    [InlineData("unsettle")]
    public async Task Every_original_command_rejects_a_mutated_source_before_any_financial_or_state_mutation(string action)
    {
        using var f = new FlowFixture(); var seed = await CandidateAsync(f, action == "unsettle");
        if (action == "unskip")
        {
            await using var db = await OccurrenceOriginFailureTests.Factory(f).CreateDbContextAsync(Ct);
            db.OccurrenceStates.Add(new OccurrenceState { ScheduleId = seed.Plan, OriginalDate = Day, Status = OccurrenceStatus.Skipped });
            await db.SaveChangesAsync(Ct);
        }
        var source = await Plans(f).ReadOccurrenceSnapshotAsync(seed.Plan, Day, Day, ct: Ct);
        var before = await GoalChooserFlowTests.SnapshotAsync(f); var publications = 0;
        source.Source.Inputs.Accounts[0].Name = "Substituted source caption";
        f.Platform.Confirmations.Enqueue(true);
        var commands = new OccurrenceCommands(Plans(f), f.Translator, f.Platform);
        Task Completed() { publications++; return Task.CompletedTask; }
        var occurrence = source.Occurrence!;
        var accepted = action switch
        {
            "full" or "partial" => await commands.PayAsync(occurrence, 123, Day, action == "partial", _ => { }, Completed, original: source),
            "link" => await commands.LinkAsync(occurrence, seed.Entry.Id, "Fictitious original", "1.23 EUR", Completed, original: source),
            "skip" => await commands.SkipAsync(occurrence, Completed, original: source),
            "change" => await commands.ChangeAsync(occurrence, Day.AddDays(1), null, "Unaccepted note", Completed, original: source),
            _ => await commands.UndoAsync(occurrence, Completed, original: source),
        };
        Assert.False(accepted); Assert.Equal(0, publications); Assert.Single(f.Platform.Failures);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }

}
