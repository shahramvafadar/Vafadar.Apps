using Vafadar.Localization;
using Vafadar.Zanance.App.Interaction;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Ledger;

namespace Vafadar.Zanance.App.Features.Entries;

/// <summary>Requires explicit consent before the editor mutates or saves income/expense on a valued asset (ZEX-S0408).</summary>
public sealed class AssetEntryConfirmation(Translator translator, IAppInteraction interaction)
{
    private bool _pending;

    /// <summary>
    /// Runs the actual editor save continuation only after consent where required. A cancelled, failed or already
    /// pending dialog never invokes it; the guard spans the continuation so repeated taps cannot overlap a write.
    /// </summary>
    public async Task RunAsync(EntryKind kind, Account? account, Func<Task> save)
    {
        if (_pending) { return; }
        _pending = true;
        try
        {
            if (kind is EntryKind.Income or EntryKind.Expense && account?.Type == AccountType.Asset
                && !await interaction.ConfirmAsync(translator["Entry_AssetAccountTitle"], translator["Entry_AssetAccountMessage"],
                    translator["Entry_AssetAccountYes"], translator["Common_Cancel"]))
            {
                return;
            }

            await save();
        }
        finally { _pending = false; }
    }
}
