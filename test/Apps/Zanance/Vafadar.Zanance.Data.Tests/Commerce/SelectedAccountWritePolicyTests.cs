using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Data;
using Vafadar.Testing;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Commerce;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Plans;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.Data.Tests.Commerce;

/// <summary>AT-133: selected-account new work and retained financial correction rights in actual SQLite writers.</summary>
[Trait("AT", "AT-133")]
public sealed class SelectedAccountWritePolicyTests : IDisposable
{
    private readonly TemporaryDirectory _directory = new();
    private readonly List<ServiceProvider> _providers = [];
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private static readonly Guid ScopeId = Guid.Parse("11111111-2222-3333-4444-555555555555");
    private static readonly DateOnly Day = new(2026, 10, 10);
    private static CapabilityContext Context(ProductPlan plan = ProductPlan.Free, bool shared = false, bool member = true, bool host = true) =>
        new(plan, new(shared ? EntitlementScopeKind.SharedSpace : EntitlementScopeKind.PersonalProfile, ScopeId),
            shared ? new(ScopeId, member, host) : null);

    private async Task<Fixture> FixtureAsync(bool trigger = false)
    {
        var path = _directory.Combine(Guid.NewGuid()+".db");
        var access = new AccessSource();
        var services = new ServiceCollection().AddSingleton<TimeProvider>(new FixedTime())
            .AddSingleton<ICommercialWriteAccessSource>(access).AddZananceData(path);
        if (trigger)
        {
            var original = services.Single(d => d.ServiceType == typeof(IDbContextFactory<ZananceDbContext>));
            services.Remove(original);
            services.AddSingleton<IDbContextFactory<ZananceDbContext>>(p => new TriggerConnections(
                (IDbContextFactory<ZananceDbContext>)ActivatorUtilities.CreateInstance(p, original.ImplementationType!), access));
        }
        var provider = services.BuildServiceProvider();
        _providers.Add(provider); provider.MigrateLocalDatabase<ZananceDbContext>();
        var store = provider.GetRequiredService<ZananceStore>();
        var accounts = Enumerable.Range(1, 4).Select(i => new Account
        { Name = "Owned account "+i, CurrencyCode = "EUR", OpeningDate = Day.AddMonths(-1), OpeningBalance = 100000 }).ToArray();
        foreach (var account in accounts) await store.SaveAccountAsync(account, Ct);
        return new(path, access, provider, store, accounts);
    }

    private static ResourceSelection Choice(params Guid[] ids) => new(QuotaKind.FinancialAccounts,
        new(QuotaScopeKind.PersonalProfile, ScopeId), ids);

    private sealed record Fixture(string Path, AccessSource Access, ServiceProvider Provider, ZananceStore Store, Account[] Accounts)
    {
        public LedgerEntry Entry(int account, EntryKind kind = EntryKind.Expense) => new()
        { Kind = kind, AccountId = Accounts[account].Id, Amount = 1000, Date = Day, Note = "Retained fictitious input", Tags = ["owned"] };
        public void Enable(ProductPlan plan = ProductPlan.Free) => Access.Current = new(Path, Context(plan));
        public void Select(params int[] indexes) => Access.Current = new(Path, Context(), Choice(indexes.Select(i => Accounts[i].Id).ToArray()));
    }

    [Theory]
    [InlineData(EntryKind.Expense, 0)]
    [InlineData(EntryKind.Income, 3)]
    [InlineData(EntryKind.Transfer, 0)]
    [InlineData(EntryKind.Transfer, 3)]
    public async Task Over_quota_accounts_require_an_explicit_choice_before_new_financial_work(EntryKind kind, int account)
    {
        var f = await FixtureAsync(); f.Enable(); var entry = f.Entry(account, kind);
        if (kind == EntryKind.Transfer) { entry.ToAccountId = f.Accounts[account == 0 ? 3 : 0].Id; entry.ToAmount = entry.Amount; }
        var before = await SnapshotAsync(f.Provider); var events = 0; f.Store.Changed += (_, _) => events++;
        await Assert.ThrowsAsync<CommercialWriteRejectedException>(() => f.Store.SaveEntryAsync(entry, Ct));
        Assert.Equal(before, await SnapshotAsync(f.Provider)); Assert.Equal(0, events); Assert.Equal(default, entry.CreatedAt);
    }

