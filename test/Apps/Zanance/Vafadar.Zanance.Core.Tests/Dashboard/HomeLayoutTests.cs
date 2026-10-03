using Vafadar.Zanance.Core.Dashboard;

namespace Vafadar.Zanance.Core.Tests.Dashboard;

public sealed class HomeLayoutTests
{
    [Fact]
    public void The_default_shows_the_daily_groups_and_keeps_the_others_one_tap_away()
    {
        var layout = HomeLayout.Parse(null);
        Assert.Equal("Budget,Upcoming,Goals,Recent,-Period,-Forecast,-Categories,-Accounts", layout.ToString());
        Assert.Equal(4, layout.Sections.Count(s => s.IsVisible));
        Assert.Equal(Enum.GetValues<HomeSection>().Order(), layout.Sections.Select(s => s.Section).Order());
        Assert.False(layout.IsCustomized);
    }

    [Theory]
    [InlineData("Period,Forecast,Budget,Upcoming,Categories,Accounts")]
    [InlineData("Period,Forecast,Budget,Upcoming,Recent,-Categories,-Accounts")]
    [InlineData("Budget,Upcoming,Recent,-Period,-Forecast,-Categories,-Accounts")]
    public void The_stored_defaults_of_earlier_versions_mean_the_default(string stored) =>
        Assert.False(HomeLayout.Parse(stored).IsCustomized);

    [Fact]
    public void A_customized_layout_is_kept_and_missing_sections_are_added_in_the_default_order()
    {
        var layout = HomeLayout.Parse("Budget,Period,Forecast,Upcoming,-Categories,-Accounts");
        Assert.Equal("Budget,Period,Forecast,Upcoming,-Categories,-Accounts,Goals,Recent", layout.ToString());
    }

    [Fact]
    public void Sections_can_be_hidden_and_moved_and_the_layout_round_trips()
    {
        var layout = HomeLayout.Default;
        layout.Move(HomeSection.Accounts, -7);
        layout.SetVisible(HomeSection.Forecast, true);
        layout.Move(HomeSection.Budget, 1);

        Assert.Equal("-Accounts,Upcoming,Budget,Goals,Recent,-Period,Forecast,-Categories", layout.ToString());
        Assert.True(layout.IsCustomized);
        Assert.Equal(layout.ToString(), HomeLayout.Parse(layout.ToString()).ToString());

        // Moves past either end change nothing.
        layout.Move(HomeSection.Accounts, -1);
        layout.Move(HomeSection.Categories, 1);
        Assert.Equal("-Accounts,Upcoming,Budget,Goals,Recent,-Period,Forecast,-Categories", layout.ToString());
    }

    [Fact]
    public void Unknown_and_duplicate_names_are_ignored_and_missing_sections_are_added()
    {
        var layout = HomeLayout.Parse("Budget, -Budget, Widgets, -Accounts");
        Assert.Equal("Budget,-Accounts,Upcoming,Goals,Recent,-Period,-Forecast,-Categories", layout.ToString());
    }
}
