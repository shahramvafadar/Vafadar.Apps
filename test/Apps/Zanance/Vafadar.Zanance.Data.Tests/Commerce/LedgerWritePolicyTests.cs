using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Data;
using Vafadar.Testing;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Commerce;
using Vafadar.Zanance.Core.Holdings;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Plans;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.Data.Tests.Commerce;

/// <summary>AT-128: actual-file ledger Save rights, complete split groups and SQLite serialization.</summary>
[Trait("AT", "AT-128")]
public sealed class LedgerWritePolicyTests : IDisposable
{
    private readonly TemporaryDirectory _directory = new();
    private readonly List<ServiceProvider> _providers = [];
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private static readonly DateOnly Day = new(2026, 10, 10);
    private static readonly Guid Scope = Guid.Parse("11111111-2222-3333-4444-555555555555");
    private static CapabilityContext Context(ProductPlan plan, bool shared = false, bool member = true, bool host = true) =>
        new(plan, new(shared ? EntitlementScopeKind.SharedSpace : EntitlementScopeKind.PersonalProfile, Scope),
            shared ? new(Scope, member, host) : null);

    private ServiceProvider Provider(string path, AccessSource source, bool trigger = false)
    {
        var services = new ServiceCollection().AddSingleton<TimeProvider>(new FixedTime())
            .AddSingleton<ICommercialWriteAccessSource>(source).AddZananceData(path);
        if (trigger)
        {
            var original = services.Single(d => d.ServiceType == typeof(IDbContextFactory<ZananceDbContext>));
            services.Remove(original);
            services.AddSingleton<IDbContextFactory<ZananceDbContext>>(p => new TriggerConnections(
                (IDbContextFactory<ZananceDbContext>)ActivatorUtilities.CreateInstance(p, original.ImplementationType!), source));
        }
        var provider = services.BuildServiceProvider(); provider.MigrateLocalDatabase<ZananceDbContext>();
        _providers.Add(provider); return provider;
    }

    private async Task<Fixture> FixtureAsync(bool trigger = false)
    {
        var path = _directory.Combine(Guid.NewGuid() + ".db"); var source = new AccessSource(path);
        var provider = Provider(path, source, trigger); var store = provider.GetRequiredService<ZananceStore>();
        var account = new Account { Name = "Original EUR", CurrencyCode = "EUR", OpeningDate = Day, OpeningBalance = 100000 };
        var second = new Account { Name = "Other USD", CurrencyCode = "USD", OpeningDate = Day, OpeningBalance = 200000 };
        await store.SaveAccountAsync(account, Ct); await store.SaveAccountAsync(second, Ct);
        var entry = new LedgerEntry { Kind = EntryKind.Expense, AccountId = account.Id, Date = Day, Amount = 10000,
            Title = "Complete original purchase", Payee = "Original payee", Note = "Original note", Tags = ["retained", "original"] };
        Assert.True((await store.SaveEntryAsync(entry, Ct)).Succeeded);
        return new(path, source, provider, store, account, second, entry);
    }

    private sealed record Fixture(string Path, AccessSource Source, ServiceProvider Provider, ZananceStore Store,
        Account Account, Account Second, LedgerEntry Original)
    {
        public void Enable(ProductPlan plan = ProductPlan.Free, bool shared = false, bool member = true, bool host = true) =>
            Source.Current = new(Path, Context(plan, shared, member, host));
        public LedgerEntry NewEntry(EntryKind kind = EntryKind.Expense, long amount = 1000) => new()
        { Kind = kind, AccountId = Account.Id, Date = Day, Amount = amount, Title = "New complete entry", Tags = ["new"] };
    }

    [Fact]
    public async Task Free_new_split_is_rejected_before_entry_cleanup_audit_or_stored_changes()
    {
        var f = await FixtureAsync(); var draft = f.Original.Copy();
        var (parts, deleted) = EntryActions.Split([draft], [(null, 6000), (null, 4000)]);
        parts[1].ToAccountId = f.Second.Id; parts[1].ToAmount = 77;
        f.Enable(); var before = await SnapshotAsync(f.Provider); var events = 0; f.Store.Changed += (_, _) => events++;
        var error = await Assert.ThrowsAsync<CommercialWriteRejectedException>(() => f.Store.SaveEntriesAsync(parts, deleted, parts[0].Id, Ct));
        Assert.Equal(CommercialFeature.SplitTransactions, error.Feature); Assert.Equal(FeaturePermission.RequiresPlus, error.Permission);
        Assert.Equal(f.Second.Id, parts[1].ToAccountId); Assert.Equal(77, parts[1].ToAmount); Assert.Equal(default, parts[1].CreatedAt);
        Assert.Equal(0, events); Assert.Equal(before, await SnapshotAsync(f.Provider));
    }

