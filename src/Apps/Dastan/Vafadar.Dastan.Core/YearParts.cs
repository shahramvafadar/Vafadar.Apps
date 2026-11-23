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
    /// none is drawn always. Somebody is drawn when a line that is drawn reaches them – and the one the tree is
    /// about always.
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
        var reached = family.Parentage.Concat(drawn).SelectMany(edge => new[] { edge.From.Id, edge.To.Id }).ToHashSet();
        return family with
        {
            Partnerships = drawn,
            Members = [.. family.Members.Where(member => reached.Contains(member.Entity.Id) || member.Entity.Id == around)],
            Years = years,
            LeftOut = partnerships.LeftOut,
            WithoutTime = family.Parentage.Length + partnerships.WithoutTime.Length,
            Possibly = [.. partnerships.Possibly.Select(edge => edge.Relation.Id)],
        };
    }
}
