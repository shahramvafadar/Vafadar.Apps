using CommunityToolkit.Mvvm.ComponentModel;
using Vafadar.Localization;
using Vafadar.Maui.Mvvm;
using Vafadar.Zanance.App.Interaction;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Features.Commerce;

/// <summary>Actual quick-resource actions over a published original packet; dialogs never reread a moving draft.</summary>
public sealed partial class QuickResourceActions(ZananceStore store, Translator translator, IAppInteraction interaction) : ViewModelBase
{
    private QuickResourceReview? _review;
    private int _generation;
    /// <summary>Gets pinned failure feedback without making a committed operation retryable.</summary>
    [ObservableProperty]
    public partial string? Feedback { get; set; }
    /// <summary>Gets retained template deletion availability, independent of creation slots.</summary>
    public bool CanDeleteTemplates => CanInteract && _review!.Work.CanDelete;
    private void RefreshAvailability()
    { OnPropertyChanged(nameof(Review)); OnPropertyChanged(nameof(CanInteract)); OnPropertyChanged(nameof(CanDeleteTemplates)); }
    protected override void OnPropertyChanged(System.ComponentModel.PropertyChangedEventArgs e)
    { base.OnPropertyChanged(e); if (e.PropertyName == nameof(IsBusy)) RefreshAvailability(); }
    /// <summary>Gets the complete published original resources and separate work rights.</summary>
    public QuickResourceReview? Review => _review;
    /// <summary>Gets whether a complete publication is available outside a pending native effect.</summary>
    public bool CanInteract => _review is not null && IsNotBusy;
    /// <summary>Gets the last unsaved template name, retained after a failed request.</summary>
    public string? TemplateNameDraft { get; private set; }

    /// <summary>Retires the previous review before reading; opening creates no preferences or financial rows.</summary>
    public async Task LoadAsync(Guid? sourceId = null)
    {
        var generation = ++_generation; _review = null; RefreshAvailability();
        var reviewed = await store.GetQuickResourceReviewAsync(sourceId);
        if (generation != _generation) throw new InvalidOperationException("The quick-resource publication was retired.");
        _review = reviewed; Feedback = null; RefreshAvailability();
    }

    private async Task ValidateAsync(QuickResourceReview review)
    {
        await store.ValidateQuickResourceReviewAsync(review);
        if (!ReferenceEquals(review, _review)) throw new InvalidOperationException("The quick-resource publication was retired.");
    }

    /// <summary>Validates selected template activity before filling an unsaved entry; no ledger write is made.</summary>
    public async Task ApplyTemplateAsync(Guid id, Action<EntryTemplate> apply)
    {
        if (!CanInteract || _review is not { } review) return;
        await RunAsync(async () =>
        {
            await ValidateAsync(review);
            var original = review.Templates.Single(t => t.Id == id);
            if (!review.Work.CanApplyTemplate(id)) throw new InvalidOperationException("Choose an available template before new draft activity.");
            apply(original);
            return false;
        });
    }

    /// <summary>Creates only an explicitly named template from the exact displayed source, never another profile's same id.</summary>
    public async Task CreateTemplateAsync(LedgerEntry displayed, string heading, string amountText)
    {
        if (!CanInteract || _review is not { } review) return;
        await RunAsync(async () =>
        {
            await ValidateAsync(review);
            if (!review.MatchesSource(displayed) || !review.Work.CanCreateTemplate)
                throw new InvalidOperationException("The original source or available template slot changed.");
            var name = await interaction.PromptAsync(translator["Templates_SaveTitle"], translator["Templates_NamePrompt"],
                translator["Common_Save"], translator["Common_Cancel"], TemplateNameDraft ?? heading, 60);
            if (string.IsNullOrWhiteSpace(name)) return false;
            TemplateNameDraft = name;
            var keep = displayed.Amount > 0 && await interaction.ConfirmAsync(translator["Templates_SaveTitle"],
                translator.Format("Templates_KeepAmount", amountText), translator["Templates_KeepAmountYes"], translator["Templates_KeepAmountNo"]);
            await ValidateAsync(review);
            var target = EntryTemplate.From(review.SourceEntry!, name, keep);
            await store.SaveTemplateAsync(target, review: review);
            return true;
        }, async () => await interaction.AlertAsync(translator["Templates_SaveTitle"],
            translator.Format("Templates_Saved", TemplateNameDraft!.Trim()), translator["Common_Ok"]));
    }

