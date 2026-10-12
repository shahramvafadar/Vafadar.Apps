using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Documents;
using Vafadar.Zanance.App.Features.Receipts;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Commerce;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Receipts;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Actual shared receipt application source, original parser and real isolated source/file/access validation.</summary>
[Trait("AT", "AT-171")]
public sealed class ReceiptWorkFlowTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private static readonly Guid Scope = Guid.Parse("24242424-3535-4646-5757-686868686868");
    private sealed class Access : ICommercialWriteAccessSource
    {
        internal CommercialWriteAccess Current = CommercialWriteAccess.Inactive;
        public CommercialWriteAccess Capture(string databasePath) => Current;
    }
    private sealed class Factory(IDbContextFactory<ZananceDbContext> original) : IDbContextFactory<ZananceDbContext>
    {
        internal IDbContextFactory<ZananceDbContext> Current = original;
        public ZananceDbContext CreateDbContext() => Current.CreateDbContext();
        public Task<ZananceDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) => Current.CreateDbContextAsync(cancellationToken);
    }
    private sealed class Native : IReceiptRuntime
    {
        internal ReceiptPickedFile? File = new("Original compact receipt.jpg", "image/jpeg", [1, 2], [3, 4, 5]);
        internal TextLayoutResult Document = new("Fictitious shop\nTOTAL EUR 24.50");
        internal int Picked, Unlocked, Read;
        internal byte[]? Recognition;
        internal Func<Task>? DuringPick, DuringRead, DuringAlert, DuringOpen;
        internal bool AllowUnlock = true;
        internal List<string> Alerts = [];
        internal List<ReceiptDraft> Drafts = [];
        public async Task<ReceiptPickedFile?> PickAsync() { Picked++; if (DuringPick is not null) await DuringPick(); return File; }
        public Task RunWhenUnlockedAsync(Func<Task> action) { Unlocked++; return AllowUnlock ? action() : Task.CompletedTask; }
        public async Task<TextLayoutResult> ReadAsync(byte[] data, string contentType)
        { Read++; Recognition = data; if (DuringRead is not null) await DuringRead(); return Document; }
        public async Task AlertAsync(string title, string message, string cancel)
        { Alerts.Add(message); if (DuringAlert is not null) { var action = DuringAlert; DuringAlert = null; await action(); } }
        public async Task OpenEditorAsync(ReceiptDraft draft) { if (DuringOpen is not null) await DuringOpen(); Drafts.Add(draft); }
    }
    private static ServiceProvider Provider(FlowFixture f, Access access) => new ServiceCollection()
        .AddSingleton<TimeProvider>(f.Time).AddSingleton<ICommercialWriteAccessSource>(access)
        .AddZananceData(f.Directory.Combine("zanance.db")).BuildServiceProvider();
    private static void Enable(FlowFixture f, Access access, ProductPlan plan = ProductPlan.Free, bool shared = false, bool member = true, bool host = true) =>
        access.Current = new(f.Directory.Combine("zanance.db"), new(plan, new(shared ? EntitlementScopeKind.SharedSpace : EntitlementScopeKind.PersonalProfile, Scope),
            shared ? new(Scope, member, host) : null));
    private static ReceiptReadingSession Session(FlowFixture f, ServiceProvider p, Native native) =>
        new(p.GetRequiredService<ReceiptWorkStore>(), native, f.Translator, f.Time);
    private static async Task<(LedgerEntry Entry, EntryAttachment Attachment)> SeedAsync(FlowFixture f)
    {
        var account = new Account { Name = "Original receipt account", CurrencyCode = "EUR", OpeningDate = new(2026, 10, 1), OpeningBalance = 10000 };
        await f.Store.SaveAccountAsync(account, Ct);
        var entry = new LedgerEntry { AccountId = account.Id, Kind = EntryKind.Expense, Date = new(2026, 10, 9), Amount = 9876, Title = "Original recorded expense" };
        Assert.True((await f.Store.SaveEntryAsync(entry, Ct)).Succeeded);
        var attachment = new EntryAttachment { EntryId = entry.Id, FileName = "Original stored receipt.jpg", ContentType = "image/jpeg", Data = [7, 8, 9] };
        await f.Store.AddAttachmentAsync(attachment, Ct); return (entry, attachment);
    }
    [Theory]
    [InlineData("en")][InlineData("fa")][InlineData("de")][InlineData("es")][InlineData("fr")][InlineData("it")]
    public async Task Free_refuses_before_any_native_source_or_read_and_creates_no_settings_or_financial_rows(string language)
    {
        using var f = new FlowFixture(); f.Localization.SetLanguage(f.Localization.SupportedLanguages.Single(item => item.CultureName == language));
        var access = new Access(); using var p = Provider(f, access); Enable(f, access); var native = new Native(); var session = Session(f, p, native);
        var before = await GoalChooserFlowTests.SnapshotAsync(f); await session.PickAsync();
        Assert.NotNull(session.Error); Assert.Contains(f.Translator["Commercial_RequiresPlus"], session.Error); Assert.Equal(0, native.Picked); Assert.Equal(0, native.Read);
        Assert.Empty(native.Drafts); Assert.Single(native.Alerts); Assert.False(session.IsBusy); Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }
    [Theory]
    [InlineData(-1)][InlineData(1)][InlineData(2)]
    public async Task Inactive_Plus_and_Pro_preserve_original_parser_and_only_compact_copy_reaches_unsaved_draft(int plan)
    {
        using var f = new FlowFixture(); var access = new Access(); using var p = Provider(f, access); if (plan >= 0) Enable(f, access, (ProductPlan)plan);
        var native = new Native(); var session = Session(f, p, native); var before = await GoalChooserFlowTests.SnapshotAsync(f); await session.PickAsync();
        var draft = Assert.Single(native.Drafts); Assert.Null(session.Error); Assert.Equal(1, native.Unlocked); Assert.Equal(native.File!.RecognitionData, native.Recognition);
        Assert.Equal(24.50m, draft.Suggestion.Amount); Assert.Equal(ReceiptAmountStatus.Found, draft.Suggestion.AmountStatus); Assert.Null(draft.File!.RecognitionData);
        Assert.Equal(native.File.Data, draft.File.Data); await draft.ValidateAsync(p.GetRequiredService<ReceiptWorkStore>(), null);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }
    [Fact]
    public async Task Picker_cancellation_and_unlock_decline_do_not_read_or_open_an_editor()
    {
        using var f = new FlowFixture(); var access = new Access(); using var p = Provider(f, access); var native = new Native { File = null }; var session = Session(f, p, native);
        await session.PickAsync(); Assert.Equal(0, native.Read); Assert.Equal(0, native.Unlocked); Assert.Empty(native.Alerts);
        native.File = new("Retained compact.jpg", "image/jpeg", [1]); native.AllowUnlock = false; await session.PickAsync();
        Assert.Equal(1, native.Unlocked); Assert.Equal(0, native.Read); Assert.Empty(native.Drafts); Assert.Null(session.Error);
    }
    [Theory]
    [InlineData(false)][InlineData(true)]
    public async Task Access_retired_while_picking_or_reading_never_publishes_a_late_suggestion(bool reading)
    {
        using var f = new FlowFixture(); var access = new Access(); using var p = Provider(f, access); Enable(f, access, ProductPlan.Plus); var native = new Native();
        Task Retire() { Enable(f, access); return Task.CompletedTask; }
        if (reading) native.DuringRead = Retire; else native.DuringPick = Retire;
        var session = Session(f, p, native); var before = await GoalChooserFlowTests.SnapshotAsync(f); await session.PickAsync();
        Assert.NotNull(session.Error); Assert.Empty(native.Drafts); Assert.Equal(reading ? 1 : 0, native.Read); Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }
    [Theory]
    [InlineData(0)][InlineData(1)][InlineData(2)]
    public async Task Shared_recognition_requires_original_membership_and_host_access_independently_of_personal_plan(int plan)
    {
        using var f = new FlowFixture(); var access = new Access(); using var p = Provider(f, access); var native = new Native(); var session = Session(f, p, native);
        Enable(f, access, (ProductPlan)plan, true, false); await session.PickAsync(); Assert.Empty(native.Drafts); Assert.Equal(0, native.Read);
        Enable(f, access, (ProductPlan)plan, true, true, false); await session.PickAsync(); Assert.Empty(native.Drafts);
        Enable(f, access, (ProductPlan)plan, true); await session.PickAsync(); Assert.Single(native.Drafts);
    }
    [Fact]
    public async Task Existing_empty_read_never_opens_editor_or_erases_original_amount_and_keeps_all_stored_rows()
    {
        using var f = new FlowFixture(); var original = await SeedAsync(f); var access = new Access(); using var p = Provider(f, access); var native = new Native { Document = new(null) };
        var before = await GoalChooserFlowTests.SnapshotAsync(f); var session = Session(f, p, native); await session.ReadAttachmentAsync(original.Entry.Id, original.Attachment.Id);
        Assert.Null(session.Error); Assert.Empty(native.Drafts); Assert.Equal(0, native.Picked); Assert.Equal(0, native.Unlocked); Assert.Equal(original.Attachment.Data, native.Recognition);
        Assert.Equal(f.Translator["Receipt_Nothing"], Assert.Single(native.Alerts)); Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
        Assert.Equal(9876, (await f.Store.GetEntryAsync(original.Entry.Id, Ct))!.Amount);
    }
    [Fact]
    public async Task New_empty_read_still_opens_only_compact_pending_attachment_after_explicit_notice_without_saving()
    {
        using var f = new FlowFixture(); var access = new Access(); using var p = Provider(f, access); var native = new Native { Document = new(null) }; var session = Session(f, p, native);
        var before = await GoalChooserFlowTests.SnapshotAsync(f); await session.PickAsync(); var draft = Assert.Single(native.Drafts);
        Assert.True(draft.Suggestion.IsEmpty); Assert.NotNull(draft.File); Assert.Null(draft.File.RecognitionData); Assert.Single(native.Alerts); Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }
    [Theory]
    [InlineData("entry")][InlineData("bytes")][InlineData("name")][InlineData("owner")][InlineData("deleted")]
    public async Task Existing_source_changed_at_unchanged_audit_time_during_native_read_is_rejected(string change)
    {
        using var f = new FlowFixture(); var original = await SeedAsync(f); var access = new Access(); using var p = Provider(f, access); var native = new Native();
        native.DuringRead = async () =>
        {
            await using var db = await p.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct);
            var sql = change switch { "entry" => "UPDATE Entries SET Amount=1234", "bytes" => "UPDATE Attachments SET Data=X'010203'",
                "name" => "UPDATE Attachments SET FileName='Changed original name'", "owner" => "UPDATE Attachments SET EntryId='00000000-0000-0000-0000-000000000000'",
                _ => "DELETE FROM Attachments" };
            if (change == "owner") await db.Database.ExecuteSqlRawAsync("PRAGMA foreign_keys=OFF", Ct);
            await db.Database.ExecuteSqlRawAsync(sql, Ct);
        };
        var session = Session(f, p, native); await session.ReadAttachmentAsync(original.Entry.Id, original.Attachment.Id);
        Assert.NotNull(session.Error); Assert.Empty(native.Drafts); Assert.Equal(1, native.Read); Assert.Single(native.Alerts);
    }
    [Fact]
    public async Task Different_attachment_owner_or_nonexpense_entry_is_refused_before_native_recognition()
    {
        using var f = new FlowFixture(); var original = await SeedAsync(f); var access = new Access(); using var p = Provider(f, access); var native = new Native(); var session = Session(f, p, native);
        await session.ReadAttachmentAsync(Guid.NewGuid(), original.Attachment.Id); Assert.Equal(0, native.Read); Assert.Empty(native.Drafts);
        await using var db = await p.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct);
        await db.Database.ExecuteSqlRawAsync("UPDATE Entries SET Kind=0", Ct); await session.ReadAttachmentAsync(original.Entry.Id, original.Attachment.Id); Assert.Equal(0, native.Read);
    }
    [Fact]
    public async Task Original_ambiguity_is_passed_to_parser_and_suggestion_requires_explicit_review()
    {
        using var f = new FlowFixture(); var access = new Access(); using var p = Provider(f, access); var native = new Native { Document = new("TOTAL EUR 24.50", true) };
        await Session(f, p, native).PickAsync(); var draft = Assert.Single(native.Drafts); Assert.Equal(ReceiptAmountStatus.Review, draft.Suggestion.AmountStatus);
        Assert.Null(draft.Suggestion.Amount); Assert.NotEmpty(draft.Suggestion.Candidates);
    }
    [Fact]
    public async Task Busy_spans_native_read_and_navigation_and_another_command_cannot_start_a_second_receipt()
    {
        using var f = new FlowFixture(); var access = new Access(); using var p = Provider(f, access); var native = new Native(); var session = Session(f, p, native);
        native.DuringRead = async () => { Assert.True(session.IsBusy); await session.PickAsync(); Assert.Equal(1, native.Read); };
        native.DuringOpen = async () => { Assert.True(session.IsBusy); await session.PickAsync(); Assert.Equal(1, native.Picked); };
        await session.PickAsync(); Assert.Single(native.Drafts); Assert.False(session.IsBusy);
    }
    [Fact]
    public async Task Late_destination_guard_rejects_changed_access_or_wrong_entry_without_another_read_or_save()
    {
        using var f = new FlowFixture(); var original = await SeedAsync(f); var access = new Access(); using var p = Provider(f, access); Enable(f, access, ProductPlan.Plus); var native = new Native();
        var before = await GoalChooserFlowTests.SnapshotAsync(f); await Session(f, p, native).ReadAttachmentAsync(original.Entry.Id, original.Attachment.Id); var draft = Assert.Single(native.Drafts);
        await Assert.ThrowsAsync<InvalidOperationException>(() => draft.ValidateAsync(p.GetRequiredService<ReceiptWorkStore>(), Guid.NewGuid()));
        Enable(f, access); await Assert.ThrowsAsync<InvalidOperationException>(() => draft.ValidateAsync(p.GetRequiredService<ReceiptWorkStore>(), original.Entry.Id));
        Assert.Equal(1, native.Read); Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }
    [Fact]
    public async Task Changed_access_during_empty_result_notice_is_rejected_before_new_pending_editor()
    {
        using var f = new FlowFixture(); var access = new Access(); using var p = Provider(f, access); Enable(f, access, ProductPlan.Plus); var native = new Native { Document = new(null) };
        native.DuringAlert = () => { Enable(f, access); return Task.CompletedTask; }; var session = Session(f, p, native); await session.PickAsync();
        Assert.Empty(native.Drafts); Assert.NotNull(session.Error); Assert.Equal(2, native.Alerts.Count);
    }
    [Fact]
    public async Task Native_read_failure_has_translated_feedback_no_exception_content_and_no_financial_write()
    {
        using var f = new FlowFixture(); var access = new Access(); using var p = Provider(f, access); var native = new Native { DuringRead = () => throw new IOException("Private source content") };
        var before = await GoalChooserFlowTests.SnapshotAsync(f); var session = Session(f, p, native); await session.PickAsync();
        Assert.Equal(f.Translator["Receipt_ReadFailed"], session.Error); Assert.DoesNotContain("Private", session.Error); Assert.Empty(native.Drafts); Assert.False(session.IsBusy);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }
    [Fact]
    public async Task Actual_factory_switch_during_recognition_cannot_open_an_editor_in_another_original_file()
    {
        using var f = new FlowFixture(); using var other = new FlowFixture(); var access = new Access();
        var factory = new Factory(f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>());
        var reader = new ReceiptWorkStore(factory, access); var native = new Native();
        native.DuringRead = () => { factory.Current = other.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>(); return Task.CompletedTask; };
        var session = new ReceiptReadingSession(reader, native, f.Translator, f.Time);
        var before = await GoalChooserFlowTests.SnapshotAsync(f); var otherBefore = await GoalChooserFlowTests.SnapshotAsync(other);
        await session.PickAsync(); Assert.NotNull(session.Error); Assert.Empty(native.Drafts); Assert.Equal(1, native.Read);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f)); Assert.Equal(otherBefore, await GoalChooserFlowTests.SnapshotAsync(other));
    }
    [Fact]
    public async Task Inactive_to_active_change_during_picker_requires_a_fresh_explicit_receipt_attempt()
    {
        using var f = new FlowFixture(); var access = new Access(); using var p = Provider(f, access); var native = new Native();
        native.DuringPick = () => { Enable(f, access, ProductPlan.Pro); return Task.CompletedTask; }; var session = Session(f, p, native);
        await session.PickAsync(); Assert.NotNull(session.Error); Assert.Equal(0, native.Read); Assert.Empty(native.Drafts);
        native.DuringPick = null; await session.PickAsync(); Assert.Null(session.Error); Assert.Single(native.Drafts);
    }
}
