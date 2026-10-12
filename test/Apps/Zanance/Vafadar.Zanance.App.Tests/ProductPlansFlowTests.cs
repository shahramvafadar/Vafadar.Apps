using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Zanance.App.Features.Commerce;
using Vafadar.Zanance.Core.Commerce;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Tests;

/// <summary>AT-164: actual comparison/read-only access flows retain all original data and never infer purchase facts.</summary>
[Trait("AT", "AT-164")]
public sealed class ProductPlansFlowTests
{
    private static readonly Guid ScopeId = Guid.Parse("12121212-5656-6767-8989-010101010101");
    private sealed class Access : ICommercialWriteAccessSource
    {
        internal CommercialWriteAccess Current = CommercialWriteAccess.Inactive;
        internal Func<int, CommercialWriteAccess>? OnCapture;
        internal int Calls;
        public CommercialWriteAccess Capture(string databasePath) => OnCapture?.Invoke(++Calls) ?? Current;
    }
    private static CommercialWriteAccess Paid(FlowFixture f, ProductPlan plan, CapabilityContext? context = null) =>
        new(f.Directory.Combine("zanance.db"), context ?? new(plan, new(EntitlementScopeKind.PersonalProfile, ScopeId)));
    private static ProductPlansViewModel ViewModel(FlowFixture f, Access access) => new(new CommercialAccessStore(
        f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>(), access), f.Translator, f.Platform);

