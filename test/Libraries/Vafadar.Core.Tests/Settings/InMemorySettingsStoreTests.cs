using Vafadar.Core.Settings;

namespace Vafadar.Core.Tests.Settings;

public sealed class InMemorySettingsStoreTests
{
    [Fact]
    public void Stores_and_returns_values()
    {
        var store = new InMemorySettingsStore();

        store.Set("a", "1");

        Assert.Equal("1", store.Get("a"));
        Assert.Null(store.Get("missing"));
    }

    [Fact]
    public void Setting_null_removes_the_key()
    {
        var store = new InMemorySettingsStore();
        store.Set("a", "1");

        store.Set("a", null);

        Assert.Null(store.Get("a"));
    }

    [Fact]
    public void Remove_deletes_the_key()
    {
        var store = new InMemorySettingsStore();
        store.Set("a", "1");

        store.Remove("a");
        store.Remove("never-existed");

        Assert.Null(store.Get("a"));
    }
}
