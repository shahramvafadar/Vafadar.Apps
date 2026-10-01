namespace Vafadar.Zanance.Core.Dashboard;

/// <summary>The Home sections the user can hide and reorder (§21.5, REP-08).</summary>
/// <remarks>The balance and "needs attention" stay at the top and cannot be hidden, so nothing that needs action is lost (DASH-03).</remarks>
public enum HomeSection
{
    /// <summary>Income, expense and result of the period.</summary>
    Period,

    /// <summary>The estimated end balance (Advanced).</summary>
    Forecast,

    /// <summary>The remaining budget.</summary>
    Budget,

    /// <summary>The next due plan items.</summary>
    Upcoming,

    /// <summary>The latest recorded entries (D-37). Stored by name, so layouts saved before it get it at the end.</summary>
    Recent,

    /// <summary>Expense by category.</summary>
    Categories,

    /// <summary>The accounts.</summary>
    Accounts,
}

/// <summary>One section in the layout.</summary>
public readonly record struct HomeSectionState(HomeSection Section, bool IsVisible);

/// <summary>
/// The order and visibility of the Home sections. It is stored as text like <c>Period,Forecast,-Budget</c> (a minus
/// hides a section); unknown names are ignored and missing sections are added at the end with their default visibility,
/// so new sections of later versions appear.
/// </summary>
public sealed class HomeLayout
{
    private readonly List<HomeSectionState> _sections;

    private HomeLayout(List<HomeSectionState> sections) => _sections = sections;

    /// <summary>Gets the sections in their order.</summary>
    public IReadOnlyList<HomeSectionState> Sections => _sections;

    /// <summary>
    /// Gets the default layout: every section in the original order; the category chart and the account list start
    /// hidden, because Insights and Accounts show them (a calm Home, D-27). The user can turn them on.
    /// </summary>
    public static HomeLayout Default => new([.. Enum.GetValues<HomeSection>().Select(s => new HomeSectionState(s, IsVisibleByDefault(s)))]);

    /// <summary>Returns whether a section is shown when the user has not chosen otherwise.</summary>
    public static bool IsVisibleByDefault(HomeSection section) => section is not (HomeSection.Categories or HomeSection.Accounts);

    /// <summary>Gets a value indicating whether the layout differs from <see cref="Default"/>.</summary>
    public bool IsCustomized => ToString() != Default.ToString();

    /// <summary>Reads a stored layout.</summary>
    public static HomeLayout Parse(string? text)
    {
        // The default of versions before D-27 (every section visible) was stored on "Reset"; it means "the default".
        if (text == "Period,Forecast,Budget,Upcoming,Categories,Accounts")
        {
            return Default;
        }

        var sections = new List<HomeSectionState>();
        foreach (var part in (text ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var hidden = part.StartsWith('-');
            if (Enum.TryParse<HomeSection>(part.TrimStart('-'), ignoreCase: false, out var section) && Enum.IsDefined(section) && sections.All(s => s.Section != section))
            {
                sections.Add(new HomeSectionState(section, !hidden));
            }
        }

        foreach (var section in Enum.GetValues<HomeSection>().Where(s => sections.All(x => x.Section != s)))
        {
            sections.Add(new HomeSectionState(section, IsVisibleByDefault(section)));
        }

        return new HomeLayout(sections);
    }

    /// <summary>Shows or hides a section.</summary>
    public void SetVisible(HomeSection section, bool visible)
    {
        var index = _sections.FindIndex(s => s.Section == section);
        _sections[index] = new HomeSectionState(section, visible);
    }

    /// <summary>Moves a section up (negative) or down (positive); a move past either end does nothing.</summary>
    public void Move(HomeSection section, int delta)
    {
        var index = _sections.FindIndex(s => s.Section == section);
        var target = index + delta;
        if (target < 0 || target >= _sections.Count || delta == 0)
        {
            return;
        }

        var item = _sections[index];
        _sections.RemoveAt(index);
        _sections.Insert(target, item);
    }

    /// <inheritdoc />
    public override string ToString() => string.Join(',', _sections.Select(s => (s.IsVisible ? string.Empty : "-") + s.Section));
}
