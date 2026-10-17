using Microsoft.EntityFrameworkCore.Metadata;
using Vafadar.Zanance.Core.Ledger;

namespace Vafadar.Zanance.Data.CompiledModel;

/// <summary>Restores persisted ledger identities without generating a discarded new identity for every read.</summary>
public partial class LedgerEntryEntityType
{
    /// <summary>Uses the existing-id constructor while retaining the normal constructor for new application entries.</summary>
    static partial void Customize(RuntimeEntityType runtimeEntityType)
    {
        // D-303: EF's default convention prefers the parameterless constructor. That generates a fresh
        // version 7 identifier before replacing it with the stored key, for every materialized ledger row.
        // The supported compiled-model extension binds the original existing-id constructor directly.
        var constructor = typeof(LedgerEntry).GetConstructor([typeof(Guid)])
            ?? throw new InvalidOperationException("The existing ledger identity constructor is unavailable.");
        var identity = runtimeEntityType.FindProperty(nameof(LedgerEntry.Id))
            ?? throw new InvalidOperationException("The original ledger identity mapping is unavailable.");
        runtimeEntityType.ConstructorBinding = new ConstructorBinding(constructor, [new PropertyParameterBinding(identity)]);
    }
}
