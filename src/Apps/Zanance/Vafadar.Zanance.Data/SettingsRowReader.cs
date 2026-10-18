using System.Runtime.CompilerServices;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Vafadar.Zanance.Core.Budgets;
using Vafadar.Zanance.Core.Settings;

namespace Vafadar.Zanance.Data;

/// <summary>Reads the complete earliest settings row under the original captured EF command authority.</summary>
internal static class SettingsRowReader
{
    private static readonly ConditionalWeakTable<IModel, ReadContract> Contracts = new();
    private static readonly (string Name, Type Type)[] Columns =
    [
        (nameof(ZananceSettings.Id), typeof(Guid)),
        (nameof(ZananceSettings.AppLockEnabled), typeof(bool)),
        (nameof(ZananceSettings.BudgetCalendar), typeof(PeriodCalendar)),
        (nameof(ZananceSettings.CreatedAt), typeof(DateTimeOffset)),
        (nameof(ZananceSettings.DefaultAccountId), typeof(Guid?)),
        (nameof(ZananceSettings.DefaultCurrencyCode), typeof(string)),
        (nameof(ZananceSettings.DisplayUnits), typeof(string)),
        (nameof(ZananceSettings.EssentialEstimate), typeof(long?)),
        (nameof(ZananceSettings.EssentialEstimateCurrency), typeof(string)),
        (nameof(ZananceSettings.EssentialEstimatePeriod), typeof(EstimatePeriod)),
        (nameof(ZananceSettings.FortnightStart), typeof(DateOnly?)),
        (nameof(ZananceSettings.HomeBudgetCurrencyCode), typeof(string)),
        (nameof(ZananceSettings.HomeLayout), typeof(string)),
        (nameof(ZananceSettings.Mode), typeof(ExperienceMode)),
        (nameof(ZananceSettings.MonthStartDay), typeof(int)),
        (nameof(ZananceSettings.NotificationsShowDetails), typeof(bool)),
        (nameof(ZananceSettings.OnboardingCompleted), typeof(bool)),
        (nameof(ZananceSettings.RateFreshnessDays), typeof(int)),
        (nameof(ZananceSettings.ReminderDaysBefore), typeof(int)),
        (nameof(ZananceSettings.ReminderTime), typeof(TimeOnly)),
        (nameof(ZananceSettings.ReportCurrencyCode), typeof(string)),
        (nameof(ZananceSettings.ReviewProgress), typeof(string)),
        (nameof(ZananceSettings.ReviewReminderEnabled), typeof(bool)),
        (nameof(ZananceSettings.UpdatedAt), typeof(DateTimeOffset)),
        (nameof(ZananceSettings.ValuationCurrencyEnabled), typeof(bool))
    ];
    private static readonly string Sql = "SELECT " + string.Join(", ", Columns.Select(column => $"\"{column.Name}\""))
        + " FROM \"Settings\" ORDER BY \"CreatedAt\" LIMIT 1";

