using System.Text.RegularExpressions;
using Vafadar.Core.Text;
using Vafadar.Zanance.Core.Money;

namespace Vafadar.Zanance.Core.Analysis;

/// <summary>Allowlisted local questions that can only produce read-only aggregates.</summary>
public enum AiQuestionIntent
{
    /// <summary>Compares recorded net spending by category in two equal intervals.</summary>
    SpendingChange,
    /// <summary>Explains recorded plans and shortfalls in a selected future interval.</summary>
    PlanShortfall,
}

/// <summary>A supported local interpretation. The originating words never enter a provider DTO.</summary>
public sealed record AiQuestion(AiQuestionIntent Intent, string? Currency, bool NextMonth);

/// <summary>Small deterministic vocabulary for recognizing supported questions in the app languages.</summary>
public static partial class AiQuestionParser
{
    private static readonly string[] Spending = ["spending", "expenses", "ausgaben", "gasto", "depenses", "spese", "هزینه", "مخارج"];
    private static readonly string[] Change = ["rise", "rose", "increase", "change", "higher", "why", "gestiegen", "verandert", "warum",
        "subio", "aumento", "cambio", "pourquoi", "augmente", "varie", "perche", "aumentat", "varia", "چرا", "تغییر", "افزایش"];
    private static readonly string[] Plans = ["plan", "payment", "scheduled", "geplant", "zahlung", "pago", "previst", "paiement",
        "planifi", "pagament", "pianificat", "برنامه", "پرداخت"];
    private static readonly string[] Shortfall = ["shortfall", "deficit", "negative", "fehlbetrag", "mancante", "scoperto",
        "negativ", "insuffisance", "manque", "saldo negativo", "کسری", "کمبود", "منفی"];
    private static readonly string[] NextMonths = ["next month", "nachsten monat", "proximo mes", "mes proximo", "mois prochain",
        "prossimo mese", "mese prossimo", "ماه بعد", "ماه اینده"];

    private static readonly string[] OtherPeriods = ["last month", "last week", "last year", "next week", "next year",
        "yesterday", "quarter", "letzten monat", "letzte woche", "mes pasado", "semana pasada", "mois dernier",
        "semaine derniere", "mese scorso", "settimana scorsa", "ماه گذشته", "ماه قبل", "هفته قبل", "سال بعد"];

    /// <summary>Returns a result only when the supported purpose and any explicit ISO currency are unambiguous.</summary>
    public static AiQuestion? Interpret(string? question)
    {
        if (string.IsNullOrWhiteSpace(question) || question.Length > 240) return null;
        var folded = SearchText.Fold(question);
        var spending = Has(Spending) && Has(Change);
        var plans = Has(Plans) && Has(Shortfall);
        var nextMonth = Has(NextMonths);
        if (spending == plans || spending && nextMonth || Has(OtherPeriods)) return null;
        var currencies = IsoWords().Matches(question).Select(match => match.Value)
            .Where(code => Currencies.TryGet(code, out _)).Distinct(StringComparer.Ordinal).Take(2).ToArray();
        return currencies.Length > 1 ? null : new(spending ? AiQuestionIntent.SpendingChange : AiQuestionIntent.PlanShortfall,
            currencies.FirstOrDefault(), nextMonth);

        bool Has(string[] words) => words.Any(word =>
        {
            for (var start = 0; start < folded.Length;)
            {
                var index = folded.IndexOf(word, start, StringComparison.Ordinal);
                if (index < 0) return false;
                if (index == 0 || !char.IsLetterOrDigit(folded[index - 1])) return true;
                start = index + 1;
            }
            return false;
        });
    }

    [GeneratedRegex(@"(?<![\p{L}\p{N}])[A-Z]{3}(?![\p{L}\p{N}])", RegexOptions.CultureInvariant)]
    private static partial Regex IsoWords();
}
