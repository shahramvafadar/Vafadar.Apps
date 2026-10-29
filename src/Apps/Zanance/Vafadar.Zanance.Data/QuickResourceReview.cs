using System.Globalization;
using System.Text.Json;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Categories;
using Vafadar.Zanance.Core.Commerce;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Settings;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.Data;

/// <summary>Complete original quick resources and references paired with explicit choice/configuration writers.</summary>
public sealed class QuickResourceReview
{
    internal QuickResourceReview(ResourceChoiceSnapshot templateChoice, ResourceChoiceSnapshot filterChoice,
        IReadOnlyList<EntryTemplate> templates, IReadOnlyList<SavedFilter> filters, IReadOnlyList<Account> accounts,
        IReadOnlyList<Category> categories, ZananceSettings? settings, LedgerEntry? source)
    {
        TemplateChoice = templateChoice; FilterChoice = filterChoice; Templates = templates; Filters = filters;
        Accounts = accounts; Categories = categories; Settings = settings; SourceEntry = source;
        NameCulture = CultureInfo.ReadOnly((CultureInfo)CultureInfo.CurrentCulture.Clone());
        InputStamp = Capture(templates, filters, accounts, categories, settings, source);
        if (templateChoice.DatabasePath != filterChoice.DatabasePath || !Equals(templateChoice.Access.Context, filterChoice.Access.Context))
            throw new InvalidOperationException("Quick-resource choices do not belong to the same original file and rights.");
        var context = templateChoice.Access.Context;
        Work = context is null ? QuickResourceWorkPolicy.Inactive : QuickResourceWorkPolicy.Resolve(context,
            StoredResourceSelection.Scope(context), templates.Select(t => t.Id), filters.Select(f => f.Id),
            new(QuotaKind.QuickTemplates, StoredResourceSelection.Scope(context), templateChoice.SelectedIds),
            new(QuotaKind.SavedFilters, StoredResourceSelection.Scope(context), filterChoice.SelectedIds),
            templateChoice.RequiresSelection, filterChoice.RequiresSelection);
    }
    /// <summary>Gets every original template, including those unavailable for new draft activity.</summary>
    public IReadOnlyList<EntryTemplate> Templates { get; }
    /// <summary>Gets every original saved query; history use never grants a transaction or creation slot.</summary>
    public IReadOnlyList<SavedFilter> Filters { get; }
    /// <summary>Gets complete original accounts, including archived/missing-reference context.</summary>
    public IReadOnlyList<Account> Accounts { get; }
    /// <summary>Gets complete original categories without filtering hidden or archived references.</summary>
    public IReadOnlyList<Category> Categories { get; }
    /// <summary>Gets nullable original preferences, never a row created by a read.</summary>
    public ZananceSettings? Settings { get; }
    /// <summary>Gets the optional original entry from which a template may explicitly be created.</summary>
    public LedgerEntry? SourceEntry { get; }
    /// <summary>Gets the reviewed template selection and original packets.</summary>
    public ResourceChoiceSnapshot TemplateChoice { get; }
    /// <summary>Gets the reviewed filter selection and original packets.</summary>
    public ResourceChoiceSnapshot FilterChoice { get; }
    /// <summary>Gets independent activity, history, correction, delete and creation rights.</summary>
    public QuickResourceWorkPolicy Work { get; }
    /// <summary>Checks cached original-file access around native publication without exposing its internal access objects.</summary>
    public void EnsureCurrent()
    {
        TemplateChoice.Access.EnsureSnapshotCurrent(); FilterChoice.Access.EnsureSnapshotCurrent();
    }
    /// <summary>Checks original file identity for retaining process-local names; it grants no capability or financial right.</summary>
    public bool BelongsToSameFile(QuickResourceReview other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return string.Equals(TemplateChoice.DatabasePath, other.TemplateChoice.DatabasePath, StringComparison.Ordinal);
    }
    /// <summary>Gets original name-comparison rules; a live formatting/language change never changes a reviewed replacement.</summary>
    public CultureInfo NameCulture { get; }
    /// <summary>Checks retained same-id/template correction without requiring a fresh selected slot.</summary>
    public bool CanSaveTemplate(EntryTemplate target) => Templates.Any(t => t.Id == target.Id) ? Work.CanCorrect : Work.CanCreateTemplate;
    /// <summary>Checks explicit same-name replacement or a fresh filter definition against separate retained/new rights.</summary>
    public bool CanSaveFilter(SavedFilter target) => Filters.Any(f => f.Id == target.Id || SameName(f.Name, target.Name.Trim()))
        ? Work.CanCorrect : Work.CanCreateFilter;
    /// <summary>Compares only user-confirmed names in the original review's culture.</summary>
    public bool SameName(string first, string second) => NameCulture.CompareInfo.Compare(first, second, CompareOptions.IgnoreCase) == 0;
    /// <summary>Rejects a stale source shown by an earlier detail view before requesting a name/amount decision.</summary>
    public bool MatchesSource(LedgerEntry source) => SourceEntry is not null
        && JsonSerializer.Serialize(SourceEntry) == JsonSerializer.Serialize(source);
    internal string InputStamp { get; }
    internal static string Capture(IReadOnlyList<EntryTemplate> templates, IReadOnlyList<SavedFilter> filters,
        IReadOnlyList<Account> accounts, IReadOnlyList<Category> categories, ZananceSettings? settings, LedgerEntry? source) =>
        JsonSerializer.Serialize(new { Templates = templates, Filters = filters, Accounts = accounts, Categories = categories, Settings = settings, Source = source });
}