    private static (IReadOnlyList<LedgerEntry> Save, IReadOnlyList<Guid> Delete) Split(Fixture f) =>
        EntryActions.Split([f.Original.Copy()], [(null, 6000), (null, 4000)]);

    [Theory]
    [InlineData(false, ProductPlan.Free)]
    [InlineData(true, ProductPlan.Free)]
    [InlineData(true, ProductPlan.Plus)]
    public async Task Holding_payment_groups_keep_corrections_and_require_holding_rights_for_new_fees(bool addFee, ProductPlan plan)
    {
        var f = await FixtureAsync(); var holdings = f.Provider.GetRequiredService<HoldingStore>();
        var type = new AssetType { Name = "Retained gold", PriceCurrencyCode = "EUR", Dimension = AssetDimension.Mass,
            Metal = Metal.Gold, PurityPer10000 = 7500 };
        await holdings.SaveTypeAsync(type, Ct); var location = await holdings.EnsureDefaultLocationAsync("Safe", Ct);
        var item = new AssetEvent { AssetTypeId = type.Id, LocationId = location.Id, Kind = AssetEventKind.Purchase,
            Date = Day, Quantity = 10000, BasisAmount = 1000 };
        var payment = f.NewEntry(EntryKind.AssetPurchase); var fee = f.NewEntry(amount: 100);
        Assert.True((await holdings.SaveEventAsync(item, [payment, fee], true, Ct)).Succeeded);
        payment.Note = "Retained payment correction"; fee.Note = "Retained fee correction";
        var incoming = new List<LedgerEntry> { payment, fee };
        if (addFee) { var extra = f.NewEntry(amount: 50); extra.GroupId = item.GroupId; incoming.Add(extra); }
        f.Enable(plan); var before = await SnapshotAsync(f.Provider); var events = 0; f.Store.Changed += (_, _) => events++;
        if (addFee && plan == ProductPlan.Free)
        {
            var error = await Assert.ThrowsAsync<CommercialWriteRejectedException>(() => f.Store.SaveEntriesAsync(incoming, [], Ct));
            Assert.Equal(CommercialFeature.ManageHoldings, error.Feature); Assert.Equal(before, await SnapshotAsync(f.Provider));
            Assert.Equal(0, events);
        }
        else
        {
            Assert.True((await f.Store.SaveEntriesAsync(incoming, [], Ct)).Succeeded);
            Assert.Equal(incoming.Count, (await f.Store.GetGroupAsync(item.GroupId!.Value, Ct)).Count); Assert.Equal(1, events);
            Assert.Equal("Retained payment correction", (await f.Store.GetEntryAsync(payment.Id, Ct))!.Note);
        }
    }

    [Fact]
    public async Task Inactive_split_and_join_preserve_complete_original_event_and_all_other_tables()
    {
        var f = await FixtureAsync(); var before = await SnapshotAsync(f.Provider); var (parts, delete) = Split(f);
        Assert.True((await f.Store.SaveEntriesAsync(parts, delete, parts[0].Id, Ct)).Succeeded);
        var stored = await f.Store.GetGroupAsync(parts[0].GroupId!.Value, Ct); Assert.Equal(10000, stored.Sum(e => e.Amount));
        Assert.All(stored, e => { Assert.Equal(f.Original.Title, e.Title); Assert.Equal(f.Original.Note, e.Note); Assert.Equal(f.Original.Tags, e.Tags); });
        var (joined, removed) = EntryActions.Join(stored.OrderBy(e => e.Id != f.Original.Id).ToList());
        Assert.True((await f.Store.SaveEntriesAsync([joined], removed, joined.Id, Ct)).Succeeded);
        Assert.Equal(before, await SnapshotAsync(f.Provider));
    }

