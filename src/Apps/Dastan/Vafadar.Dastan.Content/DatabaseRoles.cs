using System.Text.RegularExpressions;
using Npgsql;
using Vafadar.Dastan.Data;
using Vafadar.Dastan.People;
using Vafadar.Dastan.Store;

namespace Vafadar.Dastan.Content;

/// <summary>
/// What a program may do in the database, by the role it connects as (ADR 0015). The role that makes the
/// tables owns them and may do everything; the site and the tool of the editors each get a role of their own
/// that may do what the program does and no more – so that a fault of the site cannot read or change the work
/// of the editors, and a fault of the tool cannot reach what belongs to visitors. The roles are made by who
/// runs the database, with passwords this program never sees; the command grants.
/// </summary>
public static partial class DatabaseRoles
{
    /// <summary>The programs a role can be for.</summary>
    public static IReadOnlyList<string> Programs { get; } = ["site", "tool"];

    /// <summary>Returns whether a text can be the name of a role: lower-case letters, digits and underscores, as PostgreSQL writes a name without quotes.</summary>
    public static bool IsName(string? role) => role is not null && NameForm().IsMatch(role);

    /// <summary>
    /// Gives a role what its program needs, takes from it what it does not, and says the same for the tables
    /// that the role of this connection makes later. Asked again, it changes nothing.
    /// </summary>
    /// <param name="connection">The connection of the database, as the role that owns the tables.</param>
    /// <param name="program"><c>site</c> or <c>tool</c>.</param>
    /// <param name="role">The role, which exists.</param>
    public static void Grant(string connection, string program, string role)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connection);
        if (!Programs.Contains(program) || !IsName(role))
        {
            throw new ArgumentException("A role is granted for the site or for the tool, and is named in lower-case letters, digits and underscores.", nameof(role));
        }

        var (personal, editorial, published, delivery) = (PeopleStore.Schema, EditorialStore.Schema, PublishedRevision.Schema, Delivery.Schema);
        var all = new[] { personal, editorial, published, delivery };

        // What each program may do with the tables of each part; nothing means the part is closed to it.
        var may = program == "site"
            ? new Dictionary<string, string>
            {
                [personal] = "SELECT, INSERT, UPDATE, DELETE",   // accounts and what belongs to them
                [published] = "SELECT",                          // what was released, and nothing of how it came to be
                [delivery] = "SELECT, INSERT",                   // a report or a notice is written, and counted
            }
            : new Dictionary<string, string>
            {
                [editorial] = "SELECT, INSERT, UPDATE, DELETE",  // the work of the editors
                [published] = "SELECT, INSERT",                  // a revision and a word about it are added, never changed
                [delivery] = "SELECT, UPDATE",                   // a report is read and noted as looked at
            };

        using var database = new NpgsqlConnection(connection);
        database.Open();
        using var step = database.BeginTransaction();
        foreach (var schema in all)
        {
            // First nothing, then what is meant: a role that had more before has it no longer.
            Run(database, $"""REVOKE ALL ON SCHEMA {schema} FROM "{role}" """);
            Run(database, $"""REVOKE ALL ON ALL TABLES IN SCHEMA {schema} FROM "{role}" """);
            Run(database, $"""REVOKE ALL ON ALL SEQUENCES IN SCHEMA {schema} FROM "{role}" """);
            Run(database, $"""ALTER DEFAULT PRIVILEGES IN SCHEMA {schema} REVOKE ALL ON TABLES FROM "{role}" """);
            Run(database, $"""ALTER DEFAULT PRIVILEGES IN SCHEMA {schema} REVOKE ALL ON SEQUENCES FROM "{role}" """);
            if (!may.TryGetValue(schema, out var rights))
            {
                continue;
            }

            Run(database, $"""GRANT USAGE ON SCHEMA {schema} TO "{role}" """);
            Run(database, $"""GRANT {rights} ON ALL TABLES IN SCHEMA {schema} TO "{role}" """);
            Run(database, $"""ALTER DEFAULT PRIVILEGES IN SCHEMA {schema} GRANT {rights} ON TABLES TO "{role}" """);
            if (rights.Contains("INSERT", StringComparison.Ordinal))
            {
                // A table that numbers its rows takes the number from a sequence.
                Run(database, $"""GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA {schema} TO "{role}" """);
                Run(database, $"""ALTER DEFAULT PRIVILEGES IN SCHEMA {schema} GRANT USAGE, SELECT ON SEQUENCES TO "{role}" """);
            }

            // Which changes of form a part has is read by the program that starts, and written by nobody but the command.
            Run(database, $"""REVOKE INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA {schema} FROM "{role}" """, onlyIfHistory: schema);
        }

        // Mail is handed over and marked as sent by whichever program tells somebody something.
        Run(database, $"""GRANT SELECT, INSERT, UPDATE ON {delivery}."Mail" TO "{role}" """);
        step.Commit();
    }

    private static void Run(NpgsqlConnection database, string sql, string? onlyIfHistory = null)
    {
        using var command = database.CreateCommand();
        if (onlyIfHistory is not null)
        {
            // Only the table of the changes of form, where the part has one.
            command.CommandText = $"""select to_regclass('{onlyIfHistory}."__EFMigrationsHistory"') is not null""";
            if (!(bool)command.ExecuteScalar()!)
            {
                return;
            }

            sql = sql.Replace($"ALL TABLES IN SCHEMA {onlyIfHistory}", $"""{onlyIfHistory}."__EFMigrationsHistory" """, StringComparison.Ordinal);
        }

        command.CommandText = sql;
        command.ExecuteNonQuery();
    }

    [GeneratedRegex(@"^[a-z_][a-z0-9_]{0,62}\z")]
    private static partial Regex NameForm();
}
