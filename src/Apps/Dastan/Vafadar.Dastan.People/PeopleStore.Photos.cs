using System.Collections.Immutable;
using Microsoft.EntityFrameworkCore;
using Vafadar.Dastan.Core.Personal;

namespace Vafadar.Dastan.People;

/// <summary>The file of a photograph as it was taken in: its name in the folder of the photographs and what it is.</summary>
/// <param name="Name">The name of the file. It is made from the identifier of the photograph, never from what the sender said.</param>
/// <param name="ContentType">The type it is delivered as.</param>
/// <param name="Width">Its width in pixels.</param>
/// <param name="Height">Its height in pixels.</param>
/// <param name="Length">Its length in bytes.</param>
public sealed record PhotoFile(string Name, string ContentType, int Width, int Height, long Length);

/// <summary>A person a photograph is said to show, and where that stands.</summary>
/// <param name="Person">The account.</param>
/// <param name="State">Proposed, approved by the person, or taken away by the person.</param>
public sealed record PhotoTag(PersonRef Person, TagState State)
{
    /// <summary>Gets a value indicating whether the person agreed that the photograph is shown to everybody (D-053).</summary>
    public bool Everybody { get; init; }

    /// <summary>Gets a value indicating whether the person said themselves that the photograph shows them (D-059), instead of being named by its owner.</summary>
    public bool Objection { get; init; }
}

/// <summary>A photograph as somebody who may see it is shown it.</summary>
/// <param name="Id">The identifier.</param>
/// <param name="Owner">Whose it is.</param>
/// <param name="Caption">What its owner wrote about it.</param>
/// <param name="Taken">The day it was taken, when its owner said so.</param>
/// <param name="SharedAt">When it was brought in. A photograph of long ago that was shared today is new in a list of recent shares.</param>
/// <param name="Shown">Who its owner lets see it.</param>
/// <param name="Circle">For a circle: which one. Told to the owner only.</param>
/// <param name="File">Its file.</param>
/// <param name="Tags">Who it is said to show: for the owner every tag, for anybody else the approved ones.</param>
public sealed record PhotoInfo(string Id, PersonRef Owner, string Caption, DateOnly? Taken, DateTime SharedAt, ScopeKind Shown, long? Circle, PhotoFile File, ImmutableArray<PhotoTag> Tags)
{
    /// <summary>
    /// Gets a value indicating whether the photograph is held back: its owner shows it to everybody, and somebody
    /// it is said to show has not agreed to that. Until they do, only its owner and the people it names see it.
    /// </summary>
    public bool Held { get; init; }
}

/// <content>
/// The photographs of the visitors (step 16.5, PRIV-012, PRIV-013): each has an owner and an audience, and every
/// way to it – the page of its owner, the life of somebody it shows, a list of recent shares, the address of its
/// file – asks that audience through the rule of step 15.2 (<see cref="SharedMedia"/>). A tag says who a
/// photograph shows; the person approves it, and approving shows the photograph to nobody who could not see it.
/// </content>
public sealed partial class PeopleStore
{
    /// <summary>How many photographs an account can have.</summary>
    public const int MostPhotos = 200;

    /// <summary>The greatest length of what is written about a photograph.</summary>
    public const int LongestCaption = 200;

    /// <summary>How many people a photograph can be said to show.</summary>
    public const int MostTags = 20;

    /// <summary>
    /// How many objections of one account may stand at a time: enough for somebody who is in many pictures, too
    /// few to hold back the pictures of a site. Counted are those on photographs that are shown to everybody:
    /// one whose owner shows the photograph to fewer people holds nothing back, cannot be found by who made it,
    /// and takes no place.
    /// </summary>
    public const int MostObjections = 20;

    /// <summary>Returns whether an account can bring in another photograph.</summary>
    public bool CanAddPhoto(string id) => IsOpen(id) && _context.Photos.Count(photo => photo.Owner == id) < MostPhotos;