    [Theory]
    [InlineData(EntryKind.Expense)]
    [InlineData(EntryKind.Income)]
    [InlineData(EntryKind.Transfer)]
    public async Task The_explicit_selected_account_can_receive_new_work_and_a_read_only_peer_cannot(EntryKind kind)
    {
        var f = await FixtureAsync(); f.Select(0, 1, 2); var allowed = f.Entry(0, kind);
        if (kind == EntryKind.Transfer) { allowed.ToAccountId = f.Accounts[1].Id; allowed.ToAmount = allowed.Amount; }
        Assert.True((await f.Store.SaveEntryAsync(allowed, Ct)).Succeeded);
        var before = await SnapshotAsync(f.Provider); var denied = f.Entry(3, kind);
        if (kind == EntryKind.Transfer) { denied.ToAccountId = f.Accounts[0].Id; denied.ToAmount = denied.Amount; }
        var error = await Assert.ThrowsAsync<CommercialWriteRejectedException>(() => f.Store.SaveEntryAsync(denied, Ct));
        Assert.Equal(f.Accounts[3].Id, error.ResourceId); Assert.False(error.RequiresSelection);
        Assert.Equal(QuotaKind.FinancialAccounts, error.Quota); Assert.Equal(FeaturePermission.Allowed, error.Permission);
        Assert.Null(error.Maximum); Assert.Null(error.Current); Assert.Null(error.Requested);
        Assert.Equal(before, await SnapshotAsync(f.Provider));
    }

