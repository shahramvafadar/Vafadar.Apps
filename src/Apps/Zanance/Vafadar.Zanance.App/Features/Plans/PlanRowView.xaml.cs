using System.Windows.Input;

namespace Vafadar.Zanance.App.Features.Plans;

public partial class PlanRowView : ContentView
{
    /// <summary>What a tap on the row does; the row is then one real button (keyboard, screen readers, touch).</summary>
    public static readonly BindableProperty CommandProperty =
        BindableProperty.Create(nameof(Command), typeof(ICommand), typeof(PlanRowView));

    public static readonly BindableProperty CommandParameterProperty =
        BindableProperty.Create(nameof(CommandParameter), typeof(object), typeof(PlanRowView));

    public PlanRowView()
    {
        InitializeComponent();
    }

    public ICommand? Command
    {
        get => (ICommand?)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    public object? CommandParameter
    {
        get => GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }
}
