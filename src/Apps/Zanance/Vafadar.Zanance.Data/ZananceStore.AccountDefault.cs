using Microsoft.EntityFrameworkCore;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.Data;

public sealed partial class ZananceStore
{
    /// <summary>Stages only the original explicit default preference in its already-owned account transaction.</summary>
    private static async Task StageOriginalAccountDefaultAsync(ZananceDbContext db, Account account, bool makeDefault,
        bool initializeMissing, CancellationToken ct)
    {
        var wanted = makeDefault && EntryAccountContract.IsValidDefault(account);
        if (makeDefault && !wanted) throw new InvalidOperationException("The accepted account cannot be the new-entry default.");
        var settings = await SettingsRowReader.ReadAsync(db, ct);
        if (settings is null)
        {
            if (!initializeMissing) throw new InvalidOperationException("The original account preferences are no longer available.");
            // D-639: only this explicit original Save may initialize defaults; an editor read remains read-only.
            settings = new Core.Settings.ZananceSettings(); db.Settings.Add(settings);
        }
        if (wanted == (settings.DefaultAccountId == account.Id)) return;
        settings.DefaultAccountId = wanted ? account.Id : null;
        if (db.Entry(settings).State == EntityState.Detached)
        {
            db.Attach(settings); db.Entry(settings).Property(row => row.DefaultAccountId).IsModified = true;
        }
    }

    /// <summary>Applies an accepted account's explicit default choice only in its original file and live native owner.</summary>
    public async Task UpdateOriginalAccountDefaultAsync(Guid accountId, bool makeDefault, EditorWorkReview originalWork,
        Func<bool> isCurrent, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(originalWork); ArgumentNullException.ThrowIfNull(isCurrent);
        await using var db = await contextFactory.CreateDbContextAsync(ct);
        void Current()
        {
            originalWork.RequireFile(DatabaseFileIdentity.ConfiguredPath(db));
            if (!isCurrent()) throw new InvalidOperationException("The original accepted account owner retired.");
        }
        Current();
        await using var write = await CommercialWriteTransaction.OpenAsync(db, commercialAccess, ct, requireTransaction: true);
        Current();
        var account = await db.Accounts.AsNoTracking().SingleOrDefaultAsync(row => row.Id == accountId, ct)
            ?? throw new InvalidOperationException("The original accepted account is no longer available.");
        var settings = await SettingsRowReader.ReadAsync(db, ct)
            ?? throw new InvalidOperationException("The original account preferences are no longer available.");
        var wanted = makeDefault && EntryAccountContract.IsValidDefault(account);
        if (makeDefault && !wanted) throw new InvalidOperationException("The accepted account cannot be the new-entry default.");
        // Unchecking this account never clears a different account's current default choice.
        if (wanted == (settings.DefaultAccountId == accountId)) return;
        settings.DefaultAccountId = wanted ? accountId : null;
        db.Attach(settings);
        db.Entry(settings).Property(row => row.DefaultAccountId).IsModified = true;
        Current(); write.EnsureCurrent(); await db.SaveChangesAsync(ct);
        Current(); await write.CommitAsync(ct);
        OnChanged();
    }
}
