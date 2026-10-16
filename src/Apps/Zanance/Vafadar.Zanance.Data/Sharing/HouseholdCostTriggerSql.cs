using System.Text;
using Vafadar.Zanance.Core.Sync;

namespace Vafadar.Zanance.Data.Sharing;

/// <summary>Generates only new reviewed cost financial guards; previously applied ledger guards remain immutable.</summary>
public static class HouseholdCostTriggerSql
{
    /// <summary>Creates exact role/transaction guards for a reviewed additional financial manifest.</summary>
    public static string Create(SyncSchemaManifest additional)
    {
        var result = new StringBuilder();
        foreach (var table in additional.Tables)
        foreach (var action in new[] { "insert", "update", "delete" })
        {
            var source = action == "delete" ? "OLD" : "NEW";
            var key = "json_array(" + string.Join(',', table.PrimaryKey.Select(column => source + "." + Quote(column))) + ")";
            result.Append("CREATE TRIGGER ").Append(Quote("zhh_guard_" + table.Name + "_" + action)).Append(" BEFORE ").Append(action.ToUpperInvariant())
                .Append(" ON ").Append(Quote(table.Name)).AppendLine(" WHEN (EXISTS(SELECT 1 FROM \"HouseholdLocalBinding\") OR EXISTS(SELECT 1 FROM \"SyncBinding\" WHERE \"TransportKind\"=1)) BEGIN")
                .Append("    SELECT zanance_household_write('").Append(table.Name).Append("','").Append(action).Append("',").Append(key).AppendLine(");")
                .AppendLine("END;");
        }
        return result.ToString();
    }
    /// <summary>Drops only these exact new guards before removing their mapped financial tables.</summary>
    public static string Drop(SyncSchemaManifest additional) => string.Concat(additional.Tables.SelectMany(table => new[] { "insert", "update", "delete" }
        .Select(action => "DROP TRIGGER " + Quote("zhh_guard_" + table.Name + "_" + action) + ";" + Environment.NewLine)));

    private static string Quote(string name)
    {
        if (name.Length is < 1 or > 200 || name.Any(character => !char.IsAsciiLetterOrDigit(character) && character != '_'))
            throw new InvalidOperationException("The reviewed household relational identifier is invalid.");
        return "\"" + name + "\"";
    }
}
