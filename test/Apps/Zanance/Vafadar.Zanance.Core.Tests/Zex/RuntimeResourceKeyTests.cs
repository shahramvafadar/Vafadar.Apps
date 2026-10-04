using System.Xml.Linq;
using Vafadar.Testing;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Dashboard;
using Vafadar.Zanance.Core.Goals;
using Vafadar.Zanance.Core.Holdings;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Reports;

namespace Vafadar.Zanance.Core.Tests.Zex;

/// <summary>
/// ZEX-S0904: keys the app builds at run time from enum values (e.g. <c>$"HomeSection_{section}"</c>) exist in English,
/// Persian and German, so a new enum value never shows a raw key. The usage test of the localization library checks the
/// literal keys; this one checks the built ones.
/// </summary>
public sealed class RuntimeResourceKeyTests
{
    private const string Resources = "src/Apps/Zanance/Vafadar.Zanance.App/Resources/Strings/AppResources";

    public static TheoryData<string> Languages() => [string.Empty, ".fa", ".de"];

    [Theory]
    [MemberData(nameof(Languages))]
    public void Every_key_built_from_an_enum_exists(string language)
    {
        var keys = XDocument.Load(RepositoryPaths.Combine(Resources + language + ".resx")).Root!.Elements("data")
            .Select(d => (string)d.Attribute("name")!).ToHashSet(StringComparer.Ordinal);
        IEnumerable<string> Of<T>(string prefix, string suffix = "")
            where T : struct, Enum => Enum.GetValues<T>().Select(v => $"{prefix}{v}{suffix}");

        var expected = Of<HomeSection>("HomeSection_")
            .Concat(Of<EntryKind>("EntryKind_"))
            .Concat(Of<AccountType>("AccountType_"))
            .Concat(Of<AccountGroup>("AccountGroup_"))
            .Concat(Of<GoalState>("GoalState_"))
            .Concat(Of<GoalType>("GoalType_"))
            .Concat(Of<AssetKind>("AssetKind_"))
            .Concat(Of<Metal>("Metal_"))
            .Concat(Of<AssetEventKind>("AssetEvent_"))
            .Concat(Of<AssetEventKind>("AssetEvent_", "Hint"))
            .Concat(Of<DataIssueKind>("Issue_"))
            .Concat(Of<DataIssueKind>("Issue_", "_Action"))
            .Concat(Of<RemainderCause>("Report_Cause_"))
            .Concat(Of<ReviewStep>("Review_"))
            .Concat(new[] { "K01", "K02", "K03", "K04", "K05", "K06", "K07", "K08", "K09", "K10", "K11", "K12", "K13", "K14", "K10H", "Capacity" }
                .SelectMany(id => new[] { "Title", "Question", "Definition", "Included", "Excluded" }.Select(part => $"Kpi_{id}_{part}")));

        var missing = expected.Where(key => !keys.Contains(key)).ToList();
        Assert.Empty(missing);
    }
}
