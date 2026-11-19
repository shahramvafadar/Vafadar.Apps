using Vafadar.Dastan.Core.Personal;
using Vafadar.Dastan.Tests;

namespace Vafadar.Dastan.People.Tests;

/// <summary>
/// Somebody a photograph shows and its owner did not name objects to its being shown to everybody (step 17.6,
/// D-059): the photograph is held back at once, and only their own word lets it go again.
/// </summary>
public sealed class PhotoObjectionTests : IDisposable
{
    private const string Password = "a long pass phrase";

    private readonly string _folder = Path.Combine(Path.GetTempPath(), "dastan-people-" + Guid.NewGuid().ToString("N"));
    private readonly TestClock _clock = new(new DateTimeOffset(2026, 11, 18, 9, 0, 0, TimeSpan.Zero));
    private readonly PeopleStore _store;
    private readonly string _mina;
    private readonly string _sara;
    private readonly string _dara;
    private int _number;

    public PhotoObjectionTests()
    {
        _store = PeopleStore.Open(TestDatabase.For(Path.Combine(_folder, "people.db")), _clock);
        _mina = _store.Register("Mina", Password).Session!.Id;
        _sara = _store.Register("Sara", Password).Session!.Id;
        _dara = _store.Register("Dara", Password).Session!.Id;
    }

    public void Dispose()
    {
        _store.Dispose();
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, recursive: true);
        }
    }

    private string Photo(string owner, ScopeKind shown = ScopeKind.Public)
    {
        var id = $"photo{++_number:D4}";
        Assert.True(_store.AddPhoto(owner, id, new PhotoFile($"{id}-file.jpg", "image/jpeg", 800, 600, 12345), "At the sea", null, shown));
        return id;
    }

    [Fact]
    public void An_objection_holds_a_photograph_back_at_once_and_its_owner_sees_whose_it_is()
    {
        var photo = Photo(_mina);

        // Sara knows nobody here: she is somebody the photograph shows and its owner did not name.
        Assert.True(_store.ObjectToPhoto(_sara, photo));

        Assert.Null(_store.Photo(null, photo));
        Assert.Null(_store.Photo(_dara, photo));
        Assert.NotNull(_store.Photo(_sara, photo));
        var owners = _store.Photos(_mina)[0];
        Assert.True(owners.Held);
        var tag = Assert.Single(owners.Tags);
        Assert.Equal(("Sara", true, false), (tag.Person.Name, tag.Objection, tag.Everybody));
        Assert.Equal([photo], [.. _store.AskedForEverybody(_sara).Select(asked => asked.Id)]);

        // The photograph is not among hers: she objected, she did not put it into her life.
        Assert.Empty(_store.LifeOf(_mina, _sara));
    }

    [Fact]
    public void Only_who_objected_lets_the_photograph_go_again()
    {
        var photo = Photo(_mina);
        _store.ObjectToPhoto(_sara, photo);

        // Not its owner, and nobody else.
        Assert.False(_store.UntagPhoto(_mina, photo, _sara));
        Assert.False(_store.WithdrawObjection(_mina, photo));
        Assert.False(_store.WithdrawObjection(_dara, photo));
        Assert.False(_store.AgreeToEverybody(_mina, photo, agree: true));
        Assert.Null(_store.Photo(null, photo));

        Assert.True(_store.WithdrawObjection(_sara, photo));

        Assert.NotNull(_store.Photo(null, photo));
        Assert.Empty(_store.Photos(_mina)[0].Tags);
        Assert.False(_store.WithdrawObjection(_sara, photo));
    }

    [Fact]
    public void Who_objected_may_agree_after_all_and_take_that_back()
    {
        var photo = Photo(_mina);
        _store.ObjectToPhoto(_sara, photo);

        Assert.True(_store.AgreeToEverybody(_sara, photo, agree: true));

        Assert.NotNull(_store.Photo(null, photo));
        Assert.Equal("Sara", Assert.Single(_store.Photo(null, photo)!.Tags).Person.Name);
        Assert.Equal([photo], [.. _store.LifeOf(null, _sara).Select(shown => shown.Id)]);

        Assert.True(_store.AgreeToEverybody(_sara, photo, agree: false));
        Assert.Null(_store.Photo(null, photo));
    }

    [Fact]
    public void The_owner_shows_the_photograph_to_fewer_people_and_the_objection_stands_for_the_next_time()
    {
        var photo = Photo(_mina);
        _store.ObjectToPhoto(_sara, photo);

        Assert.True(_store.SharePhoto(_mina, photo, ScopeKind.Private));
        Assert.Null(_store.Photo(_sara, photo));
        Assert.Empty(_store.AskedForEverybody(_sara));

        _store.SharePhoto(_mina, photo, ScopeKind.Public);

        Assert.Null(_store.Photo(null, photo));
        Assert.NotNull(_store.Photo(_sara, photo));
    }

    [Fact]
    public void An_objection_is_to_a_photograph_of_somebody_else_that_everybody_sees()
    {
        var hidden = Photo(_mina, ScopeKind.Private);
        var open = Photo(_mina);

        Assert.False(_store.ObjectToPhoto(_sara, hidden));
        Assert.False(_store.ObjectToPhoto(_mina, open));
        Assert.False(_store.ObjectToPhoto(_sara, "no-such-photo"));
        Assert.True(_store.ObjectToPhoto(_sara, open));
        Assert.False(_store.ObjectToPhoto(_sara, open));

        // Held back, it is seen by the people it names only: nobody else finds it to object to.
        Assert.False(_store.ObjectToPhoto(_dara, open));
    }

    [Fact]
    public void Somebody_the_owner_named_answers_the_tag_and_does_not_object()
    {
        Assert.Equal("asked", _store.Ask(_mina, "Sara", "friend"));
        _store.Answer(_sara, _store.Relationships(_sara).Single().Id, accept: true);
        var photo = Photo(_mina);
        _store.TagPhoto(_mina, photo, _sara);

        Assert.False(_store.ObjectToPhoto(_sara, photo));
        Assert.False(_store.WithdrawObjection(_sara, photo));

        // What the owner said, the owner takes back.
        Assert.True(_store.UntagPhoto(_mina, photo, _sara));
    }

    [Fact]
    public void An_account_holds_back_no_more_photographs_than_it_may_object_to_at_a_time()
    {
        for (var number = 0; number < PeopleStore.MostObjections; number++)
        {
            Assert.True(_store.ObjectToPhoto(_sara, Photo(_mina)));
        }

        var one = Photo(_mina);
        Assert.False(_store.ObjectToPhoto(_sara, one));

        // One that is settled – agreed to, or taken back – makes room.
        Assert.True(_store.WithdrawObjection(_sara, "photo0001"));
        Assert.True(_store.ObjectToPhoto(_sara, one));
    }

    [Fact]
    public void A_closed_account_objects_to_nothing_and_its_objection_goes_with_it()
    {
        var photo = Photo(_mina);
        _store.ObjectToPhoto(_sara, photo);
        _store.Close(_sara, Password);

        Assert.False(_store.ObjectToPhoto(_sara, Photo(_mina)));
        Assert.Null(_store.Photo(null, photo));

        _clock.Now += PeopleStore.KeptAfterClosing + TimeSpan.FromMinutes(1);
        _store.Purge();

        Assert.NotNull(_store.Photo(null, photo));
    }
}
