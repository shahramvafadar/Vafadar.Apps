using Microsoft.EntityFrameworkCore;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Settings;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.Data;

public sealed partial class PlanStore
{
    /// <summary>Reads readiness only from the original plan parent's actual file for its explicit account child.</summary>
    public Task<EditorWorkReview> ReadEditorAccountWorkAsync(PlanEditorReadSnapshot original, CancellationToken ct = default) =>
        new EditorWorkStore(contextFactory, commercialAccess).ReadAsync(ct, path =>
        { original.EnsureCurrent(); return path == original.DatabasePath; });

    /// <summary>Creates one explicit account/default and captures the next original plan reference packet atomically.</summary>
    public async Task CreateEditorAccountAsync(PlanEditorReadSnapshot original, Account proposal, bool makeDefault,
        Func<bool> current, Action<PlanEditorReadSnapshot> accepted, CancellationToken ct = default)
    {
        var account = proposal.CopyForEditing();
        await using var db = await contextFactory.CreateDbContextAsync(ct);
        void Current()
        {
            original.RequireFile(db);
            if (!current() || original.IsPending || original.Accepted || original.Request.EntryOrigin is not null)
                throw new InvalidOperationException("The original plan account child retired or has a separate transaction origin.");
        }
        Current();
        await using var write = await CommercialWriteTransaction.OpenAsync(db, commercialAccess, ct, requireTransaction: true);
        await ValidateEditorUnderContextAsync(original, db, ct);
        if (await db.Accounts.AnyAsync(a => a.Id == account.Id, ct))
            throw new InvalidOperationException("An explicit nested account cannot replace an existing identity.");
        await ZananceStore.CheckAccountWorkAsync(db, write, account, null, ct);
        await write.PrepareAccountChangeAsync(db, account, null, ct); db.Accounts.Add(account);
        if (makeDefault)
        {
            if (!EntryAccountContract.IsValidDefault(account)) throw new InvalidOperationException("The new account cannot be the entry default.");
            var settings = await SettingsRowReader.ReadAsync(db, ct) ?? new ZananceSettings();
            settings.DefaultAccountId = account.Id;
            if (db.Entry(settings).State == EntityState.Detached)
            { if (await db.Settings.AnyAsync(ct)) db.Settings.Update(settings); else db.Settings.Add(settings); }
        }
        Current(); write.EnsureCurrent(); await db.SaveChangesAsync(ct);
        var next = new PlanEditorReadSnapshot(original.DatabasePath, new CommercialFileAccess(commercialAccess, original.DatabasePath),
            original.Request, await ReadEditorInputsAsync(db, new CommercialFileAccess(commercialAccess, original.DatabasePath), original.Request, ct));
        Current(); next.EnsureCurrent(); write.EnsureCurrent(); await write.CommitAsync(ct);
        accepted(next); CommittedWriteNotification.Publish(store.NotifyReferenceChanged);
    }
}
