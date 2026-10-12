using CommunityToolkit.Mvvm.ComponentModel;

namespace Vafadar.Zanance.App.Features.Plans;

/// <summary>The original category identity, caption and selection used by the Plan picker's plain-text rows.</summary>
public sealed partial class PlanCategoryChoice(Guid id, string name) : ObservableObject
{
    /// <summary>Gets the original stored identity.</summary>
    public Guid Id { get; } = id;
    /// <summary>Gets the original translated or user-written picker caption.</summary>
    public string Name { get; } = name;
    /// <summary>Gets the existing mutually exclusive in-memory selection.</summary>
    [ObservableProperty]
    public partial bool IsSelected { get; set; }
    /// <summary>Returns the complete plain-text caption without unused native color/icon allocation.</summary>
    public override string ToString() => Name;
}
