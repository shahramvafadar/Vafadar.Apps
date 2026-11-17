using System.Collections;
using System.Reflection;
using Vafadar.Dastan.Core;

namespace Vafadar.Dastan.Testing;

/// <summary>
/// Finds out what a projection discloses. It asks the projection every question it can answer, collects every text
/// that can be reached from the answers – names, summaries, identifiers, file names, addresses, locators – and
/// compares that with the texts that belong only to records the visitor may not see. The check does not know the
/// views: a view added tomorrow is covered as soon as it is asked here.
/// </summary>
internal static class Disclosure
{
    /// <summary>Returns every text reachable from every answer of the projection.</summary>
    public static HashSet<string> Of(Projection projection, Catalogue catalogue)
    {
        var texts = new Collector();
        texts.Add(projection.Entities, projection.Relations, projection.Steps, projection.Sources, projection.Media, projection.References);
        texts.Add(projection.Overview(), projection.Story()!, projection.Lanes()!, projection.Family(), projection.Gallery(), projection.Timeline(), projection.Revisions());

        // Questions are asked about every record of the collection, visible or not: an answer about a hidden record
        // has to be the same as about one that does not exist, so it adds nothing.
        var everybody = catalogue.Entities.Select(entity => entity.Id).ToList();
        foreach (var entity in catalogue.Entities)
        {
            texts.Add(
                projection.Find(entity.Id),
                projection.Entity(entity.Id),
                projection.Connections(entity.Id),
                projection.Connections(entity.Id, everybody),
                projection.Family(entity.Id),
                projection.Family(entity.Id, 0, 0),
                projection.Family(entity.Id, 1, 1),
                projection.Family(entity.Id, 9, 9),
                projection.Family(entity.Id, 9, 9, 9),
                projection.Family(entity.Id, 1, 0, 1),
                projection.Gallery(entity.Id),
                projection.Evidence(entity.Id));
            foreach (var language in entity.Label.Languages)
            {
                var name = entity.Label.In(language)!.Value.Value;
                texts.Add(projection.Search(name, 100), projection.Search(name[..1], 100));
            }
        }

        foreach (var relation in catalogue.Relations)
        {
            texts.Add(projection.Find(relation.Id), projection.Evidence(relation.Id));
        }

        foreach (var storyEvent in catalogue.Events)
        {
            texts.Add(projection.Find(storyEvent.Id), projection.Gallery(storyEvent.Id), projection.Evidence(storyEvent.Id));
            foreach (var entity in everybody)
            {
                texts.Add(projection.Evidence(storyEvent.Id, entity));
            }
        }

        foreach (var item in catalogue.Media)
        {
            texts.Add(projection.Find(item.Id), projection.MediaItem(item.Id), projection.Evidence(item.Id));
        }

        return texts.Found;
    }

    /// <summary>
    /// Returns the texts a visitor must not meet: everything that belongs to a hidden record and to no visible one.
    /// A label two relations share ("Father of") is not secret when one of them is visible; an identifier always is.
    /// </summary>
    public static HashSet<string> Secrets(Projection projection, Catalogue catalogue)
    {
        var visible = new Collector();
        visible.Add(projection.Entities, projection.Relations, projection.Steps, projection.Sources, projection.Media);
        visible.Add(catalogue.Info);

        // A reference is what the visitor may know of a work that may not be shown: its catalogue record. What
        // the record leaves out – the address of the image, its text alternative, its sources – stays a secret.
        visible.Add(projection.References);

        var hidden = new Collector();
        hidden.Add(
            catalogue.Entities.Where(entity => projection.Find(entity.Id) is null),
            catalogue.Relations.Where(relation => projection.Find(relation.Id) is null),
            catalogue.Events.Where(storyEvent => projection.Find(storyEvent.Id) is null),
            catalogue.Sources.Where(source => !projection.Sources.Contains(source)),
            catalogue.Media.Where(item => projection.Find(item.Id) is null));

        hidden.Found.ExceptWith(visible.Found);
        return hidden.Found;
    }

    /// <summary>Returns the secrets the projection discloses; empty when it keeps them.</summary>
    public static List<string> Leaks(Projection projection, Catalogue catalogue)
    {
        var disclosed = Of(projection, catalogue);
        return [.. Secrets(projection, catalogue).Where(disclosed.Contains).Order(StringComparer.Ordinal)];
    }

    private sealed class Collector
    {
        private readonly HashSet<object> _visited = new(ReferenceEqualityComparer.Instance);

        public HashSet<string> Found { get; } = new(StringComparer.Ordinal);

        public void Add(params object?[] values)
        {
            foreach (var value in values)
            {
                Visit(value);
            }
        }

        private void Visit(object? value)
        {
            switch (value)
            {
                case null:
                    return;
                case string text:
                    Found.Add(text);
                    return;
                case Uri address:
                    Found.Add(address.OriginalString);
                    return;
                case LocalizedText localized:
                    foreach (var language in localized.Languages)
                    {
                        Found.Add(localized.In(language)!.Value.Value);
                    }

                    return;
            }

            var type = value.GetType();
            if (type.IsPrimitive || type.IsEnum || value is decimal or DateOnly or DateTime or DateTimeOffset or TimeSpan or Type)
            {
                return;
            }

            if (!type.IsValueType && !_visited.Add(value))
            {
                return;
            }

            if (value is IEnumerable sequence)
            {
                // An immutable array that was never assigned has nothing to enumerate.
                if (type.GetProperty("IsDefault")?.GetValue(value) is true)
                {
                    return;
                }

                foreach (var item in sequence)
                {
                    Visit(item);
                }

                return;
            }

            foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (property.GetIndexParameters().Length == 0 && property.Name != "EqualityContract")
                {
                    Visit(property.GetValue(value));
                }
            }

            // Tuples keep their values in fields.
            foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                Visit(field.GetValue(value));
            }
        }
    }
}
