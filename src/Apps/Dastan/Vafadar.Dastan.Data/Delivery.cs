using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Vafadar.Dastan.Data;

/// <summary>
/// The part of the database for what comes in from outside and waits for somebody (ADR 0015): the reports of
/// visitors, the notices about content, and the mail that waits to be sent. The site writes there, the tool
/// of the editors and the command of who runs the site read and settle. The tables are made by <c>database migrate</c> (<see cref="Migrate"/>), never by a program that starts.
/// </summary>
public static class Delivery
{
    /// <summary>The schema of the database.</summary>
    public const string Schema = "delivery";

    /// <summary>Makes the tables of this part in a database, or brings them to the form this program knows.</summary>
    public static void Migrate(string connection)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connection);
        using var context = new DeliveryContext(Options(connection));
        context.Database.Migrate();
    }

    // The history of the changes of form lies in the schema itself, as each part of the database keeps its own.
    internal static DbContextOptions<DeliveryContext> Options(string connection) =>
        new DbContextOptionsBuilder<DeliveryContext>().UseNpgsql(connection, npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", Schema)).Options;

    /// <summary>Opens a connection; a database that cannot be reached says so in words an operator can act on.</summary>
    internal static NpgsqlConnection Open(string connection)
    {
        var database = new NpgsqlConnection(connection);
        try
        {
            database.Open();
            return database;
        }
        catch
        {
            database.Dispose();
            throw;
        }
    }

    /// <summary>Makes a command of a text and its values, by their names.</summary>
    internal static NpgsqlCommand Command(NpgsqlConnection database, string text, params (string Name, object? Value)[] values)
    {
        var command = database.CreateCommand();
        command.CommandText = text;
        foreach (var (name, value) in values)
        {
            command.Parameters.AddWithValue(name, value ?? DBNull.Value);
        }

        return command;
    }

    /// <summary>
    /// Takes the lock of a box for the transaction: who counts what waits before adding to it is alone until
    /// the transaction ends, so that a full box stays full however many write at the same moment.
    /// </summary>
    internal static void Lock(NpgsqlConnection database, string box)
    {
        using var command = Command(database, "select pg_advisory_xact_lock(hashtextextended(@box, 0))", ("box", "delivery:" + box));
        command.ExecuteNonQuery();
    }

    /// <summary>
    /// Returns nothing when the tables of this part are there, else what an operator has to do. Asked once by a
    /// program that starts, so that a missing table is said before a visitor meets it.
    /// </summary>
    public static string? Missing(string connection)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connection);
        try
        {
            using var database = Open(connection);
            using var command = Command(database, $"""select 1 from {Schema}."Reports" limit 0; select 1 from {Schema}."Notices" limit 0; select 1 from {Schema}."Mail" limit 0""");
            command.ExecuteNonQuery();
            return null;
        }
        catch (PostgresException missing) when (missing.SqlState is PostgresErrorCodes.UndefinedTable or PostgresErrorCodes.InvalidSchemaName)
        {
            return "The database has no tables for reports, notices and mail. Run \"dotnet Vafadar.Dastan.Content.dll database migrate --database env\" with the connection in DASTAN_DATABASE, then start again.";
        }
    }
}
