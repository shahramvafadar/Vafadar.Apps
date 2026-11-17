using System.Globalization;
using System.Text.RegularExpressions;

namespace Vafadar.Dastan.Data;

/// <summary>
/// The smallest files of each kind that the site accepts, for a writer to start from (D-042). Every text in them
/// says what belongs in its place; none of it is content. A test reads each template with the readers of the
/// site, so a template cannot fall behind its format.
/// </summary>
public static partial class ContentTemplates
{
    /// <summary>
    /// Returns the file of a new collection: one work, one story in it, one character, one place, one relation
    /// and one event, with the source they cite.
    /// </summary>
    /// <param name="id">The identifier of the collection: lower-case letters, digits and single hyphens.</param>
    /// <param name="today">The day the file is written; sources say when they were looked at.</param>
    public static string Collection(string id, DateOnly today) => Fill(CollectionText, id, today);

    /// <summary>
    /// Returns the file of a new collection that tells no story: it is about a company, a club, a person, a
    /// period – here an organisation with one person, what is said between the two with its time, and one
    /// statement with a date.
    /// </summary>
    /// <param name="id">The identifier of the collection: lower-case letters, digits and single hyphens.</param>
    /// <param name="today">The day the file is written; sources say when they were looked at.</param>
    public static string Subject(string id, DateOnly today) => Fill(SubjectText, id, today);

    /// <summary>Returns the file of a new comparison: two lanes, each with one period, and one event.</summary>
    /// <param name="id">The identifier of the comparison: lower-case letters, digits and single hyphens.</param>
    /// <param name="today">The day the file is written.</param>
    public static string Comparison(string id, DateOnly today) => Fill(ComparisonText, id, today);