    /// <summary>
    /// Keeps a photograph whose file was taken in. Returns whether it was kept; a caption that is too long or
    /// has control characters, a circle that is not the owner's and a full account keep nothing.
    /// </summary>
    public bool AddPhoto(string id, string photo, PhotoFile file, string caption, DateOnly? taken, ScopeKind shown, long? circle = null)
    {
        ArgumentNullException.ThrowIfNull(file);
        ArgumentNullException.ThrowIfNull(caption);
        var written = caption.Trim();
        var now = Now;
        if (!CanAddPhoto(id) || photo is not { Length: > 0 and <= 40 } || written.Length > LongestCaption || written.Any(char.IsControl)
            || !Enum.IsDefined(shown) || taken > DateOnly.FromDateTime(now) || !OwnsCircle(id, shown, circle)
            || _context.Photos.AsNoTracking().Any(candidate => candidate.Id == photo))
        {
            return false;
        }

        _context.Photos.Add(new StoredPhoto
        {
            Id = photo,
            Owner = id,
            Caption = written,
            Taken = taken,
            SharedAt = now,
            Scope = (int)shown,
            Circle = shown == ScopeKind.Circle ? circle : null,
            File = file.Name,
            ContentType = file.ContentType,
            Width = file.Width,
            Height = file.Height,
            Length = file.Length,
        });
        Save();
        return true;
    }

    /// <summary>Returns the photographs of an account as their owner sees them, the one brought in last first.</summary>
    public IReadOnlyList<PhotoInfo> Photos(string id) =>
        !IsOpen(id) ? [] : [.. Shown(id, _context.Photos.AsNoTracking().Where(photo => photo.Owner == id).ToList())];

    /// <summary>
    /// Returns a photograph when a viewer may see it, else <see langword="null"/> – for a photograph that does
    /// not exist and for one the viewer may not see alike. The address of a file asks here too.
    /// </summary>
    public PhotoInfo? Photo(string? viewer, string photo)
    {
        var stored = _context.Photos.AsNoTracking().SingleOrDefault(candidate => candidate.Id == photo);
        return stored is null ? null : Shown(Viewer(viewer), [stored]).FirstOrDefault();
    }

    /// <summary>Changes who may see a photograph. Only its owner can; it holds for every way to the photograph at once.</summary>
    public bool SharePhoto(string id, string photo, ScopeKind shown, long? circle = null)
    {
        var stored = _context.Photos.SingleOrDefault(candidate => candidate.Id == photo);
        if (stored is null || !IsOpen(id) || !Enum.IsDefined(shown) || !OwnsCircle(id, shown, circle)
            || Rule(stored, []).Share(new AccountId(id), Scope(id, shown, circle)) != ActOutcome.Done)
        {
            _context.ChangeTracker.Clear();
            return false;
        }

        (stored.Scope, stored.Circle) = ((int)shown, shown == ScopeKind.Circle ? circle : null);
        if (shown != ScopeKind.Public)
        {
            // An agreement was given to one showing to everybody. The next one asks again.
            foreach (var tag in _context.PhotoTags.Where(candidate => candidate.Photo == photo && candidate.Everybody))
            {
                tag.Everybody = false;
            }
        }

        Save();
        return true;
    }

    /// <summary>
    /// Removes a photograph. Only its owner can. Returns the name of its file, which the caller deletes, or
    /// <see langword="null"/> when nothing was removed.
    /// </summary>
    public string? RemovePhoto(string id, string photo)
    {
        var stored = _context.Photos.SingleOrDefault(candidate => candidate.Id == photo);
        if (stored is null || !IsOpen(id) || Rule(stored, []).Withdraw(new AccountId(id)) != ActOutcome.Done)
        {
            _context.ChangeTracker.Clear();
            return null;
        }

        _context.Photos.Remove(stored);
        Save();
        return stored.File;
    }

    /// <summary>
    /// Says that a photograph shows somebody: the owner themselves, or an account the owner shows an accepted
    /// relationship with. The person answers; the owner's own tag needs no answer. Only the owner tags.
    /// </summary>
    public bool TagPhoto(string id, string photo, string account)
    {
        if (!_context.Photos.AsNoTracking().Any(candidate => candidate.Id == photo && candidate.Owner == id) || !IsOpen(id)
            || _context.PhotoTags.AsNoTracking().Any(tag => tag.Photo == photo && tag.Account == account)
            || _context.PhotoTags.Count(tag => tag.Photo == photo) >= MostTags
            || (account != id && !Relationships(id).Any(relationship => relationship.State == RelationshipState.Accepted && relationship.Other.Id == account)))
        {
            return false;
        }

        _context.PhotoTags.Add(new StoredPhotoTag { Photo = photo, Account = account, State = (int)(account == id ? TagState.Approved : TagState.Proposed) });
        Save();
        return true;
    }

