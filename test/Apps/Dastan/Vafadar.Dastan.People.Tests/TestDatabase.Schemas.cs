namespace Vafadar.Dastan.Tests;

/// <content>The parts of the database the tests of this project need: the template of their databases has these.</content>
internal static partial class TestDatabase
{
    private static void Migrate(string connection)
    {
        Vafadar.Dastan.People.PeopleStore.Migrate(connection);
    }
}
