using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Vafadar.Zanance.Data.Sharing;

/// <summary>Requires the actual frozen native financial guards; a restored migration-history row alone cannot prove those triggers exist.</summary>
internal static class HouseholdWriteBoundary
{
    private static readonly IReadOnlyDictionary<string, string> Expected = ReadExpected();

    /// <summary>Called before granting editor or bounded maintenance scope, on the same protected writer and before any financial SQL.</summary>
    internal static void Require(ZananceDbContext db)
    {
        if (db.Database.GetDbConnection() is not SqliteConnection connection || db.Database.CurrentTransaction is null)
            throw new HouseholdWriteRejectedException(HouseholdWriteBlockReason.Unverified);
        using var command = connection.CreateCommand(); command.Transaction = (SqliteTransaction)db.Database.CurrentTransaction.GetDbTransaction();
        command.CommandText = "SELECT name,sql FROM sqlite_schema WHERE type='trigger' AND name GLOB 'zhh_guard_*'";
        using var reader = command.ExecuteReader(); var found = new HashSet<string>(StringComparer.Ordinal);
        while (reader.Read())
        {
            var name = reader.GetString(0);
            if (!found.Add(name) || !Expected.TryGetValue(name, out var expected) || reader.IsDBNull(1) || Normalize(reader.GetString(1)) != expected)
                throw new HouseholdWriteRejectedException(HouseholdWriteBlockReason.Unverified);
        }
        if (found.Count != Expected.Count) throw new HouseholdWriteRejectedException(HouseholdWriteBlockReason.Unverified);
    }

    private static IReadOnlyDictionary<string, string> ReadExpected()
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var resource in new[] { "WriteUp", "CostWriteUp" })
        {
            using var source = typeof(HouseholdWriteBoundary).Assembly.GetManifestResourceStream($"Vafadar.Zanance.Household.{resource}.v1.sql")
                ?? throw new InvalidOperationException("The household write boundary is missing.");
            using var reader = new StreamReader(source); var script = reader.ReadToEnd().Replace("\r\n", "\n");
            foreach (var block in script.Split("\nEND;", StringSplitOptions.RemoveEmptyEntries))
            {
                const string prefix = "CREATE TRIGGER \""; var start = block.IndexOf(prefix, StringComparison.Ordinal);
                if (start < 0) { if (!string.IsNullOrWhiteSpace(block)) throw new InvalidOperationException("The household write boundary is incomplete."); continue; }
                var nameStart = start + prefix.Length; var nameEnd = block.IndexOf('"', nameStart);
                if (nameEnd < 0 || !result.TryAdd(block[nameStart..nameEnd], Normalize(block[start..] + "\nEND")))
                    throw new InvalidOperationException("The household write boundary is not unique.");
            }
        }
        if (result.Count != Core.Sharing.HouseholdFinancialSchema.Manifest.Tables.Length * 3) throw new InvalidOperationException("The household write boundary does not cover the financial contract.");
        return result;
    }
    private static string Normalize(string value) => value.Replace("\r\n", "\n").Trim().TrimEnd(';');
}