    /// <summary>Takes back a tag the owner of a photograph put on it.</summary>
    public bool UntagPhoto(string id, string photo, string account)
    {
        var tag = _context.PhotoTags.SingleOrDefault(candidate => candidate.Photo == photo && candidate.Account == account);
        // A tag the person took away stays as it is: taken back and put on again, it would ask them a second time.
        // An objection is the person's own word: the owner of the photograph does not take it back for them.
        if (tag is null || (TagState)tag.State == TagState.Removed || tag.Objection
            || !_context.Photos.AsNoTracking().Any(candidate => candidate.Id == photo && candidate.Owner == id) || !IsOpen(id))
        {
            _context.ChangeTracker.Clear();
            return false;
        }

        _context.PhotoTags.Remove(tag);
        Save();
        return true;
    }

    /// <summary>
    /// Answers a tag: approves it or takes it away. Only the tagged person can, and only for a photograph they
    /// may see. Approving connects the photograph with the person; it shows it to nobody who could not see it
    /// (PRIV-013).
    /// </summary>
    public bool AnswerTag(string id, string photo, bool approve)
    {
        var stored = _context.Photos.AsNoTracking().SingleOrDefault(candidate => candidate.Id == photo);
        var tags = _context.PhotoTags.Where(candidate => candidate.Photo == photo).ToList();
        var tag = tags.SingleOrDefault(candidate => candidate.Account == id);
        if (stored is null || tag is null || !IsOpen(id) || !IsOpen(stored.Owner))
        {
            _context.ChangeTracker.Clear();
            return false;
        }

        var rule = Rule(stored, tags);
        var me = new AccountId(id);
        if (!rule.MayOpen(me) || (approve ? rule.Approve(new PersonId(id), me) : rule.RemoveTag(new PersonId(id), me)) != ActOutcome.Done)
        {
            _context.ChangeTracker.Clear();
            return false;
        }

        tag.State = (int)rule.TagOf(new PersonId(id))!.Value;
        if (!approve)
        {
            tag.Everybody = false;
        }

        Save();
        return true;
    }

    /// <summary>
    /// Says that a photograph its owner shows to everybody may be shown so, or takes that back (D-053). Only a
    /// person the photograph is said to show can, for themselves. Until everybody it names agreed, the
    /// photograph is seen by its owner and by them alone; an agreement taken back hides it at once.
    /// </summary>
    public bool AgreeToEverybody(string id, string photo, bool agree)
    {
        var stored = _context.Photos.AsNoTracking().SingleOrDefault(candidate => candidate.Id == photo);
        var tag = _context.PhotoTags.SingleOrDefault(candidate => candidate.Photo == photo && candidate.Account == id);
        if (stored is null || tag is null || stored.Owner == id || (TagState)tag.State == TagState.Removed || tag.Everybody == agree
            || (agree && stored.Scope != (int)ScopeKind.Public) || !IsOpen(id) || !IsOpen(stored.Owner))
        {
            _context.ChangeTracker.Clear();
            return false;
        }

        tag.Everybody = agree;
        Save();
        return true;
    }

    /// <summary>
    /// Says that a photograph its owner shows to everybody shows the account, and that the account does not
    /// agree to that (D-059): the way of somebody the owner did not name. The photograph is held back at once –
    /// seen by its owner and the people it names – until the account agrees or takes its word back, or the
    /// owner shows the photograph to fewer people. Only for a photograph the account may see, of somebody else,
    /// that names the account not yet.
    /// </summary>
    public bool ObjectToPhoto(string id, string photo)
    {
        var stored = _context.Photos.AsNoTracking().SingleOrDefault(candidate => candidate.Id == photo);
        if (stored is null || stored.Owner == id || stored.Scope != (int)ScopeKind.Public || !IsOpen(id) || !IsOpen(stored.Owner))
        {
            return false;
        }

        var tags = _context.PhotoTags.AsNoTracking().Where(candidate => candidate.Photo == photo).ToList();

        // Somebody the owner named once and who answered "that is not me" is named by nothing: they object
        // like anybody else, and their answer becomes the objection.
        var mine = tags.FirstOrDefault(tag => tag.Account == id);
        var standing = _context.PhotoTags.AsNoTracking().Count(tag => tag.Account == id && tag.Objection && !tag.Everybody
            && _context.Photos.Any(shown => shown.Id == tag.Photo && shown.Scope == (int)ScopeKind.Public));
        if ((mine is not null && (TagState)mine.State != TagState.Removed) || (mine is null && tags.Count >= MostTags)
            || !Rule(stored, tags).MayOpen(new AccountId(id)) || standing >= MostObjections)
        {
            return false;
        }

        if (mine is null)
        {
            _context.PhotoTags.Add(new StoredPhotoTag { Photo = photo, Account = id, State = (int)TagState.Approved, Objection = true });
            Save();
        }
        else
        {
            _context.PhotoTags.Where(tag => tag.Photo == photo && tag.Account == id)
                .ExecuteUpdate(change => change.SetProperty(tag => tag.State, (int)TagState.Approved).SetProperty(tag => tag.Objection, true).SetProperty(tag => tag.Everybody, false));
        }

        return true;
    }

