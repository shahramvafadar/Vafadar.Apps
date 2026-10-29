using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Goals;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Plans;
using Vafadar.Zanance.Core.Budgets;
using Vafadar.Zanance.Core.Reports;
using Vafadar.Zanance.Core.Settings;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.Data;

/// <summary>The original period checklist's complete inputs from one actual file and deferred transaction.</summary>
public sealed class PeriodReviewReadSnapshot
{
    private readonly string _stamp;
    /// <summary>Gets the original file identity; Retry must not adopt another profile.</summary>
    public string DatabasePath { get; }
    /// <summary>Gets existing original settings without creating a row during reading.</summary>
    public ZananceSettings? Settings => Inputs.Settings;
    /// <summary>Gets all original accounts, including archived accounts used by the existing first-data rule.</summary>
    public IReadOnlyList<Account> Accounts => Inputs.Accounts;
    /// <summary>Gets every original ledger field, without review or presentation filtering.</summary>
    public IReadOnlyList<LedgerEntry> Entries => Inputs.Entries;
    /// <summary>Gets all original plans used by the existing occurrence calculation.</summary>
    public IReadOnlyList<Schedule> Schedules => Inputs.Schedules;
    /// <summary>Gets all original occurrence changes, without generating or settling payments.</summary>
    public IReadOnlyList<OccurrenceState> States => Inputs.States;
    /// <summary>Gets all original goals for the existing active-goal count.</summary>
    public IReadOnlyList<Goal> Goals => Inputs.Goals;
    /// <summary>Gets the complete original inputs used by the private identity guard.</summary>
    internal PeriodReviewInputs Inputs { get; }

    /// <summary>Captures the complete detached inputs and their original file on the read worker.</summary>
    internal PeriodReviewReadSnapshot(string path, PeriodReviewInputs inputs)
    { DatabasePath = path; Inputs = inputs; _stamp = Stamp(inputs); }

    /// <summary>Rejects detached mutation or a newly selected file before publication and original commands.</summary>
    internal void RequireOriginal(ZananceDbContext db)
    {
        if (DatabaseFileIdentity.ConfiguredPath(db) != DatabasePath || _stamp != Stamp(Inputs))
            throw new InvalidOperationException("The original period-review file or complete inputs changed.");
    }

    /// <summary>Compares a fresh persisted packet without exporting or weakening original values.</summary>
    internal bool Matches(PeriodReviewInputs fresh) => _stamp == Stamp(fresh);

    private static string Stamp(PeriodReviewInputs inputs)
    {
        using var hash = SHA256.Create();
        using var output = new CryptoStream(Stream.Null, hash, CryptoStreamMode.Write);
        JsonSerializer.Serialize(output, inputs, PeriodReviewReadJson.Default.PeriodReviewInputs);
        output.FlushFinalBlock(); return Convert.ToHexString(hash.Hash!);
    }
}

/// <summary>Complete original persisted inputs; native backup status and local display/day remain separate owners.</summary>
internal sealed record PeriodReviewInputs(ZananceSettings? Settings, List<Account> Accounts,
    List<LedgerEntry> Entries, List<Schedule> Schedules, List<OccurrenceState> States, List<Goal> Goals);

/// <summary>Explicit AOT metadata retains complete original settings and financial fields.</summary>
[JsonSerializable(typeof(PeriodReviewInputs))]
internal sealed partial class PeriodReviewReadJson : JsonSerializerContext;

