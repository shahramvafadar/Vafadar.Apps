using System.Collections.Immutable;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Vafadar.Dastan.Core.Personal;

namespace Vafadar.Dastan.People;

/// <summary>A page a visitor kept, as the store holds it: the collection, the kind of page and which.</summary>
/// <param name="Collection">The name of the collection in addresses.</param>
/// <param name="Kind">The kind of page, as addresses write it.</param>
/// <param name="Id">The identifier of what the page is about.</param>
public sealed record KeptPage(string Collection, string Kind, string Id);

/// <summary>What follows a visitor from device to device: how far they have read each collection, and their list.</summary>
/// <param name="Positions">The reading stage by collection; a collection that is not named was not read further than its start.</param>
/// <param name="Kept">The list, the page kept last at the end.</param>
public sealed record Belongings(ImmutableDictionary<string, int> Positions, ImmutableArray<KeptPage> Kept)
{
    /// <summary>Gets what somebody has who has nothing.</summary>
    public static Belongings None { get; } = new(ImmutableDictionary<string, int>.Empty, []);
}

/// <summary>An invitation as who sent it sees it.</summary>
/// <param name="Code">The code: what the address of the invitation carries. It says nothing about anybody.</param>
/// <param name="LastDay">The last day it can be used.</param>
/// <param name="State">Where it stands.</param>
/// <param name="RunOut">Whether its last day has passed without it being used.</param>
public sealed record InvitationInfo(string Code, DateOnly LastDay, InvitationState State, bool RunOut)
{
    /// <summary>Gets a value indicating whether somebody can still use it.</summary>
    public bool IsOpen => State == InvitationState.Open && !RunOut;
}

/// <content>
/// What belongs to an account (step 16.2): the reading position and the list, which follow the visitor to every
/// device, and the invitations the visitor passes on (D-049).
/// </content>
public sealed partial class PeopleStore
{
    /// <summary>The largest number of pages in the list of an account.</summary>
    public const int MostKept = 200;

    /// <summary>How many invitations of an account can be open at the same time.</summary>
    public const int OpenInvitations = 10;

    /// <summary>How long an invitation can be used.</summary>
    public static readonly TimeSpan InvitationLastsFor = TimeSpan.FromDays(14);

    /// <summary>Returns what follows an open account.</summary>
    public Belongings Belongings(string id)
    {
        if (!IsOpen(id))
        {
            return People.Belongings.None;
        }

        return new(
            _context.ReadingPositions.AsNoTracking().Where(position => position.VisitorId == id).ToImmutableDictionary(position => position.Collection, position => position.Level, StringComparer.Ordinal),
            [.. _context.SavedPages.AsNoTracking().Where(page => page.VisitorId == id).OrderBy(page => page.Number).Select(page => new KeptPage(page.Collection, page.Kind, page.PageId))]);
    }

    /// <summary>
    /// Keeps how far an account has read a collection; <see langword="null"/> forgets it, which is the start of
    /// the collection. Moving forward and moving back are the same act.
    /// </summary>
    public void SetPosition(string id, string collection, int? level)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(collection);
        if (!IsOpen(id) || collection.Length > 120 || level < 0)
        {
            return;
        }

        var stored = _context.ReadingPositions.SingleOrDefault(position => position.VisitorId == id && position.Collection == collection);
        if (level is null)
        {
            if (stored is not null)
            {
                _context.ReadingPositions.Remove(stored);
            }
        }
        else if (stored is null)
        {
            _context.ReadingPositions.Add(new StoredReadingPosition { VisitorId = id, Collection = collection, Level = level.Value });
        }
        else
        {
            stored.Level = level.Value;
        }