    private static string Fill(string template, string id, DateOnly today)
    {
        ArgumentNullException.ThrowIfNull(id);
        if (!Name().IsMatch(id))
        {
            throw new ArgumentException($"\"{id}\" is not an identifier: lower-case letters, digits and single hyphens between them.", nameof(id));
        }

        return template
            .Replace("{{id}}", id, StringComparison.Ordinal)
            .Replace("{{today}}", today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), StringComparison.Ordinal);
    }

    [GeneratedRegex("^[a-z0-9]+(-[a-z0-9]+)*$", RegexOptions.CultureInvariant)]
    private static partial Regex Name();

    private const string CollectionText = """
        {
          "schemaVersion": "dastan-pilot-0.2",
          "datasetId": "{{id}}",
          "worldId": "world-{{id}}",
          "documentVersion": "0.1",
          "preparedAt": "{{today}}",
          "status": "draft",
          "editionContext": {
            "id": "edition-{{id}}",
            "workId": "work-{{id}}",
            "baseText": "REPLACE: the text or edition the collection follows.",
            "baseUrl": "https://example.org/replace-with-the-address-of-that-text",
            "criticalPrintEdition": "REPLACE: the printed edition it was checked against, or that none was.",
            "scope": "REPLACE: what of the work the collection covers and what it leaves out.",
            "calendarMapping": null,
            "summaryLanguage": "English"
          },
          "spoilerPolicy": {
            "kind": "minimum_reader_progress_level",
            "levels": [
              { "level": 0, "label": "REPLACE: the name of the first reading stage" }
            ],
            "defaultLevel": 0
          },
          "publication": {
            "state": "editorial_sample",
            "localDemoAllowed": true,
            "publicReleaseApproved": false
          },
          "inputContract": {
            "rawDatasetPubliclyServable": false,
            "reviewerFilesPubliclyServable": false
          },
          "sources": [
            {
              "id": "src-{{id}}-text",
              "title": "REPLACE: the title of the source",
              "url": "https://example.org/replace-with-the-address-of-the-source",
              "kind": "primary_literary_text_transcription",
              "accessedAt": "{{today}}",
              "passageLocator": "REPLACE: where in the source the statements are found.",
              "spoilerLevel": 0
            }
          ],
          "entities": [
            {
              "id": "work-{{id}}",
              "type": "Work",
              "label": "REPLACE: the name of the work",
              "summary": "REPLACE: one or two sentences about the work.",
              "sourceIds": ["src-{{id}}-text"],
              "spoilerLevel": 0
            },
            {
              "id": "story-{{id}}",
              "type": "NarrativeArc",
              "label": "REPLACE: the name of the story",
              "summary": "REPLACE: one or two sentences that give nothing of the story away.",
              "sourceIds": ["src-{{id}}-text"],
              "spoilerLevel": 0
            },
            {
              "id": "character-first",
              "type": "Character",
              "label": "REPLACE: a name",
              "summary": "REPLACE: who this is, as far as the first reading stage tells.",
              "sourceIds": ["src-{{id}}-text"],
              "spoilerLevel": 0
            },
            {
              "id": "place-first",
              "type": "NarrativePlace",
              "label": "REPLACE: a place",
              "summary": "REPLACE: what this place is in the story.",
              "sourceIds": ["src-{{id}}-text"],
              "spoilerLevel": 0
            }
          ],
          "relations": [
            {
              "id": "relation-01",
              "from": "story-{{id}}",
              "to": "work-{{id}}",
              "type": "partOf",
              "directionality": "directed",
              "label": "REPLACE: the relation as words, read from the first entity: Part of …",
              "sourceIds": ["src-{{id}}-text"],
              "spoilerLevel": 0,
              "reviewStatus": "source_backed_editorial_review_pending",
              "scope": {
                "kind": "work_structure",
                "storyId": "story-{{id}}",
                "eventIds": [],
                "calendarPeriod": null
              }
            }
          ],
          "events": [
            {
              "id": "event-01",
              "label": "REPLACE: the name of the first step of the telling",
              "summary": "REPLACE: what is told in this step.",
              "order": 1,
              "orderBasis": "narrative",
              "sourceIds": ["src-{{id}}-text"],
              "spoilerLevel": 0,
              "temporalExpression": { "kind": "narrative_position", "calendarMapping": null, "duration": null },
              "reviewStatus": "source_backed_editorial_review_pending",
              "entityAssociations": [
                { "entityId": "character-first", "role": "REPLACE: what the character does here", "presence": "acts_in_selected_scenes" },
                { "entityId": "place-first", "role": "REPLACE: where it happens", "presence": "setting" }
              ]
            }
          ],
          "media": []
        }
        """;

    private const string SubjectText = """
        {
          "schemaVersion": "dastan-pilot-0.2",
          "datasetId": "{{id}}",
          "worldId": "world-{{id}}",
          "documentVersion": "0.1",
          "preparedAt": "{{today}}",
          "status": "draft",
          "subjectId": "subject-{{id}}",
          "language": "en",
          "spoilerPolicy": {
            "kind": "minimum_reader_progress_level",
            "levels": [
              { "level": 0, "label": "REPLACE: the name of the one reading stage – a collection that tells no story has nothing to keep back" }
            ],
            "defaultLevel": 0
          },
          "publication": {
            "state": "editorial_sample",
            "localDemoAllowed": true,
            "publicReleaseApproved": false
          },
          "inputContract": {
            "rawDatasetPubliclyServable": false,
            "reviewerFilesPubliclyServable": false
          },
          "sources": [
            {
              "id": "src-{{id}}-first",
              "title": "REPLACE: the title of the source",
              "url": "https://example.org/replace-with-the-address-of-the-source",
              "kind": "scholarly_reference",
              "accessedAt": "{{today}}",
              "spoilerLevel": 0
            }
          ],
          "entities": [
            {
              "id": "subject-{{id}}",
              "type": "Organisation",
              "label": "REPLACE: the name of what the collection is about",
              "summary": "REPLACE: one or two sentences about it.",
              "sourceIds": ["src-{{id}}-first"],
              "spoilerLevel": 0,
              "dated": [
                {
                  "key": "subject-founded",
                  "label": "REPLACE: what happened, in a few words",
                  "time": { "kind": "occurrence", "at": { "frame": "calendar", "era": "commonEra", "year": 1900 } },
                  "sourceIds": ["src-{{id}}-first"],
                  "spoilerLevel": 0,
                  "reviewStatus": "proposed"
                }
              ]
            },
            {
              "id": "person-first",
              "type": "Person",
              "label": "REPLACE: a name",
              "summary": "REPLACE: who this is.",
              "sourceIds": ["src-{{id}}-first"],
              "spoilerLevel": 0
            }
          ],
          "relations": [
            {
              "id": "relation-01",
              "from": "person-first",
              "to": "subject-{{id}}",
              "type": "memberOf",
              "directionality": "directed",
              "label": "REPLACE: the relation as words, read from the first entity: Member of …",
              "sourceIds": ["src-{{id}}-first"],
              "spoilerLevel": 0,
              "reviewStatus": "source_backed_editorial_review_pending",
              "time": {
                "kind": "period",
                "start": { "state": "known", "at": { "frame": "calendar", "era": "commonEra", "year": 1900 } },
                "end": { "state": "unknown" },
                "continuity": "notAsserted"
              },
              "scope": { "kind": "outside_the_story", "eventIds": [] }
            }
          ],
          "events": [],
          "media": []
        }
        """;

    private const string ComparisonText = """
        {
          "formatVersion": "dastan-comparison-0.1",
          "id": "{{id}}",
          "status": "draft",
          "note": "REPLACE or remove: a remark of the editors; the site does not show it.",
          "frame": "commonEraYears",
          "title": "REPLACE: the title of the comparison",
          "summary": "REPLACE: one or two sentences about what is compared.",
          "show": {
            "from": { "frame": "calendar", "era": "commonEra", "year": 1 },
            "to": { "frame": "calendar", "era": "commonEra", "year": 200 }
          },
          "publication": { "localDemoAllowed": true, "publicReleaseApproved": false },
          "sources": [
            {
              "id": "src-first",
              "title": "REPLACE: the title of the source",
              "url": "https://example.org/replace-with-the-address-of-the-source",
              "kind": "scholarlyReference",
              "accessedAt": "{{today}}"
            }
          ],
          "lanes": [
            {
              "key": "first",
              "label": "REPLACE: the first subject",
              "periods": [
                {
                  "key": "first-period",
                  "label": "REPLACE: what lasted",
                  "time": {
                    "kind": "period",
                    "start": { "state": "known", "at": { "frame": "calendar", "era": "commonEra", "year": 10 } },
                    "end": { "state": "known", "at": { "frame": "calendar", "era": "commonEra", "year": 120 } },
                    "continuity": "notAsserted"
                  },
                  "sourceIds": ["src-first"],
                  "reviewStatus": "proposed"
                }
              ],
              "events": [
                {
                  "key": "first-event",
                  "label": "REPLACE: what happened",
                  "time": { "kind": "occurrence", "at": { "frame": "calendar", "era": "commonEra", "year": 60 } },
                  "sourceIds": ["src-first"],
                  "reviewStatus": "proposed"
                }
              ]
            },
            {
              "key": "second",
              "label": "REPLACE: the second subject",
              "periods": [
                {
                  "key": "second-period",
                  "label": "REPLACE: what lasted",
                  "time": {
                    "kind": "period",
                    "start": { "state": "known", "at": { "frame": "calendar", "era": "commonEra", "year": 40, "approximate": true } },
                    "end": { "state": "known", "at": { "frame": "calendar", "era": "commonEra", "year": 180 } },
                    "continuity": "notAsserted"
                  },
                  "sourceIds": [],
                  "reviewStatus": "proposed"
                }
              ],
              "events": []
            }
          ]
        }
        """;
}
