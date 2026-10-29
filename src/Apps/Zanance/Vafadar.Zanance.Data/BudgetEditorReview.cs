using System.Text.Json;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Categories;
using Vafadar.Zanance.Core.Settings;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.Data;

/// <summary>Complete original budget editor references and rights, paired with the eventual actual-file writer.</summary>
public sealed class BudgetEditorReview
{
    /// <summary>Passes only original-file and cached-access authority to a child form, without retaining this graph.</summary>
    public BudgetEditorOrigin CreateEditorOrigin() => new(Choices.DatabasePath, Choices.Access);

    internal BudgetEditorReview(BudgetChoiceSnapshot choices, IReadOnlyList<Account> accounts,
        IReadOnlyList<Category> categories, ZananceSettings? settings)
    {
        Choices = choices; Accounts = accounts; Categories = categories; Settings = settings;
        InputStamp = Capture(accounts, categories, settings);
    }
    /// <summary>Gets immutable original periods, file identity and separate current work rights.</summary>
    public BudgetChoiceSnapshot Choices { get; }
    /// <summary>Gets every original account, including archived references.</summary>
    public IReadOnlyList<Account> Accounts { get; }
    /// <summary>Gets every original category; hidden and missing references must never be guessed away.</summary>
    public IReadOnlyList<Category> Categories { get; }
    /// <summary>Gets the original profile preferences without creating a settings row during a read.</summary>
    public ZananceSettings? Settings { get; }
    /// <summary>Compares the original complete financial/configuration context after an explicit draft-preserving reload.</summary>
    public bool MatchesInputs(BudgetEditorReview other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return InputStamp == other.InputStamp && Choices.DatabasePath == other.Choices.DatabasePath
            && Choices.Revision == other.Choices.Revision && Choices.StoredValue == other.Choices.StoredValue
            && Choices.Today == other.Choices.Today && Choices.MonthStartDay == other.Choices.MonthStartDay
            && Choices.Items.SequenceEqual(other.Choices.Items) && Equals(Choices.Access.Context, other.Choices.Access.Context);
    }
    internal string InputStamp { get; }
    internal static string Capture(IReadOnlyList<Account> accounts, IReadOnlyList<Category> categories, ZananceSettings? settings) =>
        JsonSerializer.Serialize(new { Accounts = accounts, Categories = categories, Settings = settings });
}
