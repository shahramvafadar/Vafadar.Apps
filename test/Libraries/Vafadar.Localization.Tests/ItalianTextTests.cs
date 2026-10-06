using System.Globalization;
using System.Xml.Linq;
using Vafadar.Testing;

namespace Vafadar.Localization.Tests;

/// <summary>
/// Italian texts put together from several resources read as one sentence once the values are inserted, and weekday
/// phrases agree with the weekday's gender: "il primo lunedì" but "la prima domenica" (D-57).
/// </summary>
public sealed class ItalianTextTests
{
    private const string AppResources = "src/Apps/Zanance/Vafadar.Zanance.App/Resources/Strings/AppResources";
    private static readonly CultureInfo Italian = CultureInfo.GetCultureInfo("it");
    private static readonly Lazy<Dictionary<string, string>> App = new(() => Read($"{AppResources}.it.resx"));

    [Theory]
    [InlineData("Rule_Weekly", "Ogni settimana, di lunedì", "lunedì")]
    [InlineData("Rule_EveryNWeeks", "Ogni 2 settimane, di domenica", "domenica", 2)]
    [InlineData("Rule_Monthly", "Ogni mese, il primo lunedì", "il primo lunedì")]
    [InlineData("Rule_Monthly", "Ogni mese, l’ultimo venerdì", "l’ultimo venerdì")]
    [InlineData("Rule_EveryNMonths", "Ogni 2 mesi, il giorno 8", "il giorno 8", 2)]
    [InlineData("Rule_Yearly", "Ogni anno, l’ultimo giorno del mese di aprile", "l’ultimo giorno del mese di aprile")]
    [InlineData("Rule_Yearly", "Ogni anno, l’ultimo giorno del mese di Esfand", "l’ultimo giorno del mese di Esfand")]
    [InlineData("Rule_Yearly", "Ogni anno, in data 8 agosto", "in data 8 agosto")]
    [InlineData("Onb_Step", "Passaggio 1 di 3", 1, 3)]
    [InlineData("Entry_ToAmount", "Importo ricevuto (CHF)", "CHF")]
    [InlineData("Loan_ConfirmPay", "La quota capitale di 100,00 EUR viene registrata come trasferimento e gli interessi di 5,00 EUR come spesa sul conto Principale.", "100,00 EUR", "5,00 EUR", "Principale")]
    [InlineData("Loan_ConfirmReceive", "La quota capitale di 100,00 EUR viene registrata come trasferimento e gli interessi di 5,00 EUR come entrata sul conto Risparmio.", "100,00 EUR", "5,00 EUR", "Risparmio")]
    [InlineData("Report_ChangeRange", "Dalla fine del 30 settembre 2026 al 31 ottobre 2026", "30 settembre 2026", "31 ottobre 2026")]
    [InlineData("Report_Months", "Mesi: 1", "1")]
    [InlineData("Report_Months", "Mesi: 1,5", "1,5")]
    [InlineData("Occurrence_OverdueDays", "In ritardo di 2 giorni", 2)]
    [InlineData("Plan_ReminderText", "3 giorni prima, alle 09:00", 3, "09:00")]
    [InlineData("Unit_AmountsIn", "Importi in Toman (Toman = 10 IRR)", "Toman", "10 IRR")]
    public void Composed_text_reads_as_a_sentence(string key, string expected, params object[] args)
    {
        Assert.Equal(expected, string.Format(Italian, App.Value[key], args));
    }

