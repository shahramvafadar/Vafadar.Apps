using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vafadar.Localization;
using Vafadar.Maui.Mvvm;
using Vafadar.Zanance.App.Interaction;
using Vafadar.Zanance.Core.Commerce;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Features.Home;

/// <summary>Actual original Home template navigation; publication and destination handoff never post money.</summary>
public sealed partial class TemplateNavigationViewModel(ZananceStore store, Translator translator,
    IAppInteraction interaction, Func<Task> reload) : ViewModelBase
{
    private QuickResourceReview? _review;
    private bool _loading = true, _needsReload;
    /// <summary>Gets original shortcuts, with explicitly available templates first within the existing six-item surface.</summary>
    public IReadOnlyList<EntryTemplate> Items { get; private set; } = [];
    /// <summary>Gets whether original activity can be invoked outside publication/navigation.</summary>
    public bool CanInteract => !_loading && !_needsReload && _review is not null && IsNotBusy;
    /// <summary>Gets whether retained unavailable originals need an explicit selection, without hiding history.</summary>
    public bool CanChoose => CanInteract && _review!.TemplateChoice.CanChoose
        && _review.Templates.Any(template => !_review.Work.CanApplyTemplate(template.Id));
    /// <summary>Gets whether read-only Reload can recover a rejected or failed navigation.</summary>
    public bool CanReload => !_loading && IsNotBusy;
    /// <summary>Gets explanatory work feedback without granting a selected slot or account permission.</summary>
    public string? WorkText => _review is { } review && review.Templates.Any(template => !review.Work.CanApplyTemplate(template.Id))
        ? translator["Templates_WorkUnavailable"] : null;
    /// <summary>Gets pinned failure feedback; Retry reads rather than repeating a previous navigation.</summary>
    [ObservableProperty]
    public partial string? Feedback { get; set; }

    /// <summary>Retires invocation before the complete parent snapshot is read.</summary>
    public void BeginLoad() { _loading = true; RefreshAvailability(); }
    /// <summary>Keeps rejected publication unavailable until explicit fresh read.</summary>
    public void FailLoad() { _loading = false; _needsReload = true; Feedback = translator["Templates_SourceChanged"]; RefreshAvailability(); }
    /// <summary>Publishes only after complete parent/source validation, retaining every original in its source review.</summary>
    public void Publish(QuickResourceReview review)
    {
        ArgumentNullException.ThrowIfNull(review);
        _review = review;
        // A chosen later definition remains reachable; unrestricted ordering and the original list stay unchanged.
        Items = review.Templates.OrderBy(template => review.Work.CanApplyTemplate(template.Id) ? 0 : 1)
            .ThenBy(template => template.SortOrder).ThenBy(template => StoredNameOrderKey(template.Name), StringComparer.Ordinal).ThenBy(template => template.Id).Take(6).ToArray();
        _loading = false; _needsReload = false; Feedback = null;
        OnPropertyChanged(nameof(Items)); RefreshAvailability();
    }
    /// <summary>Retains SQLite BINARY UTF-8 name order across display cultures, including supplementary characters.</summary>
    internal static string StoredNameOrderKey(string name) => Convert.ToHexString(System.Text.Encoding.UTF8.GetBytes(name));

    /// <summary>Checks the displayed original identity, separate from later account and ledger writer rights.</summary>
    public bool CanUse(Guid id) => CanInteract && Items.Any(template => template.Id == id) && _review!.Work.CanApplyTemplate(id);
    /// <summary>Refreshes translated work/failure captions without reconstructing a navigation or saving preferences.</summary>
    public void RefreshDisplay()
    {
        if (_needsReload) Feedback = translator["Templates_SourceChanged"];
        RefreshAvailability();
    }
    /// <inheritdoc />
    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    { base.OnPropertyChanged(e); if (e.PropertyName == nameof(IsBusy)) RefreshAvailability(); }
    private void RefreshAvailability()
    {
        foreach (var property in new[] { nameof(CanInteract), nameof(CanChoose), nameof(CanReload), nameof(WorkText) }) OnPropertyChanged(property);
        ChooseCommand.NotifyCanExecuteChanged(); ReloadCommand.NotifyCanExecuteChanged();
    }
    private async Task ValidateAsync(QuickResourceReview review)
    {
        await store.ValidateQuickResourceReviewAsync(review);
        DemandPublication(review);
    }
    private void DemandPublication(QuickResourceReview review)
    {
        if (!ReferenceEquals(review, _review) || _loading || _needsReload) throw new InvalidOperationException("The Home template publication was retired.");
    }
    /// <summary>Opens the existing new-entry route with an in-memory reviewed original handoff, never a guessed same-id row.</summary>
    public async Task OpenAsync(Guid id)
    {
        if (!CanUse(id) || _review is not { } review) return;
        await RunAsync(async () =>
        {
            DemandPublication(review);
            // Creation performs the complete store recheck once; synchronous publication guards bracket that read.
            var visit = await store.CreateTemplateVisitAsync(review, id);
            DemandPublication(review);
            await interaction.NavigateAsync(AppRoutes.EntryEditorRoute, new Dictionary<string, object> { ["template"] = id, ["templateVisit"] = visit });
        });
    }
    [RelayCommand(CanExecute = nameof(CanChoose))]
    private async Task ChooseAsync()
    {
        if (!CanChoose || _review is not { } review) return;
        await RunAsync(async () =>
        {
            await ValidateAsync(review);
            await interaction.NavigateAsync(AppRoutes.ActiveItemsRoute, new Dictionary<string, object> { ["kind"] = QuotaKind.QuickTemplates });
        });
    }
    private async Task RunAsync(Func<Task> operation)
    {
        if (!CanInteract) return;
        IsBusy = true;
        try { await operation(); }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        { FailLoad(); await interaction.ShowFailureAsync(exception); }
        finally { IsBusy = false; }
    }
    [RelayCommand(CanExecute = nameof(CanReload))]
    private async Task ReloadAsync()
    {
        if (!CanReload) return;
        IsBusy = true;
        try { await reload(); }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        { FailLoad(); await interaction.ShowFailureAsync(exception); }
        finally { IsBusy = false; }
    }
}
