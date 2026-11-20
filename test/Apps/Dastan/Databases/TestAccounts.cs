using System.Security.Cryptography;
using System.Text;
using Vafadar.Dastan.People;

namespace Vafadar.Dastan.Tests;

/// <summary>
/// Accounts for tests that are not about making an account (ADR 0016): a test of trees, photographs or circles
/// needs somebody who is signed in, not the journey through a mailed link. An account made here has a name, a
/// password and a made-up address of its own that counts as confirmed – it comes into being the way an account
/// through a provider does, gets its password, and gives the made-up provider up again. The journeys
/// themselves are tried where they are the subject.
/// </summary>
internal static class TestAccounts
{
    private const string Provider = "made-up-for-tests";

    /// <summary>Returns the made-up mail address of the account of a name: the same name, the same address.</summary>
    public static string MailOf(string name) =>
        $"{Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(name.Trim().Normalize().ToUpperInvariant())))[..16]}@example.org";

    /// <summary>
    /// Makes an account with a name and a password, confirmed and signed in. Problems by their names, as the
    /// store says them: <c>name-length</c>, <c>name-letters</c>, <c>taken</c>, <c>password-short</c>,
    /// <c>password-long</c>, <c>invitation-needed</c>.
    /// </summary>
    public static AccountResult Register(this PeopleStore store, string name, string password, string? invitation = null, bool invitationNeeded = false, string? terms = null)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(password);
        if (password.Length < PeopleStore.PasswordLength || password.Length > 1000)
        {
            return new([password.Length > 1000 ? "password-long" : "password-short"]);
        }

        var made = store.Accounts.RegisterWith(name, Provider, "subject of " + MailOf(name), MailOf(name), "en", invitation, invitationNeeded, terms, proven: true);
        if (!made.Done)
        {
            // The made-up sign-in and address stand for the name: taken, they mean the name is.
            return new([.. made.Problems.Select(problem => problem is "login-taken" or "address-has-account" ? "taken" : problem)]);
        }

        var id = made.Session!.Id;
        var set = store.Accounts.SetPassword(id, password);
        Assert.True(set.Done, string.Join(", ", set.Problems));
        Assert.True(store.Accounts.Unlink(id, Provider));
        return new([]) { Session = set.Session, RecoveryCodes = made.RecoveryCodes };
    }

    /// <summary>Signs in by name or address and password; nothing when that was refused.</summary>
    public static VisitorSession? SignIn(this PeopleStore store, string name, string password) => store.Accounts.SignIn(name, password).Session;

    /// <summary>Closes an account, proved by its password.</summary>
    public static AccountResult Close(this PeopleStore store, string id, string password) => store.Accounts.Close(id, password);

    /// <summary>Opens an account again that its visitor closed.</summary>
    public static VisitorSession? Reopen(this PeopleStore store, string name, string password) => store.Accounts.Reopen(name, password);
}

/// <summary>
/// The people of a tree in tests that are not about their times: a year is written as the number it is, in
/// the calendar of the common era, and read back the same way.
/// </summary>
public static class TestPersons
{
    /// <summary>Returns a year of the common era as the time a record states; nothing for nothing.</summary>
    public static Vafadar.Temporal.TemporalExpression? Year(int? year) => year is { } number
        ? new Vafadar.Temporal.Occurrence(Vafadar.Temporal.CalendarPosition.OfYear(Vafadar.Temporal.CalendarSystem.Gregorian, Vafadar.Temporal.Era.CommonEra, number))
        : null;

    /// <summary>Returns the year a time of a record states; nothing for a time that states none.</summary>
    public static int? Year(this Vafadar.Temporal.TemporalExpression? time) =>
        time is Vafadar.Temporal.Occurrence { At: Vafadar.Temporal.CalendarPosition at } ? at.Year : null;

    /// <summary>Adds a record whose times are years.</summary>
    public static string? AddPerson(this PeopleStore store, string id, string tree, string name, int? born, int? died, bool deceased) =>
        store.AddPerson(id, tree, name, Year(born), Year(died), deceased);

    /// <summary>Changes a record to times that are years.</summary>
    public static bool ChangePerson(this PeopleStore store, string id, string tree, string person, string name, int? born, int? died, bool deceased) =>
        store.ChangePerson(id, tree, person, name, Year(born), Year(died), deceased);
}
