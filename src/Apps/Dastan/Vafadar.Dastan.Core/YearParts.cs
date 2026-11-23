using System.Collections.Immutable;
using Vafadar.Temporal;

namespace Vafadar.Dastan.Core;

/// <summary>
/// What is stated about a name, told apart by a stretch of years (D-084, delivery plan 25.3): what held in the
/// years, what states no time – shown apart and never hidden – and how much states another time and is left
/// out. One rule for the page of a name and its family tree, the same the connections and the pictures keep.
/// </summary>
/// <typeparam name="T">What is told apart: a statement, a connection.</typeparam>
/// <param name="Years">The stretch of years, or nothing: then everything is <see cref="In"/>.</param>
/// <param name="In">What states a time that reaches the years – or everything, without years.</param>
/// <param name="WithoutTime">What states no time. Empty without years.</param>
/// <param name="LeftOut">How many state another time.</param>
/// <param name="Possibly">Those of <see cref="In"/> of which it is not sure that they held in the years.</param>
public sealed record YearParts<T>(YearSpan? Years, ImmutableArray<T> In, ImmutableArray<T> WithoutTime, int LeftOut, ImmutableHashSet<T> Possibly)
    where T : notnull
{
    /// <summary>Gets a value indicating whether nothing is shown.</summary>
    public bool IsEmpty => In.IsEmpty && WithoutTime.IsEmpty;
}

/// <summary>Tells what is stated apart by a stretch of years.</summary>
public static class YearParts
{
    /// <summary>Tells items apart by the time each states.</summary>
    /// <typeparam name="T">What is told apart.</typeparam>
    /// <param name="items">The items, in the order they are shown in.</param>
    /// <param name="time">The time an item states, or nothing.</param>
    /// <param name="years">The stretch of years, or nothing for every year.</param>
    /// <param name="present">The year of the common era a depth of time is counted back from.</param>
    public static YearParts<T> Of<T>(IEnumerable<T> items, Func<T, TemporalExpression?> time, YearSpan? years, int? present)
        where T : notnull
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(time);
        if (years is not { } asked)
        {
            return new YearParts<T>(null, [.. items], [], 0, []);
        }

        var held = ImmutableArray.CreateBuilder<T>();
        var undated = ImmutableArray.CreateBuilder<T>();
        var possibly = ImmutableHashSet.CreateBuilder<T>();
        var leftOut = 0;
        foreach (var item in items)
        {
            if (time(item) is not { } stated)
            {
                undated.Add(item);
                continue;
            }

            switch (YearCover.Of(stated, asked, present))
            {
                case Holding.No:
                    leftOut++;
                    break;
                case Holding.Possibly:
                    possibly.Add(item);
                    held.Add(item);
                    break;
                default:
                    held.Add(item);
                    break;
            }
        }

        return new YearParts<T>(asked, held.ToImmutable(), undated.ToImmutable(), leftOut, possibly.ToImmutable());
    }

    /// <summary>
    /// Returns a family tree in a stretch of years. Descent states no time and is drawn at every time: a parent
    /// is a parent. A partnership that states a time is drawn only when it held in the years; one that states
    /// none is drawn always. Somebody is drawn when the lines that are drawn lead to them from the one the tree is
    /// about – a partner whose partnership is left out is still drawn as the parent of a child that is, and
    /// not with a family of their own beside the tree.
    /// </summary>
    /// <param name="family">The family tree.</param>
    /// <param name="around">The entity the tree is about, or nothing for the families of a collection.</param>
    /// <param name="years">The stretch of years, or nothing for every year.</param>
    /// <param name="present">The year of the common era a depth of time is counted back from.</param>
    public static FamilyView Within(this FamilyView family, EntityId? around, YearSpan? years, int? present)
    {
        ArgumentNullException.ThrowIfNull(family);
        if (years is null)
        {
            return family;
        }

        var partnerships = Of(family.Partnerships, edge => edge.Relation.Time, years, present);
        ImmutableArray<Edge> drawn = [.. family.Partnerships.Where(edge => partnerships.In.Contains(edge) || partnerships.WithoutTime.Contains(edge))];
        var parentage = family.Parentage;
        HashSet<EntityId> reached;
        if (around is { } centre)
        {
            // What the lines that are drawn lead to from the one the tree is about. What is left beside it – a
            // family reached only through a partnership of another time – is not part of the tree in these years.
            reached = [centre];
            for (var grown = true; grown;)
            {
                grown = false;
                foreach (var edge in parentage.Concat(drawn))
                {
                    if (reached.Contains(edge.From.Id) != reached.Contains(edge.To.Id))
                    {
                        reached.Add(edge.From.Id);
                        reached.Add(edge.To.Id);
                        grown = true;
                    }
                }
            }

            parentage = [.. parentage.Where(edge => reached.Contains(edge.From.Id))];
            drawn = [.. drawn.Where(edge => reached.Contains(edge.From.Id))];
        }
        else
        {
            reached = [.. parentage.Concat(drawn).SelectMany(edge => new[] { edge.From.Id, edge.To.Id })];
        }

        return family with
        {
            Parentage = parentage,
            Partnerships = drawn,
            Members = [.. family.Members.Where(member => reached.Contains(member.Entity.Id))],
            Years = years,
            LeftOut = partnerships.LeftOut,
            WithoutTime = parentage.Count(edge => edge.Relation.Time is null) + drawn.Count(edge => edge.Relation.Time is null),
            Possibly = [.. partnerships.Possibly.Select(edge => edge.Relation.Id)],
        };
    }
}