    [Theory]
    [InlineData(ProductPlan.Plus, false)]
    [InlineData(ProductPlan.Pro, false)]
    [InlineData(ProductPlan.Free, true)]
    public async Task New_split_is_saved_with_paid_personal_or_exact_shared_guest_rights(ProductPlan plan, bool shared)
    {
        var f = await FixtureAsync(); f.Enable(plan, shared); var before = await SnapshotAsync(f.Provider, omitEntries: true);
        var (parts, removed) = Split(f); var events = 0; f.Store.Changed += (_, _) => events++;
        Assert.True((await f.Store.SaveEntriesAsync(parts, removed, parts[0].Id, Ct)).Succeeded);
        Assert.Equal(2, (await f.Store.GetGroupAsync(parts[0].GroupId!.Value, Ct)).Count); Assert.Equal(1, events);
        Assert.Equal(10000, (await f.Store.GetEntriesAsync(cancellationToken: Ct)).Sum(e => e.Amount));
        Assert.Equal(before, await SnapshotAsync(f.Provider, omitEntries: true));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Retained_single_part_correction_and_whole_join_keep_Free_rights_after_host_expiry(bool expiredHost)
    {
        var f = await FixtureAsync(); var (parts, removed) = Split(f);
        await f.Store.SaveEntriesAsync(parts, removed, Ct); f.Enable(shared: expiredHost, host: !expiredHost);
        var before = await SnapshotAsync(f.Provider, omitEntries: true); var original = parts[0].CreatedAt;
        parts[0].Note = "Explicit retained correction"; parts[0].CreatedAt = default;
        Assert.True((await f.Store.SaveEntryAsync(parts[0], Ct)).Succeeded);
        var saved = await f.Store.GetEntryAsync(parts[0].Id, Ct); Assert.Equal(original, saved!.CreatedAt);
        Assert.Equal("Explicit retained correction", saved.Note);
        var (joined, delete) = EntryActions.Join(parts);
        Assert.True((await f.Store.SaveEntriesAsync([joined], delete, joined.Id, Ct)).Succeeded);
        Assert.Null((await f.Store.GetEntryAsync(joined.Id, Ct))!.GroupId);
        Assert.Equal(10000, Assert.Single(await f.Store.GetEntriesAsync(cancellationToken: Ct)).Amount);
        Assert.Equal(before, await SnapshotAsync(f.Provider, omitEntries: true));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Retained_group_cannot_be_used_to_add_parts_or_create_another_group_in_Free(bool changeGroup)
    {
        var f = await FixtureAsync(); var (parts, removed) = Split(f); await f.Store.SaveEntriesAsync(parts, removed, Ct);
        f.Enable(); var before = await SnapshotAsync(f.Provider);
        IReadOnlyList<LedgerEntry> next;
        if (changeGroup)
        {
            var group = Guid.NewGuid(); next = parts.Select(e => e.Copy()).ToList(); foreach (var e in next) e.GroupId = group;
        }
        else next = EntryActions.Split(parts.Select(e => e.Copy()).ToList(), [(null, 4000), (null, 3000), (null, 3000)]).Save;
        var error = await Assert.ThrowsAsync<CommercialWriteRejectedException>(() => f.Store.SaveEntriesAsync(next, [], Ct));
        Assert.Equal(CommercialFeature.SplitTransactions, error.Feature); Assert.Equal(before, await SnapshotAsync(f.Provider));
    }

    [Fact]
    public async Task Incremental_single_entry_saves_cannot_bypass_split_creation_checks()
    {
        var f = await FixtureAsync(); var first = f.Original.Copy(); first.GroupId = Guid.NewGuid();
        await f.Store.SaveEntryAsync(first, Ct); f.Enable(); var before = await SnapshotAsync(f.Provider);
        var second = f.NewEntry(); second.GroupId = first.GroupId;
        var error = await Assert.ThrowsAsync<CommercialWriteRejectedException>(() => f.Store.SaveEntryAsync(second, Ct));
        Assert.Equal(CommercialFeature.SplitTransactions, error.Feature); Assert.Equal(before, await SnapshotAsync(f.Provider));
    }

    [Fact]
    public async Task Transfer_with_source_and_destination_fees_is_Free_and_never_a_category_split()
    {
        var f = await FixtureAsync(); f.Enable(); var before = await SnapshotAsync(f.Provider, omitEntries: true);
        var group = Guid.NewGuid(); var transfer = f.NewEntry(EntryKind.Transfer, 5000);
        transfer.ToAccountId = f.Second.Id; transfer.ToAmount = 6000; transfer.GroupId = group;
        var sourceFee = f.NewEntry(amount: 50); sourceFee.GroupId = group;
        var destinationFee = f.NewEntry(amount: 70); destinationFee.AccountId = f.Second.Id; destinationFee.GroupId = group;
        Assert.True((await f.Store.SaveEntriesAsync([transfer, sourceFee, destinationFee], [], Ct)).Succeeded);
        var entries = await f.Store.GetEntriesAsync(cancellationToken: Ct);
        Assert.Equal(-15050, entries.Sum(e => e.EffectOn(f.Account.Id))); Assert.Equal(5930, entries.Sum(e => e.EffectOn(f.Second.Id)));
        Assert.False(EntryActions.IsSplit(await f.Store.GetGroupAsync(group, Ct)));
        Assert.Equal(before, await SnapshotAsync(f.Provider, omitEntries: true));
    }

    [Fact]
    public async Task New_fee_of_retained_transfer_is_a_correction_after_shared_host_expiry()
    {
        var f = await FixtureAsync(); var transfer = f.NewEntry(EntryKind.Transfer, 5000); transfer.GroupId = Guid.NewGuid();
        transfer.ToAccountId = f.Second.Id; transfer.ToAmount = 6000; await f.Store.SaveEntryAsync(transfer, Ct);
        f.Enable(shared: true, host: false); var before = await SnapshotAsync(f.Provider, omitEntries: true);
        var fee = f.NewEntry(amount: 50); fee.GroupId = transfer.GroupId;
        Assert.True((await f.Store.SaveEntryAsync(fee, Ct)).Succeeded);
        Assert.Equal(2, (await f.Store.GetGroupAsync(transfer.GroupId.Value, Ct)).Count);
        Assert.Equal(before, await SnapshotAsync(f.Provider, omitEntries: true));
    }

    [Theory]
    [InlineData("single")]
    [InlineData("split")]
    [InlineData("join")]
    [InlineData("remove")]
    public async Task Personal_Pro_does_not_replace_membership_for_any_Save_overload(string operation)
    {
        var f = await FixtureAsync(); var (parts, removed) = Split(f);
        if (operation == "join") await f.Store.SaveEntriesAsync(parts, removed, Ct);
        f.Enable(ProductPlan.Pro, shared: true, member: false); var before = await SnapshotAsync(f.Provider); var events = 0;
        f.Store.Changed += (_, _) => events++;
        var error = await Assert.ThrowsAsync<CommercialWriteRejectedException>(async () =>
        {
            if (operation == "single") await f.Store.SaveEntryAsync(f.NewEntry(), Ct);
            else if (operation == "remove") await f.Store.SaveEntriesAsync([], [f.Original.Id], Ct);
            else if (operation == "split") await f.Store.SaveEntriesAsync(parts, removed, parts[0].Id, Ct);
            else
            {
                var (joined, delete) = EntryActions.Join(parts); await f.Store.SaveEntriesAsync([joined], delete, joined.Id, Ct);
            }
        });
        Assert.Equal(FeaturePermission.RequiresMembership, error.Permission); Assert.Equal(0, events);
        Assert.Equal(before, await SnapshotAsync(f.Provider));
    }

    [Theory]
    [InlineData(EntryKind.Adjustment)]
    [InlineData(EntryKind.Refund)]
    [InlineData(EntryKind.IncomeReversal)]
    public async Task New_corrective_money_keeps_owned_data_rights_after_shared_host_expiry(EntryKind kind)
    {
        var f = await FixtureAsync(); f.Enable(shared: true, host: false); var before = await SnapshotAsync(f.Provider, omitEntries: true);
        var entry = f.NewEntry(kind, 500); entry.Direction = kind == EntryKind.Adjustment ? AdjustmentDirection.Increase : null;
        if (kind == EntryKind.Refund) entry.RefundOfId = f.Original.Id;
        Assert.True((await f.Store.SaveEntryAsync(entry, Ct)).Succeeded); Assert.Equal(2, (await f.Store.GetEntriesAsync(cancellationToken: Ct)).Count);
        Assert.Equal(before, await SnapshotAsync(f.Provider, omitEntries: true));
    }

    [Fact]
    public async Task Expired_shared_host_does_not_allow_new_ordinary_transactions()
    {
        var f = await FixtureAsync(); f.Enable(shared: true, host: false); var before = await SnapshotAsync(f.Provider);
        var error = await Assert.ThrowsAsync<CommercialWriteRejectedException>(() => f.Store.SaveEntryAsync(f.NewEntry(), Ct));
        Assert.Equal(CommercialFeature.Transactions, error.Feature); Assert.Equal(FeaturePermission.RequiresPro, error.Permission);
        Assert.Equal(before, await SnapshotAsync(f.Provider));
    }

    [Fact]
    public async Task Mismatched_file_facts_reject_before_stored_reads_or_draft_cleanup()
    {
        var f = await FixtureAsync(); var before = await SnapshotAsync(f.Provider); var entry = f.NewEntry();
        entry.ToAccountId = f.Second.Id; f.Source.Current = new(_directory.Combine("wrong.db"), Context(ProductPlan.Pro));
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Store.SaveEntryAsync(entry, Ct));
        Assert.Equal(f.Second.Id, entry.ToAccountId); Assert.Equal(before, await SnapshotAsync(f.Provider));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Retired_access_after_SQL_rolls_back_complete_split_or_join_without_Changed(bool join)
    {
        var f = await FixtureAsync(trigger: true); var (parts, removed) = Split(f);
        if (join) await f.Store.SaveEntriesAsync(parts, removed, Ct);
        f.Enable(ProductPlan.Plus); var before = await SnapshotAsync(f.Provider); var events = 0; f.Store.Changed += (_, _) => events++;
        await SqlAsync(f, "CREATE TRIGGER RetireLedger AFTER UPDATE ON Entries BEGIN SELECT RetireLedgerAccess(); END;"); f.Source.Retire = true;
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            if (!join) await f.Store.SaveEntriesAsync(parts, removed, parts[0].Id, Ct);
            else { var (joined, delete) = EntryActions.Join(parts); await f.Store.SaveEntriesAsync([joined], delete, joined.Id, Ct); }
        });
        Assert.Equal(1, f.Source.Retired); Assert.Equal(0, events); Assert.Equal(before, await SnapshotAsync(f.Provider));
    }

