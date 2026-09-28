using Vafadar.Zanance.Core.Dashboard;

namespace Vafadar.Zanance.Core.Tests.Dashboard;

public sealed class HomeLayoutTests
{
    [Fact]
    public void The_default_shows_every_section_in_the_original_order()
    {
        var layout = HomeLayout.Parse(null);
        Assert.Equal(Enum.GetValues<HomeSection>(), layout.Sections.Select(s => s.Section));
        Assert.All(layout.Sections, s => Assert.True(s.IsVisible));
        Assert.False(layout.IsCustomized);
    }

    [Fact]
    public void Sections_can_be_hidden_and_moved_and_the_layout_round_trips()
    {
        var layout = HomeLayout.Default;
        layout.Move(HomeSection.Accounts, -5);
        layout.SetVisible(HomeSection.Forecast, false);
        layout.Move(HomeSection.Period, -1);

        Assert.Equal("Period,Accounts,-Forecast,Budget,Upcoming,Categories", layout.ToString());
        Assert.True(layout.IsCustomized);
        Assert.Equal(layout.ToString(), HomeLayout.Parse(layout.ToString()).ToString());

        // Moves past either end change nothing.
        layout.Move(HomeSection.Period, -1);
        layout.Move(HomeSection.Categories, 1);
        Assert.Equal("Period,Accounts,-Forecast,Budget,Upcoming,Categories", layout.ToString());
    }

    [Fact]
    public void Unknown_and_duplicate_names_are_ignored_and_missing_sections_are_added()
    {
        var layout = HomeLayout.Parse("Budget, -Budget, Widgets, -Accounts");
        Assert.Equal("Budget,-Accounts,Period,Forecast,Upcoming,Categories", layout.ToString());
    }
}