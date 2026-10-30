using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vafadar.Localization;
using Vafadar.Localization.Formatting;
using Vafadar.Maui.Mvvm;
using Vafadar.Zanance.App.Interaction;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Core.Commerce;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Features.Commerce;

/// <summary>One complete translated comparison row; it carries no stored identity or write right.</summary>
public sealed record ProductPlanRow(string Title, string Value);

/// <summary>Read-only approved plan comparison and original file access, separate from future store purchases.</summary>
public sealed partial class ProductPlansViewModel : ViewModelBase
{
    private readonly CommercialAccessStore _store;
    private readonly Translator _translator;
    private readonly IAppInteraction _interaction;
    private readonly IDateFormatter? _billingDates;
    private CommercialAccessSnapshot? _snapshot;
    private CancellationTokenSource? _sourcePage;
    private Task? _sourceRead;
    private bool _sourceAccessPending;
    private bool _visible;
    private bool _rootRetired;
    private Func<bool>? _ownsRoot;
    private long _frameEpoch;
    private long _disclosure;
    private bool _navigationActive;
    private bool OwnsPresentation => _visible && !_rootRetired && (_ownsRoot?.Invoke() ?? true);
    /// <summary>Binds cached access and native confirmations to their original actual root/page.</summary>
    internal void BindRootOwner(Func<bool> ownsOriginal) => _ownsRoot = ownsOriginal;
    /// <summary>A replaced root cannot reactivate its original store or comparison continuations.</summary>
    internal void RetireRoot() { _rootRetired = true; DeactivateBilling(); }
    /// <summary>Read-only Back may cancel a pending read; submitted native work retains its owner.</summary>
    public bool CanLeave => !_billingReview && (!_billingOperation || _billingRead);
    /// <summary>Explicit source Retry cannot overlap another source or native command.</summary>
    public bool CanRead => OwnsPresentation && _sourceRead is null && !_billingOperation && !_billingReview && !_navigationActive;
    /// <summary>Original complete source is shown only by its current page.</summary>
    public bool IsReady => OwnsPresentation && Loading.IsReady;
    // Comparison scope is fictitious and fixed: it never enters a provider, profile, writer or portable data.
    private static readonly EntitlementScope ComparisonScope = new(EntitlementScopeKind.PersonalProfile,
        Guid.Parse("98989898-4545-5656-6767-898989898989"));

    /// <summary>Creates a covered form without starting reads or changing access.</summary>
    public ProductPlansViewModel(CommercialAccessStore store, Translator translator, IAppInteraction interaction,
        Billing.IProductBillingEnvironment? billing = null, IDateFormatter? dates = null)
    {
        _store = store; _translator = translator; _interaction = interaction; _billing = billing; _billingDates = dates;
        Loading.PropertyChanged += (_, _) => NotifyReadiness();
    }
    /// <summary>Gets covered publication and translated retry state.</summary>
    public SnapshotLoadState Loading { get; } = new();
    /// <summary>Gets whether native choices/navigation may be invoked.</summary>
    public bool CanInteract => IsReady && !IsBusy && !_navigationActive;
    /// <summary>Gets the three approved tier names, independent of experience mode.</summary>
    public string[] PlanNames => ["Free", "Plus", "Pro"];
    /// <summary>Gets the comparison choice only; it never changes current access.</summary>
    [ObservableProperty]
    public partial int PlanIndex { get; set; }
    /// <summary>Gets the current comparison's nonfinancial quota and feature rows.</summary>
    [ObservableProperty]
    public partial IReadOnlyList<ProductPlanRow> Rows { get; private set; } = [];
    /// <summary>Gets the actually captured access description, not an inferred purchase.</summary>
    public string AccessText => _snapshot?.Context is not { } context ? _translator["ProductPlans_TestAccess"]
        : context.Scope.Kind == EntitlementScopeKind.SharedSpace
            ? _translator[!context.HasSharedMembership ? "ProductPlans_NoMembership"
                : !context.HasSharedAccess ? "ProductPlans_SharedPaused" : "ProductPlans_SharedPlus"]
            : _translator.Format("ProductPlans_PersonalAccess", context.PersonalPlan.ToString());
    /// <summary>Gets the separate original personal-profile choice warning.</summary>
    public string? ProfileText => _snapshot?.Context?.ProfileWork is { CanStartNewWork: false }
        ? _translator["Commercial_ProfileSelection"] : null;
    /// <summary>Gets the selected comparison description without advertising an unavailable sale.</summary>
    public string Description => _translator[$"ProductPlans_{(ProductPlan)PlanIndex}Description"];
    /// <summary>The comparison note must not contradict an actual separately available store surface.</summary>
    public string AvailabilityText => _translator[ShowBilling ? "Billing_ComparisonNote" : "ProductPlans_Availability"];
    /// <summary>Gets the Lifetime explanation only with the local Plus capability set.</summary>
    public bool ShowLifetime => PlanIndex != (int)ProductPlan.Free;
    /// <summary>Gets the unavailable-online-services roadmap note only for the Pro comparison.</summary>
    public bool ShowOnline => PlanIndex == (int)ProductPlan.Pro;
    /// <summary>Gets an accurate return caption; returning never cancels or downgrades a purchase.</summary>
    public string ContinueText => _snapshot?.Context is { Scope.Kind: EntitlementScopeKind.PersonalProfile, PersonalPlan: ProductPlan.Free }
        ? _translator["ProductPlans_ContinueFree"] : _translator["ProductPlans_Continue"];

