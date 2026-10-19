using System.Reflection;
using Vafadar.Testing;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Budgets;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Settings;

namespace Vafadar.Zanance.Core.Tests.Zex;

/// <summary>
/// ZEX-S0501, S0503: the single Simple/Advanced policy (05 §1). Every feature has one rule, the features that are never
/// hidden show in both modes, no page reads the mode itself, and no calculation takes the mode (ZEX-AT28).
/// </summary>
public sealed class FeaturePolicyTests
{
    private const string App = "src/Apps/Zanance/Vafadar.Zanance.App";

    public static TheoryData<Feature> Features() => [.. Enum.GetValues<Feature>()];

    [Fact]
    public void Every_feature_has_exactly_one_rule_with_an_area_of_the_matrix()
    {
        Assert.Equal(Enum.GetValues<Feature>().Order(), FeaturePolicy.Rules.Select(r => r.Feature).Order());
        Assert.All(FeaturePolicy.Rules, rule => Assert.Matches("^SA(0[1-9]|[12][0-9]|30)$", rule.Area));
    }

    [Theory]
    [MemberData(nameof(Features))]
    public void Advanced_shows_everything_and_Simple_keeps_existing_data_visible(Feature feature)
    {
        var simple = new ZananceSettings { Mode = ExperienceMode.Simple };
        var advanced = new ZananceSettings { Mode = ExperienceMode.Advanced };
        var always = FeaturePolicy.RuleOf(feature).Simple == FeatureVisibility.Always;

        Assert.True(advanced.Shows(feature));
        Assert.Equal(always, simple.Shows(feature));

        // Whatever exists stays reachable in Simple, at least as a summary (05 §1 rule 2).
        Assert.True(simple.ShowsExisting(feature, hasData: true));
        Assert.Equal(always, simple.ShowsExisting(feature, hasData: false));
    }

    [Theory]
    [InlineData(Feature.NativeTotals)]
    [InlineData(Feature.Reconcile)]
    [InlineData(Feature.Warnings)]
    [InlineData(Feature.Security)]
    [InlineData(Feature.BasicExport)]
    [InlineData(Feature.DataStatus)]
    [InlineData(Feature.HoldingsList)]
    public void What_is_never_hidden_shows_in_Simple(Feature feature) =>
        Assert.True(FeaturePolicy.Shows(feature, ExperienceMode.Simple));

    [Fact]
    public void No_page_reads_the_mode_itself()
    {
        // Settings and onboarding configure the choice; independent snapshot fixtures set their own review context.
        // Keep the additional fixture exception exact and unavailable to ordinary Release, rather than exempting Diagnostics.
        const string retainedFixture = "Diagnostics/RetainedAggregateReview.cs";
        var root = RepositoryPaths.Combine(App);
        var fixtureSource = File.ReadAllText(Path.Combine(root, retainedFixture));
        var fixtureConditions = fixtureSource.Split('\n').Select(line => line.Trim())
            .Where(line => line.StartsWith("#if", StringComparison.Ordinal)
                || line.StartsWith("#elif", StringComparison.Ordinal)
                || line.StartsWith("#else", StringComparison.Ordinal)
                || line.StartsWith("#endif", StringComparison.Ordinal)).ToArray();
        Assert.NotEmpty(fixtureConditions);
        Assert.Equal("#if DEBUG || ZANANCE_INDEPENDENT_HOME_REVIEW", fixtureConditions[0]);
        Assert.StartsWith(fixtureConditions[0], fixtureSource, StringComparison.Ordinal);
        Assert.EndsWith("#endif", fixtureSource.TrimEnd(), StringComparison.Ordinal);
        // Nested platform branches are valid; the outer independent-build gate must never have a Release fallback.
        var depth = 0;
        for (var index = 0; index < fixtureConditions.Length; index++)
        {
            var condition = fixtureConditions[index];
            if (condition.StartsWith("#if", StringComparison.Ordinal)) { depth++; }
            else if (condition.StartsWith("#endif", StringComparison.Ordinal))
            {
                Assert.True(depth > 0, "The independent fixture condition must remain balanced.");
                depth--;
                Assert.True(depth > 0 || index == fixtureConditions.Length - 1, "The fixture must stay inside its outer build condition.");
            }
            else { Assert.True(depth > 1, "The independent fixture must not compile an ordinary Release fallback."); }
        }
        Assert.Equal(0, depth);
        Assert.Contains("AppInfo.Current.PackageName.StartsWith(\"pro.vafadar.zanance.storepreview.\", StringComparison.Ordinal)", fixtureSource, StringComparison.Ordinal);
        Assert.Contains("AppInfo.Current.PackageName == \"pro.vafadar.zanance.androidhomereview\"", fixtureSource, StringComparison.Ordinal);
        string[] writers = ["Features/Settings/SettingsViewModel.cs", "Features/Onboarding/OnboardingViewModel.cs", "Diagnostics/DebugSnapshots.cs", retainedFixture];
        var readers = Directory.EnumerateFiles(root, "*.*", SearchOption.AllDirectories)
            .Where(path => path.EndsWith(".cs", StringComparison.Ordinal) || path.EndsWith(".xaml", StringComparison.Ordinal))
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                && !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Select(path => Path.GetRelativePath(root, path).Replace('\\', '/'))
            .Where(path => !writers.Contains(path))
            .Where(path =>
            {
                var source = File.ReadAllText(Path.Combine(root, path));
                // The sync comparison translates a stored wire value; it does not select app capabilities.
                // Exempt only that caption expression, so any other mode read in the same file still fails.
                if (path == "Features/Sync/PersonalSyncReviewPresenter.Portable.cs")
                    source = source.Replace("\"Mode_\" + (ExperienceMode)value.GetInt64()", string.Empty, StringComparison.Ordinal);
                return source.Contains("ExperienceMode", StringComparison.Ordinal);
            })
            .ToList();

        Assert.Empty(readers);
    }

    [Fact]
    public void No_calculation_takes_the_mode()
    {
        // The mode is presentation only: no public calculation of the core receives it, directly or with the settings.
        var methods = typeof(LedgerCalculator).Assembly.GetExportedTypes()
            .Where(type => type != typeof(FeaturePolicy))
            .SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            .Where(method => !method.IsSpecialName);
        var takingMode = methods
            .Where(method => method.GetParameters().Any(p => p.ParameterType == typeof(ExperienceMode) || p.ParameterType == typeof(ZananceSettings)))
            .Select(method => $"{method.DeclaringType!.Name}.{method.Name}")
            .ToList();

        // The budget currency reads the settings for the chosen currency, not for the mode; checked below.
        Assert.Equal(["BudgetCurrency.Resolve"], takingMode);
    }

    [Fact]
    public void The_budget_currency_is_the_same_in_both_modes()
    {
        var accounts = new[] { new Account { Name = "Main", CurrencyCode = "USD" }, new Account { Name = "Second", CurrencyCode = "EUR" } };
        var simple = new ZananceSettings { Mode = ExperienceMode.Simple, DefaultAccountId = accounts[0].Id, HomeBudgetCurrencyCode = "EUR" };
        var advanced = new ZananceSettings { Mode = ExperienceMode.Advanced, DefaultAccountId = accounts[0].Id, HomeBudgetCurrencyCode = "EUR" };

        Assert.Equal(BudgetCurrency.Resolve(advanced, accounts), BudgetCurrency.Resolve(simple, accounts));
    }
}
