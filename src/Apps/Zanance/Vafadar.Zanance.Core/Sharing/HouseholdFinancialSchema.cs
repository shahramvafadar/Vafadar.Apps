using System.Text.Json;
using Vafadar.Zanance.Core.Sync;

namespace Vafadar.Zanance.Core.Sharing;

/// <summary>Explicit household financial contract; the already delivered personal v1 manifest and digest remain unchanged.</summary>
public static class HouseholdFinancialSchema
{
    /// <summary>Only mapped financial contacts/costs/shares/repayments, never local authority or private key tables.</summary>
    public static SyncSchemaManifest Additional { get; } = ReadAdditional();
    /// <summary>Complete household graph, retaining all original ledger rules plus explicit cost references.</summary>
    public static SyncSchemaManifest Manifest { get; } = new(2, SyncSchema.Manifest.Tables.Concat(Additional.Tables)
        .OrderBy(table => table.Name, StringComparer.Ordinal).ToArray());
    /// <summary>Negotiates the actual complete household contract independently of readable personal sync.</summary>
    public static string Hash { get; } = SyncSchemaDigest.Compute(Manifest);

    private static SyncSchemaManifest ReadAdditional()
    {
        using var stream = typeof(HouseholdFinancialSchema).Assembly.GetManifestResourceStream("Vafadar.Zanance.Household.financial-v1.json")
            ?? throw new InvalidOperationException("The household financial contract is missing.");
        var result = JsonSerializer.Deserialize(stream, PersonalSyncJsonContext.Default.SyncSchemaManifest)
            ?? throw new InvalidOperationException("The household financial contract is invalid.");
        string[] expected = ["HouseholdAllocations", "HouseholdContacts", "HouseholdCosts", "HouseholdSettlements"];
        if (result.Version != 1 || !result.Tables.Select(table => table.Name).Order(StringComparer.Ordinal).SequenceEqual(expected))
            throw new InvalidOperationException("The household financial contract is not reviewed.");
        return result;
    }
}
