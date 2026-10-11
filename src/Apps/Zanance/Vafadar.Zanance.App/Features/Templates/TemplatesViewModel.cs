using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentIcons.Common;
using Vafadar.Localization;
using Vafadar.Maui.Mvvm;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Money;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Features.Templates;

/// <summary>A quick template in the list.</summary>
public sealed record TemplateRow(Guid Id, string Name, string Details, Symbol Icon, Color IconColor, Color IconBackground, Color IconStroke);

/// <summary>
/// Quick templates (TX-04): created from an entry ("Save as template"), used from the entry form, deleted here.
/// </summary>
public sealed partial class TemplatesViewModel(ZananceStore store, Translator translator, ILocalizationService localization, Interaction.IAppInteraction interaction) : ViewModelBase, Presentation.IThemeAware
{
    /// <summary>Gets the actual reviewed action flow, independent of native color presentation.</summary>
    public Commerce.QuickResourceActions Actions { get; } = new(store, translator, interaction);

    public ObservableCollection<TemplateRow> Templates { get; } = [];

    [ObservableProperty]
    public partial bool HasTemplates { get; set; }

    // The page's colors are computed while loading; loading again keeps the filters and choices of the page.
    Task Presentation.IThemeAware.RefreshThemeAsync() => LoadAsync();

    /// <summary>Gets the shared covered publication; concurrent appearance/theme loads use the same read.</summary>
    public SnapshotLoadState Loading { get; } = new();
    private List<TemplateRow> _prepared = [];
    public Task LoadAsync() => Loading.RunAsync(ReadSnapshotAsync, PresentSnapshot);
    private async Task ReadSnapshotAsync()
    {
        await Actions.LoadAsync();
        var review = Actions.Review!;
        var accounts = review.Accounts.ToDictionary(a => a.Id);
        var categories = new CategoryLookup(review.Categories, translator);
        var culture = localization.CurrentCulture;
        var prepared = new List<TemplateRow>();
        foreach (var template in review.Templates.OrderBy(t => t.SortOrder).ThenBy(t => t.Name))
        {
            var account = accounts.GetValueOrDefault(template.AccountId);
            var parts = new List<string> { translator[$"EntryKind_{template.Kind}"] };
            if (template.Kind != EntryKind.Transfer)
            {
                parts.Add(categories.Name(template.CategoryId));
            }

            if (account is not null)
            {
                parts.Add(account.Name);
            }

            if (template.Amount is { } amount && account is not null)
            {
                parts.Add(MoneyText.Format(amount, account.CurrencyCode, culture));
            }

            // The same icon and colour as on Home and in the entry lists (category colour; slate for a transfer).
            var transfer = template.Kind == EntryKind.Transfer;
            var icon = transfer ? Symbol.ArrowSwap : Icons.Parse(template.Icon, categories.Get(template.CategoryId) is { } category ? Icons.Parse(category.Icon, Symbol.Flash) : Symbol.Flash);
            var color = transfer ? Presentation.Palette.TransferText : categories.Color(template.CategoryId);
            prepared.Add(new TemplateRow(template.Id, template.Name, string.Join(" · ", parts), icon, color, color.WithAlpha(0.12f), color.WithAlpha(0.3f)));
        }

        await store.ValidateQuickResourceReviewAsync(review);
        _prepared = prepared;
    }
    private void PresentSnapshot()
    {
        Templates.Clear(); foreach (var row in _prepared) Templates.Add(row);
        HasTemplates = Templates.Count > 0;
    }

    [RelayCommand]
    private Task ReloadAsync() => LoadAsync();

    [RelayCommand]
    private Task DeleteAsync(TemplateRow row) => Actions.DeleteTemplateAsync(row.Id, LoadAsync);
}