    [Fact]
    public async Task Failed_attachment_move_rolls_back_join_entries_and_complete_attachment_ownership()
    {
        var f = await FixtureAsync(); var (parts, removed) = Split(f); await f.Store.SaveEntriesAsync(parts, removed, Ct);
        await f.Store.AddAttachmentAsync(new() { EntryId = parts[1].Id, FileName = "owned-receipt.txt", ContentType = "text/plain", Data = [1, 2, 3] }, Ct);
        f.Enable(); var before = await SnapshotAsync(f.Provider); var events = 0; f.Store.Changed += (_, _) => events++;
        await SqlAsync(f, "CREATE TRIGGER RejectMove BEFORE UPDATE ON Attachments BEGIN SELECT RAISE(ABORT,'Owned attachment move failure'); END;");
        var (joined, delete) = EntryActions.Join(parts);
        await Assert.ThrowsAsync<SqliteException>(() => f.Store.SaveEntriesAsync([joined], delete, joined.Id, Ct));
        Assert.Equal(0, events); Assert.Equal(before, await SnapshotAsync(f.Provider));
        await SqlAsync(f, "DROP TRIGGER RejectMove"); Assert.True((await f.Store.SaveEntriesAsync([joined], delete, joined.Id, Ct)).Succeeded);
        Assert.Single(await f.Store.GetAttachmentsAsync(joined.Id, Ct)); Assert.Equal(1, events);
    }