    [Theory]
    [InlineData(DayOfWeek.Monday, 1, "primo lunedì", "il primo lunedì")]
    [InlineData(DayOfWeek.Monday, 2, "secondo lunedì", "il secondo lunedì")]
    [InlineData(DayOfWeek.Monday, 3, "terzo lunedì", "il terzo lunedì")]
    [InlineData(DayOfWeek.Monday, 4, "quarto lunedì", "il quarto lunedì")]
    [InlineData(DayOfWeek.Monday, 5, "quinto lunedì", "il quinto lunedì")]
    [InlineData(DayOfWeek.Tuesday, 1, "primo martedì", "il primo martedì")]
    [InlineData(DayOfWeek.Tuesday, 2, "secondo martedì", "il secondo martedì")]
    [InlineData(DayOfWeek.Tuesday, 3, "terzo martedì", "il terzo martedì")]
    [InlineData(DayOfWeek.Tuesday, 4, "quarto martedì", "il quarto martedì")]
    [InlineData(DayOfWeek.Tuesday, 5, "quinto martedì", "il quinto martedì")]
    [InlineData(DayOfWeek.Wednesday, 1, "primo mercoledì", "il primo mercoledì")]
    [InlineData(DayOfWeek.Wednesday, 2, "secondo mercoledì", "il secondo mercoledì")]
    [InlineData(DayOfWeek.Wednesday, 3, "terzo mercoledì", "il terzo mercoledì")]
    [InlineData(DayOfWeek.Wednesday, 4, "quarto mercoledì", "il quarto mercoledì")]
    [InlineData(DayOfWeek.Wednesday, 5, "quinto mercoledì", "il quinto mercoledì")]
    [InlineData(DayOfWeek.Thursday, 1, "primo giovedì", "il primo giovedì")]
    [InlineData(DayOfWeek.Thursday, 2, "secondo giovedì", "il secondo giovedì")]
    [InlineData(DayOfWeek.Thursday, 3, "terzo giovedì", "il terzo giovedì")]
    [InlineData(DayOfWeek.Thursday, 4, "quarto giovedì", "il quarto giovedì")]
    [InlineData(DayOfWeek.Thursday, 5, "quinto giovedì", "il quinto giovedì")]
    [InlineData(DayOfWeek.Friday, 1, "primo venerdì", "il primo venerdì")]
    [InlineData(DayOfWeek.Friday, 2, "secondo venerdì", "il secondo venerdì")]
    [InlineData(DayOfWeek.Friday, 3, "terzo venerdì", "il terzo venerdì")]
    [InlineData(DayOfWeek.Friday, 4, "quarto venerdì", "il quarto venerdì")]
    [InlineData(DayOfWeek.Friday, 5, "quinto venerdì", "il quinto venerdì")]
    [InlineData(DayOfWeek.Saturday, 1, "primo sabato", "il primo sabato")]
    [InlineData(DayOfWeek.Saturday, 2, "secondo sabato", "il secondo sabato")]
    [InlineData(DayOfWeek.Saturday, 3, "terzo sabato", "il terzo sabato")]
    [InlineData(DayOfWeek.Saturday, 4, "quarto sabato", "il quarto sabato")]
    [InlineData(DayOfWeek.Saturday, 5, "quinto sabato", "il quinto sabato")]
    [InlineData(DayOfWeek.Sunday, 1, "prima domenica", "la prima domenica")]
    [InlineData(DayOfWeek.Sunday, 2, "seconda domenica", "la seconda domenica")]
    [InlineData(DayOfWeek.Sunday, 3, "terza domenica", "la terza domenica")]
    [InlineData(DayOfWeek.Sunday, 4, "quarta domenica", "la quarta domenica")]
    [InlineData(DayOfWeek.Sunday, 5, "quinta domenica", "la quinta domenica")]
    public void An_nth_weekday_agrees_with_the_gender_of_the_weekday(DayOfWeek day, int week, string choice, string sentence)
    {
        // Composed as PlanEditorViewModel (choice) and PlanText (sentence) do; the fifth week only checks the words,
        // the recurrence itself still treats it as the last weekday (REC-12).
        var weekday = Italian.DateTimeFormat.GetDayName(day);
        var ordinal = App.Value[WeekdayGrammar.OrdinalKey(week, Italian, day)];

        Assert.Equal(choice, string.Format(Italian, App.Value["DayRule_NthWeekday"], ordinal, weekday));
        Assert.Equal(sentence, string.Format(Italian, App.Value[WeekdayGrammar.TemplateKey("Rule_NthWeekday", Italian, day)], ordinal, weekday));
    }

