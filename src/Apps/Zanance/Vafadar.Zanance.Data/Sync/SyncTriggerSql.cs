using System.Text;
using Vafadar.Zanance.Core.Sync;

namespace Vafadar.Zanance.Data.Sync;

/// <summary>Generates reviewed built-in SQLite capture statements; migrations freeze their own emitted snapshot.</summary>
public static class SyncTriggerSql
{
    /// <summary>Captures complete values only under explicit opt-in and outside incoming publication.</summary>
    public static string Create(SyncSchemaManifest manifest)
    {
        var sql = new StringBuilder();
        foreach (var table in manifest.Tables)
        {
            var quoted = Identifier(table.Name);
            var newKey = Key(table, "new"); var oldKey = Key(table, "old");
            var body = Body(table, "new");
            var changed = string.Join(" OR ", table.Columns.Select(column => $"old.{Identifier(column.Name)} IS NOT new.{Identifier(column.Name)}"));
            foreach (var action in new[] { "insert", "update", "delete" })
            {
                sql.Append("CREATE TRIGGER ").Append(Identifier(Name(table, action))).Append(" AFTER ")
                    .Append(action.ToUpperInvariant()).Append(" ON ").Append(quoted)
                    .Append(" WHEN EXISTS(SELECT 1 FROM \"SyncBinding\" WHERE \"Id\"=1 AND \"Enabled\"=1 AND \"Applying\"=0)");
                if (action == "update") sql.Append(" AND (").Append(changed).Append(')');
                sql.AppendLine(" BEGIN");
                if (action == "update")
                    sql.Append("INSERT INTO \"SyncOutbox\"(\"TableName\",\"RecordKey\",\"Body\") SELECT ")
                        .Append(Literal(table.Name)).Append(',').Append(oldKey).Append(",NULL WHERE ")
                        .Append(oldKey).Append(" <> ").Append(newKey).AppendLine(";");
                sql.Append("INSERT INTO \"SyncOutbox\"(\"TableName\",\"RecordKey\",\"Body\") VALUES (")
                    .Append(Literal(table.Name)).Append(',').Append(action == "delete" ? oldKey : newKey)
                    .Append(',').Append(action == "delete" ? "NULL" : body).AppendLine(");");
                sql.AppendLine("END;");
            }
        }
        return sql.ToString();
    }

    /// <summary>Drops only the exact capture names owned by this contract before removing metadata tables.</summary>
    public static string Drop(SyncSchemaManifest manifest) => string.Join(Environment.NewLine,
        manifest.Tables.SelectMany(table => new[] { "insert", "update", "delete" }
            .Select(action => "DROP TRIGGER " + Identifier(Name(table, action)) + ";"))) + Environment.NewLine;

    /// <summary>Selects complete after-values for an explicitly reviewed initial capture inside one writer transaction.</summary>
    public static string CaptureExisting(SyncTable table) =>
        "INSERT INTO \"SyncOutbox\"(\"TableName\",\"RecordKey\",\"Body\") SELECT " + Literal(table.Name) + "," +
        Key(table, "source") + "," + Body(table, "source") + " FROM " + Identifier(table.Name) + " AS source;";

    /// <summary>Reads actual original values through the same reviewed projection used by capture triggers.</summary>
    internal static string ReadExisting(SyncTable table) => "SELECT " + Key(table, "source") + "," +
        Body(table, "source") + " FROM " + Identifier(table.Name) + " AS source;";

    /// <summary>Names only the reviewed projection columns for a parameterized one-row recapture.</summary>
    internal static string ReadExistingAliased(SyncTable table) => "SELECT " + Key(table, "source") + " AS record_key," +
        Body(table, "source") + " AS record_body FROM " + Identifier(table.Name) + " AS source";

    /// <summary>Reads logical live finance; physical orphan receipts belong only to the original local Undo lifetime.</summary>
    internal static string ReadLiveExisting(SyncTable table) => ReadLiveExistingAliased(table) + ";";

    /// <summary>Provides the same bounded logical projection for a one-record recapture.</summary>
    internal static string ReadLiveExistingAliased(SyncTable table) => ReadExistingAliased(table) +
        (table.Name == "Attachments" ? " WHERE EXISTS(SELECT 1 FROM Entries AS parent WHERE parent.Id=source.EntryId)" : "");

    /// <summary>Derives receipt changes from durable parent events under the same writer before freezing/applying a round.</summary>
    internal static string CaptureRelatedAttachments(SyncTable table) =>
        "INSERT INTO SyncOutbox(TableName,RecordKey,Body) SELECT 'Attachments'," + Key(table, "source") +
        ",CASE WHEN EXISTS(SELECT 1 FROM Entries AS parent WHERE parent.Id=source.EntryId) THEN " + Body(table, "source") +
        " ELSE NULL END FROM Attachments AS source WHERE EXISTS(SELECT 1 FROM SyncOutbox AS changed " +
        "WHERE changed.TableName='Entries' AND changed.Sequence>$after AND changed.Sequence<=$through AND changed.RecordKey=json_array(source.EntryId));";

    private static string Name(SyncTable table, string action) => "zsync_" + table.Name + "_" + action;

    private static string Key(SyncTable table, string alias) => "json_array(" +
        string.Join(',', table.PrimaryKey.Select(column => alias + "." + Identifier(column))) + ")";

    private static string Body(SyncTable table, string alias) => "json_object(" + string.Join(',',
        table.Columns.SelectMany(column => new[] { Literal(column.Name), Cell(column, alias) })) + ")";

    private static string Cell(SyncColumn column, string alias)
    {
        var cell = alias + "." + Identifier(column.Name);
        return column.Kind == "blob" ? "CASE WHEN " + cell + " IS NULL THEN NULL ELSE json_object('$blobHex',hex(" + cell + ")) END" : cell;
    }

    private static string Literal(string name) { ValidateName(name); return "'" + name + "'"; }
    private static string Identifier(string name) { ValidateName(name); return "\"" + name + "\""; }

    private static void ValidateName(string name)
    {
        if (name.Length is < 1 or > 128 || !char.IsAsciiLetter(name[0]) ||
            name.Any(character => !char.IsAsciiLetterOrDigit(character) && character != '_'))
            throw new ArgumentException("Only reviewed relational identifiers are allowed.");
    }
}
