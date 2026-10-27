using System.Globalization;
using System.Resources;
using Vafadar.Localization;
using Vafadar.Zanance.PhysicalReaderGestures;

namespace Vafadar.Zanance.App.Tests;

/// <summary>AT-593: only original read-only primary destinations extend the physical reader cohort.</summary>
[Trait("AT", "AT-593")]
public sealed class PhysicalReaderPrimaryTabTests
{
    private static readonly ResourceManager App = new("Vafadar.Zanance.App.Tests.Resources.AppResources", typeof(PhysicalReaderPrimaryTabTests).Assembly);
    private static readonly ResourceManager Shared = new("Vafadar.Localization.Resources.SharedStrings", typeof(Translator).Assembly);

    /// <summary>Real translations admit the three original tabs without expanding authority to financial commands.</summary>
    [Theory]
    [InlineData("en")]
    [InlineData("fa")]
    [InlineData("de")]
    [InlineData("es")]
    [InlineData("fr")]
    [InlineData("it")]
    public void Original_primary_tabs_are_the_only_added_destinations(string language)
    {
        var culture = CultureInfo.GetCultureInfo(language);
        string Read(string key) => App.GetString(key, culture) ?? Shared.GetString(key, culture)
            ?? throw new InvalidOperationException("Original resource missing.");
        var captions = PhysicalReaderCaptions.PrimaryTabsFromOriginalResources(Read);
        var expected = new HashSet<string>(StringComparer.Ordinal);
        foreach (var key in new[] { "Tab_Home", "Tab_Transactions", "Tab_Plans", "Tab_Insights" })
            expected.Add(Read(key));
        Assert.Contains(Read("Tab_Transactions"), captions);
        Assert.Contains(Read("Tab_Plans"), captions);
        Assert.Contains(Read("Tab_Insights"), captions);
        Assert.True(expected.SetEquals(captions));
        Assert.DoesNotContain(Read("Common_Save"), captions);
        Assert.DoesNotContain(Read("Common_Delete"), captions);
        Assert.DoesNotContain(Read("Common_Undo"), captions);
        Assert.DoesNotContain("arbitrary destination", captions);
    }

    /// <summary>The whole original peer row is required; a matching button caption cannot inherit tab authority.</summary>
    [Theory]
    [InlineData("correct", true)]
    [InlineData("button-alias", false)]
    [InlineData("missing-peer", false)]
    [InlineData("duplicate-target", false)]
    [InlineData("overlap", false)]
    [InlineData("two-selected", false)]
    [InlineData("no-selected", false)]
    [InlineData("foreign-target", false)]
    [InlineData("outside-owner", false)]
    [InlineData("unclickable-target", false)]
    public void Only_a_complete_original_native_tab_row_can_dispatch(string shape, bool admitted)
    {
        string[] captions = ["Home", "Transactions", "Plans", "Insights", "More"];
        var nodes = captions.Select((caption, index) => new ReaderTabNode(caption, "android.widget.FrameLayout",
            true, true, true, false, index == 0, [index * 216, 2047, (index + 1) * 216, 2205])).ToList();
        if (shape == "button-alias") nodes[1] = nodes[1] with { Class = "android.widget.Button" };
        if (shape == "missing-peer") nodes.RemoveAt(4);
        if (shape == "duplicate-target") nodes.Add(nodes[1]);
        if (shape == "overlap") nodes[1] = nodes[1] with { Bounds = [210, 2047, 432, 2205] };
        if (shape == "two-selected") nodes[1] = nodes[1] with { Selected = true };
        if (shape == "no-selected") nodes[0] = nodes[0] with { Selected = false };
        if (shape == "unclickable-target") nodes[1] = nodes[1] with { Clickable = false };
        var target = shape == "foreign-target" ? "Undo" : "Transactions";
        int[] viewport = [0, 0, shape == "outside-owner" ? 1000 : 1080, 2340];
        Assert.Equal(admitted, PhysicalReaderTabRow.Admits(nodes, captions, target, 124, viewport));
    }

    /// <summary>Real TalkBack omits the redundant click action on the selected peer, not on the requested destination.</summary>
    [Fact]
    public void Selected_native_peer_may_omit_its_redundant_click_action()
    {
        string[] captions = ["Home", "Transactions", "Plans", "Insights", "More"];
        var nodes = captions.Select((caption, index) => new ReaderTabNode(caption, "android.widget.FrameLayout",
            true, true, index != 0, false, index == 0, [index * 216, 2047, (index + 1) * 216, 2205])).ToArray();
        Assert.True(PhysicalReaderTabRow.Admits(nodes, captions, "Transactions", 124, [0, 0, 1080, 2340]));
        Assert.False(PhysicalReaderTabRow.Admits(nodes, captions, "Home", 124, [0, 0, 1080, 2340]));
    }
}
