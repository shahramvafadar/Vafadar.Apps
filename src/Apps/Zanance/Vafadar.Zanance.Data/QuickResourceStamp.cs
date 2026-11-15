using System.Text.Json.Serialization;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Categories;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Settings;

namespace Vafadar.Zanance.Data;

/// <summary>Preserves the complete original stamp's JSON property names, order and entity payloads.</summary>
/// <remarks>Keep whole entities and list order: same-timestamp changes and mutable nested lists must invalidate a review.</remarks>
internal sealed record QuickResourceStamp(IReadOnlyList<EntryTemplate> Templates, IReadOnlyList<SavedFilter> Filters,
    IReadOnlyList<Account> Accounts, IReadOnlyList<Category> Categories, ZananceSettings? Settings, LedgerEntry? Source);

/// <summary>Builds the complete stamp without runtime reflection discovery on the first quick-resource read.</summary>
/// <remarks>D-1210 keeps the existing JSON string comparison and all original revalidation guards.</remarks>
[JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Serialization)]
[JsonSerializable(typeof(QuickResourceStamp))]
internal partial class QuickResourceStampContext : JsonSerializerContext;
