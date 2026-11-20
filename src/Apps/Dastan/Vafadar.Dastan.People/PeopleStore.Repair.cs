using System.Collections.Immutable;
using Vafadar.Dastan.Core.Personal;
using Microsoft.EntityFrameworkCore;

namespace Vafadar.Dastan.People;

/// <summary>What <see cref="PeopleStore.RepairMemberships"/> found and did.</summary>
/// <param name="CircleMembers">How many accounts were taken out of a circle of somebody they show no accepted relationship with.</param>
/// <param name="TreeMembers">How many accounts were taken out of a tree of somebody they show no accepted relationship with.</param>
/// <param name="TreesWithALoop">The trees in which somebody is their own ancestor. Which link is the wrong one nobody but who knows the family can say: nothing was changed.</param>
public sealed record MembershipRepair(int CircleMembers, int TreeMembers, ImmutableArray<string> TreesWithALoop);

/// <content>
/// What two requests at the same moment could leave behind before every act took its locks (step 20.2,
/// ADR 0022), looked for and – where the rule decides it – put right.
/// </content>
public sealed partial class PeopleStore
{
    /// <summary>
    /// Takes out of circles and trees who is there without an accepted relationship with the owner – as the end
    /// of the last relationship would have – and names the trees in which somebody is their own ancestor. It
    /// can be run as often as wanted: what is in order is left as it is, and a second run finds nothing.
    /// </summary>
    /// <param name="change">Whether to take anything out, or only to say what would be.</param>
    public MembershipRepair RepairMemberships(bool change)
    {
        var circles = _context.CircleMembers.AsNoTracking()
            .Join(_context.Circles.AsNoTracking(), member => member.Circle, circle => circle.Number, (member, circle) => new { circle.Owner, Guest = member.Member })
            .Distinct().ToList();
        var trees = _context.TreeMembers.AsNoTracking()
            .Join(_context.Trees.AsNoTracking(), member => member.Tree, tree => tree.Id, (member, tree) => new { tree.Owner, Guest = member.Account })
            .Distinct().ToList();

        var (fromCircles, fromTrees) = (0, 0);
        foreach (var two in circles.Select(found => (found.Owner, found.Guest)).Union(trees.Select(found => (found.Owner, found.Guest))))
        {
            // Under the lock of the two, as the end of a relationship is: what is decided is decided on what
            // nobody changes meanwhile.
            var (circle, tree) = Guarded([OfTwo(two.Owner, two.Guest)], () =>
            {
                if (Know(two.Owner, two.Guest))
                {
                    return (0, 0);
                }

                var inCircles = _context.CircleMembers.Count(member => member.Member == two.Guest && _context.Circles.Any(circle => circle.Number == member.Circle && circle.Owner == two.Owner));
                var inTrees = _context.TreeMembers.Count(member => member.Account == two.Guest && _context.Trees.Any(tree => tree.Id == member.Tree && tree.Owner == two.Owner));
                if (change)
                {
                    Parted(two.Owner, two.Guest);
                    Save();
                }

                return (inCircles, inTrees);
            });
            (fromCircles, fromTrees) = (fromCircles + circle, fromTrees + tree);
        }

        var links = _context.Kinships.AsNoTracking().ToList();
        var looped = links.GroupBy(link => link.Tree, StringComparer.Ordinal)
            .Where(tree => tree.Where(link => IsParentage(link.Kind)).Any(link => Descends([.. tree.Where(other => other.Number != link.Number)], of: link.To, who: link.From)))
            .Select(tree => tree.Key)
            .Order(StringComparer.Ordinal)
            .ToImmutableArray();
        return new(fromCircles, fromTrees, looped);
    }
}
