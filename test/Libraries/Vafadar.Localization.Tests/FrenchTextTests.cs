using System.Globalization;
using System.Xml.Linq;
using Vafadar.Testing;

namespace Vafadar.Localization.Tests;

/// <summary>
/// French texts put together from several resources read as one sentence once the values are inserted (counts,
/// ordinals, months, dates and accounts in the right place).
/// </summary>
public sealed class FrenchTextTests
{
    private static readonly Lazy<Dictionary<string, string>> App = new(() => Read("src/Apps/Zanance/Vafadar.Zanance.App/Resources/Strings/AppResources.fr.resx"));

    [Theory]
    [InlineData("Rule_Weekly", "Chaque semaine, le vendredi", "vendredi")]
    [InlineData("Rule_EveryNWeeks", "Toutes les 2 semaines, le lundi", "lundi", 2)]
    [InlineData("Rule_NthWeekday", "le premier lundi", "premier", "lundi")]
    [InlineData("Rule_NthWeekday", "le troisième vendredi", "troisième", "vendredi")]
    [InlineData("Rule_LastDayOfNamedMonth", "le dernier jour du mois (février)", "février")]
    [InlineData("Rule_LastDayOfNamedMonth", "le dernier jour du mois (avril)", "avril")]
    [InlineData("Rule_LastDayOfNamedMonth", "le dernier jour du mois (août)", "août")]
    [InlineData("Rule_LastDayOfNamedMonth", "le dernier jour du mois (Esfand)", "Esfand")]
    [InlineData("Occurrence_OverdueOneDay", "1 jour de retard")]
    [InlineData("Occurrence_OverdueDays", "3 jours de retard", 3)]
    [InlineData("Plan_ReminderOnDueDay", "Le jour de l’échéance, à 09:00", "09:00")]
    [InlineData("Plan_ReminderOneDay", "1 jour avant, à 09:00", "09:00")]
    [InlineData("Plan_ReminderText", "3 jours avant, à 09:00", 3, "09:00")]
    [InlineData("Entry_ReimbursementOpen", "Camille doit rembourser 120,00 EUR ; reste à recevoir : 20,00 EUR", "120,00 EUR", "Camille", "20,00 EUR")]
    [InlineData("Loan_ConfirmPay", "Le capital de 180,00 EUR est enregistré comme virement depuis le compte Principal, et les intérêts de 20,00 EUR comme dépense sur ce compte.", "180,00 EUR", "20,00 EUR", "Principal")]
    [InlineData("Loan_ConfirmReceive", "Le capital de 180,00 EUR est enregistré comme virement vers le compte Principal, et les intérêts de 20,00 EUR comme revenu sur ce compte.", "180,00 EUR", "20,00 EUR", "Principal")]
    [InlineData("Report_ChangeRange", "De la fin du 30 septembre 2026 au 5 octobre 2026", "30 septembre 2026", "5 octobre 2026")]
    [InlineData("Forecast_Shortfall", "Le solde pourrait passer sous zéro vers le 24 octobre 2026.", "24 octobre 2026")]
    [InlineData("Unit_AmountsIn", "Montants en Toman (Toman = 10 IRR)", "Toman", "10 IRR")]
    public void Composed_text_reads_as_a_sentence(string key, string expected, params object[] args)
    {
        // The pack writes typographic spaces (e.g. before ";" and ":"); compare them as ordinary spaces.
        Assert.Equal(expected, Spaces(string.Format(CultureInfo.GetCultureInfo("fr"), App.Value[key], args)));
    }

    private static string Spaces(string text) => text.Replace(' ', ' ').Replace(' ', ' ');

    private static Dictionary<string, string> Read(string path) =>
        XDocument.Load(RepositoryPaths.Combine(path)).Root!.Elements("data")
            .ToDictionary(d => (string)d.Attribute("name")!, d => (string?)d.Element("value") ?? string.Empty);
}
