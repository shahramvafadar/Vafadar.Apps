using System.Collections.Immutable;
using Microsoft.EntityFrameworkCore;

namespace Vafadar.Dastan.People;

/// <summary>An album: photographs its owner put together under a name, in the order they were put there.</summary>
/// <param name="Number">The number of the album.</param>
/// <param name="Owner">Whose it is.</param>
/// <param name="Name">Its name.</param>
/// <param name="Note">What its owner wrote about it; may be empty.</param>
/// <param name="Photos">Its photographs as the viewer may see them, in the order of the album.</param>
public sealed record AlbumInfo(long Number, PersonRef Owner, string Name, string Note, ImmutableArray<PhotoInfo> Photos);

/// <content>
/// Albums (step 16.10, R-MED-07): an owner puts photographs of theirs together under a name. An album is a way
/// to photographs and nothing more – it has no audience of its own, every photograph in it keeps the one its
/// owner chose, and a viewer is shown the photographs they may see and no others (PRIV-012). An album in which
/// a viewer may see nothing is not there for them: not its name either. Taking a photograph out of an album
/// leaves the photograph as it is.
/// </content>
public sealed partial class PeopleStore
{
    /// <summary>How many albums an account may have.</summary>
    public const int MostAlbums = 50;

    /// <summary>The longest name of an album.</summary>
    public const int LongestAlbumName = 80;

    /// <summary>The longest note about an album.</summary>
    public const int LongestAlbumNote = 400;

    /// <summary>Makes an album. Returns its number, or <see langword="null"/> when the name is none, is taken or the account has as many as it may.</summary>
    private long? AddAlbumUnguarded(string id, string name, string note = "")
    {
        if (!IsOpen(id) || Written(name, LongestAlbumName, required: true) is not { } written || Written(note, LongestAlbumNote, required: false) is not { } about)
        {
            return null;
        }

        var has = _context.Albums.AsNoTracking().Where(album => album.Owner == id).Select(album => album.Name).ToList();
        if (has.Count >= MostAlbums || has.Contains(written, StringComparer.OrdinalIgnoreCase))
        {
            return null;
        }

        var album = new StoredAlbum { Owner = id, Name = written, Note = about };
        _context.Albums.Add(album);
        _context.SaveChanges();
        _context.ChangeTracker.Clear();
        return album.Number;
    }

    /// <summary>Changes the name of an album and what is written about it. Only its owner can.</summary>
    public bool ChangeAlbum(string id, long album, string name, string note)
    {
        var stored = _context.Albums.SingleOrDefault(candidate => candidate.Number == album && candidate.Owner == id);
        if (stored is null || !IsOpen(id) || Written(name, LongestAlbumName, required: true) is not { } written || Written(note, LongestAlbumNote, required: false) is not { } about
            || _context.Albums.AsNoTracking().Where(other => other.Owner == id && other.Number != album).Select(other => other.Name).AsEnumerable().Contains(written, StringComparer.OrdinalIgnoreCase))
        {
            _context.ChangeTracker.Clear();
            return false;
        }

        (stored.Name, stored.Note) = (written, about);
        Save();
        return true;
    }

    /// <summary>Removes an album. Only its owner can. Its photographs stay as they are.</summary>
    public bool RemoveAlbum(string id, long album)
    {
        var stored = _context.Albums.SingleOrDefault(candidate => candidate.Number == album && candidate.Owner == id);
        if (stored is null || !IsOpen(id))
        {
            _context.ChangeTracker.Clear();
            return false;
        }

        _context.Albums.Remove(stored);
        Save();
        return true;
    }

    /// <summary>Puts a photograph at the end of an album. Both are the account's own; a photograph stands in an album once.</summary>
    public bool PlaceInAlbum(string id, long album, string photo)
    {
        if (!IsOpen(id)
            || !_context.Albums.AsNoTracking().Any(candidate => candidate.Number == album && candidate.Owner == id)
            || !_context.Photos.AsNoTracking().Any(candidate => candidate.Id == photo && candidate.Owner == id)
            || _context.AlbumPhotos.AsNoTracking().Any(placed => placed.Album == album && placed.Photo == photo))
        {
            return false;
        }

        var last = _context.AlbumPhotos.AsNoTracking().Where(placed => placed.Album == album).Select(placed => (int?)placed.Position).Max() ?? 0;
        _context.AlbumPhotos.Add(new StoredAlbumPhoto { Album = album, Photo = photo, Position = last + 1 });
        Save();
        return true;
    }