public sealed partial class ZananceStore
{
    /// <summary>Reads the original complete checklist inputs on a worker under one captured actual-file lease.</summary>
    public async Task<PeriodReviewReadSnapshot> ReadPeriodReviewAsync(Func<string, bool>? acceptsFile = null,
        CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var path = DatabaseFileIdentity.ConfiguredPath(db);
        if (acceptsFile?.Invoke(path) == false)
            throw new InvalidOperationException("The original period review cannot follow another file.");
        return await Task.Run(async () =>
        {
            await db.Database.OpenConnectionAsync(cancellationToken);
            await using var transaction = ((SqliteConnection)db.Database.GetDbConnection()).BeginTransaction(deferred: true);
            await using var joined = await db.Database.UseTransactionAsync(transaction, cancellationToken);
            var inputs = await ReadPeriodReviewInputsAsync(db, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested(); return new PeriodReviewReadSnapshot(path, inputs);
        }, cancellationToken);
    }

    /// <summary>Revalidates the same complete original source before publication or a native checklist command.</summary>
    public async Task ValidatePeriodReviewAsync(PeriodReviewReadSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await Task.Run(async () =>
        {
            snapshot.RequireOriginal(db);
            await db.Database.OpenConnectionAsync(cancellationToken);
            await using var transaction = ((SqliteConnection)db.Database.GetDbConnection()).BeginTransaction(deferred: true);
            await using var joined = await db.Database.UseTransactionAsync(transaction, cancellationToken);
            if (!snapshot.Matches(await ReadPeriodReviewInputsAsync(db, cancellationToken)))
                throw new InvalidOperationException("The original period-review source changed.");
            cancellationToken.ThrowIfCancellationRequested(); snapshot.RequireOriginal(db);
        }, cancellationToken);
    }

    /// <summary>Changes only original checklist progress under its reviewed source, due period and actual current owner.</summary>
    public async Task<string> SavePeriodReviewProgressAsync(PeriodReviewReadSnapshot snapshot, PeriodCalendar calendar,
        DateOnly reviewedDay, string periodKey, ReviewStep? step, bool done, Func<bool> isCurrent,
        CancellationToken cancellationToken = default, Action? accepted = null)
    {
        ArgumentNullException.ThrowIfNull(snapshot); ArgumentNullException.ThrowIfNull(isCurrent);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await Task.Run(async () =>
        {
            void Current()
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!isCurrent() || DateOnly.FromDateTime(time.GetLocalNow().DateTime) != reviewedDay)
                    throw new OperationCanceledException("The original period-review owner or local day retired.");
                snapshot.RequireOriginal(db);
            }
            Current();
            await using var write = await CommercialWriteTransaction.OpenAsync(db, commercialAccess, cancellationToken, requireTransaction: true);
            if (!snapshot.Matches(await ReadPeriodReviewInputsAsync(db, cancellationToken)))
                throw new InvalidOperationException("The original period-review source changed before saving progress.");
            var original = snapshot.Settings;
            var first = snapshot.Accounts.Count == 0 ? (DateOnly?)null : snapshot.Accounts.Min(a => a.OpeningDate);
            var state = PeriodReview.Due(original?.ReviewProgress, reviewedDay, calendar, original?.MonthStartDay ?? 1, first);
            if (state is null || state.Key != periodKey || step is { } item && !Enum.IsDefined(item))
                throw new InvalidOperationException("Only the original currently due financial period may be changed.");
            var progress = step is { } selected ? PeriodReview.Toggle(state, selected, done) : PeriodReview.Finish(state);
            var settings = await db.Settings.SingleOrDefaultAsync(cancellationToken);
            if (settings is null) { settings = new ZananceSettings(); db.Settings.Add(settings); }
            settings.ReviewProgress = progress;
            Current(); write.EnsureCurrent();
            await db.SaveChangesAsync(cancellationToken);
            Current(); write.EnsureCurrent();
            await write.CommitAsync(cancellationToken);
            accepted?.Invoke();
            try { OnChanged(); }
            catch (Exception exception) when (exception is not OutOfMemoryException)
            { throw new CommittedWriteNotificationException<string>(progress, exception); }
            return progress;
        }, cancellationToken);
    }

    private static async Task<PeriodReviewInputs> ReadPeriodReviewInputsAsync(ZananceDbContext db, CancellationToken ct) => new(
        await db.Settings.AsNoTracking().SingleOrDefaultAsync(ct),
        await db.Accounts.AsNoTracking().OrderBy(a => a.IsArchived).ThenBy(a => a.SortOrder).ThenBy(a => a.Name).ThenBy(a => a.Id).ToListAsync(ct),
        await ReadCompleteHomeLedgerAsync(db, ct),
        await db.Schedules.AsNoTracking().OrderBy(s => s.State).ThenBy(s => s.Name).ThenBy(s => s.Id).ToListAsync(ct),
        await db.OccurrenceStates.AsNoTracking().OrderBy(s => s.Id).ToListAsync(ct),
        GoalStore.Ordered(await db.Goals.AsNoTracking().OrderBy(g => g.Id).ToListAsync(ct)));
}
