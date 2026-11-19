using Vafadar.Dastan.Core.Personal;
using Vafadar.Dastan.Tests;

namespace Vafadar.Dastan.People.Tests;

/// <summary>
/// Albums (step 16.10, R-MED-07, PRIV-012): a way to photographs with a name, without an audience of its own.
/// Every photograph in an album keeps the audience its owner chose, and an album shows a viewer nothing they
/// could not see – not its name when they may see nothing in it.
/// </summary>
public sealed class AlbumsTests : IDisposable
{
    private const string Password = "a long pass phrase";

    private readonly string _folder = Path.Combine(Path.GetTempPath(), "dastan-people-" + Guid.NewGuid().ToString("N"));
    private readonly TestClock _clock = new(new DateTimeOffset(2026, 11, 18, 9, 0, 0, TimeSpan.Zero));
    private readonly PeopleStore _store;
    private readonly string _mina;
    private readonly string _sara;
    private int _number;

    public AlbumsTests()
    {
        _store = PeopleStore.Open(TestDatabase.For(Path.Combine(_folder, "people.db")), _clock);
        _mina = _store.Register("Mina", Password).Session!.Id;
        _sara = _store.Register("Sara", Password).Session!.Id;
    }

    public void Dispose()
    {
        _store.Dispose();
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, recursive: true);
        }
    }

    private string Photo(string owner, ScopeKind shown = ScopeKind.Private)
    {
        var id = $"photo{++_number:D4}";
        Assert.True(_store.AddPhoto(owner, id, new PhotoFile($"{id}-file.jpg", "image/jpeg", 800, 600, 12345), "At the sea", null, shown));
        return id;
    }

    private static string[] In(AlbumInfo album) => [.. album.Photos.Select(photo => photo.Id)];

    [Fact]
    public void An_album_has_a_name_and_keeps_its_photographs_in_the_order_they_were_put_there()
    {
        var (first, second, third) = (Photo(_mina), Photo(_mina), Photo(_mina));

        var album = _store.AddAlbum(_mina, "  Summer 1998  ", " At the Caspian ");

        Assert.NotNull(album);
        Assert.True(_store.PlaceInAlbum(_mina, album.Value, third));
        Assert.True(_store.PlaceInAlbum(_mina, album.Value, first));
        Assert.True(_store.PlaceInAlbum(_mina, album.Value, second));
        Assert.False(_store.PlaceInAlbum(_mina, album.Value, first));

        var kept = Assert.Single(_store.Albums(_mina));
        Assert.Equal(("Summer 1998", "At the Caspian", "Mina"), (kept.Name, kept.Note, kept.Owner.Name));
        Assert.Equal([third, first, second], In(kept));
    }

    [Fact]
    public void The_owner_of_an_album_changes_its_order_one_place_at_a_time()
    {
        var (first, second, third) = (Photo(_mina), Photo(_mina), Photo(_mina));
        var album = _store.AddAlbum(_mina, "Summer 1998")!.Value;
        foreach (var photo in new[] { first, second, third })
        {
            _store.PlaceInAlbum(_mina, album, photo);
        }

        Assert.True(_store.MoveInAlbum(_mina, album, third, earlier: true));
        Assert.Equal([first, third, second], In(_store.Albums(_mina)[0]));
        Assert.True(_store.MoveInAlbum(_mina, album, first, earlier: false));
        Assert.Equal([third, first, second], In(_store.Albums(_mina)[0]));

        // At an end there is nowhere further; nobody else moves anything, and nothing that is not in the album.
        Assert.False(_store.MoveInAlbum(_mina, album, third, earlier: true));
        Assert.False(_store.MoveInAlbum(_mina, album, second, earlier: false));
        Assert.False(_store.MoveInAlbum(_sara, album, first, earlier: true));
        Assert.False(_store.MoveInAlbum(_mina, album, Photo(_mina), earlier: true));
        Assert.Equal([third, first, second], In(_store.Albums(_mina)[0]));

        // A photograph that is put in afterwards comes last, also after the order was changed.
        var fourth = Photo(_mina);
        _store.PlaceInAlbum(_mina, album, fourth);
        Assert.Equal(fourth, In(_store.Albums(_mina)[0])[^1]);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Summer\n1998")]
    [InlineData("summer 1998")]
    public void An_album_needs_a_name_of_its_own(string name)
    {
        _store.AddAlbum(_mina, "Summer 1998");

        Assert.Null(_store.AddAlbum(_mina, name));
        Assert.Null(_store.AddAlbum(_mina, new string('a', PeopleStore.LongestAlbumName + 1)));
        Assert.Null(_store.AddAlbum(_mina, "Winter", new string('a', PeopleStore.LongestAlbumNote + 1)));
        Assert.Single(_store.Albums(_mina));

        // Somebody else may have an album of that name.
        Assert.NotNull(_store.AddAlbum(_sara, "Summer 1998"));
    }

    [Fact]
    public void An_account_has_as_many_albums_as_it_may_and_no_more()
    {
        for (var number = 0; number < PeopleStore.MostAlbums; number++)
        {
            Assert.NotNull(_store.AddAlbum(_mina, $"Album {number}"));
        }

        Assert.Null(_store.AddAlbum(_mina, "One too many"));
    }

    [Fact]
    public void Only_its_owner_changes_an_album_and_only_with_photographs_of_their_own()
    {
        var album = _store.AddAlbum(_mina, "Summer 1998")!.Value;
        var minas = Photo(_mina);
        var saras = Photo(_sara, ScopeKind.Public);

        Assert.False(_store.PlaceInAlbum(_sara, album, saras));
        Assert.False(_store.PlaceInAlbum(_mina, album, saras));
        Assert.False(_store.PlaceInAlbum(_mina, album, "no-such-photo"));
        Assert.False(_store.ChangeAlbum(_sara, album, "Hers now", string.Empty));
        Assert.False(_store.RemoveAlbum(_sara, album));
        _store.PlaceInAlbum(_mina, album, minas);
        Assert.False(_store.TakeFromAlbum(_sara, album, minas));

        Assert.True(_store.ChangeAlbum(_mina, album, "Summer of 1998", "Two weeks"));
        Assert.Equal(("Summer of 1998", "Two weeks"), (_store.Albums(_mina)[0].Name, _store.Albums(_mina)[0].Note));

        _store.AddAlbum(_mina, "Winter");
        Assert.False(_store.ChangeAlbum(_mina, album, "winter", string.Empty));
        Assert.True(_store.ChangeAlbum(_mina, album, "SUMMER OF 1998", string.Empty));
    }

    [Fact]
    public void An_album_shows_a_viewer_the_photographs_they_may_see_and_no_others()
    {
        var album = _store.AddAlbum(_mina, "Summer 1998")!.Value;
        var open = Photo(_mina, ScopeKind.Public);
        var own = Photo(_mina);
        _store.PlaceInAlbum(_mina, album, own);
        _store.PlaceInAlbum(_mina, album, open);

        Assert.Equal([own, open], In(_store.AlbumsOf(_mina, _mina)[0]));
        Assert.Equal([open], In(Assert.Single(_store.AlbumsOf(_sara, _mina))));
        Assert.Equal([open], In(Assert.Single(_store.AlbumsOf(null, _mina))));

        // Being in an album shows a photograph to nobody: its audience is asked, and it holds at once.
        _store.SharePhoto(_mina, open, ScopeKind.Private);
        Assert.Empty(_store.AlbumsOf(_sara, _mina));
        Assert.Null(_store.Photo(_sara, own));
    }

    [Fact]
    public void An_album_in_which_a_viewer_may_see_nothing_is_not_there_for_them()
    {
        var hidden = _store.AddAlbum(_mina, "A name that tells something")!.Value;
        _store.PlaceInAlbum(_mina, hidden, Photo(_mina));
        _store.AddAlbum(_mina, "Empty");

        Assert.Empty(_store.AlbumsOf(_sara, _mina));
        Assert.Empty(_store.AlbumsOf(null, _mina));
        Assert.Equal(2, _store.Albums(_mina).Count);

        // Its owner sees the empty one where albums are changed, not on the page everybody sees.
        Assert.Single(_store.AlbumsOf(_mina, _mina));
    }

    [Fact]
    public void A_photograph_that_is_held_back_is_held_back_in_an_album_too()
    {
        Assert.Equal("asked", _store.Ask(_mina, "Sara", "friend"));
        _store.Answer(_sara, _store.Relationships(_sara).Single().Id, accept: true);
        var album = _store.AddAlbum(_mina, "Summer 1998")!.Value;
        var photo = Photo(_mina, ScopeKind.Public);
        _store.PlaceInAlbum(_mina, album, photo);
        _store.TagPhoto(_mina, photo, _sara);

        Assert.Empty(_store.AlbumsOf(null, _mina));

        _store.AgreeToEverybody(_sara, photo, agree: true);
        Assert.Single(_store.AlbumsOf(null, _mina));
    }

    [Fact]
    public void Taking_a_photograph_out_of_an_album_or_removing_the_album_leaves_the_photograph()
    {
        var first = _store.AddAlbum(_mina, "Summer 1998")!.Value;
        var second = _store.AddAlbum(_mina, "The sea")!.Value;
        var photo = Photo(_mina, ScopeKind.Public);
        _store.PlaceInAlbum(_mina, first, photo);
        _store.PlaceInAlbum(_mina, second, photo);

        Assert.True(_store.TakeFromAlbum(_mina, first, photo));
        Assert.False(_store.TakeFromAlbum(_mina, first, photo));

        // One file stands in several places; out of one it is still in the other, and as visible as before.
        Assert.Equal("The sea", Assert.Single(_store.AlbumsOf(null, _mina)).Name);
        Assert.True(_store.RemoveAlbum(_mina, second));
        Assert.NotNull(_store.Photo(null, photo));
        Assert.Single(_store.Photos(_mina));
        Assert.Empty(_store.AlbumsOf(null, _mina));
    }

    [Fact]
    public void A_photograph_that_is_removed_leaves_its_albums_and_a_closed_account_shows_none()
    {
        var album = _store.AddAlbum(_mina, "Summer 1998")!.Value;
        var (first, second) = (Photo(_mina, ScopeKind.Public), Photo(_mina, ScopeKind.Public));
        _store.PlaceInAlbum(_mina, album, first);
        _store.PlaceInAlbum(_mina, album, second);

        Assert.NotNull(_store.RemovePhoto(_mina, first));
        Assert.Equal([second], In(_store.AlbumsOf(null, _mina)[0]));

        _store.Close(_mina, Password);
        Assert.Empty(_store.AlbumsOf(null, _mina));
        Assert.Empty(_store.Albums(_mina));
        Assert.Null(_store.AddAlbum(_mina, "Another"));

        _clock.Now += PeopleStore.KeptAfterClosing + TimeSpan.FromMinutes(1);
        _store.Purge();
        Assert.Empty(_store.AlbumsOf(null, _mina));
    }

    [Fact]
    public void The_copy_of_ones_own_data_names_the_albums_and_what_is_in_them()
    {
        var album = _store.AddAlbum(_mina, "Summer 1998", "At the Caspian")!.Value;
        _store.PlaceInAlbum(_mina, album, Photo(_mina));

        var copy = _store.Export(_mina, Password)!;

        Assert.Equal("""[{"name":"Summer 1998","note":"At the Caspian","photographs":["photo0001-file.jpg"]}]""", copy["albums"]!.ToJsonString());
    }
}
