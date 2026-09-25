using CommunityToolkit.Mvvm.ComponentModel;

namespace Vafadar.Maui.Mvvm;

/// <summary>
/// Base class for view models (CommunityToolkit.Mvvm).
/// </summary>
public abstract partial class ViewModelBase : ObservableObject
{
    /// <summary>Gets or sets a value indicating whether the view model is running a long operation.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotBusy))]
    public partial bool IsBusy { get; set; }

    /// <summary>Gets a value indicating whether the view model is idle.</summary>
    public bool IsNotBusy => !IsBusy;
}