    /// <summary>Takes an objection back: the account is not named by the photograph any more. Only who objected can.</summary>
    public bool WithdrawObjection(string id, string photo)
    {
        var tag = _context.PhotoTags.SingleOrDefault(candidate => candidate.Photo == photo && candidate.Account == id && candidate.Objection);
        if (tag is null || !IsOpen(id))
        {
            _context.ChangeTracker.Clear();
            return false;
        }

        _context.PhotoTags.Remove(tag);
        Save();
        return true;
    }

    /// <summary>
    /// Returns the photographs their owners show to everybody and that are said to show an account – those that
    /// wait for its agreement and those it agreed to, which it can take back.
    /// </summary>
    public IReadOnlyList<PhotoInfo> AskedForEverybody(string id)
    {
        if (!IsOpen(id))
        {
            return [];
        }

        var photos = _context.PhotoTags.AsNoTracking().Where(tag => tag.Account == id && tag.State != (int)TagState.Removed).Select(tag => tag.Photo).ToList();
        return [.. Shown(id, _context.Photos.AsNoTracking().Where(photo => photos.Contains(photo.Id) && photo.Owner != id && photo.Scope == (int)ScopeKind.Public).ToList(), all: true)];
    }

    /// <summary>Returns the photographs that are said to show an account and wait for its answer – those it may see.</summary>
    public IReadOnlyList<PhotoInfo> TagsWaiting(string id)
    {
        if (!IsOpen(id))
        {
            return [];
        }

        var photos = _context.PhotoTags.AsNoTracking().Where(tag => tag.Account == id && tag.State == (int)TagState.Proposed).Select(tag => tag.Photo).ToList();
        return [.. Shown(id, _context.Photos.AsNoTracking().Where(photo => photos.Contains(photo.Id)).ToList(), all: true)];
    }

    /// <summary>
    /// Returns what the people an account shows a relationship with brought in since a time and let this account
    /// see, the one brought in last first. A relationship that was accepted lately adds nothing by itself: only
    /// the audience the owner chose for a photograph counts.
    /// </summary>
    public IReadOnlyList<PhotoInfo> RecentShares(string id, DateTime since)
    {
        if (!IsOpen(id))
        {
            return [];
        }

        var known = Relationships(id).Where(relationship => relationship.State == RelationshipState.Accepted).Select(relationship => relationship.Other.Id).ToList();
        return [.. Shown(id, _context.Photos.AsNoTracking().Where(photo => known.Contains(photo.Owner) && photo.SharedAt >= since).ToList())];
    }

    /// <summary>
    /// Returns the photographs of the life of an account as a viewer may see them: its own, and those of others
    /// in which it approved its tag – each only when its owner lets this viewer see it. That the viewer knows
    /// the person is not enough (PRIV-013).
    /// </summary>
    public IReadOnlyList<PhotoInfo> LifeOf(string? viewer, string account)
    {
        if (!IsOpen(account))
        {
            return [];
        }

        var tagged = _context.PhotoTags.AsNoTracking().Where(tag => tag.Account == account && tag.State == (int)TagState.Approved && (!tag.Objection || tag.Everybody)).Select(tag => tag.Photo).ToList();
        return [.. Shown(Viewer(viewer), _context.Photos.AsNoTracking().Where(photo => photo.Owner == account || tagged.Contains(photo.Id)).ToList())];
    }