    [Theory]
    [InlineData("en")][InlineData("fa")][InlineData("de")][InlineData("es")][InlineData("fr")][InlineData("it")]
    public async Task All_comparison_choices_and_live_translations_preserve_actual_plus_access_and_complete_rows(string language)
    {
        using var f = new FlowFixture(); f.Localization.SetLanguage(f.Localization.SupportedLanguages.Single(l => l.CultureName == language));
        var before = await GoalChooserFlowTests.SnapshotAsync(f); var access = new Access { Current = Paid(f, ProductPlan.Plus) };
        var vm = ViewModel(f, access); Assert.True(vm.Loading.IsLoading); Assert.False(vm.ContinueCommand.CanExecute(null));
        await vm.LoadAsync(); Assert.True(vm.Loading.IsReady);
        var actual = f.Translator.Format("ProductPlans_PersonalAccess", "Plus"); Assert.Equal(actual, vm.AccessText);
        var limits = new[] { 3, 1, 1, 5, 3, 1, 1 };
        for (var plan = 0; plan < 3; plan++)
        {
            vm.PlanIndex = plan; Assert.Equal(19, vm.Rows.Count); Assert.Equal(actual, vm.AccessText);
            for (var row = 0; row < 7; row++)
                if (plan == 0) Assert.Contains(limits[row].ToString(), vm.Rows[row].Value);
                else Assert.Equal(f.Translator["ProductPlans_Unlimited"], vm.Rows[row].Value);
            Assert.All(vm.Rows.Skip(7), row => Assert.Equal(f.Translator[plan == 0 ? "ProductPlans_PlusRequired" : "ProductPlans_Included"], row.Value));
            Assert.Equal(plan != 0, vm.ShowLifetime); Assert.Equal(plan == 2, vm.ShowOnline);
        }
        vm.PlanIndex = 9; Assert.Equal(2, vm.PlanIndex);
        f.Localization.SetLanguage(f.Localization.SupportedLanguages.Single(l => l.CultureName == (language == "de" ? "fa" : "de")));
        vm.RefreshDisplay(); Assert.Equal(2, vm.PlanIndex); Assert.Equal(f.Translator.Format("ProductPlans_PersonalAccess", "Plus"), vm.AccessText);
        await vm.LoadAsync(); Assert.Equal(2, vm.PlanIndex);
        await vm.ActiveItemsCommand.ExecuteAsync(null); await vm.ContinueCommand.ExecuteAsync(null);
        Assert.Equal(new[] { AppRoutes.ActiveItemsRoute, ".." }, f.Platform.Routes.Select(r => r.Route));
        Assert.Empty(f.Platform.ConfirmationDialogs); Assert.Empty(f.Platform.Alerts); Assert.Empty(f.Platform.Failures);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f)); Assert.Equal(ProductPlan.Plus, access.Current.Context!.PersonalPlan);
    }

    [Theory]
    [InlineData("en")][InlineData("fa")][InlineData("de")][InlineData("es")][InlineData("fr")][InlineData("it")]
    public async Task Inactive_access_never_claims_free_plus_a_purchase_or_renewal_and_return_saves_nothing(string language)
    {
        using var f = new FlowFixture(); f.Localization.SetLanguage(f.Localization.SupportedLanguages.Single(l => l.CultureName == language));
        var before = await GoalChooserFlowTests.SnapshotAsync(f); var vm = ViewModel(f, new Access()); await vm.LoadAsync();
        Assert.Equal(f.Translator["ProductPlans_TestAccess"], vm.AccessText); Assert.Equal(f.Translator["ProductPlans_Continue"], vm.ContinueText);
        vm.PlanIndex = 1; Assert.Equal(f.Translator["ProductPlans_TestAccess"], vm.AccessText);
        await vm.ContinueCommand.ExecuteAsync(null); Assert.Equal("..", Assert.Single(f.Platform.Routes).Route);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }

    [Theory]
    [InlineData(false, false, "ProductPlans_NoMembership")]
    [InlineData(true, false, "ProductPlans_SharedPaused")]
    [InlineData(true, true, "ProductPlans_SharedPlus")]
    public async Task Personal_pro_never_leaks_into_exact_shared_membership_or_host_state(bool membership, bool host, string key)
    {
        using var f = new FlowFixture(); var context = new CapabilityContext(ProductPlan.Pro,
            new(EntitlementScopeKind.SharedSpace, ScopeId), new(ScopeId, membership, host));
        var vm = ViewModel(f, new Access { Current = Paid(f, ProductPlan.Pro, context) }); await vm.LoadAsync();
        Assert.Equal(f.Translator[key], vm.AccessText); Assert.Null(vm.ProfileText); Assert.Equal(f.Translator["ProductPlans_Continue"], vm.ContinueText);
        vm.PlanIndex = 2; Assert.Equal(f.Translator[key], vm.AccessText);
    }

    [Fact]
    public async Task Changed_actual_file_facts_cover_old_access_and_retry_retains_comparison_without_writing()
    {
        using var f = new FlowFixture(); var before = await GoalChooserFlowTests.SnapshotAsync(f);
        var free = Paid(f, ProductPlan.Free); var plus = Paid(f, ProductPlan.Plus); var access = new Access { Current = free };
        var vm = ViewModel(f, access); await vm.LoadAsync(); Assert.Equal(f.Translator["ProductPlans_ContinueFree"], vm.ContinueText);
        vm.PlanIndex = 2;
        access.Calls = 0; access.OnCapture = n => n == 1 ? free : plus;
        await vm.LoadAsync(); Assert.True(vm.Loading.HasFailed); Assert.False(vm.CanInteract); Assert.False(vm.ContinueCommand.CanExecute(null));
        vm.PlanIndex = 1; Assert.Equal(2, vm.PlanIndex); Assert.Empty(f.Platform.Routes);
        access.OnCapture = null; access.Current = plus; await vm.ReloadCommand.ExecuteAsync(null);
        Assert.True(vm.CanInteract); Assert.Equal(2, vm.PlanIndex); Assert.Equal(f.Translator.Format("ProductPlans_PersonalAccess", "Plus"), vm.AccessText);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }

    [Fact]
    public async Task Access_read_does_not_open_or_create_an_uninitialized_database_and_rejects_a_foreign_file()
    {
        using var f = new FlowFixture(); var path = f.Directory.Combine("independent-unopened.db"); var access = new Access();
        using var services = new ServiceCollection().AddSingleton<ICommercialWriteAccessSource>(access).AddZananceData(path).BuildServiceProvider();
        var store = services.GetRequiredService<CommercialAccessStore>(); var snapshot = await store.ReadAsync(TestContext.Current.CancellationToken);
        Assert.Null(snapshot.Context); Assert.False(File.Exists(path));
        access.Current = Paid(f, ProductPlan.Pro);
        await Assert.ThrowsAsync<InvalidOperationException>(() => store.ReadAsync(TestContext.Current.CancellationToken)); Assert.False(File.Exists(path));
        access.Current = new(path, new(ProductPlan.Free, new(EntitlementScopeKind.PersonalProfile, ScopeId)));
        Assert.Throws<InvalidOperationException>(snapshot.EnsureCurrent);
        using var cancelled = new CancellationTokenSource(); cancelled.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => store.ReadAsync(cancelled.Token)); Assert.False(File.Exists(path));
    }
}