    /// <summary>
    /// Takes a photograph out of an album. The photograph is neither removed nor shown to anybody else or
    /// anybody less: who may see it is a question of the photograph.
    /// </summary>
    public bool TakeFromAlbum(string id, long album, string photo)
    {
        var placed = _context.AlbumPhotos.SingleOrDefault(candidate => candidate.Album == album && candidate.Photo == photo);
        if (placed is null || !IsOpen(id) || !_context.Albums.AsNoTracking().Any(candidate => candidate.Number == album && candidate.Owner == id))
        {
            _context.ChangeTracker.Clear();
            return false;
        }

        _context.AlbumPhotos.Remove(placed);
        Save();
        return true;
    }

    /// <summary>
    /// Moves a photograph one place earlier or later in an album (step 18.3.3): it changes places with its
    /// neighbour. Only the owner of the album can; at the end of the album there is nowhere further to go.
    /// </summary>
    public bool MoveInAlbum(string id, long album, string photo, bool earlier)
    {
        if (!IsOpen(id) || !_context.Albums.AsNoTracking().Any(candidate => candidate.Number == album && candidate.Owner == id))
        {
            return false;
        }

        var placed = _context.AlbumPhotos.Where(candidate => candidate.Album == album).OrderBy(candidate => candidate.Position).ToList();
        var at = placed.FindIndex(candidate => candidate.Photo == photo);
        var other = earlier ? at - 1 : at + 1;
        if (at < 0 || other < 0 || other >= placed.Count)
        {
            _context.ChangeTracker.Clear();
            return false;
        }

        (placed[at].Position, placed[other].Position) = (placed[other].Position, placed[at].Position);
        Save();
        return true;
    }

    /// <summary>Returns the albums of an account as their owner sees them – the empty ones too – by name.</summary>
    public IReadOnlyList<AlbumInfo> Albums(string id) => !IsOpen(id) ? [] : AlbumsShown(id, id, empty: true);

    /// <summary>
    /// Returns the albums of an account as a viewer may see them: each with the photographs the viewer may see,
    /// and only those in which there is one. Who may see nothing of an album learns nothing of it.
    /// </summary>
    public IReadOnlyList<AlbumInfo> AlbumsOf(string? viewer, string account) => !IsOpen(account) ? [] : AlbumsShown(Viewer(viewer), account, empty: false);

    private List<AlbumInfo> AlbumsShown(string? viewer, string owner, bool empty)
    {
        var albums = _context.Albums.AsNoTracking().Where(album => album.Owner == owner).ToList();
        var numbers = albums.Select(album => album.Number).ToList();
        var placed = _context.AlbumPhotos.AsNoTracking().Where(candidate => numbers.Contains(candidate.Album)).ToList();
        var ids = placed.Select(candidate => candidate.Photo).Distinct().ToList();

        // The scope of each photograph is asked before anything is made of it.
        var seen = Shown(viewer, _context.Photos.AsNoTracking().Where(photo => ids.Contains(photo.Id)).ToList()).ToDictionary(photo => photo.Id, StringComparer.Ordinal);
        var name = Names([owner]);
        return !name.TryGetValue(owner, out var whose)
            ? []
            : [.. albums
                .Select(album => new AlbumInfo(
                    album.Number,
                    whose,
                    album.Name,
                    album.Note,
                    [.. placed.Where(candidate => candidate.Album == album.Number && seen.ContainsKey(candidate.Photo)).OrderBy(candidate => candidate.Position).Select(candidate => seen[candidate.Photo])]))
                .Where(album => empty || !album.Photos.IsEmpty)
                .OrderBy(album => album.Name, StringComparer.CurrentCultureIgnoreCase)
                .ThenBy(album => album.Number)];
    }

    // What somebody wrote, without the space around it; nothing when it is too long, has control characters or
    // is missing where it is asked for.
    private static string? Written(string? text, int longest, bool required)
    {
        var written = (text ?? string.Empty).Trim();
        return written.Length > longest || written.Any(char.IsControl) || (required && written.Length == 0) ? null : written;
    }
}