    /// <summary>Coalesces actual appearance and never retries a visible original failure implicitly.</summary>
    public async Task LoadAsync()
    {
        if (_rootRetired || _ownsRoot?.Invoke() == false) return;
        if (_visible) { if (_sourceRead is { } pending) await pending; return; }
        _visible = true; var epoch = ++_frameEpoch;
        _sourcePage = new(); var token = _sourcePage.Token;
        if (_sourceRead is { } retired) await retired;
        if (!OwnsPresentation || epoch != _frameEpoch || token.IsCancellationRequested) return;
        await ReadOriginalAsync();
    }
    private Task ReadOriginalAsync()
    {
        if (!OwnsPresentation) return Task.CompletedTask;
        if (_sourceRead is { } pending) return pending;
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _sourceRead = completion.Task; _sourceAccessPending = true;
        _ = ExecuteOriginalReadAsync(completion);
        return completion.Task;
    }
    private async Task ExecuteOriginalReadAsync(TaskCompletionSource completion)
    {
        var epoch = _frameEpoch; var token = _sourcePage!.Token;
        bool Current() => OwnsPresentation && epoch == _frameEpoch && !token.IsCancellationRequested;
        CommercialAccessSnapshot? next = null; NotifyReadiness();
        try
        {
            await Loading.RunAsync(async () => next = await _store.ReadAsync(token), () =>
            {
                token.ThrowIfCancellationRequested();
                if (!Current()) throw new OperationCanceledException("The original product comparison retired.");
                next!.EnsureCurrent(); _snapshot = next; RefreshDisplay();
            });
            if (Current()) _sourceAccessPending = false;
            if (Current() && Loading.IsReady) await LoadBillingAsync();
        }
        catch (Exception error) when (error is not OutOfMemoryException)
        { /* Only the original covered source surface owns read failures; no payment or accepted result is inferred. */ }
        finally
        {
            if (ReferenceEquals(_sourceRead, completion.Task)) { _sourceRead = null; _sourceAccessPending = false; }
            completion.TrySetResult(); NotifyReadiness();
        }
    }
    /// <summary>Refreshes translated captions without changing the selected tier or stored data.</summary>
    public void RefreshDisplay()
    {
        _disclosure++;
        RefreshBillingDisplay();
        var context = new CapabilityContext((ProductPlan)PlanIndex, ComparisonScope);
        var rows = new List<ProductPlanRow>();
        foreach (var (kind, key) in QuotaRows)
        {
            var max = QuotaPolicy.Get(kind, context).Maximum;
            rows.Add(new(_translator[key], max is null ? _translator["ProductPlans_Unlimited"]
                : "\u2066\u200E" + NativeDigits.Apply(max.Value.ToString("N0", _translator.Culture)) + "\u200E\u2069"));
        }
        foreach (var (feature, key) in FeatureRows)
            rows.Add(new(_translator[key], _translator[PlanPolicy.Check(feature, context) == FeaturePermission.Allowed
                ? "ProductPlans_Included" : "ProductPlans_PlusRequired"]));
        Rows = rows;
        foreach (var name in new[] { nameof(AccessText), nameof(ProfileText), nameof(Description), nameof(AvailabilityText), nameof(ShowLifetime),
            nameof(ShowOnline), nameof(ContinueText) }) OnPropertyChanged(name);
    }
    private bool _restoringIndex;
    partial void OnPlanIndexChanged(int oldValue, int newValue)
    {
        if (_restoringIndex) return;
        if (!CanInteract || newValue is < 0 or > 2)
        {
            _restoringIndex = true;
            try { PlanIndex = oldValue; } finally { _restoringIndex = false; }
            return;
        }
        RefreshDisplay();
    }
    private void NotifyReadiness()
    {
        foreach (var name in new[] { nameof(CanInteract), nameof(CanRead), nameof(CanLeave), nameof(IsReady) }) OnPropertyChanged(name);
        ContinueCommand.NotifyCanExecuteChanged(); ActiveItemsCommand.NotifyCanExecuteChanged(); ReloadCommand.NotifyCanExecuteChanged();
    }
    [RelayCommand(CanExecute = nameof(CanRead))]
    private Task ReloadAsync() => CanRead ? ReadOriginalAsync() : Task.CompletedTask;
    [RelayCommand(CanExecute = nameof(CanInteract))]
    private Task ContinueAsync() => NavigateOriginalAsync("..");
    [RelayCommand(CanExecute = nameof(CanInteract))]
    private Task ActiveItemsAsync() => NavigateOriginalAsync(AppRoutes.ActiveItemsRoute);
    /// <summary>Opens only the original approved external reference under current source/page ownership.</summary>
    internal Task OpenReferenceAsync(Func<Task> open) => NavigateOriginalCoreAsync(open);
    private Task NavigateOriginalAsync(string route) => NavigateOriginalCoreAsync(() => _interaction.NavigateAsync(route));
    private async Task NavigateOriginalCoreAsync(Func<Task> navigate)
    {
        if (!CanInteract || !CanLeave || !await RequireOriginalSourceAsync()) return;
        _navigationActive = true; NotifyReadiness();
        try { if (OwnsPresentation) await navigate(); }
        finally { _navigationActive = false; NotifyReadiness(); }
    }
    /// <summary>A changed original cached source covers stale claims without an implicit read or native request.</summary>
    private async Task<bool> RequireOriginalSourceAsync()
    {
        if (!IsReady || _snapshot is null) return false;
        try { _snapshot.EnsureCurrent(); return OwnsPresentation; }
        catch (Exception error) when (error is not OutOfMemoryException)
        {
            _billingPresentation = null; BillingOffers = [];
            try { await Loading.RunAsync(() => Task.FromException(error), () => { }); }
            catch (Exception failure) when (failure is not OutOfMemoryException) { }
            NotifyBillingCommands(); return false;
        }
    }

