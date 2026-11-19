using Vafadar.Dastan.Core.Personal;
using Vafadar.Dastan.Tests;

namespace Vafadar.Dastan.People.Tests;

/// <summary>
/// A photograph that names people is shown to everybody only when each of them agreed (step 16.9, D-053). Until
/// then its owner and the people it names see it; an agreement that is taken back hides it at once.
/// </summary>
public sealed class PhotoAgreementTests : IDisposable
{
    private const string Password = "a long pass phrase";

    private readonly string _folder = Path.Combine(Path.GetTempPath(), "dastan-people-" + Guid.NewGuid().ToString("N"));
    private readonly TestClock _clock = new(new DateTimeOffset(2026, 11, 18, 9, 0, 0, TimeSpan.Zero));
    private readonly PeopleStore _store;
    private readonly string _mina;
    private readonly string _sara;
    private readonly string _dara;
    private int _number;

    public PhotoAgreementTests()
    {
        _store = PeopleStore.Open(TestDatabase.For(Path.Combine(_folder, "people.db")), _clock);
        _mina = _store.Register("Mina", Password).Session!.Id;
        _sara = _store.Register("Sara", Password).Session!.Id;
        _dara = _store.Register("Dara", Password).Session!.Id;
        Friends(_mina, _sara, "Sara");
        Friends(_mina, _dara, "Dara");
    }

