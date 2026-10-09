using System.Collections.Specialized;
using System.ComponentModel;
using Vafadar.Zanance.App.Presentation;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Complete presentation snapshots retain failure/rebinding semantics without per-row notifications.</summary>
public sealed class SnapshotCollectionTests
{
    [Fact, Trait("AT", "AT-98")]
    public void A_large_replacement_publishes_complete_ordered_items_in_one_reset()
    {
        var collection = new SnapshotCollection<int> { -1 };
        var expected = Enumerable.Range(0, 200).ToArray();
        var events = new List<NotifyCollectionChangedAction>();
        collection.CollectionChanged += (_, e) =>
        {
            events.Add(e.Action);
            Assert.Equal(expected, collection);
        };

        collection.ReplaceAll(expected);

        Assert.Equal(new[] { NotifyCollectionChangedAction.Reset }, events);
    }

    [Fact, Trait("AT", "AT-98")]
    public void Source_failure_preserves_the_existing_snapshot_without_notifications()
    {
        var collection = new SnapshotCollection<int> { 1, 2 };
        var notifications = 0;
        collection.CollectionChanged += (_, _) => notifications++;
        ((INotifyPropertyChanged)collection).PropertyChanged += (_, _) => notifications++;
        IEnumerable<int> Failing() { yield return 3; throw new IOException("Fictitious snapshot read failure"); }

        Assert.Throws<IOException>(() => collection.ReplaceAll(Failing()));

        Assert.Equal(new[] { 1, 2 }, collection);
        Assert.Equal(0, notifications);
    }

    [Fact, Trait("AT", "AT-98")]
    public void A_replacement_notifies_count_and_indexer_before_reset()
    {
        var collection = new SnapshotCollection<int>();
        var notifications = new List<string>();
        ((INotifyPropertyChanged)collection).PropertyChanged += (_, e) => notifications.Add(e.PropertyName!);
        collection.CollectionChanged += (_, e) => notifications.Add(e.Action.ToString());

        collection.ReplaceAll([4, 5]);

        Assert.Equal(new[] { "Count", "Item[]", "Reset" }, notifications);
    }

    [Fact, Trait("AT", "AT-98")]
    public void Replacing_from_the_collection_itself_keeps_the_items()
    {
        var collection = new SnapshotCollection<int> { 1, 2 };
        collection.ReplaceAll(collection);
        Assert.Equal(new[] { 1, 2 }, collection);
    }

    [Fact, Trait("AT", "AT-98")]
    public void An_empty_snapshot_removes_all_rows_with_one_reset()
    {
        var collection = new SnapshotCollection<int> { 1, 2 };
        var events = new List<NotifyCollectionChangedAction>();
        collection.CollectionChanged += (_, e) => events.Add(e.Action);

        collection.ReplaceAll([]);

        Assert.Empty(collection);
        Assert.Equal(new[] { NotifyCollectionChangedAction.Reset }, events);
    }

    [Fact, Trait("AT", "AT-98")]
    public void Equal_records_still_publish_fresh_objects_for_theme_dependent_bindings()
    {
        var original = new Row("Same");
        var replacement = new Row("Same");
        var collection = new SnapshotCollection<Row> { original };
        var notifications = 0;
        collection.CollectionChanged += (_, _) => notifications++;

        collection.ReplaceAll([replacement]);

        Assert.Same(replacement, Assert.Single(collection));
        Assert.Equal(1, notifications);
    }

    [Fact, Trait("AT", "AT-98")]
    public void A_nested_replacement_during_multiple_observers_is_rejected()
    {
        var collection = new SnapshotCollection<int>();
        collection.CollectionChanged += (_, _) => Assert.Throws<InvalidOperationException>(() => collection.ReplaceAll([2]));
        collection.CollectionChanged += (_, _) => Assert.Equal(new[] { 1 }, collection);

        collection.ReplaceAll([1]);

        Assert.Equal(new[] { 1 }, collection);
    }

    [Fact, Trait("AT", "AT-98")]
    public void The_snapshot_source_is_materialized_once()
    {
        var collection = new SnapshotCollection<int>();
        var enumerations = 0;
        IEnumerable<int> Once() { Assert.Equal(1, ++enumerations); yield return 9; }

        collection.ReplaceAll(Once());

        Assert.Equal(9, Assert.Single(collection));
    }

    private sealed record Row(string Text);
}
