using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Zanance.App;
using Vafadar.Zanance.App.Features.Commerce;
using Vafadar.Zanance.App.Features.Home;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Commerce;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Tests;

/// <summary>AT-157: actual Home template commands and original destination packets with isolated SQLite.</summary>
[Trait("AT", "AT-157")]
public sealed class TemplateNavigationFlowTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private sealed class AccessSource : ICommercialWriteAccessSource
    {
        internal CommercialWriteAccess Current = CommercialWriteAccess.Inactive;
        public CommercialWriteAccess Capture(string databasePath) => Current;
    }
    private sealed record Setup(FlowFixture F, AccessSource Access, ZananceStore Store, ResourceChoiceStore Choices,
        IDbContextFactory<ZananceDbContext> Factory, TemplateNavigationViewModel Home, EntryTemplate[] Originals);
    private static async Task<Setup> CreateAsync(bool bounded = false)
    {
        var f = new FlowFixture(); var account = new Account { Name = "Fictitious original", CurrencyCode = "EUR", OpeningDate = new(2026, 10, 1) };
        await f.Store.SaveAccountAsync(account, Ct);
        var source = new LedgerEntry { AccountId = account.Id, Date = new(2026, 10, 9), Kind = EntryKind.Expense,
            Amount = 12345, Title = "Original title", Payee = "Original payee", Tags = ["original"] };
        Assert.True((await f.Store.SaveEntryAsync(source, cancellationToken: Ct)).Succeeded);
        var originals = new List<EntryTemplate>();
        for (var i = 0; i < 8; i++)
        {
            var template = EntryTemplate.From(source, "Original template " + i, true); template.SortOrder = i;
            await f.Store.SaveTemplateAsync(template, Ct); originals.Add(template);
        }
        var access = new AccessSource();
        if (bounded) access.Current = new(f.Directory.Combine("zanance.db"), new(ProductPlan.Free,
            new(EntitlementScopeKind.PersonalProfile, Guid.Parse("57575757-3434-4545-6767-898989898989"))));
        var factory = f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>();
        var store = new ZananceStore(factory, f.Time, access); var choices = new ResourceChoiceStore(factory, access);
        TemplateNavigationViewModel? home = null;
        home = new(store, f.Translator, f.Platform, async () => { home!.BeginLoad(); home.Publish(await store.GetQuickResourceReviewAsync(cancellationToken: Ct)); });
        home.Publish(await store.GetQuickResourceReviewAsync(cancellationToken: Ct));
        return new(f, access, store, choices, factory, home, [.. originals]);
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Opening_preserves_all_originals_and_history_without_creating_preferences_or_guessing_work(bool bounded)
    {
        var s = await CreateAsync(bounded); using var f = s.F; var before = await GoalChooserFlowTests.SnapshotAsync(f);
        Assert.Equal(s.Originals.Take(6).Select(t => t.Id), s.Home.Items.Select(t => t.Id));
        Assert.All(s.Home.Items, t => Assert.Equal(!bounded, s.Home.CanUse(t.Id)));
        Assert.Equal(bounded, s.Home.CanChoose); Assert.Equal(bounded, s.Home.WorkText is not null);
        await s.Home.OpenAsync(s.Originals[0].Id);
        Assert.Equal(bounded ? 0 : 1, f.Platform.Routes.Count);
        Assert.Equal(8, (await s.Store.GetTemplatesAsync(Ct)).Count); Assert.Single(await s.Store.GetEntriesAsync(cancellationToken: Ct));
        Assert.Null((await s.Store.GetQuickResourceReviewAsync(cancellationToken: Ct)).Settings);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }
    [Theory]
    [InlineData("en")]
    [InlineData("fa")]
    [InlineData("de")]
    public async Task Equal_sort_orders_retain_original_SQLite_name_order_in_every_display_culture(string language)
    {
        var s = await CreateAsync(); using var f = s.F;
        f.Localization.SetLanguage(f.Localization.SupportedLanguages.First(l => l.CultureName == language));
        var names = new[] { "Ä original", "Z original", "a original", "A original", "ا original", "😀 original", "\uE000 original", "𐐀 original" };
        await using (var db = await s.Factory.CreateDbContextAsync(Ct))
        {
            for (var i = 0; i < names.Length; i++)
            {
                var id = s.Originals[i].Id; var name = names[i];
                await db.Templates.Where(t => t.Id == id).ExecuteUpdateAsync(u => u.SetProperty(t => t.SortOrder, 0).SetProperty(t => t.Name, name), Ct);
            }
        }
        var before = await GoalChooserFlowTests.SnapshotAsync(f);
        var expected = (await s.Store.GetTemplatesAsync(Ct)).Take(6).Select(t => t.Id).ToArray();
        s.Home.Publish(await s.Store.GetQuickResourceReviewAsync(cancellationToken: Ct));
        Assert.Equal(expected, s.Home.Items.Select(t => t.Id)); Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }

    [Fact]
    public async Task Explicit_later_choices_are_reachable_first_without_filtering_the_original_list_or_saving_money()
    {
        var s = await CreateAsync(true); using var f = s.F;
        var selected = s.Originals.Skip(6).Select(t => t.Id).ToArray();
        await s.Choices.SaveAsync(await s.Choices.ReadAsync(QuotaKind.QuickTemplates, Ct), selected, Ct);
        var before = await GoalChooserFlowTests.SnapshotAsync(f);
        s.Home.Publish(await s.Store.GetQuickResourceReviewAsync(cancellationToken: Ct));
        Assert.Equal(selected, s.Home.Items.Take(2).Select(t => t.Id)); Assert.All(selected, id => Assert.True(s.Home.CanUse(id)));
        Assert.False(s.Home.CanUse(s.Originals[0].Id)); await s.Home.OpenAsync(s.Originals[0].Id); Assert.Empty(f.Platform.Routes);
        await s.Home.OpenAsync(selected[0]); var route = Assert.Single(f.Platform.Routes);
        Assert.Equal(AppRoutes.EntryEditorRoute, route.Route); Assert.Equal(selected[0], route.Parameters!["template"]);
        var visit = Assert.IsType<TemplateVisit>(route.Parameters["templateVisit"]);
        var actions = new QuickResourceActions(s.Store, f.Translator, f.Platform); await actions.LoadAsync();
        EntryTemplate? detached = null; await actions.ApplyTemplateVisitAsync(visit, t => detached = t);
        Assert.Equal(selected[0], detached!.Id); Assert.Equal(12345, detached.Amount); Assert.Equal("Original title", detached.Title);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }
    [Theory]
    [InlineData("amount")]
    [InlineData("account")]
    [InlineData("filter")]
    [InlineData("rights")]
    [InlineData("deleted")]
    public async Task Changed_original_data_or_rights_reject_Home_navigation_without_automatic_retry(string changed)
    {
        var s = await CreateAsync(); using var f = s.F; var id = s.Originals[0].Id;
        await ChangeAsync(s, changed, id); var before = await GoalChooserFlowTests.SnapshotAsync(f);
        await s.Home.OpenAsync(id); Assert.Empty(f.Platform.Routes); Assert.Single(f.Platform.Failures);
        Assert.False(s.Home.CanInteract); Assert.Equal(f.Translator["Templates_SourceChanged"], s.Home.Feedback);
        await s.Home.OpenAsync(id); Assert.Single(f.Platform.Failures); Assert.Empty(f.Platform.Routes);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }
    private static async Task ChangeAsync(Setup s, string changed, Guid id)
    {
        await using var db = await s.Factory.CreateDbContextAsync(Ct);
        switch (changed)
        {
            case "amount": await db.Templates.Where(t => t.Id == id).ExecuteUpdateAsync(u => u.SetProperty(t => t.Amount, 98765), Ct); break;
            case "account": await db.Accounts.ExecuteUpdateAsync(u => u.SetProperty(a => a.Name, "Changed without timestamp"), Ct); break;
            case "filter": await s.F.Store.SaveSavedFilterAsync(new SavedFilter { Name = "New original filter" }, Ct); break;
            case "rights": s.Access.Current = new(s.F.Directory.Combine("zanance.db"), new(ProductPlan.Free, new(EntitlementScopeKind.PersonalProfile, Guid.NewGuid()))); break;
            case "deleted": await db.Templates.Where(t => t.Id == id).ExecuteDeleteAsync(Ct); break;
            default: throw new ArgumentOutOfRangeException(nameof(changed));
        }
    }
    [Theory]
    [InlineData("amount")]
    [InlineData("account")]
    [InlineData("filter")]
    [InlineData("rights")]
    [InlineData("deleted")]
    public async Task Changed_packet_between_Home_and_destination_does_not_apply_a_newer_same_id_template(string changed)
    {
        var s = await CreateAsync(); using var f = s.F; var id = s.Originals[0].Id;
        await s.Home.OpenAsync(id); var visit = Assert.IsType<TemplateVisit>(Assert.Single(f.Platform.Routes).Parameters!["templateVisit"]);
        await ChangeAsync(s, changed, id); var before = await GoalChooserFlowTests.SnapshotAsync(f);
        var actions = new QuickResourceActions(s.Store, f.Translator, f.Platform); await actions.LoadAsync();
        var draft = "Complete unsaved draft"; await actions.ApplyTemplateVisitAsync(visit, _ => draft = "Wrong replacement");
        Assert.Equal("Complete unsaved draft", draft); Assert.Single(f.Platform.Failures); Assert.False(actions.CanInteract);
        Assert.Equal(f.Translator["Templates_SourceChanged"], actions.Feedback);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }
    [Fact]
    public async Task Another_actual_file_with_exact_same_original_ids_cannot_consume_the_source_handoff()
    {
        var s = await CreateAsync(); using var source = s.F;
        await s.Home.OpenAsync(s.Originals[0].Id); var visit = Assert.IsType<TemplateVisit>(Assert.Single(source.Platform.Routes).Parameters!["templateVisit"]);
        using var other = new FlowFixture();
        foreach (var a in await s.Store.GetAccountsAsync(cancellationToken: Ct)) await other.Store.SaveAccountAsync(a, Ct);
        foreach (var t in s.Originals) await other.Store.SaveTemplateAsync(t, Ct);
        var before = await GoalChooserFlowTests.SnapshotAsync(other);
        var actions = new QuickResourceActions(other.Store, other.Translator, other.Platform); await actions.LoadAsync();
        var applied = false; await actions.ApplyTemplateVisitAsync(visit, _ => applied = true);
        Assert.False(applied); Assert.Single(other.Platform.Failures); Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(other));
    }
    [Fact]
    public async Task Pending_navigation_blocks_duplicate_open_and_a_failure_requires_only_an_explicit_fresh_read()
    {
        var s = await CreateAsync(); using var f = s.F; var before = await GoalChooserFlowTests.SnapshotAsync(f);
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        f.Platform.PendingNavigation = () => { entered.TrySetResult(); return gate.Task; };
        var pending = s.Home.OpenAsync(s.Originals[0].Id);
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(10), Ct); Assert.True(s.Home.IsBusy); Assert.False(s.Home.CanInteract);
        await s.Home.OpenAsync(s.Originals[1].Id); Assert.Single(f.Platform.Routes);
        gate.SetException(new InvalidOperationException("Fictitious route failure")); await pending;
        Assert.False(s.Home.CanInteract); f.Platform.PendingNavigation = null;
        await s.Home.ReloadCommand.ExecuteAsync(null); Assert.True(s.Home.CanInteract); Assert.Null(s.Home.Feedback);
        Assert.Single(f.Platform.Routes); Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }
    [Fact]
    public async Task Covered_parent_reload_keeps_old_shortcuts_unavailable_until_complete_publication()
    {
        var s = await CreateAsync(); using var f = s.F; var before = await GoalChooserFlowTests.SnapshotAsync(f);
        s.Home.BeginLoad(); Assert.False(s.Home.CanInteract); Assert.All(s.Home.Items, t => Assert.False(s.Home.CanUse(t.Id)));
        await s.Home.OpenAsync(s.Originals[0].Id); Assert.Empty(f.Platform.Routes);
        s.Home.Publish(await s.Store.GetQuickResourceReviewAsync(cancellationToken: Ct)); Assert.True(s.Home.CanUse(s.Originals[0].Id));
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }
    [Fact]
    public async Task Native_choice_destination_selects_only_the_initial_kind_and_never_saves_a_guessed_subset()
    {
        var s = await CreateAsync(true); using var f = s.F; var before = await GoalChooserFlowTests.SnapshotAsync(f);
        await s.Home.ChooseCommand.ExecuteAsync(null); var route = Assert.Single(f.Platform.Routes);
        Assert.Equal(AppRoutes.ActiveItemsRoute, route.Route); Assert.Equal(QuotaKind.QuickTemplates, route.Parameters!["kind"]);
        var vm = new ActiveItemsViewModel(s.Choices, f.Translator, f.Platform); vm.SetInitialKind((QuotaKind)route.Parameters["kind"]);
        Assert.Equal(4, vm.KindIndex); await vm.LoadAsync(); Assert.Equal(8, vm.Items.Count); Assert.True(vm.CanChoose);
        Assert.All(vm.Items, row => Assert.False(row.IsSelected)); Assert.Throws<InvalidOperationException>(() => vm.SetInitialKind(QuotaKind.SavedFilters));
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }
    [Fact]
    public async Task Unsupported_initial_kind_and_unknown_original_identity_never_guess_a_destination()
    {
        var s = await CreateAsync(); using var f = s.F;
        var vm = new ActiveItemsViewModel(s.Choices, f.Translator, f.Platform);
        Assert.Throws<ArgumentOutOfRangeException>(() => vm.SetInitialKind((QuotaKind)999)); Assert.Equal(0, vm.KindIndex);
        await s.Home.OpenAsync(Guid.NewGuid()); Assert.Empty(f.Platform.Routes);
    }
}