    public void Dispose()
    {
        _store.Dispose();
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, recursive: true);
        }
    }

    private void Friends(string from, string to, string toName)
    {
        Assert.Equal("asked", _store.Ask(from, toName, "friend"));
        Assert.True(_store.Answer(to, _store.Relationships(to).Single(relationship => relationship.Other.Id == from).Id, accept: true));
    }

    private string Photo(string owner, ScopeKind shown = ScopeKind.Public)
    {
        var id = $"photo{++_number:D4}";
        Assert.True(_store.AddPhoto(owner, id, new PhotoFile($"{id}-file.jpg", "image/jpeg", 800, 600, 12345), "At the sea", null, shown));
        return id;
    }

    [Fact]
    public void A_photograph_that_names_nobody_else_is_shown_to_everybody_at_once()
    {
        var photo = Photo(_mina);
        _store.TagPhoto(_mina, photo, _mina);

        Assert.NotNull(_store.Photo(null, photo));
        Assert.False(_store.Photos(_mina)[0].Held);
    }

    [Fact]
    public void A_photograph_that_names_somebody_waits_for_them_and_is_seen_by_them_and_its_owner_meanwhile()
    {
        var photo = Photo(_mina);

        Assert.True(_store.TagPhoto(_mina, photo, _sara));

        // Held back from the moment it names her – before she said anything about the tag.
        Assert.Null(_store.Photo(null, photo));
        Assert.Null(_store.Photo(_dara, photo));
        Assert.Empty(_store.RecentShares(_dara, _clock.Now.UtcDateTime.AddDays(-1)));
        Assert.True(_store.Photos(_mina)[0].Held);
        Assert.NotNull(_store.Photo(_sara, photo));
        Assert.Equal([photo], [.. _store.AskedForEverybody(_sara).Select(asked => asked.Id)]);
        Assert.Empty(_store.AskedForEverybody(_dara));
        Assert.Empty(_store.AskedForEverybody(_mina));

        // Approving the tag says "this is me" – not "show it to everybody".
        Assert.True(_store.AnswerTag(_sara, photo, approve: true));
        Assert.Null(_store.Photo(null, photo));
        Assert.Empty(_store.LifeOf(null, _sara));

        Assert.True(_store.AgreeToEverybody(_sara, photo, agree: true));

        Assert.NotNull(_store.Photo(null, photo));
        Assert.NotNull(_store.Photo(_dara, photo));
        Assert.False(_store.Photos(_mina)[0].Held);
        Assert.True(_store.Photos(_mina)[0].Tags[0].Everybody);
        Assert.Equal([photo], [.. _store.LifeOf(null, _sara).Select(shown => shown.Id)]);
    }

    [Fact]
    public void Everybody_a_photograph_names_is_asked_and_one_who_did_not_agree_holds_it_back()
    {
        var photo = Photo(_mina);
        _store.TagPhoto(_mina, photo, _sara);
        _store.TagPhoto(_mina, photo, _dara);

        Assert.True(_store.AgreeToEverybody(_sara, photo, agree: true));

        Assert.Null(_store.Photo(null, photo));
        Assert.NotNull(_store.Photo(_dara, photo));

        Assert.True(_store.AgreeToEverybody(_dara, photo, agree: true));
        Assert.NotNull(_store.Photo(null, photo));
    }

    [Fact]
    public void An_agreement_that_is_taken_back_hides_the_photograph_at_once()
    {
        var photo = Photo(_mina);
        _store.TagPhoto(_mina, photo, _sara);
        _store.AgreeToEverybody(_sara, photo, agree: true);

        Assert.True(_store.AgreeToEverybody(_sara, photo, agree: false));

        Assert.Null(_store.Photo(null, photo));
        Assert.NotNull(_store.Photo(_sara, photo));
        Assert.False(_store.AgreeToEverybody(_sara, photo, agree: false));
    }

    [Fact]
    public void Only_a_person_the_photograph_names_agrees_and_only_for_themselves()
    {
        var photo = Photo(_mina);
        _store.TagPhoto(_mina, photo, _sara);

        Assert.False(_store.AgreeToEverybody(_dara, photo, agree: true));
        Assert.False(_store.AgreeToEverybody(_mina, photo, agree: true));
        Assert.False(_store.AgreeToEverybody(_sara, "no-such-photo", agree: true));

        Assert.Null(_store.Photo(null, photo));
    }

    [Fact]
    public void Somebody_who_takes_their_tag_away_or_whose_tag_the_owner_takes_back_holds_nothing_back()
    {
        var first = Photo(_mina);
        var second = Photo(_mina);
        _store.TagPhoto(_mina, first, _sara);
        _store.TagPhoto(_mina, second, _sara);

        Assert.True(_store.AnswerTag(_sara, first, approve: false));
        Assert.True(_store.UntagPhoto(_mina, second, _sara));

        Assert.NotNull(_store.Photo(null, first));
        Assert.NotNull(_store.Photo(null, second));
        Assert.Empty(_store.AskedForEverybody(_sara));
        Assert.False(_store.AgreeToEverybody(_sara, first, agree: true));
    }

    [Fact]
    public void A_person_who_is_named_later_holds_a_photograph_back_that_everybody_saw()
    {
        var photo = Photo(_mina);
        Assert.NotNull(_store.Photo(null, photo));

        _store.TagPhoto(_mina, photo, _sara);

        Assert.Null(_store.Photo(null, photo));
    }

    [Fact]
    public void An_agreement_is_given_to_one_showing_and_asked_again_for_the_next()
    {
        var photo = Photo(_mina, ScopeKind.Private);
        _store.TagPhoto(_mina, photo, _sara);

        // Nothing is asked while the photograph is not shown to everybody, and nothing can be agreed ahead.
        Assert.Empty(_store.AskedForEverybody(_sara));
        Assert.False(_store.AgreeToEverybody(_sara, photo, agree: true));
        Assert.False(_store.Photos(_mina)[0].Held);

        _store.SharePhoto(_mina, photo, ScopeKind.Public);
        _store.AgreeToEverybody(_sara, photo, agree: true);
        Assert.NotNull(_store.Photo(null, photo));

        _store.SharePhoto(_mina, photo, ScopeKind.Private);
        _store.SharePhoto(_mina, photo, ScopeKind.Public);

        Assert.Null(_store.Photo(null, photo));
        Assert.False(_store.Photos(_mina)[0].Tags[0].Everybody);
    }

    [Fact]
    public void A_closed_account_that_did_not_agree_holds_the_photograph_back_until_it_is_deleted()
    {
        var photo = Photo(_mina);
        _store.TagPhoto(_mina, photo, _sara);
        _store.Close(_sara, Password);

        Assert.Null(_store.Photo(null, photo));

        _clock.Now += PeopleStore.KeptAfterClosing + TimeSpan.FromMinutes(1);
        _store.Purge();

        Assert.NotNull(_store.Photo(null, photo));
    }
}
