using Microsoft.EntityFrameworkCore;
using Vafadar.Dastan.Core.Personal;

namespace Vafadar.Dastan.People;

/// <summary>What an account says about itself on its page, as the account itself reads it (D-086).</summary>
/// <param name="About">The few words, or nothing.</param>
/// <param name="AboutShown">Who may read them.</param>
/// <param name="AboutCircle">The circle, when they are for one.</param>
/// <param name="Picture">The photograph that is the picture of the account, or nothing – also when the photograph is gone.</param>
public sealed record AccountDescription(string? About, ScopeKind AboutShown, long? AboutCircle, PhotoInfo? Picture);

/// <content>
/// What an account says about itself on its page (D-086): a few words with an audience of their own, and a
/// picture. The words are a statement of the account about itself and are read by who their scope allows. The
/// picture is one of the account's own photographs and is shown to who may see that photograph – it has no
/// audience of its own, so that a photograph cannot be seen as a picture by somebody who may not see it as a
/// photograph. What a viewer may not see is not hinted at (PRIV-012): the page of an account that says nothing
/// and of one that says nothing to this viewer are the same.
/// </content>
public sealed partial class PeopleStore
{
    /// <summary>The greatest length of the words of an account about itself: a lead under a name, not a life.</summary>
    public const int LongestAccountAbout = 300;

    /// <summary>Returns what an open account says about itself, as the account itself reads it; nothing for no such account.</summary>
    public AccountDescription? Description(string id)
    {
        var visitor = _context.Visitors.AsNoTracking().SingleOrDefault(candidate => candidate.Id == id && candidate.ClosedAt == null);
        return visitor is null
            ? null
            : new(visitor.About, (ScopeKind)visitor.AboutScope, visitor.AboutCircle, visitor.Picture is { } picture ? Photo(id, picture) : null);
    }

    /// <summary>
    /// Keeps what an account says about itself. Returns <c>done</c>; <c>refused</c> for no such account or a
    /// scope that is none; <c>long</c> for words that are too long or hold what is no text; <c>picture</c> for
    /// a photograph that is not the account's own.
    /// </summary>
    /// <param name="id">The account.</param>
    /// <param name="about">The few words; nothing or blank for none.</param>
    /// <param name="shown">Who may read them.</param>
    /// <param name="circle">The circle, when they are for one of the account's.</param>
    /// <param name="picture">The photograph that is the picture – one of the account's own; nothing for none.</param>
    public string DescribeAccount(string id, string? about, ScopeKind shown, long? circle, string? picture) =>
        Guarded([OfAccount(id)], () =>
        {
            var stored = _context.Visitors.SingleOrDefault(candidate => candidate.Id == id && candidate.ClosedAt == null);
            if (stored is null || !Enum.IsDefined(shown) || !OwnsCircle(id, shown, circle))
            {
                _context.ChangeTracker.Clear();
                return "refused";
            }

            var words = Text(about, LongestAccountAbout);
            if (words.Refused)
            {
                _context.ChangeTracker.Clear();
                return "long";
            }

            // A picture that is kept needs no asking; a new one is one of the account's own photographs.
            var photo = string.IsNullOrWhiteSpace(picture) ? null : picture.Trim();
            if (photo is not null && photo != stored.Picture && !_context.Photos.AsNoTracking().Any(candidate => candidate.Id == photo && candidate.Owner == id))
            {
                _context.ChangeTracker.Clear();
                return "picture";
            }

            // Words that are not there are for nobody: no scope is kept for nothing.
            (stored.About, stored.Picture) = (words.Value, photo);
            (stored.AboutScope, stored.AboutCircle) = words.Value is null ? ((int)ScopeKind.Private, null) : ((int)shown, shown == ScopeKind.Circle ? circle : null);
            Save();
            return "done";
        });

    // What a viewer is shown of what an account says about itself: the words where their scope allows, the
    // picture where the viewer may see that photograph. Asked before anything is made of either (PRIV-012).
    private (string? About, PhotoInfo? Picture) Described(StoredVisitor person, string? viewer)
    {
        AccountId? account = viewer is null ? null : new AccountId(viewer);
        var about = person.About is { } words && Scope(person.Id, (ScopeKind)person.AboutScope, person.AboutCircle).Allows(account) ? words : null;
        var picture = person.Picture is null
            ? null
            : Shown(viewer, _context.Photos.AsNoTracking().Where(photo => photo.Id == person.Picture && photo.Owner == person.Id).ToList()).FirstOrDefault();
        return (about, picture);
    }
}
