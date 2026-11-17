namespace Vafadar.Dastan.Web.Site;

/// <summary>How a page marks a text that exists in the language of the collection only.</summary>
public static class ForeignText
{
    /// <summary>Returns the direction of writing of a language.</summary>
    public static string Direction(string language)
    {
        ArgumentNullException.ThrowIfNull(language);
        var primary = language.Split('-')[0];
        return primary is "fa" or "ar" or "he" or "ur" ? "rtl" : "ltr";
    }

    /// <summary>Returns the name of the text of the pages that says in which language such a text is.</summary>
    public static string Note(string language) => language switch
    {
        "en" => "Foreign_Note",
        "fa" => "Foreign_Note_fa",
        _ => "Foreign_Note_Other",
    };
}
