#if DEBUG
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Zanance.Core.Sharing;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Diagnostics;

internal static partial class DebugSnapshots
{
    /// <summary>Exact allowlisted financial columns only; real session and membership observation journals necessarily change during discovery.</summary>
    internal static async Task<string> HouseholdFinancialFingerprintAsync(IServiceProvider services)
    {
        await using var db = await services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync();
        await db.Database.OpenConnectionAsync(); using var command = db.Database.GetDbConnection().CreateCommand();
        var tables = new SortedDictionary<string, List<string>>();
        foreach (var table in HouseholdFinancialSchema.Manifest.Tables)
        {
            static string Quote(string name) => "\"" + name.Replace("\"", "\"\"") + "\"";
            command.CommandText = "SELECT " + string.Join(",", table.Columns.Select(column => Quote(column.Name))) + " FROM " + Quote(table.Name);
            using var reader = await command.ExecuteReaderAsync(); var rows = new List<string>();
            while (await reader.ReadAsync()) rows.Add(JsonSerializer.Serialize(Enumerable.Range(0, reader.FieldCount).Select(index => reader.IsDBNull(index) ? null : reader.GetValue(index)).ToArray()));
            rows.Sort(StringComparer.Ordinal); tables[table.Name] = rows;
        }
        var clear = JsonSerializer.SerializeToUtf8Bytes(tables);
        try { return Convert.ToHexStringLower(SHA256.HashData(clear)); }
        finally { CryptographicOperations.ZeroMemory(clear); }
    }
}
#endif
