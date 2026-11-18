using Vafadar.Dastan.Core.Personal;

namespace Vafadar.Dastan.People.Tests;

/// <summary>
/// The photographs of the visitors (step 16.5, PRIV-012, PRIV-013): an owner and an audience that every way to
/// a photograph asks; tags the tagged person approves, which show a photograph to nobody who could not see it;
/// the life of a person; recent shares.
/// </summary>
public sealed class PhotographsTests : IDisposable
{
    private const string Password = "a long pass phrase";

    private readonly string _folder = Path.Combine(Path.GetTempPath(), "dastan-people-" + Guid.NewGuid().ToString("N"));
    private readonly TestClock _clock = new(new DateTimeOffset(2026, 11, 18, 9, 0, 0, TimeSpan.Zero));
    private readonly PeopleStore _store;
    private readonly string _mina;
    private readonly string _sara;
    private readonly string _dara;
    private int _number;

    public PhotographsTests()
    {
        _store = PeopleStore.Open(Path.Combine(_folder, "people.db"), _clock);
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

    private string Friends(string from, string to, string toName)
    {
        Assert.Equal("asked", _store.Ask(from, toName, "friend"));
        var id = _store.Relationships(to).Single(relationship => relationship.Other.Id == from).Id;
        Assert.True(_store.Answer(to, id, accept: true));
        return id;
    }

    // Brings a photograph in and returns its identifier.
    private string Photo(string owner, ScopeKind shown = ScopeKind.Private, long? circle = null, string caption = "At the sea", DateOnly? taken = null)
    {
        var id = $"photo{++_number:D4}";
        Assert.True(_store.AddPhoto(owner, id, new PhotoFile($"{id}-file.jpg", "image/jpeg", 800, 600, 12345), caption, taken, shown, circle));
        return id;
    }

    private string[] Seen(IEnumerable<PhotoInfo> photos) => [.. photos.Select(photo => photo.Id)];

    [Fact]
    public void A_photograph_belongs_to_who_brought_it_in_and_is_seen_by_nobody_else_at_first()
    {
        var photo = Photo(_mina, caption: "  At the sea  ", taken: new DateOnly(1998, 7, 4));

        var own = Assert.Single(_store.Photos(_mina));
        Assert.Equal(("At the sea", new DateOnly(1998, 7, 4), ScopeKind.Private, "Mina"), (own.Caption, own.Taken, own.Shown, own.Owner.Name));
        Assert.Equal(("photo0001-file.jpg", "image/jpeg", 800, 600, 12345L), (own.File.Name, own.File.ContentType, own.File.Width, own.File.Height, own.File.Length));
        Assert.Equal(_clock.Now.UtcDateTime, own.SharedAt);
        Assert.NotNull(_store.Photo(_mina, photo));
        Assert.Null(_store.Photo(_sara, photo));
        Assert.Null(_store.Photo(null, photo));
        Assert.Null(_store.Photo(_mina, "no-such-photo"));
        Assert.Empty(_store.Photos(_sara));
    }

    [Fact]
    public void A_photograph_that_says_something_impossible_is_not_kept()
    {
        var file = new PhotoFile("x-file.jpg", "image/jpeg", 1, 1, 1);
        var saras = _store.AddCircle(_sara, "Family")!.Value;

        Assert.False(_store.AddPhoto(_mina, "a", file, new string('x', PeopleStore.LongestCaption + 1), null, ScopeKind.Private));
        Assert.False(_store.AddPhoto(_mina, "a", file, "two\nlines", null, ScopeKind.Private));
        Assert.False(_store.AddPhoto(_mina, "a", file, "From tomorrow", new DateOnly(2026, 11, 19), ScopeKind.Private));
        Assert.False(_store.AddPhoto(_mina, "a", file, string.Empty, null, (ScopeKind)9));
        Assert.False(_store.AddPhoto(_mina, "a", file, string.Empty, null, ScopeKind.Circle));
        Assert.False(_store.AddPhoto(_mina, "a", file, string.Empty, null, ScopeKind.Circle, saras));
        Assert.False(_store.AddPhoto("nobody", "a", file, string.Empty, null, ScopeKind.Private));
        Assert.True(_store.AddPhoto(_mina, "a", file, string.Empty, null, ScopeKind.Private));
        Assert.False(_store.AddPhoto(_sara, "a", file, string.Empty, null, ScopeKind.Private));
        Assert.Single(_store.Photos(_mina));
    }

    [Fact]
    public void An_account_has_only_so_many_photographs()
    {
        for (var number = 0; number < PeopleStore.MostPhotos; number++)
        {
            Photo(_mina);
        }

        Assert.False(_store.CanAddPhoto(_mina));
        Assert.False(_store.AddPhoto(_mina, "one-more", new PhotoFile("one-more-file.jpg", "image/jpeg", 1, 1, 1), string.Empty, null, ScopeKind.Private));
        Assert.True(_store.CanAddPhoto(_sara));
    }

    [Fact]
    public void Every_way_to_a_photograph_asks_the_audience_its_owner_chose()
    {
        Friends(_mina, _sara, "Sara");
        Friends(_mina, _dara, "Dara");
        var circle = _store.AddCircle(_mina, "Close")!.Value;
        _store.SetCircle(_mina, circle, [_sara]);
        var forCircle = Photo(_mina, ScopeKind.Circle, circle);
        var forAll = Photo(_mina, ScopeKind.Public);
        var forMe = Photo(_mina);
        var since = _clock.Now.UtcDateTime.AddDays(-1);

        // The file, the page of the owner and the list of recent shares give the same answer to a viewer.
        foreach (var (viewer, expected) in new[] { (_sara, new[] { forAll, forCircle }), (_dara, [forAll]) })
        {
            Assert.Equal(expected.Order(), Seen(_store.LifeOf(viewer, _mina)).Order());
            Assert.Equal(expected.Order(), Seen(_store.RecentShares(viewer, since)).Order());
            foreach (var photo in new[] { forCircle, forAll, forMe })
            {
                Assert.Equal(expected.Contains(photo), _store.Photo(viewer, photo) is not null);
            }
        }

        Assert.Equal([forAll], Seen(_store.LifeOf(null, _mina)));
        Assert.Equal(3, _store.Photos(_mina).Count);

        // Only the owner is told which circle a photograph is for.
        Assert.Equal(circle, _store.Photo(_mina, forCircle)!.Circle);
        Assert.Null(_store.Photo(_sara, forCircle)!.Circle);
    }

    [Fact]
    public void A_change_of_the_audience_holds_at_once_and_only_the_owner_makes_it()
    {
        Friends(_mina, _sara, "Sara");
        var photo = Photo(_mina, ScopeKind.Public);
        var saras = _store.AddCircle(_sara, "Family")!.Value;

        Assert.False(_store.SharePhoto(_sara, photo, ScopeKind.Private));
        Assert.False(_store.SharePhoto(_mina, photo, ScopeKind.Circle, saras));
        Assert.False(_store.SharePhoto(_mina, "no-such-photo", ScopeKind.Private));
        Assert.NotNull(_store.Photo(null, photo));

        Assert.True(_store.SharePhoto(_mina, photo, ScopeKind.Private));
        Assert.Null(_store.Photo(null, photo));
        Assert.Null(_store.Photo(_sara, photo));
        Assert.Empty(_store.LifeOf(_sara, _mina));
        Assert.Empty(_store.RecentShares(_sara, _clock.Now.UtcDateTime.AddDays(-1)));
    }

    [Fact]
    public void A_circle_that_is_removed_narrows_the_photographs_shown_to_it()
    {
        Friends(_mina, _sara, "Sara");
        var circle = _store.AddCircle(_mina, "Close")!.Value;
        _store.SetCircle(_mina, circle, [_sara]);
        var photo = Photo(_mina, ScopeKind.Circle, circle);
        Assert.NotNull(_store.Photo(_sara, photo));

        _store.RemoveCircle(_mina, circle);

        Assert.Null(_store.Photo(_sara, photo));
        Assert.Equal((ScopeKind.Private, (long?)null), (_store.Photo(_mina, photo)!.Shown, _store.Photo(_mina, photo)!.Circle));
    }

    [Fact]
    public void A_tag_waits_for_the_person_it_names_and_only_that_person_answers()
    {
        Friends(_mina, _sara, "Sara");
        Friends(_mina, _dara, "Dara");
        var photo = Photo(_mina, ScopeKind.Public);

        Assert.True(_store.TagPhoto(_mina, photo, _sara));
        Assert.False(_store.TagPhoto(_mina, photo, _sara));
        Assert.False(_store.TagPhoto(_sara, photo, _dara));

        // Sara agrees that the photograph is shown to everybody (D-053, PhotoAgreementTests); the tag is another question.
        Assert.True(_store.AgreeToEverybody(_sara, photo, agree: true));

        // The owner sees the tag that waits; nobody else does, and it puts the photograph in nobody's life.
        Assert.Equal(("Sara", TagState.Proposed), (_store.Photos(_mina)[0].Tags[0].Person.Name, _store.Photos(_mina)[0].Tags[0].State));
        Assert.Empty(_store.Photo(_dara, photo)!.Tags);
        Assert.Empty(_store.LifeOf(_dara, _sara));
        Assert.Equal([photo], Seen(_store.TagsWaiting(_sara)));
        Assert.Empty(_store.TagsWaiting(_dara));

        Assert.False(_store.AnswerTag(_dara, photo, approve: true));
        Assert.False(_store.AnswerTag(_mina, photo, approve: true));
        Assert.True(_store.AnswerTag(_sara, photo, approve: true));
        Assert.False(_store.AnswerTag(_sara, photo, approve: true));

        Assert.Empty(_store.TagsWaiting(_sara));
        Assert.Equal("Sara", Assert.Single(_store.Photo(_dara, photo)!.Tags).Person.Name);
        Assert.Equal([photo], Seen(_store.LifeOf(_dara, _sara)));
        Assert.Equal([photo], Seen(_store.LifeOf(null, _sara)));
    }

    [Fact]
    public void An_approved_tag_shows_a_photograph_to_nobody_who_could_not_see_it()
    {
        Friends(_mina, _sara, "Sara");
        Friends(_sara, _dara, "Dara");
        var circle = _store.AddCircle(_mina, "Close")!.Value;
        _store.SetCircle(_mina, circle, [_sara]);
        var photo = Photo(_mina, ScopeKind.Circle, circle);
        _store.TagPhoto(_mina, photo, _sara);
        _store.AnswerTag(_sara, photo, approve: true);

        // Dara is Sara's friend, not in Mina's circle: the photograph is not in Sara's life for Dara.
        Assert.Equal([photo], Seen(_store.LifeOf(_sara, _sara)));
        Assert.Equal([photo], Seen(_store.LifeOf(_mina, _sara)));
        Assert.Empty(_store.LifeOf(_dara, _sara));
        Assert.Empty(_store.LifeOf(null, _sara));
        Assert.Null(_store.Photo(_dara, photo));
    }

    [Fact]
    public void A_person_takes_their_tag_away_and_is_not_asked_again()
    {
        Friends(_mina, _sara, "Sara");
        var photo = Photo(_mina, ScopeKind.Public);
        _store.TagPhoto(_mina, photo, _sara);
        _store.AnswerTag(_sara, photo, approve: true);
        _store.AgreeToEverybody(_sara, photo, agree: true);

        Assert.True(_store.AnswerTag(_sara, photo, approve: false));

        Assert.Empty(_store.LifeOf(null, _sara));
        Assert.Empty(_store.Photo(null, photo)!.Tags);
        Assert.Equal(TagState.Removed, _store.Photos(_mina)[0].Tags[0].State);
        Assert.False(_store.UntagPhoto(_mina, photo, _sara));
        Assert.False(_store.TagPhoto(_mina, photo, _sara));
        Assert.Empty(_store.TagsWaiting(_sara));

        // She may change her mind herself.
        Assert.True(_store.AnswerTag(_sara, photo, approve: true));
        Assert.True(_store.AgreeToEverybody(_sara, photo, agree: true));
        Assert.Equal([photo], Seen(_store.LifeOf(null, _sara)));
    }

    [Fact]
    public void An_owner_tags_themselves_without_an_answer_and_only_people_they_know()
    {
        var photo = Photo(_mina, ScopeKind.Public);

        Assert.True(_store.TagPhoto(_mina, photo, _mina));
        Assert.Equal(TagState.Approved, _store.Photos(_mina)[0].Tags[0].State);

        // Somebody the owner shows no relationship with is not tagged: a tag is no way to reach a stranger.
        Assert.False(_store.TagPhoto(_mina, photo, _sara));
        Assert.False(_store.TagPhoto(_mina, photo, "nobody"));

        Friends(_mina, _sara, "Sara");
        Assert.True(_store.TagPhoto(_mina, photo, _sara));
        Assert.False(_store.UntagPhoto(_sara, photo, _sara));
        Assert.True(_store.UntagPhoto(_mina, photo, _sara));
        Assert.Empty(_store.TagsWaiting(_sara));
    }

    [Fact]
    public void A_tag_on_a_photograph_its_person_may_not_see_is_not_shown_to_them_and_not_answered()
    {
        Friends(_mina, _sara, "Sara");
        var photo = Photo(_mina);
        Assert.True(_store.TagPhoto(_mina, photo, _sara));

        Assert.Empty(_store.TagsWaiting(_sara));
        Assert.False(_store.AnswerTag(_sara, photo, approve: true));

        _store.SharePhoto(_mina, photo, ScopeKind.Public);
        Assert.Equal([photo], Seen(_store.TagsWaiting(_sara)));
    }

    [Fact]
    public void Recent_shares_are_what_was_brought_in_lately_whenever_it_was_taken()
    {
        Friends(_mina, _sara, "Sara");
        var old = Photo(_mina, ScopeKind.Public, caption: "Long ago", taken: new DateOnly(1960, 1, 1));
        _clock.Now += TimeSpan.FromDays(40);
        var fresh = Photo(_mina, ScopeKind.Public, caption: "Long ago too", taken: new DateOnly(1961, 1, 1));
        var own = Photo(_sara, ScopeKind.Public);

        var shares = _store.RecentShares(_sara, _clock.Now.UtcDateTime.AddDays(-30));

        // The photograph of 1961 was shared today: it is recent. Sara's own and what was shared before are not.
        Assert.Equal([fresh], Seen(shares));
        Assert.Equal([fresh, old], Seen(_store.RecentShares(_sara, _clock.Now.UtcDateTime.AddDays(-60))));
        Assert.DoesNotContain(own, Seen(shares));

        // A stranger's public photograph is not "shared with" somebody; a relationship accepted now adds what its
        // owner lets them see – and nothing else.
        Assert.Empty(_store.RecentShares(_dara, _clock.Now.UtcDateTime.AddDays(-60)));
        var hidden = Photo(_mina);
        Friends(_mina, _dara, "Dara");
        Assert.Equal([fresh, old], Seen(_store.RecentShares(_dara, _clock.Now.UtcDateTime.AddDays(-60))));
        Assert.DoesNotContain(hidden, Seen(_store.RecentShares(_dara, DateTime.MinValue)));
    }

    [Fact]
    public void Only_the_owner_removes_a_photograph_and_its_file_is_named_for_deleting()
    {
        Friends(_mina, _sara, "Sara");
        var photo = Photo(_mina, ScopeKind.Public);
        _store.TagPhoto(_mina, photo, _sara);
        _store.AnswerTag(_sara, photo, approve: true);

        Assert.Null(_store.RemovePhoto(_sara, photo));
        Assert.Equal("photo0001-file.jpg", _store.RemovePhoto(_mina, photo));
        Assert.Null(_store.RemovePhoto(_mina, photo));

        Assert.Null(_store.Photo(_mina, photo));
        Assert.Empty(_store.LifeOf(null, _sara));
        Assert.Empty(_store.PhotoFiles());
    }

    [Fact]
    public void A_closed_account_shows_no_photograph_and_a_deleted_one_leaves_files_that_belong_to_nothing()
    {
        Friends(_mina, _sara, "Sara");
        var photo = Photo(_mina, ScopeKind.Public);
        var saras = Photo(_sara, ScopeKind.Public);
        _store.TagPhoto(_sara, saras, _mina);
        _store.AnswerTag(_mina, saras, approve: true);
        _store.AgreeToEverybody(_mina, saras, agree: true);
        _store.Close(_mina, Password);

        // Closed: nothing of it is given at once – not its file either – and it is in no photograph any more.
        Assert.Null(_store.Photo(null, photo));
        Assert.Null(_store.Photo(_sara, photo));
        Assert.Empty(_store.LifeOf(_sara, _mina));
        Assert.Empty(_store.Photo(null, saras)!.Tags);
        Assert.Contains("photo0001-file.jpg", _store.PhotoFiles());

        _clock.Now += PeopleStore.KeptAfterClosing + TimeSpan.FromMinutes(1);
        _store.Purge();

        // Deleted: the store names its file no more, so whoever keeps the files deletes it.
        Assert.Equal(["photo0002-file.jpg"], _store.PhotoFiles());
        Assert.Empty(_store.Photos(_sara)[0].Tags);
    }

    [Fact]
    public void The_copy_of_ones_own_data_names_the_photographs()
    {
        Friends(_mina, _sara, "Sara");
        var photo = Photo(_mina, ScopeKind.Public, caption: "At the sea", taken: new DateOnly(1998, 7, 4));
        _store.TagPhoto(_mina, photo, _sara);

        var copy = _store.Export(_mina, Password)!;

        Assert.Equal(
            """[{"caption":"At the sea","taken":"1998-07-04","broughtInAt":"2026-11-18T09:00:00Z","shownTo":"everybody","file":"photo0001-file.jpg","shows":[{"account":"Sara","state":"proposed"}]}]""",
            copy["photographs"]!.ToJsonString());
        Assert.Empty(_store.Export(_sara, Password)!["photographs"]!.AsArray());
    }
}
