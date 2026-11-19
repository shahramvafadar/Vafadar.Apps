using System.Collections.Immutable;
using System.Text.Json.Nodes;
using Npgsql;
using Vafadar.Dastan.Data;

namespace Vafadar.Dastan.Web.Site;

/// <summary>A released revision of a collection as the database gives it to the site.</summary>
/// <param name="Collection">The identifier of the collection.</param>
/// <param name="Number">The number of the revision.</param>
/// <param name="Json">The file of the revision.</param>
/// <param name="Translations">The translations published with it, each as its document.</param>
public sealed record ReleasedRevision(string Collection, int Number, string Json, ImmutableArray<string> Translations);

/// <summary>
/// The published revisions the site shows, read from the database (ADR 0015): for every collection the
/// revision that was released last, unless the collection was withdrawn since. The site reads the schema
/// <c>published</c> and nothing else of the editors, and only reads.
/// </summary>
public static class PublishedCollections
{
    /// <summary>
    /// Reads the released comparisons, in the order of their identifiers: for each the text of its last release,
    /// unless it was withdrawn since. Each as its file and the documents of its translations.
    /// </summary>
    /// <param name="connection">The connection of the database.</param>
    /// <exception cref="InvalidOperationException">The database has no such table, or a comparison is not the text it was released as.</exception>
    public static ImmutableArray<(string Id, string Json, ImmutableArray<string> Translations)> ReadComparisons(string connection)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connection);
        var released = ImmutableArray.CreateBuilder<(string, string, ImmutableArray<string>)>();
        try
        {
            using var database = new NpgsqlConnection(connection);
            database.Open();
            using var command = database.CreateCommand();
            command.CommandText = $"""
                select said."ComparisonId", said."Json"::text, said."Translations"::text, said."Hash"
                from (select distinct on ("ComparisonId") * from {PublishedRevision.Schema}."Comparisons" order by "ComparisonId", "Number" desc) said
                where said."Hash" is not null
                order by said."ComparisonId"
                """;
            using var rows = command.ExecuteReader();
            while (rows.Read())
            {
                var (id, json, translations, hash) = (rows.GetString(0), rows.GetString(1), rows.GetString(2), rows.GetString(3));
                if (!string.Equals(PublishedRevision.HashOf(json, translations), hash, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException($"The comparison {id} is not the text it was released as: its hash does not match. The site does not show it.");
                }

                released.Add((id, json, [.. JsonNode.Parse(translations)!.AsObject().Select(translation => translation.Value!.ToJsonString())]));
            }
        }
        catch (PostgresException missing) when (missing.SqlState is PostgresErrorCodes.UndefinedTable or PostgresErrorCodes.InvalidSchemaName)
        {
            throw new InvalidOperationException("The database has no tables of published revisions. Run \"dotnet Vafadar.Dastan.Content.dll database migrate --database env\" with the connection in DASTAN_DATABASE, then start the site again.", missing);
        }

        return released.ToImmutable();
    }

    /// <summary>Reads the released revisions, in the order of the identifiers of their collections.</summary>
    /// <param name="connection">The connection of the database.</param>
    /// <exception cref="InvalidOperationException">The database has no published revisions to read, or a revision is not the text it was published as.</exception>
    public static ImmutableArray<ReleasedRevision> Read(string connection)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connection);
        var released = ImmutableArray.CreateBuilder<ReleasedRevision>();
        try
        {
            using var database = new NpgsqlConnection(connection);
            database.Open();
            using var command = database.CreateCommand();

            // The last word about a collection counts: a release names a revision, a withdrawal names none.
            command.CommandText = $"""
                select revision."CollectionId", revision."Number", revision."Json"::text, revision."Translations"::text, revision."Hash"
                from (select distinct on ("CollectionId") "CollectionId", "Number" from {PublishedRevision.Schema}."Releases" order by "CollectionId", "Id" desc) said
                join {PublishedRevision.Schema}."Revisions" revision on revision."CollectionId" = said."CollectionId" and revision."Number" = said."Number"
                order by revision."CollectionId"
                """;
            using var rows = command.ExecuteReader();
            while (rows.Read())
            {
                var (collection, number, json, translations, hash) = (rows.GetString(0), rows.GetInt32(1), rows.GetString(2), rows.GetString(3), rows.GetString(4));
                if (!string.Equals(PublishedRevision.HashOf(json, translations), hash, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException($"Revision {number} of {collection} is not the text it was published as: its hash does not match. The site does not show it.");
                }

                released.Add(new ReleasedRevision(collection, number, json, [.. JsonNode.Parse(translations)!.AsObject().Select(translation => translation.Value!.ToJsonString())]));
            }
        }
        catch (PostgresException missing) when (missing.SqlState is PostgresErrorCodes.UndefinedTable or PostgresErrorCodes.InvalidSchemaName)
        {
            throw new InvalidOperationException("The database has no tables of published revisions. Run \"dotnet Vafadar.Dastan.Content.dll database migrate --database env\" with the connection in DASTAN_DATABASE, then start the site again.", missing);
        }

        return released.ToImmutable();
    }
}