    /// <summary>Returns the names of the files of every photograph the store holds: a file that is not among them belongs to nothing.</summary>
    public IReadOnlySet<string> PhotoFiles() => _context.Photos.AsNoTracking().Select(photo => photo.File).ToHashSet(StringComparer.Ordinal);

    private string? Viewer(string? viewer) => viewer is not null && IsOpen(viewer) ? viewer : null;

    // The photographs of a set that a viewer may see, as they are shown to that viewer, the newest first. The
    // scope is asked before anything is made of a photograph (PRIV-012). With "all", every tag is told – to
    // the person who is to answer one; otherwise only the owner sees tags that are not approved.
    private IEnumerable<PhotoInfo> Shown(string? viewer, List<StoredPhoto> photos, bool all = false)
    {
        var ids = photos.Select(photo => photo.Id).ToList();
        var tags = _context.PhotoTags.AsNoTracking().Where(tag => ids.Contains(tag.Photo)).ToList();
        var names = Names(photos.Select(photo => photo.Owner).Concat(tags.Select(tag => tag.Account)));
        AccountId? account = viewer is null ? null : new AccountId(viewer);
        return photos
            .Where(photo => names.ContainsKey(photo.Owner) && Rule(photo, [.. tags.Where(tag => tag.Photo == photo.Id)]).MayOpen(account))
            .OrderByDescending(photo => photo.SharedAt)
            .ThenBy(photo => photo.Id, StringComparer.Ordinal)
            .Select(photo => new PhotoInfo(
                photo.Id,
                names[photo.Owner],
                photo.Caption,
                photo.Taken,
                photo.SharedAt,
                (ScopeKind)photo.Scope,
                photo.Owner == viewer ? photo.Circle : null,
                new PhotoFile(photo.File, photo.ContentType, photo.Width, photo.Height, photo.Length),
                [.. tags.Where(tag => tag.Photo == photo.Id && names.ContainsKey(tag.Account) && (all || photo.Owner == viewer || tag.Account == viewer || (tag.State == (int)TagState.Approved && (!tag.Objection || tag.Everybody))))
                    .Select(tag => new PhotoTag(names[tag.Account], (TagState)tag.State) { Everybody = tag.Everybody, Objection = tag.Objection })
                    .OrderBy(tag => tag.Person.Name, StringComparer.CurrentCultureIgnoreCase)])
            {
                Held = IsHeld(photo, tags.Where(tag => tag.Photo == photo.Id)),
            });
    }

    // Shown to everybody by its owner, and somebody it names has not agreed (D-053). A tag its person took away
    // names nobody; the owner's own tag needs no agreement.
    private static bool IsHeld(StoredPhoto photo, IEnumerable<StoredPhotoTag> tags) =>
        photo.Scope == (int)ScopeKind.Public && tags.Any(tag => tag.Account != photo.Owner && (TagState)tag.State != TagState.Removed && !tag.Everybody);

    // The photograph as the rules know it, brought to where the store says it stands.
    private SharedMedia Rule(StoredPhoto photo, IReadOnlyList<StoredPhotoTag> tags)
    {
        var shared = DateOnly.FromDateTime(photo.SharedAt);
        // A photograph that is held back is seen by its owner and the people it names – they are asked.
        var scope = IsHeld(photo, tags)
            ? AccessScope.Circle(new AccountId(photo.Owner), [.. tags.Where(tag => (TagState)tag.State != TagState.Removed).Select(tag => new AccountId(tag.Account))])
            : Scope(photo.Owner, (ScopeKind)photo.Scope, photo.Circle);
        var rule = new SharedMedia(photo.Id, scope, photo.Taken ?? shared, shared);
        foreach (var tag in tags)
        {
            var (person, account) = (new PersonId(tag.Account), new AccountId(tag.Account));
            rule.Tag(person, account);
            if ((TagState)tag.State == TagState.Approved)
            {
                rule.Approve(person, account);
            }
            else if ((TagState)tag.State == TagState.Removed)
            {
                rule.RemoveTag(person, account);
            }
        }

        return rule;
    }

    private bool OwnsCircle(string id, ScopeKind shown, long? circle) =>
        shown != ScopeKind.Circle || _context.Circles.AsNoTracking().Any(candidate => candidate.Number == circle && candidate.Owner == id);
}
