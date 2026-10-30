using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Budgets;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Reports;
using Vafadar.Zanance.Core.Settings;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.Data;

/// <summary>Original complete preferences and only the account/history window used by their unchanged suggestion.</summary>
public sealed class SettingsWorkSnapshot
{
    private readonly CommercialFileAccess _access;
    private readonly string _stamp;
    /// <summary>Gets the actual original file, independently of a later selected profile.</summary>
    public string DatabasePath { get; }
    /// <summary>Gets the captured device-local day of the original suggestion.</summary>
    public DateOnly Day { get; }
    /// <summary>Gets the deliberately selected original financial calendar.</summary>
    public PeriodCalendar Calendar { get; }
    /// <summary>Gets the complete original settings row.</summary>
    public ZananceSettings Settings => Inputs.Settings;
    /// <summary>Gets all original account fields in their existing display order.</summary>
    public IReadOnlyList<Account> Accounts => Inputs.Accounts;
    /// <summary>Gets all original entry fields within the original complete-history suggestion window.</summary>
    public IReadOnlyList<LedgerEntry> Entries => Inputs.Entries;
    /// <summary>Gets the original estimate writer contract from this same transaction.</summary>
    public EssentialEstimateReview EstimateReview { get; }
    internal SettingsWorkInputs Inputs { get; }
    internal (DateOnly From, DateOnly To) OriginalRange { get; }
    internal SettingsWorkSnapshot(string path, CommercialFileAccess access, DateOnly day, PeriodCalendar calendar, SettingsWorkInputs inputs)
    { DatabasePath = path; _access = access; Day = day; Calendar = calendar; Inputs = inputs; _stamp = Stamp(inputs); EstimateReview = new(path, inputs.Settings); OriginalRange = LiquidityCalculator.SuggestionRange(day, calendar, inputs.Settings.MonthStartDay); }
    /// <summary>Refuses retired cached access without creating settings or requesting permission.</summary>
    public void EnsureCurrent() => _access.EnsureSnapshotCurrent();
    internal void RequireOriginal(ZananceDbContext db)
    {
        _access.RequireFile(DatabaseFileIdentity.ConfiguredPath(db));
        if (_stamp != Stamp(Inputs)) throw new InvalidOperationException("The original settings inputs changed.");
    }
    internal bool Matches(SettingsWorkInputs current) => _stamp == Stamp(current);
    internal void RequireIdentity(ZananceDbContext db, ZananceSettings current)
    {
        _access.RequireFile(DatabaseFileIdentity.ConfiguredPath(db));
        if (current.Id != Settings.Id) throw new InvalidOperationException("The original settings identity changed.");
    }
    private static string Stamp(SettingsWorkInputs inputs)
    {
        using var hash = SHA256.Create(); using var output = new CryptoStream(Stream.Null, hash, CryptoStreamMode.Write);
        JsonSerializer.Serialize(output, inputs, SettingsWorkJson.Default.SettingsWorkInputs); output.FlushFinalBlock(); return Convert.ToHexString(hash.Hash!);
    }
}

/// <summary>Only original sources consumed by preferences/default-account choices and the unchanged suggestion.</summary>
internal sealed record SettingsWorkInputs(ZananceSettings Settings, List<Account> Accounts, List<LedgerEntry> Entries);
/// <summary>Explicit complete-field metadata for trimmed native preference reads.</summary>
[JsonSerializable(typeof(SettingsWorkInputs))]
internal sealed partial class SettingsWorkJson : JsonSerializerContext;