        Save();
    }

    /// <summary>Puts a page at the end of the list of an account. A full list lets its oldest page go.</summary>
    private void KeepUnguarded(string id, KeptPage page)
    {
        ArgumentNullException.ThrowIfNull(page);
        if (!IsOpen(id) || !Fits(page))
        {
            return;
        }

        var list = _context.SavedPages.Where(kept => kept.VisitorId == id).OrderBy(kept => kept.Number).ToList();
        var same = list.Where(kept => Same(kept, page)).ToList();
        _context.SavedPages.RemoveRange(same);
        _context.SavedPages.RemoveRange(list.Except(same).Take(Math.Max(0, list.Count - same.Count + 1 - MostKept)));
        _context.SavedPages.Add(new StoredSavedPage { VisitorId = id, Collection = page.Collection, Kind = page.Kind, PageId = page.Id });
        Save();
    }

    /// <summary>Takes a page out of the list of an account.</summary>
    public void Drop(string id, KeptPage page)
    {
        ArgumentNullException.ThrowIfNull(page);
        _context.SavedPages.RemoveRange(_context.SavedPages.Where(kept => kept.VisitorId == id && kept.Collection == page.Collection && kept.Kind == page.Kind && kept.PageId == page.Id));
        Save();
    }

    /// <summary>
    /// Brings what a device holds into an account: a reading position where the account has none for the
    /// collection – what the account says is never overwritten –, and the pages its list does not have yet.
    /// Returns how many positions and pages came in.
    /// </summary>
    public (int Positions, int Pages) BringIn(string id, Belongings device)
    {
        ArgumentNullException.ThrowIfNull(device);
        if (!IsOpen(id))
        {
            return (0, 0);
        }

        var has = Belongings(id);
        var positions = device.Positions.Where(position => position.Value >= 0 && position.Key.Length is > 0 and <= 120 && !has.Positions.ContainsKey(position.Key)).ToList();
        foreach (var position in positions)
        {
            _context.ReadingPositions.Add(new StoredReadingPosition { VisitorId = id, Collection = position.Key, Level = position.Value });
        }

        var pages = device.Kept.Where(Fits).Distinct().Except(has.Kept).Take(Math.Max(0, MostKept - has.Kept.Length)).ToList();
        foreach (var page in pages)
        {
            _context.SavedPages.Add(new StoredSavedPage { VisitorId = id, Collection = page.Collection, Kind = page.Kind, PageId = page.Id });
        }

        Save();
        return (positions.Count, pages.Count);
    }

    /// <summary>
    /// Makes an invitation to join: a code the sender passes on, by whatever way they choose – the site sends
    /// nothing. Returns the code, or <see langword="null"/> when the account has as many open invitations as it
    /// may have.
    /// </summary>
    private string? InviteUnguarded(string id)
    {
        if (!IsOpen(id) || Invitations(id).Count(invitation => invitation.IsOpen) >= OpenInvitations)
        {
            return null;
        }

        var now = _time.GetUtcNow().UtcDateTime;
        var code = new string([.. Enumerable.Range(0, 20).Select(_ => CodeLetters[RandomNumberGenerator.GetInt32(CodeLetters.Length)])]);
        _context.Invitations.Add(new StoredInvitation
        {
            Id = code,
            By = id,
            Purpose = (int)InvitationPurpose.Join,
            CreatedAt = now,
            LastDay = DateOnly.FromDateTime(now + InvitationLastsFor),
        });
        Save();
        return code;
    }

    /// <summary>Returns the invitations an account made, the newest first.</summary>
    public IReadOnlyList<InvitationInfo> Invitations(string id)
    {
        var today = Today;
        return [.. _context.Invitations.AsNoTracking().Where(invitation => invitation.By == id).OrderByDescending(invitation => invitation.CreatedAt).AsEnumerable()
            .Select(stored => (Stored: stored, Rule: Rule(stored)))
            .Select(invitation => new InvitationInfo(invitation.Stored.Id, invitation.Stored.LastDay, invitation.Rule.State, invitation.Rule.State == InvitationState.Open && today > invitation.Stored.LastDay))];
    }

    /// <summary>Takes an invitation back. Only who made it can, and only while nobody used it.</summary>
    public bool Revoke(string id, string code)
    {
        ArgumentNullException.ThrowIfNull(code);
        var stored = _context.Invitations.SingleOrDefault(invitation => invitation.Id == code);
        if (stored is null || Rule(stored).Revoke(new AccountId(id)) != ActOutcome.Done)
        {
            _context.ChangeTracker.Clear();
            return false;
        }

        stored.RevokedAt = _time.GetUtcNow().UtcDateTime;
        Save();
        return true;
    }

    /// <summary>Returns whether a code is an invitation somebody can use today: made by an open account, not used, not taken back, not run out.</summary>
    public bool IsInvited(string? code) => Usable(code) is not null;

    private StoredInvitation? Usable(string? code)
    {
        if (code is not { Length: > 0 and <= 40 })
        {
            return null;
        }

        var stored = _context.Invitations.SingleOrDefault(invitation => invitation.Id == code);
        return stored is not null && stored.UsedOn is null && stored.RevokedAt is null && Today <= stored.LastDay && IsOpen(stored.By) ? stored : null;
    }

    // The invitation as the rules of private spaces know it, brought to where the store says it stands.
    private static Invitation Rule(StoredInvitation stored)
    {
        var rule = new Invitation(stored.Id, new AccountId(stored.By), (InvitationPurpose)stored.Purpose, stored.LastDay);
        if (stored.RevokedAt is not null)
        {
            rule.Revoke(rule.By);
        }
        else if (stored.UsedOn is { } day)
        {
            // Who used it may be deleted since; that it was used stays.
            rule.Use(new AccountId(stored.UsedBy ?? string.Empty), day);
        }

        return rule;
    }

    private DateOnly Today => DateOnly.FromDateTime(_time.GetUtcNow().UtcDateTime);

    private bool IsOpen(string id) => _context.Visitors.AsNoTracking().Any(visitor => visitor.Id == id && visitor.ClosedAt == null);

    private void Save()
    {
        _context.SaveChanges();
        _context.ChangeTracker.Clear();
    }

    private static bool Same(StoredSavedPage kept, KeptPage page) => kept.Collection == page.Collection && kept.Kind == page.Kind && kept.PageId == page.Id;

    private static bool Fits(KeptPage page) =>
        page.Collection.Length is > 0 and <= 120 && page.Kind.Length is > 0 and <= 20 && page.Id.Length is > 0 and <= 120;
}