    [Fact]
    public async Task A_selected_transfer_source_does_not_bypass_the_read_only_destination()
    {
        var f = await FixtureAsync(); f.Select(0,1,2); var transfer = f.Entry(0, EntryKind.Transfer);
        transfer.ToAccountId = f.Accounts[3].Id; transfer.ToAmount = transfer.Amount;
        var before = await SnapshotAsync(f.Provider); var events = 0; f.Store.Changed += (_, _) => events++;
        var error = await Assert.ThrowsAsync<CommercialWriteRejectedException>(() => f.Store.SaveEntryAsync(transfer, Ct));
        Assert.Equal(f.Accounts[3].Id, error.ResourceId); Assert.Equal(before, await SnapshotAsync(f.Provider)); Assert.Equal(0, events);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Retained_corrections_and_transfer_fees_remain_writable_in_unselected_accounts(bool transfer)
    {
        var f = await FixtureAsync(); var original = f.Entry(3, transfer ? EntryKind.Transfer : EntryKind.Expense);
        if (transfer) { original.ToAccountId = f.Accounts[0].Id; original.ToAmount = original.Amount; original.GroupId = Guid.NewGuid(); }
        await f.Store.SaveEntryAsync(original, Ct); f.Select(0,1,2); original.Amount = 900; original.Note = "Explicit owned correction";
        if (transfer)
        {
            var fee = EntryActions.SyncTransferFee(original, null, 50, (await f.Store.GetCategoriesAsync(Ct)).FirstOrDefault()?.Id ?? Guid.NewGuid());
            // Only the existing default fee category is needed; seed it through the actual store while inactive.
            f.Access.Current = CommercialWriteAccess.Inactive; await f.Store.EnsureDefaultCategoriesAsync(Ct);
            fee!.CategoryId = (await f.Store.GetCategoriesAsync(Ct)).Single(c => c.SystemKey == "Fees").Id; f.Select(0,1,2);
            Assert.True((await f.Store.SaveEntriesAsync([original,fee], [], Ct)).Succeeded);
        }
        else Assert.True((await f.Store.SaveEntryAsync(original, Ct)).Succeeded);
        Assert.Equal("Explicit owned correction", (await f.Store.GetEntryAsync(original.Id, Ct))!.Note);
    }

    [Theory]
    [InlineData(EntryKind.Adjustment)]
    [InlineData(EntryKind.Refund)]
    [InlineData(EntryKind.IncomeReversal)]
    public async Task Owned_reconciliation_refund_and_income_corrections_do_not_require_active_selection(EntryKind kind)
    {
        var f = await FixtureAsync(); var original = f.Entry(3); await f.Store.SaveEntryAsync(original, Ct); f.Select(0,1,2);
        var correction = kind == EntryKind.Refund ? EntryActions.CreateRefund(original, 100, original.AccountId, Day) : f.Entry(3,kind);
        if (kind == EntryKind.Adjustment) correction.Direction = AdjustmentDirection.Increase;
        Assert.True((await f.Store.SaveEntryAsync(correction, Ct)).Succeeded);
        Assert.NotNull(await f.Store.GetEntryAsync(correction.Id, Ct));
    }

    [Fact]
    public async Task Delete_and_original_file_Undo_retain_history_receipts_and_newer_selection_choices()
    {
        var f = await FixtureAsync(); var original = f.Entry(3); await f.Store.SaveEntryAsync(original, Ct);
        await f.Store.AddAttachmentAsync(new() {EntryId=original.Id,FileName="owned.txt",ContentType="text/plain",Data=[1,2,3]}, Ct);
        f.Select(0,1,2); var before = await SnapshotAsync(f.Provider); var deleted = await f.Store.DeleteEntryAsync(original.Id,Ct);
        f.Select(1,2); await f.Store.RestoreEntriesAsync(deleted,Ct);
        Assert.Equal(before, await SnapshotAsync(f.Provider)); Assert.Equal(2,f.Access.Current.AccountSelection!.SelectedIds.Count);
    }

    [Theory]
    [InlineData(ProductPlan.Plus)]
    [InlineData(ProductPlan.Pro)]
    public async Task Upgrades_restore_all_account_new_work_without_rewriting_an_old_choice(ProductPlan plan)
    {
        var f = await FixtureAsync(); f.Access.Current = new(f.Path,Context(plan),Choice(f.Accounts[0].Id));
        Assert.True((await f.Store.SaveEntryAsync(f.Entry(3),Ct)).Succeeded);
        Assert.Single(f.Access.Current.AccountSelection!.SelectedIds);
    }

    [Fact]
    public async Task Inactive_current_builds_keep_all_four_accounts_available_and_accept_a_complete_batch()
    {
        var f = await FixtureAsync(); var entries=f.Accounts.Select((_,i)=>f.Entry(i)).ToArray();
        Assert.True((await f.Store.SaveEntriesAsync(entries,[],Ct)).Succeeded);
        Assert.Equal(4,(await f.Store.GetEntriesAsync(cancellationToken: Ct)).Count); Assert.False(f.Access.Current.Enforced);
    }

    [Fact]
    public async Task A_whole_batch_preserves_existing_edits_when_one_new_account_is_read_only()
    {
        var f=await FixtureAsync(); var original=f.Entry(3); await f.Store.SaveEntryAsync(original,Ct);
        f.Select(0,1,2); var edit=original.Copy();edit.Note="Unsaved retained correction";
        var before=await SnapshotAsync(f.Provider);var events=0;f.Store.Changed+=(_,_)=>events++;
        await Assert.ThrowsAsync<CommercialWriteRejectedException>(()=>f.Store.SaveEntriesAsync([edit,f.Entry(0),f.Entry(3)],[],Ct));
        Assert.Equal(before,await SnapshotAsync(f.Provider));Assert.Equal(0,events);Assert.Equal("Unsaved retained correction",edit.Note);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public async Task Missing_overlarge_and_stale_choices_require_review_without_any_write(int choice)
    {
        var f=await FixtureAsync();f.Enable();
        if(choice==1)f.Access.Current=new(f.Path,Context(),Choice(f.Accounts.Select(a=>a.Id).ToArray()));
        if(choice==2)f.Access.Current=new(f.Path,Context(),Choice(Guid.NewGuid()));
        var before=await SnapshotAsync(f.Provider);
        var error=await Assert.ThrowsAsync<CommercialWriteRejectedException>(()=>f.Store.SaveEntryAsync(f.Entry(0),Ct));
        Assert.True(error.RequiresSelection);Assert.Equal(before,await SnapshotAsync(f.Provider));
    }

    [Theory]
    [InlineData(false,true)]
    [InlineData(true,false)]
    public async Task A_personal_Pro_purchase_or_selection_never_replaces_shared_membership_or_host_access(bool member,bool host)
    {
        var f=await FixtureAsync();var scope=new QuotaScope(QuotaScopeKind.SharedSpace,ScopeId);
        f.Access.Current=new(f.Path,Context(ProductPlan.Pro,true,member,host),new(QuotaKind.FinancialAccounts,scope,[f.Accounts[0].Id]));
        var before=await SnapshotAsync(f.Provider);
        var error=await Assert.ThrowsAsync<CommercialWriteRejectedException>(()=>f.Store.SaveEntryAsync(f.Entry(0),Ct));
        Assert.Null(error.ResourceId);Assert.Equal(before,await SnapshotAsync(f.Provider));
    }

    [Fact]
    public async Task A_matching_active_shared_guest_keeps_its_local_unlimited_account_capacity()
    {
        var f=await FixtureAsync();f.Access.Current=new(f.Path,Context(ProductPlan.Free,true));
        Assert.True((await f.Store.SaveEntryAsync(f.Entry(3),Ct)).Succeeded);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Unknown_and_archived_accounts_keep_the_existing_ledger_validation_instead_of_a_tier_error(bool archived)
    {
        var f=await FixtureAsync();var entry=f.Entry(3);
        if(archived){f.Accounts[3].IsArchived=true;await f.Store.SaveAccountAsync(f.Accounts[3],Ct);}else entry.AccountId=Guid.NewGuid();
        f.Select(0,1,2);var before=await SnapshotAsync(f.Provider);var result=await f.Store.SaveEntryAsync(entry,Ct);
        Assert.False(result.Succeeded);Assert.Equal(before,await SnapshotAsync(f.Provider));
    }

    [Fact]
    public void The_cached_account_choice_must_match_the_resource_kind_and_exact_financial_scope()
    {
        Assert.Throws<ArgumentException>(()=>new CommercialWriteAccess(_directory.Combine("scope.db"),Context(),
            new(QuotaKind.FinancialAccounts,new(QuotaScopeKind.PersonalProfile,Guid.NewGuid()),[])));
        Assert.Throws<ArgumentException>(()=>new CommercialWriteAccess(_directory.Combine("kind.db"),Context(),
            new(QuotaKind.Goals,new(QuotaScopeKind.PersonalProfile,ScopeId),[])));
    }

    [Fact]
    public async Task A_confirmed_open_overdue_occurrence_keeps_its_owned_correction_right_on_an_unselected_account()
    {
        var f=await FixtureAsync();var plans=f.Provider.GetRequiredService<PlanStore>();
        var plan=new Schedule { Name="Retained overdue rent",AccountId=f.Accounts[3].Id,Amount=1000,Rule=new(){Start=Day.AddDays(-1)} };
        await plans.SaveScheduleAsync(plan,Ct);var occurrence=Assert.Single(Occurrences.Between(plan,[],Day.AddDays(-1),Day.AddDays(-1),Day));
        f.Select(0,1,2);var entry=Occurrences.CreateEntry(occurrence,1000,Day,ReviewState.Confirmed);
        Assert.True((await plans.SettleAsync(occurrence,entry,Ct)).Succeeded);
        Assert.Equal(OccurrenceStatus.Settled,Assert.Single(await plans.GetStatesAsync(plan.Id,Ct)).Status);
    }

    [Theory]
    [InlineData("future")]
    [InlineData("moved-future")]
    [InlineData("skipped")]
    [InlineData("settled")]
    [InlineData("outside-rule")]
    [InlineData("wrong-account")]
    [InlineData("wrong-kind")]
    [InlineData("unreviewed")]
    [InlineData("manual")]
    [InlineData("missing-plan")]
    [InlineData("outside-slice")]
    public async Task A_forged_or_future_schedule_marker_cannot_bypass_selected_account_new_work(string fault)
    {
        var f=await FixtureAsync();var plans=f.Provider.GetRequiredService<PlanStore>();
        var original=Day.AddDays(-1);var plan=new Schedule {Name="Owned rent",AccountId=f.Accounts[3].Id,Amount=1000,Rule=new(){Start=original}};
        if(fault=="future")plan.Rule.Start=Day.AddDays(1);
        if(fault=="outside-slice")plan.ActiveFrom=Day;
        await plans.SaveScheduleAsync(plan,Ct);
        if(fault is "moved-future" or "skipped" or "settled")
        {
            await using var db=await f.Provider.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct);
            db.OccurrenceStates.Add(new(){ScheduleId=plan.Id,OriginalDate=original,DueDate=fault=="moved-future"?Day.AddDays(1):null,
                Status=fault=="skipped"?OccurrenceStatus.Skipped:fault=="settled"?OccurrenceStatus.Settled:OccurrenceStatus.Open});
            await db.SaveChangesAsync(Ct);
        }
        var entry=f.Entry(3);entry.ScheduleId=plan.Id;entry.OccurrenceDate=plan.Rule.Start;entry.Source=EntrySource.Schedule;entry.Review=ReviewState.Confirmed;
        if(fault=="outside-rule")entry.OccurrenceDate=original.AddDays(-1);
        if(fault=="wrong-account")entry.AccountId=f.Accounts[2].Id;
        if(fault=="wrong-kind")entry.Kind=EntryKind.Income;
        if(fault=="unreviewed")entry.Review=ReviewState.Unreviewed;
        if(fault=="manual")entry.Source=EntrySource.Manual;
        if(fault=="missing-plan")entry.ScheduleId=Guid.NewGuid();
        f.Select(0,1);var before=await SnapshotAsync(f.Provider);var events=0;f.Store.Changed+=(_,_)=>events++;
        await Assert.ThrowsAsync<CommercialWriteRejectedException>(()=>f.Store.SaveEntryAsync(entry,Ct));
        Assert.Equal(before,await SnapshotAsync(f.Provider));Assert.Equal(0,events);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Real_overdue_partial_and_final_payments_remain_available_after_a_downgrade_or_host_expiry(bool expiredHost)
    {
        var f=await FixtureAsync();var plans=f.Provider.GetRequiredService<PlanStore>();var original=Day.AddDays(-1);
        var plan=new Schedule {Name="Retained overdue",AccountId=f.Accounts[3].Id,Amount=1000,Rule=new(){Start=original}};
        await plans.SaveScheduleAsync(plan,Ct);
        if(expiredHost)f.Access.Current=new(f.Path,Context(ProductPlan.Free,true,true,false));else f.Select(0,1,2);
        var occurrence=Assert.Single(Occurrences.Between(plan,[],original,original,Day));
        Assert.True((await plans.PayPartAsync(occurrence,Occurrences.CreateEntry(occurrence,400,Day,ReviewState.Confirmed),Ct)).Succeeded);
        var state=Assert.Single(await plans.GetStatesAsync(plan.Id,Ct));Assert.Equal(400,state.PaidAmount);
        occurrence=Assert.Single(Occurrences.Between(plan,[state],original,original,Day));Assert.Equal(600,occurrence.Outstanding);
        Assert.True((await plans.PayPartAsync(occurrence,Occurrences.CreateEntry(occurrence,600,Day,ReviewState.Confirmed),Ct)).Succeeded);
        Assert.Equal(OccurrenceStatus.Settled,Assert.Single(await plans.GetStatesAsync(plan.Id,Ct)).Status);
        var before=await SnapshotAsync(f.Provider);
        await Assert.ThrowsAsync<CommercialWriteRejectedException>(()=>f.Store.SaveEntryAsync(Occurrences.CreateEntry(occurrence,600,Day,ReviewState.Confirmed),Ct));
        Assert.Equal(before,await SnapshotAsync(f.Provider));
    }

    private async Task<(Fixture Fixture, Schedule Plan, SettlementResult Review)> AdvanceFixtureAsync(long actual, bool trigger = false)
    {
        var f=await FixtureAsync(trigger);var plans=f.Provider.GetRequiredService<PlanStore>();
        var plan=new Schedule {Name="Retained utility advances",AccountId=f.Accounts[3].Id,Amount=1000,Rule=new(){Start=Day.AddMonths(-1)}};
        await plans.SaveScheduleAsync(plan,Ct);var advance=f.Entry(3);advance.ScheduleId=plan.Id;advance.Date=Day.AddDays(-10);
        await f.Store.SaveEntryAsync(advance,Ct);
        return(f,plan,AdvanceSettlement.Compute(plan,await f.Store.GetEntriesAsync(cancellationToken:Ct),Day.AddMonths(-1),Day,actual));
    }

    [Theory]
    [InlineData(1400,false)]
    [InlineData(400,false)]
    [InlineData(1400,true)]
    [InlineData(400,true)]
    public async Task An_owned_advance_bill_saves_only_the_actual_difference_in_unselected_or_expired_shared_accounts(long actual,bool expiredHost)
    {
        var(f,plan,review)=await AdvanceFixtureAsync(actual);
        if(expiredHost)f.Access.Current=new(f.Path,Context(ProductPlan.Free,true,true,false));else f.Select(0,1,2);
        var events=0;f.Store.Changed+=(_,_)=>events++;
        Assert.True((await f.Store.SaveAdvanceSettlementAsync(plan,Day.AddMonths(-1),Day,review,"Reviewed owned bill",Ct)).Succeeded);
        var entries=await f.Store.GetEntriesAsync(cancellationToken:Ct);Assert.Equal(2,entries.Count);Assert.Equal(1,events);
        var delta=Assert.Single(entries,e=>e.Id!=review.Advances[0].Id);Assert.Equal(Math.Abs(actual-1000),delta.Amount);
        Assert.Equal(actual>1000?EntryKind.Expense:EntryKind.Refund,delta.Kind);
        Assert.Equal(actual>1000?null:review.Advances[0].Id,delta.RefundOfId);
    }

    [Theory]
    [InlineData("advance")]
    [InlineData("refund")]
    [InlineData("plan")]
    [InlineData("period")]
    [InlineData("review")]
    public async Task A_changed_review_cannot_silently_post_a_different_advance_bill(string change)
    {
        var(f,plan,review)=await AdvanceFixtureAsync(1400);var from=Day.AddMonths(-1);
        if(change=="advance"){var edit=review.Advances[0].Copy();edit.Amount=1200;await f.Store.SaveEntryAsync(edit,Ct);}
        if(change=="refund")await f.Store.SaveEntryAsync(EntryActions.CreateRefund(review.Advances[0],200,f.Accounts[3].Id,Day),Ct);
        if(change=="plan"){var loaded=await f.Provider.GetRequiredService<PlanStore>().GetScheduleAsync(plan.Id,Ct);loaded!.AccountId=f.Accounts[0].Id;await f.Provider.GetRequiredService<PlanStore>().SaveScheduleAsync(loaded,Ct);}
        if(change=="period")from=Day;
        if(change=="review")review=new(review.Advances,1200,review.Actual);
        f.Select(0,1,2);var before=await SnapshotAsync(f.Provider);var events=0;f.Store.Changed+=(_,_)=>events++;
        await Assert.ThrowsAsync<InvalidOperationException>(()=>f.Store.SaveAdvanceSettlementAsync(plan,from,Day,review,"Retained draft",Ct));
        Assert.Equal(before,await SnapshotAsync(f.Provider));Assert.Equal(0,events);
    }

    [Fact]
    public async Task No_advances_or_matching_bill_is_never_a_new_expense_grant()
    {
        var(f,plan,review)=await AdvanceFixtureAsync(1000);f.Select(0,1,2);var before=await SnapshotAsync(f.Provider);var events=0;f.Store.Changed+=(_,_)=>events++;
        Assert.True((await f.Store.SaveAdvanceSettlementAsync(plan,Day.AddMonths(-1),Day,review,"Even bill",Ct)).Succeeded);
        Assert.Equal(before,await SnapshotAsync(f.Provider));Assert.Equal(0,events);
        await Assert.ThrowsAsync<InvalidOperationException>(()=>f.Store.SaveAdvanceSettlementAsync(plan,Day,Day,new([],0,1000),"No advances",Ct));
        Assert.Equal(before,await SnapshotAsync(f.Provider));
    }

    [Theory]
    [InlineData(false,false)]
    [InlineData(false,true)]
    [InlineData(true,false)]
    [InlineData(true,true)]
    public async Task SQL_failure_or_retired_cached_choice_rolls_back_new_work_and_retained_bill_with_no_Changed(bool bill,bool retire)
    {
        Fixture f;Schedule? plan=null;SettlementResult? review=null;
        if(bill)(f,plan,review)=await AdvanceFixtureAsync(1400,true);else f=await FixtureAsync(true);
        f.Select(0,1,2);
        await using(var db=await f.Provider.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct))
            await db.Database.ExecuteSqlRawAsync(retire
                ?"CREATE TRIGGER SelectionWriterFailure AFTER INSERT ON Entries BEGIN SELECT AfterSelectionSql(); END"
                :"CREATE TRIGGER SelectionWriterFailure AFTER INSERT ON Entries BEGIN SELECT RAISE(ABORT, 'Owned fixture write failure'); END",Ct);
        f.Access.AfterSql=()=>{f.Access.AfterSql=null;f.Select(1,2);};
        var before=await SnapshotAsync(f.Provider);var events=0;f.Store.Changed+=(_,_)=>events++;
        Task<SaveResult> Save()=>bill?f.Store.SaveAdvanceSettlementAsync(plan!,Day.AddMonths(-1),Day,review!,"Retained bill",Ct):f.Store.SaveEntryAsync(f.Entry(0),Ct);
        if(retire)await Assert.ThrowsAsync<InvalidOperationException>(Save);else Assert.IsType<SqliteException>((await Assert.ThrowsAsync<DbUpdateException>(Save)).InnerException);
        Assert.Equal(before,await SnapshotAsync(f.Provider));Assert.Equal(0,events);
        await using(var db=await f.Provider.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct))
            await db.Database.ExecuteSqlRawAsync("DROP TRIGGER SelectionWriterFailure",Ct);
        f.Select(0,1,2);Assert.True((await Save()).Succeeded);Assert.Equal(1,events);
    }

    private static async Task<string> SnapshotAsync(ServiceProvider provider)
    {
        await using var db = await provider.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct);
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

    private sealed class AccessSource : ICommercialWriteAccessSource
    {
        public CommercialWriteAccess Current = CommercialWriteAccess.Inactive;
        public Action? AfterSql;
        public CommercialWriteAccess Capture(string databasePath) => Current;
    }
    /// <summary>Only the fixture's actual SQLite connection can retire its cached choice from an insert trigger.</summary>
    private sealed class TriggerConnections(IDbContextFactory<ZananceDbContext> inner, AccessSource source) : IDbContextFactory<ZananceDbContext>
    {
        public ZananceDbContext CreateDbContext()
        {
            var db=inner.CreateDbContext();
            ((SqliteConnection)db.Database.GetDbConnection()).CreateFunction("AfterSelectionSql",()=>{source.AfterSql?.Invoke();return 1;});
            return db;
        }
    }
    private sealed class FixedTime : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(2026, 10, 10, 12, 0, 0, TimeSpan.Zero);
        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
    }
    public void Dispose()
    {
        foreach (var provider in _providers) provider.Dispose(); SqliteTestPools.Clear(_directory); _directory.Dispose();
    }
}
