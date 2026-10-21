using Microsoft.EntityFrameworkCore;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Settings;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.Data;

public sealed partial class ZananceStore
{
    /// <summary>Reads only the original nested account form's cached rights and actual-file quota inputs.</summary>
    public Task<EditorWorkReview> ReadEditorAccountWorkAsync(EntryEditorReadSnapshot original, CancellationToken ct = default) =>
        new EditorWorkStore(contextFactory, commercialAccess).ReadAsync(ct, path =>
        { original.EnsureCurrent(); return path == original.DatabasePath; });

    /// <summary>Creates one explicitly requested account/default under its parent editor's original-file writer.</summary>
    public async Task CreateEditorAccountAsync(EntryEditorReadSnapshot original, Account proposal, bool makeDefault,
        Func<bool> isCurrent, Action<EntryEditorReadSnapshot> accepted, CancellationToken ct = default)
    {
        var account = proposal.CopyForEditing();
        await using var db = await contextFactory.CreateDbContextAsync(ct);
        void Current()
        {
            original.RequireFile(db);
            if (!isCurrent() || original.Accepted || original.IsPending)
                throw new InvalidOperationException("The original nested account owner retired.");
        }
        Current();
        await using var write = await CommercialWriteTransaction.OpenAsync(db, commercialAccess, ct, requireTransaction: true);
        await ValidateEntryEditorUnderContextAsync(original, db, ct);
        if (await db.Accounts.AnyAsync(a => a.Id == account.Id, ct))
            throw new InvalidOperationException("A nested account create cannot replace an existing identity.");
        await CheckAccountWorkAsync(db, write, account, null, ct);
        await write.PrepareAccountChangeAsync(db, account, null, ct);
        db.Accounts.Add(account);
        if (makeDefault)
        {
            if (!EntryAccountContract.IsValidDefault(account)) throw new InvalidOperationException("This account cannot be the new-entry default.");
            var settings = await SettingsRowReader.ReadAsync(db, ct) ?? new ZananceSettings();
            settings.DefaultAccountId = account.Id;
            if (db.Entry(settings).State == EntityState.Detached)
            {
                if (await db.Settings.AnyAsync(ct)) db.Settings.Update(settings); else db.Settings.Add(settings);
            }
        }
        Current(); write.EnsureCurrent(); await db.SaveChangesAsync(ct);
        var next = new EntryEditorReadSnapshot(original.DatabasePath, new CommercialFileAccess(commercialAccess, original.DatabasePath),
            original.Request, await ReadEntryEditorInputsAsync(db, original.Request, true, ct));
        Current(); next.EnsureCurrent(); await write.CommitAsync(ct);
        accepted(next);
        CommittedWriteNotification.Publish(OnChanged);
    }
}