    /// <summary>Retains cancellation, native connection checks, command interceptors and any joined transaction.</summary>
    internal static async Task<ZananceSettings?> ReadAsync(ZananceDbContext db, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var contract = Contracts.GetValue(db.Model, CreateContract);
        // D-363: avoid the measured cold LINQ materializer cost in covered startup. Use the
        // same EF relational command boundary as the complete ledger reader, never a bypass connection.
        var command = db.GetService<IRelationalCommandBuilderFactory>().Create().Append(Sql, sensitive: false).Build();
        var parameters = new RelationalCommandParameterObject(db.GetService<IRelationalConnection>(), null, null,
            db, db.GetService<IRelationalCommandDiagnosticsLogger>(), CommandSource.FromSqlQuery);
        await using var result = await command.ExecuteReaderAsync(parameters, ct);
        var row = result.DbDataReader;
        if (!await row.ReadAsync(ct)) { ct.ThrowIfCancellationRequested(); return null; }
        var settings = new ZananceSettings
        {
            AppLockEnabled = row.GetBoolean(1),
            BudgetCalendar = (PeriodCalendar)row.GetInt32(2),
            CreatedAt = (DateTimeOffset)contract.CreatedAt.ConvertFromProvider(row.GetInt64(3))!,
            DefaultAccountId = row.IsDBNull(4) ? null : row.GetGuid(4),
            DefaultCurrencyCode = row.GetString(5),
            DisplayUnits = NullableText(row, 6),
            EssentialEstimate = row.IsDBNull(7) ? null : row.GetInt64(7),
            EssentialEstimateCurrency = NullableText(row, 8),
            EssentialEstimatePeriod = (EstimatePeriod)row.GetInt32(9),
            FortnightStart = row.IsDBNull(10) ? null : row.GetFieldValue<DateOnly>(10),
            HomeBudgetCurrencyCode = NullableText(row, 11),
            HomeLayout = NullableText(row, 12),
            Mode = (ExperienceMode)row.GetInt32(13),
            MonthStartDay = row.GetInt32(14),
            NotificationsShowDetails = row.GetBoolean(15),
            OnboardingCompleted = row.GetBoolean(16),
            RateFreshnessDays = row.GetInt32(17),
            ReminderDaysBefore = row.GetInt32(18),
            ReminderTime = row.GetFieldValue<TimeOnly>(19),
            ReportCurrencyCode = row.GetString(20),
            ReviewProgress = NullableText(row, 21),
            ReviewReminderEnabled = row.GetBoolean(22),
            UpdatedAt = (DateTimeOffset)contract.UpdatedAt.ConvertFromProvider(row.GetInt64(23))!,
            ValuationCurrencyEnabled = row.GetBoolean(24)
        };
        // Preserve even legacy identifiers exactly; no new tracked entity or replacement row is created.
        contract.Id.SetValue(settings, row.GetGuid(0));
        ct.ThrowIfCancellationRequested();
        return settings;
    }

    /// <summary>Retains the provider's text and null semantics without changing stored content.</summary>
    private static string? NullableText(DbDataReader row, int ordinal) => row.IsDBNull(ordinal) ? null : row.GetString(ordinal);

    /// <summary>Refuses schema/converter/filter drift instead of returning incomplete security or display preferences.</summary>
    private static ReadContract CreateContract(IModel model)
    {
        var entity = model.FindEntityType(typeof(ZananceSettings))
            ?? throw new InvalidOperationException("The original settings mapping is unavailable.");
        var table = StoreObjectIdentifier.Table("Settings", null);
        if (entity.GetTableName() != "Settings" || entity.BaseType is not null || entity.GetSchema() is not null
            || entity.GetProperties().Count() != Columns.Length || entity.GetDeclaredQueryFilters().Any()
            || Columns.Any(column => entity.FindProperty(column.Name) is not { } property
                || property.ClrType != column.Type || property.GetColumnName(table) != column.Name
                || !HasOriginalConversion(property)))
            throw new InvalidOperationException("The complete settings reader must be refreshed for the current mapping.");
        return new(entity.FindProperty(nameof(ZananceSettings.Id))!.PropertyInfo!,
            entity.FindProperty(nameof(ZananceSettings.CreatedAt))!.GetTypeMapping().Converter!,
            entity.FindProperty(nameof(ZananceSettings.UpdatedAt))!.GetTypeMapping().Converter!);
    }

    /// <summary>Accepts only the original provider scalar mappings and the model's UTC-tick converters.</summary>
    private static bool HasOriginalConversion(IProperty property)
    {
        if (property.Name is nameof(ZananceSettings.CreatedAt) or nameof(ZananceSettings.UpdatedAt))
            return property.GetTypeMapping().Converter is Vafadar.Data.Conventions.UtcTicksDateTimeOffsetConverter;
        if (property.GetValueConverter() is not null) return false;
        var type = Nullable.GetUnderlyingType(property.ClrType) ?? property.ClrType;
        return type.IsEnum ? property.GetTypeMapping().Converter?.ProviderClrType == typeof(int)
            : property.GetTypeMapping().Converter is null;
    }

    /// <summary>Immutable model-owned accessors; never retains a context, connection, key or settings value.</summary>
    private sealed record ReadContract(System.Reflection.PropertyInfo Id, ValueConverter CreatedAt, ValueConverter UpdatedAt);
}