public sealed partial class ZananceStore
{
    /// <summary>Pins the existing first-settings path, then reads all original preference inputs in one worker transaction.</summary>
    public async Task<SettingsWorkSnapshot> ReadSettingsWorkSnapshotAsync(PeriodCalendar calendar, Func<string, bool>? acceptsFile = null,
        CancellationToken cancellationToken = default)
    {
        EssentialEstimateReview? initial = null;
        await GetSettingsCoreAsync(null, cancellationToken, (context, settings) => initial = new(DatabaseFileIdentity.ConfiguredPath(context), settings), acceptsFile);
        var first = initial ?? throw new InvalidOperationException("The original settings source was not captured.");
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken); first.RequireFile(db);
        var path = DatabaseFileIdentity.ConfiguredPath(db);
        if (acceptsFile?.Invoke(path) == false) throw new InvalidOperationException("The original settings file changed.");
        var access = new CommercialFileAccess(commercialAccess, path); var day = DateOnly.FromDateTime(time.GetLocalNow().DateTime);
        return await Task.Run(async () =>
        {
            access.EnsureSnapshotCurrent(); await db.Database.OpenConnectionAsync(cancellationToken);
            await using var transaction = ((SqliteConnection)db.Database.GetDbConnection()).BeginTransaction(deferred: true);
            await using var joined = await db.Database.UseTransactionAsync(transaction, cancellationToken);
            var inputs = await ReadSettingsWorkInputsAsync(db, day, calendar, cancellationToken);
            if (inputs.Settings.Id != first.SettingsId) throw new InvalidOperationException("The initial settings identity changed.");
            cancellationToken.ThrowIfCancellationRequested(); access.EnsureSnapshotCurrent();
            return new SettingsWorkSnapshot(path, access, day, calendar, inputs);
        }, cancellationToken);
    }
    /// <summary>Rechecks the original full preference/account/history packet after device availability or native waits.</summary>
    public async Task ValidateSettingsWorkSnapshotAsync(SettingsWorkSnapshot original, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(original);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await Task.Run(async () =>
        {
            original.RequireOriginal(db);
            if (original.Day != DateOnly.FromDateTime(time.GetLocalNow().DateTime)) throw new InvalidOperationException("The original settings day changed.");
            await db.Database.OpenConnectionAsync(cancellationToken);
            await using var transaction = ((SqliteConnection)db.Database.GetDbConnection()).BeginTransaction(deferred: true);
            await using var joined = await db.Database.UseTransactionAsync(transaction, cancellationToken);
            if (!original.Matches(await ReadSettingsWorkInputsAsync(db, original.Day, original.Calendar, cancellationToken)))
                throw new InvalidOperationException("The original settings sources changed; refresh before continuing.");
            cancellationToken.ThrowIfCancellationRequested(); original.RequireOriginal(db);
        }, cancellationToken);
    }
    private static async Task<SettingsWorkInputs> ReadSettingsWorkInputsAsync(ZananceDbContext db, DateOnly day, PeriodCalendar calendar, CancellationToken ct)
    {
        var settings = await SettingsRowReader.ReadAsync(db, ct) ?? throw new InvalidOperationException("The original settings are unavailable.");
        var range = LiquidityCalculator.SuggestionRange(day, calendar, settings.MonthStartDay);
        return new(settings,
            await db.Accounts.AsNoTracking().OrderBy(account => account.IsArchived).ThenBy(account => account.SortOrder).ThenBy(account => account.Name).ToListAsync(ct),
            await db.Entries.AsNoTracking().Where(entry => entry.Date >= range.From && entry.Date <= range.To)
                .OrderByDescending(entry => entry.Date).ThenByDescending(entry => entry.CreatedAt).ToListAsync(ct));
    }
    /// <summary>Serializes one original native preference change, preserving unrelated current fields and refusing another file/row/frame.</summary>
    public async Task UpdateSettingsWorkAsync(SettingsWorkSnapshot original, Action<ZananceSettings> change, Func<bool> isCurrent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(original); ArgumentNullException.ThrowIfNull(change); ArgumentNullException.ThrowIfNull(isCurrent);
        await _settingsUpdate.WaitAsync(cancellationToken);
        try
        {
            await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
            void Current() { original.EnsureCurrent(); if (!isCurrent()) throw new InvalidOperationException("The original settings command retired."); }
            Current(); original.RequireOriginal(db);
            await using var write = await CommercialWriteTransaction.OpenAsync(db, commercialAccess, cancellationToken, requireTransaction: true);
            var current = await SettingsRowReader.ReadAsync(db, cancellationToken) ?? throw new InvalidOperationException("The original settings are unavailable.");
            original.RequireIdentity(db, current);
            // Independent preference edits may compose, but their published account/history references cannot change underneath them.
            var range = original.OriginalRange;
            var references = new SettingsWorkInputs(original.Inputs.Settings,
                await db.Accounts.AsNoTracking().OrderBy(account => account.IsArchived).ThenBy(account => account.SortOrder).ThenBy(account => account.Name).ToListAsync(cancellationToken),
                await db.Entries.AsNoTracking().Where(entry => entry.Date >= range.From && entry.Date <= range.To)
                    .OrderByDescending(entry => entry.Date).ThenByDescending(entry => entry.CreatedAt).ToListAsync(cancellationToken));
            if (!original.Matches(references)) throw new InvalidOperationException("The original settings account or suggestion references changed.");
            Current(); db.Attach(current); change(current);
            Current(); write.EnsureCurrent(); await db.SaveChangesAsync(cancellationToken); original.RequireIdentity(db, current); Current();
            await write.CommitAsync(cancellationToken); CommittedWriteNotification.Publish(OnChanged, current.Id);
        }
        finally { _settingsUpdate.Release(); }
    }
}