    private static readonly (QuotaKind Kind, string Key)[] QuotaRows =
    [
        (QuotaKind.FinancialAccounts, "Accounts_Title"), (QuotaKind.BudgetDefinitions, "Commercial_BudgetDefinitions"),
        (QuotaKind.Goals, "Goals_Title"), (QuotaKind.RecurringPlans, "Tab_Plans"),
        (QuotaKind.QuickTemplates, "Templates_Title"), (QuotaKind.SavedFilters, "Filter_Title"), (QuotaKind.LocalProfiles, "Profile_Title"),
    ];
    // List only already built local features. Online services are an explicit roadmap note, never a sale or readiness claim.
    private static readonly (CommercialFeature Feature, string Key)[] FeatureRows =
    [
        (CommercialFeature.AdvancedBudgets, "ProductPlans_AdvancedBudgets"), (CommercialFeature.AdvancedGoals, "ProductPlans_AdvancedGoals"),
        (CommercialFeature.AdvancedPlans, "ProductPlans_AdvancedPlans"), (CommercialFeature.AdvancedReports, "ProductPlans_AdvancedReports"),
        (CommercialFeature.AdvancedPdf, "ProductPlans_AdvancedPdf"), (CommercialFeature.AdvancedForecast, "ProductPlans_AdvancedForecast"),
        (CommercialFeature.ManageHoldings, "ProductPlans_ManageHoldings"), (CommercialFeature.LoanAnalysis, "ProductPlans_LoanAnalysis"),
        (CommercialFeature.SplitTransactions, "Split_Title"), (CommercialFeature.CategorizationRules, "Rules_Title"),
        (CommercialFeature.AccountantExport, "Tax_Title"),
        (CommercialFeature.ReceiptReading, "ProductPlans_ReceiptReading"), (CommercialFeature.ContributionReviewReminders, "ProductPlans_ReviewReminders"),
    ];
}
