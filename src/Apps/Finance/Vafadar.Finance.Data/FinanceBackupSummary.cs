using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Vafadar.Backup;

namespace Vafadar.Finance.Data;

/// <summary>
/// Counts shown before a restore (BAK-09). Only numbers – never names, amounts or notes – because the manifest of an
/// encrypted backup is readable only with the password, but a summary must still be harmless.
/// </summary>
public sealed class FinanceBackupSummary(IDbContextFactory<FinanceDbContext> contextFactory) : IBackupSummaryProvider
{
    /// <summary>Summary key: number of accounts.</summary>
    public const string Accounts = "accounts";

    /// <summary>Summary key: number of entries.</summary>
    public const string Entries = "entries";

    /// <summary>Summary key: number of plans.</summary>
    public const string Plans = "plans";

    /// <summary>Summary key: number of budgets.</summary>
    public const string Budgets = "budgets";

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<string, string>> GetSummaryAsync(CancellationToken cancellationToken)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        return new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [Accounts] = Count(await db.Accounts.CountAsync(cancellationToken)),
            [Entries] = Count(await db.Entries.CountAsync(cancellationToken)),
            [Plans] = Count(await db.Schedules.CountAsync(cancellationToken)),
            [Budgets] = Count(await db.Budgets.CountAsync(cancellationToken)),
        };
    }

    private static string Count(int value) => value.ToString(CultureInfo.InvariantCulture);
}
