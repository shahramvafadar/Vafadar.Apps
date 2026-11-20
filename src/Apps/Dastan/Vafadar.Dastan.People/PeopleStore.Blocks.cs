using Microsoft.EntityFrameworkCore;

namespace Vafadar.Dastan.People;

/// <content>
/// Blocking an account (step 19.6, ADR 0018, PRIV-009): somebody says that another account is to leave them
/// alone. Everything the two showed together ends at once; from then on the blocked account cannot ask for a
/// relationship, tag, invite or find the one that blocked it – and is not told: what it tries reads like an
/// account that is not there. Blocking is one-sided and can be taken back; taking it back restores nothing.
/// </content>
public sealed partial class PeopleStore
{
    /// <summary>How many accounts an account can block.</summary>
    public const int MostBlocks = 200;

    /// <summary>
    /// Blocks another account. Returns whether it is blocked now; an account that is not open, the account
    /// itself, one that is blocked already and a full list block nothing.
    /// </summary>
    public bool Block(string id, string other)
    {
        var name = _context.Visitors.AsNoTracking().Where(visitor => visitor.Id == other && visitor.ClosedAt == null).Select(visitor => visitor.Name).SingleOrDefault();
        return name is not null && id != other && Guarded(OfPair(id, name), () =>
        {
            if (!IsOpen(id) || Blocks(id, other) || _context.Blocks.Count(block => block.By == id) >= MostBlocks)
            {
                return false;
            }

            // What the two showed together, and what waited for an answer between them, is gone – with the
            // circles and the trees they shared and the photographs of one that are said to show the other.
            _context.Relationships.RemoveRange(_context.Relationships.Where(stored => (stored.From == id && stored.To == other) || (stored.From == other && stored.To == id)));
            Parted(id, other);
            var photographs = _context.Photos.AsNoTracking().Where(photo => photo.Owner == id || photo.Owner == other).Select(photo => new { photo.Id, photo.Owner }).ToList();
            foreach (var photograph in photographs)
            {
                var shown = photograph.Owner == id ? other : id;
                _context.PhotoTags.RemoveRange(_context.PhotoTags.Where(tag => tag.Photo == photograph.Id && tag.Account == shown));
            }

            _context.Blocks.Add(new StoredBlock { By = id, Blocked = other, At = Now });
            Save();
            return true;
        });
    }

    /// <summary>Takes a block back. Nothing that ended with it comes back.</summary>
    public bool Unblock(string id, string other)
    {
        var stored = _context.Blocks.SingleOrDefault(block => block.By == id && block.Blocked == other);
        if (stored is null)
        {
            return false;
        }

        _context.Blocks.Remove(stored);
        Save();
        return true;
    }

    /// <summary>Returns the open accounts an account blocks, by their names.</summary>
    public IReadOnlyList<PersonRef> Blocked(string id)
    {
        var blocked = _context.Blocks.AsNoTracking().Where(block => block.By == id).Select(block => block.Blocked).ToList();
        return [.. Names(blocked).Values.OrderBy(person => person.Name, StringComparer.CurrentCultureIgnoreCase)];
    }

    // Whether one account blocks another.
    private bool Blocks(string by, string blocked) => _context.Blocks.AsNoTracking().Any(block => block.By == by && block.Blocked == blocked);

    // Two accounts that show nothing together any more are no longer people each other knows: neither stays in
    // a circle or a tree of the other.
    private void Parted(string one, string other)
    {
        var circles = _context.Circles.Where(circle => circle.Owner == one || circle.Owner == other).ToList();
        foreach (var circle in circles)
        {
            var guest = circle.Owner == one ? other : one;
            _context.CircleMembers.RemoveRange(_context.CircleMembers.Where(member => member.Circle == circle.Number && member.Member == guest));
        }

        LeaveTrees(one, other);
    }
}