    /// <summary>Deletes the original template only after explicit confirmation and paired writer revalidation.</summary>
    public async Task DeleteTemplateAsync(Guid id, Func<Task> reload)
    {
        if (!CanInteract || _review is not { } review) return;
        await RunAsync(async () =>
        {
            await ValidateAsync(review);
            var original = review.Templates.Single(t => t.Id == id);
            if (!review.Work.CanDelete) throw new InvalidOperationException("Deletion is unavailable.");
            if (!await interaction.ConfirmAsync(translator["Templates_Delete"], original.Name,
                translator["Common_Delete"], translator["Common_Cancel"])) return false;
            await ValidateAsync(review);
            await store.DeleteTemplateAsync(id, review: review); return true;
        }, reload);
    }

    /// <summary>Captures the entire current query before the menu/name dialogs; retained filter use remains history-only.</summary>
    public async Task FilterMenuAsync(SavedFilter draft, Func<Task> reload)
    {
        if (!CanInteract || _review is not { } review) return;
        // This detached fresh definition is owned by the operation. Later typing cannot alter the submitted query.
        var target = new SavedFilter { Name = string.Empty, Period = draft.Period, From = draft.From, To = draft.To,
            Kind = draft.Kind, AccountId = draft.AccountId, CategoryIds = [.. draft.CategoryIds], CategoryName = draft.CategoryName,
            UnreviewedOnly = draft.UnreviewedOnly, InTotalsOnly = draft.InTotalsOnly, Search = draft.Search };
        await RunAsync(async () =>
        {
            await ValidateAsync(review);
            var save = translator["Filter_SaveCurrent"]; var remove = translator["Filter_Remove"];
            var actions = new List<string>();
            // A full quota still permits an explicitly confirmed replacement of a retained same-name query.
            if (review.Work.CanCreateFilter || review.Work.CanCorrect && review.Filters.Count > 0) actions.Add(save);
            if (review.Work.CanDelete && review.Filters.Count > 0) actions.Add(remove);
            if (actions.Count == 0) return false;
            var choice = await interaction.ChooseAsync(translator["Filter_Title"], translator["Common_Cancel"], [.. actions]);
            if (choice == save)
            {
                var name = (await interaction.PromptAsync(save, translator["Filter_NameMessage"],
                    translator["Common_Save"], translator["Common_Cancel"], maxLength: SavedFilter.MaxNameLength))?.Trim();
                if (string.IsNullOrEmpty(name)) return false;
                target.Name = name;
                if (!review.CanSaveFilter(target)) throw new InvalidOperationException("A fresh filter needs an available chosen slot.");
                if (review.Filters.Any(f => review.SameName(f.Name, name)) && !await interaction.ConfirmAsync(save,
                    translator.Format("Filter_Replace", name), translator["Common_Save"], translator["Common_Cancel"])) return false;
                await ValidateAsync(review);
                await store.SaveSavedFilterAsync(target, review: review); return true;
            }
            if (choice != remove) return false;
            // Ordinals disambiguate duplicate retained user names without using the label as an entity identity.
            var originals = review.Filters.OrderBy(f => f.SortOrder).ThenBy(f => f.Id).ToArray();
            var names = originals.Select((f, i) => $"{Vafadar.Localization.Formatting.NativeDigits.Apply((i + 1).ToString(translator.Culture))}. {f.Name}").ToArray();
            var chosen = await interaction.ChooseAsync(remove, translator["Common_Cancel"], names);
            var index = Array.IndexOf(names, chosen); if (index < 0) return false;
            var original = originals[index];
            if (!await interaction.ConfirmAsync(remove, original.Name, translator["Common_Delete"], translator["Common_Cancel"])) return false;
            await ValidateAsync(review);
            await store.DeleteSavedFilterAsync(original.Id, review: review); return true;
        }, reload);
    }

    private async Task RunAsync(Func<Task<bool>> operation, Func<Task>? afterCommit = null)
    {
        if (!CanInteract) return;
        IsBusy = true; Feedback = null; var committed = false;
        try
        {
            committed = await operation();
            if (committed)
            {
                _review = null; ++_generation; // Retire before any native feedback/reload can fail: never resubmit a committed write.
                if (afterCommit is not null) await afterCommit();
            }
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            _review = null; ++_generation;
            Feedback = translator[committed ? "Occurrence_SavedRefreshFailed" : "Error_Unexpected"];
            if (committed) await interaction.AlertAsync(translator["Error_Title"], translator["Occurrence_SavedRefreshFailed"], translator["Common_Ok"]);
            else await interaction.ShowFailureAsync(exception);
        }
        finally { IsBusy = false; }
    }
}