    [Fact]
    public async Task One_invalid_entry_rejects_the_complete_batch_without_cleanup_or_money_writes()
    {
        var f = await FixtureAsync(); f.Enable(); var before = await SnapshotAsync(f.Provider); var valid = f.NewEntry();
        valid.ToAccountId = f.Second.Id; var invalid = f.NewEntry(amount: 0); var events = 0; f.Store.Changed += (_, _) => events++;
        var result = await f.Store.SaveEntriesAsync([valid, invalid], [], Ct); Assert.False(result.Succeeded);
        Assert.Contains(LedgerError.AmountMustBePositive, result.Errors); Assert.Equal(f.Second.Id, valid.ToAccountId);
        Assert.Equal(0, events); Assert.Equal(before, await SnapshotAsync(f.Provider));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Independent_same_id_writers_save_one_entry_in_inactive_and_enabled_operations(bool enabled)
    {
        var f = await FixtureAsync(); var other = Provider(f.Path, f.Source).GetRequiredService<ZananceStore>();
        if (enabled) f.Enable(); var before = await SnapshotAsync(f.Provider, omitEntries: true);
        var first = f.NewEntry(); var second = first.Copy(); using var barrier = new Barrier(2); f.Source.Rendezvous = barrier; f.Source.Captures = 0;
        var results = await Task.WhenAll(Task.Run(() => f.Store.SaveEntryAsync(first, Ct), Ct), Task.Run(() => other.SaveEntryAsync(second, Ct), Ct));
        Assert.All(results, r => Assert.True(r.Succeeded)); Assert.Equal(2, (await f.Store.GetEntriesAsync(cancellationToken: Ct)).Count);
        Assert.Equal(before, await SnapshotAsync(f.Provider, omitEntries: true));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Independent_refunds_recheck_the_actual_stored_total_under_the_writer(bool enabled)
    {
        var f = await FixtureAsync(); var other = Provider(f.Path, f.Source).GetRequiredService<ZananceStore>();
        if (enabled) f.Enable(); var first = EntryActions.CreateRefund(f.Original, 7000, f.Account.Id, Day);
        var second = EntryActions.CreateRefund(f.Original, 7000, f.Account.Id, Day);
        using var barrier = new Barrier(2); f.Source.Rendezvous = barrier; f.Source.Captures = 0;
        var results = await Task.WhenAll(Task.Run(() => f.Store.SaveEntryAsync(first, Ct), Ct), Task.Run(() => other.SaveEntryAsync(second, Ct), Ct));
        Assert.Single(results, r => r.Succeeded); Assert.Contains(LedgerError.RefundExceedsPurchase, results.Single(r => !r.Succeeded).Errors);
        Assert.Equal(7000, Assert.Single(await f.Store.GetRefundsAsync(f.Original.Id, Ct)).Amount);
        Assert.Equal(2, (await f.Store.GetEntriesAsync(cancellationToken: Ct)).Count);
    }

    [Fact]
    public async Task Access_capture_keeps_the_initial_file_after_the_profile_provider_moves()
    {
        var f = await FixtureAsync(); var otherPath = _directory.Combine("untouched-profile.db");
        var other = Provider(otherPath, new AccessSource(otherPath)); var before = await SnapshotAsync(other);
        f.Enable(); f.Source.OnCapture = () => f.Provider.GetRequiredService<LocalDatabaseLocation<ZananceDbContext>>().MoveTo(otherPath);
        Assert.True((await f.Store.SaveEntryAsync(f.NewEntry(), Ct)).Succeeded); Assert.Equal(before, await SnapshotAsync(other));
        f.Source.OnCapture = null; f.Provider.GetRequiredService<LocalDatabaseLocation<ZananceDbContext>>().MoveTo(f.Path);
        Assert.Equal(2, (await f.Store.GetEntriesAsync(cancellationToken: Ct)).Count); Assert.All(f.Source.Paths, path => Assert.Equal(f.Path, path));
    }

    [Fact]
    public async Task Two_new_refunds_in_one_batch_cannot_exceed_the_purchase_together()
    {
        var f = await FixtureAsync(); f.Enable(); var before = await SnapshotAsync(f.Provider);
        var first = EntryActions.CreateRefund(f.Original, 7000, f.Account.Id, Day);
        var second = EntryActions.CreateRefund(f.Original, 7000, f.Account.Id, Day);
        var result = await f.Store.SaveEntriesAsync([first, second], [], Ct);
        Assert.False(result.Succeeded); Assert.Contains(LedgerError.RefundExceedsPurchase, result.Errors);
        Assert.Equal(before, await SnapshotAsync(f.Provider));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Purchase_and_refund_edits_validate_the_complete_final_batch(bool changeKind)
    {
        var f = await FixtureAsync(); var refund = EntryActions.CreateRefund(f.Original, 7000, f.Account.Id, Day);
        await f.Store.SaveEntryAsync(refund, Ct); f.Enable(); var before = await SnapshotAsync(f.Provider);
        var purchase = f.Original.Copy(); if (changeKind) purchase.Kind = EntryKind.Income; else purchase.Amount = 5000;
        var result = await f.Store.SaveEntryAsync(purchase, Ct); Assert.False(result.Succeeded);
        Assert.Contains(changeKind ? LedgerError.RefundOriginalNotExpense : LedgerError.RefundExceedsPurchase, result.Errors);
        Assert.Equal(before, await SnapshotAsync(f.Provider));
    }

    [Fact]
    public async Task Positive_refund_sum_cannot_wrap_around_Int64_and_pass_validation()
    {
        var f = await FixtureAsync(); f.Original.Amount = long.MaxValue; await f.Store.SaveEntryAsync(f.Original, Ct);
        await f.Store.SaveEntryAsync(EntryActions.CreateRefund(f.Original, 1, f.Account.Id, Day), Ct);
        f.Enable(); var before = await SnapshotAsync(f.Provider);
        var result = await f.Store.SaveEntryAsync(EntryActions.CreateRefund(f.Original, long.MaxValue, f.Account.Id, Day), Ct);
        Assert.False(result.Succeeded); Assert.Contains(LedgerError.RefundExceedsPurchase, result.Errors);
        Assert.Equal(before, await SnapshotAsync(f.Provider));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task New_purchase_and_its_refund_validate_the_same_final_batch_in_either_order(bool refundFirst)
    {
        var f = await FixtureAsync(); f.Enable(); var before = await SnapshotAsync(f.Provider, omitEntries: true);
        var purchase = f.NewEntry(amount: 8000); var refund = EntryActions.CreateRefund(purchase, 7000, f.Account.Id, Day);
        var result = await f.Store.SaveEntriesAsync(refundFirst ? [refund, purchase] : [purchase, refund], [], Ct);
        Assert.True(result.Succeeded); Assert.Equal(7000, Assert.Single(await f.Store.GetRefundsAsync(purchase.Id, Ct)).Amount);
        Assert.Equal(before, await SnapshotAsync(f.Provider, omitEntries: true));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Final_refund_validation_uses_edited_or_removed_refunds_rather_than_their_old_amounts(bool remove)
    {
        var f = await FixtureAsync(); var old = EntryActions.CreateRefund(f.Original, 7000, f.Account.Id, Day);
        await f.Store.SaveEntryAsync(old, Ct); f.Enable(); var before = await SnapshotAsync(f.Provider, omitEntries: true);
        var purchase = f.Original.Copy(); purchase.Amount = 5000;
        var refund = remove ? EntryActions.CreateRefund(f.Original, 5000, f.Account.Id, Day) : old.Copy();
        if (!remove) refund.Amount = 4000;
        Assert.True((await f.Store.SaveEntriesAsync([purchase, refund], remove ? [old.Id] : [], Ct)).Succeeded);
        Assert.Equal(remove ? 5000 : 4000, Assert.Single(await f.Store.GetRefundsAsync(purchase.Id, Ct)).Amount);
        Assert.Equal(before, await SnapshotAsync(f.Provider, omitEntries: true));
    }

    [Fact]
    public async Task Missing_refund_purchase_returns_the_existing_field_error_without_SQL_or_money_changes()
    {
        var f = await FixtureAsync(); f.Enable(); var before = await SnapshotAsync(f.Provider); var refund = f.NewEntry(EntryKind.Refund);
        refund.RefundOfId = Guid.NewGuid(); var result = await f.Store.SaveEntryAsync(refund, Ct);
        Assert.False(result.Succeeded); Assert.Equal([LedgerError.RefundOriginalMissing], result.Errors);
        Assert.Equal(before, await SnapshotAsync(f.Provider));
    }

    [Fact]
    public async Task Exact_Int64_purchase_refund_boundary_remains_valid()
    {
        var f = await FixtureAsync(); f.Original.Amount = long.MaxValue; await f.Store.SaveEntryAsync(f.Original, Ct);
        await f.Store.SaveEntryAsync(EntryActions.CreateRefund(f.Original, 1, f.Account.Id, Day), Ct); f.Enable();
        Assert.True((await f.Store.SaveEntryAsync(EntryActions.CreateRefund(f.Original, long.MaxValue - 1, f.Account.Id, Day), Ct)).Succeeded);
        Assert.Equal(long.MaxValue, (await f.Store.GetRefundsAsync(f.Original.Id, Ct)).Sum(e => e.Amount));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Partial_payment_and_derived_paid_total_roll_back_together_on_SQL_or_access_failure(bool retire)
    {
        var f = await FixtureAsync(trigger: retire); var plans = f.Provider.GetRequiredService<PlanStore>();
        var plan = new Schedule { Name = "Owned planned expense", AccountId = f.Account.Id, Amount = 10000, Rule = new() { Start = Day } };
        await plans.SaveScheduleAsync(plan, Ct);
        var part = f.NewEntry(amount: 300); part.ScheduleId = plan.Id; part.OccurrenceDate = Day; part.IsPartialPayment = true; part.Source = EntrySource.Schedule;
        await f.Store.SaveEntryAsync(part, Ct); f.Enable(ProductPlan.Plus); var before = await SnapshotAsync(f.Provider);
        var events = 0; f.Store.Changed += (_, _) => events++;
        await SqlAsync(f, retire
            ? "CREATE TRIGGER RetirePaid AFTER UPDATE ON OccurrenceStates BEGIN SELECT RetireLedgerAccess(); END;"
            : "CREATE TRIGGER RejectPaid BEFORE UPDATE ON OccurrenceStates BEGIN SELECT RAISE(ABORT,'Owned paid-total failure'); END;");
        f.Source.Retire = retire; var edit = part.Copy(); edit.Amount = 600;
        if (retire) await Assert.ThrowsAsync<InvalidOperationException>(() => f.Store.SaveEntryAsync(edit, Ct));
        else await Assert.ThrowsAsync<DbUpdateException>(() => f.Store.SaveEntryAsync(edit, Ct));
        Assert.Equal(0, events); Assert.Equal(before, await SnapshotAsync(f.Provider));
        await SqlAsync(f, "DROP TRIGGER " + (retire ? "RetirePaid" : "RejectPaid"));
        Assert.True((await f.Store.SaveEntryAsync(edit, Ct)).Succeeded);
        Assert.Equal(600, Assert.Single(await plans.GetStatesAsync(plan.Id, Ct)).PaidAmount); Assert.Equal(1, events);
    }

    private static async Task SqlAsync(Fixture f, string sql)
    {
        await using var db = await f.Provider.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct);
        await db.Database.ExecuteSqlRawAsync(sql, Ct);
    }

    private static async Task<string> SnapshotAsync(ServiceProvider provider, bool omitEntries = false)
    {
        await using var db = await provider.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct);
        await db.Database.OpenConnectionAsync(Ct); var connection = db.Database.GetDbConnection(); var tables = new List<string>();
        using (var command = connection.CreateCommand())
        {
            command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%' ORDER BY name";
            using var reader = await command.ExecuteReaderAsync(Ct); while (await reader.ReadAsync(Ct)) tables.Add(reader.GetString(0));
        }
        Assert.Equal(25, tables.Count); var all = new Dictionary<string, List<string>>();
        foreach (var table in tables)
        {
            if (omitEntries && table == "Entries") continue;
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

    /// <summary>Synchronous independent cached facts, with coordination before the actual writer wait.</summary>
    private sealed class AccessSource(string path) : ICommercialWriteAccessSource
    {
        public CommercialWriteAccess Current = CommercialWriteAccess.Inactive;
        public Barrier? Rendezvous = null;
        public int Captures;
        public bool Retire;
        public int Retired;
        public Action? OnCapture = null;
        public readonly List<string> Paths = [];
        public CommercialWriteAccess Capture(string databasePath)
        {
            Assert.Equal(System.IO.Path.GetFullPath(path), databasePath); lock (Paths) Paths.Add(databasePath);
            OnCapture?.Invoke();
            if (Rendezvous is { } barrier && Interlocked.Increment(ref Captures) <= 2
                && !barrier.SignalAndWait(TimeSpan.FromSeconds(15), Ct)) throw new TimeoutException("Ledger writers did not rendezvous.");
            return Current;
        }
    }

    /// <summary>Changes only cached fixture rights from the production factory's actual native SQL connection.</summary>
    private sealed class TriggerConnections(IDbContextFactory<ZananceDbContext> inner, AccessSource source) : IDbContextFactory<ZananceDbContext>
    {
        public ZananceDbContext CreateDbContext()
        {
            var db = inner.CreateDbContext();
            ((SqliteConnection)db.Database.GetDbConnection()).CreateFunction("RetireLedgerAccess", () =>
            {
                if (source.Retire)
                {
                    source.Current = new(source.Current.DatabasePath!, Context(ProductPlan.Free)); source.Retire = false; source.Retired++;
                }
                return 1;
            });
            return db;
        }
    }

    /// <summary>Stable audit time for complete stored-column comparisons.</summary>
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