    [Theory]
    [InlineData(DayOfWeek.Monday, "Ultimo lunedì", "l’ultimo lunedì")]
    [InlineData(DayOfWeek.Friday, "Ultimo venerdì", "l’ultimo venerdì")]
    [InlineData(DayOfWeek.Sunday, "Ultima domenica", "l’ultima domenica")]
    public void The_last_weekday_agrees_with_the_gender_of_the_weekday(DayOfWeek day, string choice, string sentence)
    {
        var weekday = Italian.DateTimeFormat.GetDayName(day);

        Assert.Equal(choice, string.Format(Italian, App.Value[WeekdayGrammar.TemplateKey("DayRule_LastWeekday", Italian, day)], weekday));
        Assert.Equal(sentence, string.Format(Italian, App.Value[WeekdayGrammar.TemplateKey("Rule_LastWeekday", Italian, day)], weekday));
    }

    [Theory]
    [InlineData("it", DayOfWeek.Sunday, true)]
    [InlineData("it-IT", DayOfWeek.Sunday, true)]
    [InlineData("it-CH", DayOfWeek.Sunday, true)]
    [InlineData("it", DayOfWeek.Saturday, false)]
    [InlineData("it", DayOfWeek.Monday, false)]
    [InlineData("en", DayOfWeek.Sunday, false)]
    [InlineData("es", DayOfWeek.Sunday, false)]
    [InlineData("fr", DayOfWeek.Sunday, false)]
    [InlineData("de", DayOfWeek.Sunday, false)]
    [InlineData("fa", DayOfWeek.Sunday, false)]
    public void Only_an_Italian_Sunday_takes_the_feminine_keys(string culture, DayOfWeek day, bool feminine)
    {
        var info = CultureInfo.GetCultureInfo(culture);

        Assert.Equal(feminine, WeekdayGrammar.IsFeminine(info, day));
        Assert.Equal(feminine ? "Ordinal_Feminine_2" : "Ordinal_2", WeekdayGrammar.OrdinalKey(2, info, day));
        Assert.Equal(feminine ? "Rule_LastWeekday_Feminine" : "Rule_LastWeekday", WeekdayGrammar.TemplateKey("Rule_LastWeekday", info, day));
    }

    [Theory]
    [InlineData("")]
    [InlineData(".fa")]
    [InlineData(".de")]
    [InlineData(".es")]
    [InlineData(".fr")]
    public void Other_languages_keep_their_texts_in_the_feminine_keys(string suffix)
    {
        // The feminine keys exist in every language for key parity; outside Italian they copy the plain key unchanged.
        var values = Read($"{AppResources}{suffix}.resx");
        foreach (var (feminine, plain) in new[]
                 {
                     ("Ordinal_Feminine_1", "Ordinal_1"), ("Ordinal_Feminine_2", "Ordinal_2"), ("Ordinal_Feminine_3", "Ordinal_3"),
                     ("Ordinal_Feminine_4", "Ordinal_4"), ("Ordinal_Feminine_5", "Ordinal_5"), ("Rule_NthWeekday_Feminine", "Rule_NthWeekday"),
                     ("Rule_LastWeekday_Feminine", "Rule_LastWeekday"), ("DayRule_LastWeekday_Feminine", "DayRule_LastWeekday"),
                 })
        {
            Assert.Equal(values[plain], values[feminine]);
        }
    }

    private static Dictionary<string, string> Read(string path) =>
        XDocument.Load(RepositoryPaths.Combine(path)).Root!.Elements("data")
            .ToDictionary(d => (string)d.Attribute("name")!, d => (string?)d.Element("value") ?? string.Empty);
}
