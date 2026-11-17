using System.Collections.Immutable;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;

namespace Vafadar.Dastan.Store;

/// <summary>A collection of the store at a glance.</summary>
/// <param name="Id">The identifier of the collection.</param>
/// <param name="Records">How many records it has in each group, in the order of the file.</param>
/// <param name="Statements">How many statements it has.</param>
/// <param name="Waiting">How many of them still wait for a review.</param>
/// <param name="Languages">The languages it is translated into.</param>
/// <param name="Revisions">How many revisions the store published of it.</param>
/// <param name="UpdatedAt">When it was last changed.</param>
public sealed record CollectionSummary(
    string Id,
    ImmutableArray<(string Group, int Count)> Records,
    int Statements,
    int Waiting,
    ImmutableArray<string> Languages,
    int Revisions,
    DateTime UpdatedAt);

/// <summary>A record of the store without its content.</summary>
/// <param name="Group">The group of the record.</param>
/// <param name="Id">Its identifier.</param>
/// <param name="Label">Its name in the editors' words: the label of a record, the title of a source.</param>
/// <param name="ReviewStatus">The state of its review, for a statement.</param>
/// <param name="Version">How often it was written.</param>
/// <param name="UpdatedAt">When it was last changed.</param>
/// <param name="UpdatedBy">Who changed it last.</param>
public sealed record RecordInfo(string Group, string Id, string Label, string? ReviewStatus, int Version, DateTime UpdatedAt, string UpdatedBy);

/// <summary>A record of the store with its content, as it is written in the file of a collection.</summary>
/// <param name="Info">What the store knows about the record.</param>
/// <param name="Body">The record.</param>
public sealed record RecordView(RecordInfo Info, JsonObject Body);

// What the tools of the editors ask the store (step 11.4). Nothing here changes the store.
public sealed partial class EditorialStore
{
    /// <summary>Returns a collection at a glance, or nothing for a collection the store does not hold.</summary>
    public CollectionSummary? Summary(string collectionId)
    {
        var collection = _context.Collections.AsNoTracking().SingleOrDefault(item => item.Id == collectionId);
        if (collection is null)
        {
            return null;
        }

        var counts = _context.Records.AsNoTracking().Where(record => record.CollectionId == collectionId)
            .GroupBy(record => record.Group).Select(group => new { Group = group.Key, Count = group.Count() }).ToDictionary(group => group.Group, group => group.Count);
        var statements = Statements(collectionId);
        return new CollectionSummary(
            collection.Id,
            [.. Groups.Select(group => (group, counts.GetValueOrDefault(group)))],
            statements.Length,
            statements.Count(statement => !Settled.Contains(statement.ReviewStatus)),
            [.. _context.Translations.AsNoTracking().Where(translation => translation.CollectionId == collectionId).OrderBy(translation => translation.Language).Select(translation => translation.Language)],
            _context.Revisions.AsNoTracking().Count(revision => revision.CollectionId == collectionId),
            collection.UpdatedAt);
    }

    /// <summary>Returns the records of a group of a collection, in the editors' order; empty for an unknown group or collection.</summary>
    public ImmutableArray<RecordInfo> Records(string collectionId, string group) =>
    [
        .. _context.Records.AsNoTracking().Where(record => record.CollectionId == collectionId && record.Group == group).OrderBy(record => record.Position).AsEnumerable().Select(Info),
    ];

    /// <summary>Returns a record with its content, or nothing when the store does not hold it.</summary>
    public RecordView? Record(string collectionId, string group, string recordId)
    {
        var record = _context.Records.AsNoTracking().SingleOrDefault(item => item.CollectionId == collectionId && item.Group == group && item.RecordId == recordId);
        return record is null ? null : new RecordView(Info(record), JsonNode.Parse(record.Body)!.AsObject());
    }

    /// <summary>Returns the reading stages of a collection, in their order; empty for a collection the store does not hold.</summary>
    public ImmutableArray<(int Level, string Label)> Stages(string collectionId)
    {
        var collection = _context.Collections.AsNoTracking().SingleOrDefault(item => item.Id == collectionId);
        return collection is null
            ? []
            : [.. JsonNode.Parse(collection.Header)!["spoilerPolicy"]!["levels"]!.AsArray().Select(stage => ((int)stage!["level"]!, (string)stage["label"]!))];
    }

    private static RecordInfo Info(StoredRecord record)
    {
        var body = JsonNode.Parse(record.Body)!;
        var label = (body["label"] ?? body["title"]) is JsonValue named && named.TryGetValue<string>(out var text) ? text : record.RecordId;
        return new RecordInfo(record.Group, record.RecordId, label, record.ReviewStatus, record.Version, record.UpdatedAt, record.UpdatedBy);
    }
}
