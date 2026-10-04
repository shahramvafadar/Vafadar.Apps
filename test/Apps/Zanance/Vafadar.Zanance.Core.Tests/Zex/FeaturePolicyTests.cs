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
        // Settings switches the mode, onboarding sets Simple and the snapshot walk-through sets Advanced; pages ask the
        // policy (S0501 acceptance).
        string[] writers = ["Features/Settings/SettingsViewModel.cs", "Features/Onboarding/OnboardingViewModel.cs", "Diagnostics/DebugSnapshots.cs"];
        var root = RepositoryPaths.Combine(App);
        var readers = Directory.EnumerateFiles(root, "*.*", SearchOption.AllDirectories)
            .Where(path => path.EndsWith(".cs", StringComparison.Ordinal) || path.EndsWith(".xaml", StringComparison.Ordinal))
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                && !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Select(path => Path.GetRelativePath(root, path).Replace('\\', '/'))
            .Where(path => !writers.Contains(path) && File.ReadAllText(Path.Combine(root, path)).Contains("ExperienceMode", StringComparison.Ordinal))
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
