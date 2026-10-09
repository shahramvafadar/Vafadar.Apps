using System.Windows.Input;
using Microsoft.Maui.Controls.Shapes;

namespace Vafadar.Zanance.App.Presentation;

/// <summary>The existing semantic action surfaces supported by a growing action.</summary>
public enum ActionAppearance
{
    /// <summary>The main blue action.</summary>
    Primary,
    /// <summary>An outlined neutral action.</summary>
    Secondary,
    /// <summary>A transparent blue action.</summary>
    Text,
    /// <summary>A rounded blue tag suggestion on the existing page surface (D-89).</summary>
    Suggestion,
    /// <summary>An outlined red action where data can be lost.</summary>
    Destructive,
}

/// <summary>
/// A wrapping, natively scaled caption with a real transparent button for commands, keyboard and spoken name.
/// Native button captions can clip long translations at large text even when the outer height is automatic (D-80).
/// </summary>
public sealed class WrappingAction : ContentView
{
    /// <summary>Identifies the translated action caption.</summary>
    public static readonly BindableProperty TextProperty = BindableProperty.Create(nameof(Text), typeof(string),
        typeof(WrappingAction), string.Empty, propertyChanged: (bindable, _, value) =>
            SemanticProperties.SetDescription(((WrappingAction)bindable)._button, (string?)value ?? string.Empty));

    /// <summary>Identifies the existing native button command.</summary>
    public static readonly BindableProperty CommandProperty = BindableProperty.Create(nameof(Command), typeof(ICommand), typeof(WrappingAction));

    /// <summary>Identifies the existing command argument.</summary>
    public static readonly BindableProperty CommandParameterProperty = BindableProperty.Create(nameof(CommandParameter), typeof(object), typeof(WrappingAction));

    /// <summary>Identifies the semantic surface, independent of the current theme.</summary>
    public static readonly BindableProperty AppearanceProperty = BindableProperty.Create(nameof(Appearance), typeof(ActionAppearance),
        typeof(WrappingAction), ActionAppearance.Secondary, propertyChanged: (bindable, _, _) => ((WrappingAction)bindable).RefreshAppearance());

    /// <summary>Identifies the existing compact bulk caption size, with native scaling and smaller side padding.</summary>
    public static readonly BindableProperty IsCompactProperty = BindableProperty.Create(nameof(IsCompact), typeof(bool),
        typeof(WrappingAction), false, propertyChanged: (bindable, _, _) => ((WrappingAction)bindable).RefreshAppearance());

    private readonly Label _caption;
    private readonly Border _face;
    private readonly Button _button;

    /// <summary>Creates a growing caption and keeps its real command button last in the visual stack (D-42).</summary>
    public WrappingAction()
    {
        MinimumHeightRequest = 44;
        _caption = new Label { FontSize = 15, LineBreakMode = LineBreakMode.WordWrap,
            HorizontalTextAlignment = TextAlignment.Center, VerticalOptions = LayoutOptions.Center, InputTransparent = true };
        _caption.SetDynamicResource(Label.FontFamilyProperty, "AppFont");
        _caption.SetBinding(Label.TextProperty, new Binding(nameof(Text), source: this));
        _face = new Border { Content = _caption, StrokeShape = new RoundRectangle { CornerRadius = 14 }, InputTransparent = true };
        AutomationProperties.SetIsInAccessibleTree(_face, false);
        _button = new Button { Text = string.Empty, Padding = 0, BorderWidth = 0, CornerRadius = 14, BackgroundColor = Colors.Transparent };
        if (Application.Current?.Resources.TryGetValue("OverlayButton", out var style) == true && style is Style overlay)
        { _button.Style = overlay; }
        _button.SetBinding(Button.CommandProperty, new Binding(nameof(Command), source: this));
        _button.SetBinding(Button.CommandParameterProperty, new Binding(nameof(CommandParameter), source: this));
        // Command CanExecute and inherited enablement remain native button behavior; only the painted face is dimmed.
        _button.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(Button.IsEnabled)) { _face.Opacity = _button.IsEnabled ? 1 : .5; }
        };
        Content = new Grid { Children = { _face, _button } };
        RefreshAppearance();
    }

    /// <summary>Gets or sets the complete translated caption.</summary>
    public string Text { get => (string)GetValue(TextProperty); set => SetValue(TextProperty, value); }

    /// <summary>Gets or sets the existing command, including its CanExecute behavior.</summary>
    public ICommand? Command { get => (ICommand?)GetValue(CommandProperty); set => SetValue(CommandProperty, value); }

    /// <summary>Gets or sets the argument forwarded unchanged by the native button.</summary>
    public object? CommandParameter { get => GetValue(CommandParameterProperty); set => SetValue(CommandParameterProperty, value); }

    /// <summary>Gets or sets the existing action meaning; dynamic resources follow theme changes.</summary>
    public ActionAppearance Appearance { get => (ActionAppearance)GetValue(AppearanceProperty); set => SetValue(AppearanceProperty, value); }

    /// <summary>Gets or sets whether to retain the original 12-point bulk captions with native scaling enabled.</summary>
    public bool IsCompact { get => (bool)GetValue(IsCompactProperty); set => SetValue(IsCompactProperty, value); }

    private void RefreshAppearance()
    {
        var primary = Appearance == ActionAppearance.Primary;
        var text = Appearance == ActionAppearance.Text;
        var destructive = Appearance == ActionAppearance.Destructive;
        var suggestion = Appearance == ActionAppearance.Suggestion;
        _face.Padding = suggestion ? new Thickness(12, 8) : text ? new Thickness(8, 8) : new Thickness(IsCompact ? 8 : 16, 10);
        _caption.FontSize = suggestion ? 13 : IsCompact ? 12 : 15;
        _face.StrokeShape = new RoundRectangle { CornerRadius = suggestion ? 20 : 14 };
        _button.CornerRadius = suggestion ? 20 : 14;
        _face.MinimumHeightRequest = text ? 44 : 48;
        _face.StrokeThickness = primary || text || suggestion ? 0 : 1;
        if (text) { _face.RemoveDynamicResource(BackgroundColorProperty); _face.BackgroundColor = Colors.Transparent; }
        else { _face.SetDynamicResource(BackgroundColorProperty, primary ? "Primary" : suggestion ? "PageBackground" : "CardBackground"); }
        _face.SetDynamicResource(Border.StrokeProperty, destructive ? "ExpenseText" : "StrokeStrong");
        _caption.SetDynamicResource(Label.TextColorProperty, primary ? "OnPrimary" : text || suggestion ? "Primary" : destructive ? "ExpenseText" : "AmountText");
        _caption.FontAttributes = primary ? FontAttributes.Bold : FontAttributes.None;
    }
}
