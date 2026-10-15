using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Budgets;
using Vafadar.Zanance.Core.Categories;
using Vafadar.Zanance.Core.Forecasts;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Rates;
using Vafadar.Zanance.Core.Settings;
using Vafadar.Zanance.Core.Sync;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App;

internal static partial class AndroidSyncReview
{
    private static async Task SeedPortableReviewAsync(IServiceProvider services, Account account, CancellationToken ct)
    {
        var store = services.GetRequiredService<ZananceStore>();
        var target = new Account { Name = "Fictitious native JPY account", CurrencyCode = "JPY", OpeningDate = new(2026, 10, 1) };
        await store.SaveAccountAsync(target, ct);
        var parent = new Category { Name = "Fictitious native parent", Kind = CategoryKind.Expense }; await store.SaveCategoryAsync(parent, ct);
        var child = new Category { Name = "Fictitious native child", Kind = CategoryKind.Expense, ParentId = parent.Id,
            SpendingType = SpendingType.Fixed, IsEssential = true, Icon = "Home", Color = "#112233" }; await store.SaveCategoryAsync(child, ct);
        var rule = new CategoryRule { Match = "Fictitious native shop", CategoryId = child.Id, Kind = CategoryKind.Expense };
        await store.SaveCategoryRuleAsync(rule, ct);
        var template = new EntryTemplate { Name = "Fictitious native transfer", Kind = EntryKind.Transfer, AccountId = account.Id,
            ToAccountId = target.Id, Amount = 1250, Icon = "Home" }; await store.SaveTemplateAsync(template, ct);
        var filter = new SavedFilter { Name = "Fictitious native filter", AccountId = account.Id, CategoryIds = [child.Id], CategoryName = child.Name,
            From = new(2026, 10, 1), To = new(2026, 10, 15), Period = 2, Kind = 1, InTotalsOnly = true, UnreviewedOnly = true, Search = "#fictitious" };
        await store.SaveSavedFilterAsync(filter, ct);
        var rate = new ExchangeRate { Date = new(2026, 10, 15), FromCurrencyCode = "KWD", ToCurrencyCode = "JPY", Rate = 123.456789m, IsEstimate = true };
        await store.SaveRateAsync(rate, ct);
        var settings = await store.GetSettingsAsync(ct); settings.DefaultAccountId = account.Id;
        settings.ReportCurrencyCode = "JPY"; settings.DefaultCurrencyCode = "KWD"; settings.Mode = ExperienceMode.Advanced;
        settings.BudgetCalendar = PeriodCalendar.Persian; settings.EssentialEstimate = 1250; settings.EssentialEstimateCurrency = "KWD";
        settings.EssentialEstimatePeriod = EstimatePeriod.Week; settings.RateFreshnessDays = 0;
        settings.DisplayUnits = "KWD|Fictitious unit|2"; settings.HomeLayout = "Budget,-Accounts,Recent";
        settings.ReviewProgress = "2026-09:Unreviewed,Plans"; await store.SaveSettingsAsync(settings, ct);
        var forecast = new ForecastSnapshot { Name = "Fictitious native forecast", BaseDate = new(2026, 10, 15), Horizon = new(2026, 10, 17),
            CurrencyCode = "KWD", AccountIds = account.Id.ToString("D"), Path = "1250,-1250,-9223372036854775808", Minimum = long.MinValue,
            MinimumDate = new(2026, 10, 17), UnknownCount = 2, Assumptions = "Fictitious native assumptions", AppVersion = "0.1" };
        await store.SaveForecastSnapshotAsync(forecast, ct);
        var identities = new Dictionary<string, Guid>
        { ["Categories"] = child.Id, ["CategoryRules"] = rule.Id, ["Templates"] = template.Id, ["SavedFilters"] = filter.Id,
            ["ExchangeRates"] = rate.Id, ["ForecastSnapshots"] = forecast.Id };
        await using var db = await services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(ct);
        await db.Database.OpenConnectionAsync(ct); var captured = new List<(SyncRecord Record, long Sequence)>();
        await using (var query = db.Database.GetDbConnection().CreateCommand())
        {
            query.CommandText = "SELECT TableName,RecordKey,Body,Sequence FROM SyncOutbox WHERE Body IS NOT NULL ORDER BY Sequence";
            await using var rows = await query.ExecuteReaderAsync(ct);
            while (await rows.ReadAsync(ct)) captured.Add((new(new(rows.GetString(0), rows.GetString(1)), 5,
                JsonSerializer.Deserialize<JsonElement>(rows.GetString(2))), rows.GetInt64(3)));
        }
        var latest = captured.GroupBy(row => row.Record.Record).Select(group => group.Last()).ToArray();
        var selected = latest.Where(row => row.Record.Record.Table == "Settings" || identities.TryGetValue(row.Record.Record.Table, out var id) &&
            row.Record.Values!.Value.GetProperty("Id").GetGuid() == id).ToArray();
        Check(selected.Length == 7, "Seven original portable records required.");
        var remote = selected.Select(row =>
        {
            var body = JsonNode.Parse(row.Record.Values!.Value.GetRawText())!.AsObject();
            if (row.Record.Record.Table == "Settings") body["EssentialEstimate"] = 0;
            else if (row.Record.Record.Table == "ExchangeRates") body["Rate"] = "130.000001";
            else if (row.Record.Record.Table == "CategoryRules") body["Match"] = "Fictitious changed match";
            else body["Name"] = "Fictitious changed name";
            return row.Record with { Version = 7, Values = JsonSerializer.SerializeToElement(body) };
        }).ToArray();
        var context = remote.Concat(latest.Where(row => selected.All(member => member.Record.Record != row.Record.Record)).Select(row => row.Record)).ToArray();
        var operation = Guid.NewGuid(); var cutoff = latest.Max(row => row.Sequence);
        await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO SyncClientRound(Id,OperationId,RemoteId,Cutoff,Phase,PartCount,SentParts,ContentHash,ThroughCursor,ReadCursor) VALUES(1,{operation},NULL,{cutoff},1,1,1,{new string('1', 64)},NULL,0)", ct);
        foreach (var row in selected)
        {
            var counterpart = remote.Single(item => item.Record == row.Record.Record);
            var fields = row.Record.Values!.Value.EnumerateObject().Where(property => !JsonElement.DeepEquals(property.Value, counterpart.Values!.Value.GetProperty(property.Name)))
                .Select(property => property.Name).ToArray();
            var conflict = new SyncConflict(new(row.Record.Record, 6, row.Record.Values), null, counterpart, fields, "CompetingChanges", Context: context);
            var payload = JsonSerializer.Serialize(conflict, PersonalSyncJsonContext.Default.SyncConflict);
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO SyncConflicts(TableName,RecordKey,Payload,LocalSequence,UseProposal) VALUES({row.Record.Record.Table},{row.Record.Record.Key},{payload},{row.Sequence},NULL)", ct);
        }
    }
}
