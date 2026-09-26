using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentIcons.Common;
using Vafadar.Finance.App.Presentation;
using Vafadar.Finance.Core.Ledger;
using Vafadar.Finance.Core.Money;
using Vafadar.Finance.Data;
using Vafadar.Localization;
using Vafadar.Maui.Mvvm;

namespace Vafadar.Finance.App.Features.Templates;

/// <summary>A quick template in the list.</summary>
public sealed record TemplateRow(Guid Id, string Name, string Details, Symbol Icon);

/// <summary>
/// Quick templates (TX-04): created from an entry ("Save as template"), used from the entry form, deleted here.
/// </summary>
public sealed partial class TemplatesViewModel(FinanceStore store, Translator translator, ILocalizationService localization) : ViewModelBase
{
    public ObservableCollection<TemplateRow> Templates { get; } = [];

    [ObservableProperty]
    public partial bool HasTemplates { get; set; }

    public async Task LoadAsync()
    {
        var accounts = (await store.GetAccountsAsync()).ToDictionary(a => a.Id);
        var categories = new CategoryLookup(await store.GetCategoriesAsync(), translator);
        var culture = localization.CurrentCulture;
        Templates.Clear();
        foreach (var template in await store.GetTemplatesAsync())
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

            var icon = Icons.Parse(template.Icon, categories.Get(template.CategoryId) is { } category ? Icons.Parse(category.Icon, Symbol.Flash) : Symbol.Flash);
            Templates.Add(new TemplateRow(template.Id, template.Name, string.Join(" · ", parts), icon));
        }

        HasTemplates = Templates.Count > 0;
    }

    [RelayCommand]
    private async Task DeleteAsync(TemplateRow row)
    {
        if (await Shell.Current.DisplayAlertAsync(translator["Templates_Delete"], row.Name, translator["Common_Delete"], translator["Common_Cancel"]))
        {
            await store.DeleteTemplateAsync(row.Id);
            await LoadAsync();
        }
    }
}