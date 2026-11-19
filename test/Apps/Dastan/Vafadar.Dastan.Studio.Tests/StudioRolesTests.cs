using System.Net;
using Vafadar.Dastan.Store;

namespace Vafadar.Dastan.Studio.Tests;

/// <summary>
/// Who may do what in the tool, and the page of the own account (steps 13.3 and 13.4): a form is there for who
/// has its role and cannot be sent by who has not; an editor gives the own account another password and sees
/// when its password was tried.
/// </summary>
public sealed class StudioRolesTests : IDisposable
{
    private const string Collection = Studio.Collection;
    private const string Relation = $"/en/{Collection}/record/relations/relation-02";

    private readonly Studio _studio = new(accounts: true);
    private readonly Desk _desk;

    public StudioRolesTests() => _desk = new Desk(_studio);

    public void Dispose()
    {
        _desk.Dispose();
        _studio.Dispose();
    }

    private async Task SignedInAs(EditorRoles roles)
    {
        using (var store = _studio.Store())
        {
            Assert.True(store.SetRoles("Mina", roles).Done);
        }

        await _desk.Submit("/en/sign-in?to=", "signin", ("Signing.Name", "Mina"), ("Signing.Password", Studio.Password));
    }

    [Theory]
    [InlineData(EditorRoles.Reviewer, true, false, false)]
    [InlineData(EditorRoles.Writer, false, true, false)]
    [InlineData(EditorRoles.Publisher, false, false, true)]
    [InlineData(EditorRoles.None, false, false, false)]
    [InlineData(EditorRoles.All, true, true, true)]
    public async Task A_form_is_there_for_who_has_its_role(EditorRoles roles, bool reviews, bool writes, bool publishes)
    {
        await SignedInAs(roles);

        var record = await _desk.Get(Relation);
        var records = await _desk.Get($"/en/{Collection}/records/entities");
        var stages = await _desk.Get($"/en/{Collection}/stages");
        var revisions = await _desk.Get($"/en/{Collection}/revisions");
        var comparisons = await _desk.Get("/en/comparisons");

        Assert.Equal(reviews, record.Html.Contains("name=\"_handler\" value=\"review\"", StringComparison.Ordinal));
        Assert.Equal(writes, record.Html.Contains("name=\"_handler\" value=\"fields\"", StringComparison.Ordinal));
        Assert.Equal(writes, record.Html.Contains("name=\"_handler\" value=\"change\"", StringComparison.Ordinal));
        Assert.Equal(writes, record.Html.Contains("name=\"_handler\" value=\"remove\"", StringComparison.Ordinal));
        Assert.Equal(writes, records.Html.Contains("name=\"_handler\" value=\"add\"", StringComparison.Ordinal));
        Assert.Equal(writes, stages.Html.Contains("name=\"_handler\" value=\"stages\"", StringComparison.Ordinal));
        Assert.Equal(writes, comparisons.Html.Contains("name=\"_handler\" value=\"add\"", StringComparison.Ordinal));
        Assert.Equal(publishes, revisions.Html.Contains("name=\"_handler\" value=\"publish\"", StringComparison.Ordinal));
        Assert.Equal(publishes, revisions.Html.Contains("name=\"_handler\" value=\"release\"", StringComparison.Ordinal));

        // Who may not is told so, and still reads everything.
        Assert.Equal(!reviews, record.Text.Contains("Your account does not review.", StringComparison.Ordinal));
        Assert.Equal(!writes, record.Text.Contains("Your account does not write.", StringComparison.Ordinal));
        Assert.Equal(!publishes, revisions.Text.Contains("Your account does not publish.", StringComparison.Ordinal));
        Assert.Contains("relation-02", record.Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_form_that_is_sent_by_who_has_not_its_role_does_nothing()
    {
        await SignedInAs(EditorRoles.All);
        var opened = await _desk.Get(Relation);
        using (var store = _studio.Store())
        {
            Assert.True(store.SetRoles("Mina", EditorRoles.Reviewer).Done);
        }

        var removed = await _desk.Send(opened, "remove", ("Removing.Reason", "A try."));
        var changed = await _desk.Send(opened, "fields", ("f.label", "Changed without the role"), ("Filling.Reason", "A try."));

        Assert.NotEqual(HttpStatusCode.OK, removed.Status);
        Assert.DoesNotContain("?done=", removed.Arrived ?? string.Empty, StringComparison.Ordinal);
        Assert.DoesNotContain("?done=", changed.Arrived ?? string.Empty, StringComparison.Ordinal);
        using var after = _studio.Store();
        Assert.NotEqual("Changed without the role", (string?)after.Record(Collection, "relations", "relation-02")!.Body["label"]);
    }

    [Theory]
    [InlineData("en", "Your account", "Reviews:", "signed in")]
    [InlineData("fa", "حساب شما", "بازبینی می‌کند", "وارد شد")]
    public async Task The_page_of_the_account_says_what_it_may_do_and_when_its_password_was_tried(string language, string title, string role, string signedIn)
    {
        await SignedInAs(EditorRoles.Reviewer);

        var page = await _desk.Get($"/{language}/account");
        var home = await _desk.Get($"/{language}/");

        Assert.Equal(HttpStatusCode.OK, page.Status);
        Assert.Contains(title, page.Text, StringComparison.Ordinal);
        Assert.Contains(role, page.Text, StringComparison.Ordinal);
        Assert.Contains(signedIn, page.Text, StringComparison.Ordinal);
        Assert.Contains("href=\"account\"", home.Html, StringComparison.Ordinal);
        Assert.Matches("type=\"password\"[^>]*autocomplete=\"new-password\"", page.Html);
    }

    [Fact]
    public async Task An_editor_gives_the_own_account_another_password_and_signs_in_with_it()
    {
        await SignedInAs(EditorRoles.All);

        var differ = await _desk.Submit("/en/account", "password", ("Changing.Current", Studio.Password), ("Changing.Password", "another long phrase"), ("Changing.Again", "yet another phrase"));
        var wrong = await _desk.Submit("/en/account", "password", ("Changing.Current", "not the password"), ("Changing.Password", "another long phrase"), ("Changing.Again", "another long phrase"));
        var changed = await _desk.Submit("/en/account", "password", ("Changing.Current", Studio.Password), ("Changing.Password", "another long phrase"), ("Changing.Again", "another long phrase"));
        var after = await _desk.Get("/en/account");

        Assert.Contains("differ", differ.Text, StringComparison.Ordinal);
        Assert.Contains("the present password is not right.", wrong.Text, StringComparison.Ordinal);
        Assert.Contains("a wrong password", wrong.Text, StringComparison.Ordinal);

        // The old password signs in nowhere any more, here neither: the way back leads over the sign-in.
        Assert.Equal("/en/account?done=changed", changed.Arrived);
        Assert.Equal(HttpStatusCode.Redirect, after.Status);
        Assert.Contains("sign-in", after.Headers["Location"], StringComparison.Ordinal);
        using var store = _studio.Store();
        Assert.Null(store.SignIn("Mina", Studio.Password));
        Assert.NotNull(store.SignIn("Mina", "another long phrase"));
    }

    [Fact]
    public async Task A_tool_without_accounts_has_no_page_of_an_account_and_its_one_editor_may_do_everything()
    {
        using var local = new Studio();
        using var desk = new Desk(local);

        var account = await desk.Get("/en/account");
        var record = await desk.Get(Relation);

        Assert.Equal(HttpStatusCode.NotFound, account.Status);
        Assert.Contains("name=\"_handler\" value=\"review\"", record.Html, StringComparison.Ordinal);
        Assert.Contains("name=\"_handler\" value=\"fields\"", record.Html, StringComparison.Ordinal);
    }
}
