using Microsoft.EntityFrameworkCore;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.Data;

public sealed partial class ZananceStore
{
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
