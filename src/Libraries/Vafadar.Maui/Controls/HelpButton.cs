using Vafadar.Localization;

namespace Vafadar.Maui.Controls;

/// <summary>
/// The round "?" next to a field or a switch whose effect is not obvious. Tapping it explains the setting in full,
/// with an example. The texts are looked up by <see cref="Topic"/>: <c>Help_{Topic}_Title</c>, <c>Help_{Topic}_Text</c>
/// and the optional <c>Help_{Topic}_Example</c>. Colors come from the app's <c>Primary</c>, <c>PrimarySoft</c> and
/// <c>PrimaryLine</c> resources when it defines them.
/// </summary>
/// <remarks>
/// The circle is <see cref="CircleSize"/> wide, the button that takes taps and keyboard focus around it
/// <see cref="TouchSize"/>: a 30 px target was hard to hit on a phone (touch targets should be at least 44 px), while a
/// larger circle would crowd the labels it stands next to.
/// </remarks>
public sealed class HelpButton : ContentView
{
    /// <summary>The diameter of the visible circle.</summary>
    public const double CircleSize = 30;

    /// <summary>The width and height of the area that reacts to a tap.</summary>
    public const double TouchSize = 44;

    /// <summary>Identifies the <see cref="Topic"/> property.</summary>
    public static readonly BindableProperty TopicProperty = BindableProperty.Create(
        nameof(Topic), typeof(string), typeof(HelpButton), string.Empty,
        propertyChanged: (bindable, _, _) => ((HelpButton)bindable).UpdateDescription());

    private readonly Button _button;

    /// <summary>Creates the button.</summary>
    public HelpButton()
    {
        WidthRequest = TouchSize;
        HeightRequest = TouchSize;
        VerticalOptions = LayoutOptions.Center;
        HorizontalOptions = LayoutOptions.Start;

        var circle = new Border
        {
            WidthRequest = CircleSize,
            HeightRequest = CircleSize,
            StrokeThickness = 1,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = CircleSize / 2 },
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            InputTransparent = true,
        };
        AutomationProperties.SetIsInAccessibleTree(circle, false);

        // A transparent round button over the whole touch area carries the "?" (centred on the circle), the touch
        // feedback, the keyboard focus and the spoken name.
        _button = new Button
        {
            Text = "?",
            FontAttributes = FontAttributes.Bold,
            FontSize = 15,
            WidthRequest = TouchSize,
            HeightRequest = TouchSize,
            MinimumWidthRequest = TouchSize,
            MinimumHeightRequest = TouchSize,
            Padding = 0,
            CornerRadius = (int)(TouchSize / 2),
            BorderWidth = 0,
            BackgroundColor = Colors.Transparent,
        };

        // A value set in code outranks a dynamic resource, so a fallback colour set here would hide the theme colours
        // (the dark theme showed the light ones). The fallback is only for apps that do not define these resources.
        Colour(_button, Button.TextColorProperty, "Primary", "#1D56C9");
        Colour(circle, BackgroundColorProperty, "PrimarySoft", "#EAF1FD");
        Colour(circle, Border.StrokeProperty, "PrimaryLine", "#C7D8F7");
        _button.Clicked += async (_, _) => await ShowAsync(Topic);
        Content = new Grid { Children = { circle, _button } };

        // The language can change while the page is shown (onboarding); the spoken name follows it.
        Loaded += (_, _) =>
        {
            Translator.Instance.PropertyChanged += OnTranslatorChanged;
            UpdateDescription();
        };
        Unloaded += (_, _) => Translator.Instance.PropertyChanged -= OnTranslatorChanged;
    }

    /// <summary>Gets or sets the help topic, e.g. <c>IncludeInTotals</c>.</summary>
    public string Topic
    {
        get => (string)GetValue(TopicProperty);
        set => SetValue(TopicProperty, value);
    }

    /// <summary>Shows the help of <paramref name="topic"/>: title, explanation and, when there is one, an example.</summary>
    public static async Task ShowAsync(string topic)
    {
        if (string.IsNullOrWhiteSpace(topic) || Application.Current?.Windows.FirstOrDefault()?.Page is not { } page)
        {
            return;
        }

        var translator = Translator.Instance;
        var text = translator[$"Help_{topic}_Text"];
        if (translator.TryGetString($"Help_{topic}_Example", out var example))
        {
            text += Environment.NewLine + Environment.NewLine + translator.Format("Help_Example", example);
        }

        // Modal pages (editors) show the dialog themselves; otherwise the window's page does.
        var host = page.Navigation.ModalStack.LastOrDefault() ?? (page as Shell)?.CurrentPage ?? page;
        await host.DisplayAlertAsync(translator[$"Help_{topic}_Title"], text, translator["Common_Ok"]);
    }

    private static void Colour(VisualElement target, BindableProperty property, string key, string fallback)
    {
        if (Application.Current?.Resources.TryGetValue(key, out _) == true)
        {
            target.SetDynamicResource(property, key);
        }
        else
        {
            var colour = Color.FromArgb(fallback);
            target.SetValue(property, property.ReturnType == typeof(Brush) ? new SolidColorBrush(colour) : colour);
        }
    }

    private void OnTranslatorChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e) =>
        Dispatcher.Dispatch(UpdateDescription);

    // Screen readers announce "Help: <title>", so the button is never just a question mark.
    private void UpdateDescription()
    {
        if (!string.IsNullOrEmpty(Topic))
        {
            SemanticProperties.SetDescription(_button, Translator.Instance.Format("Help_ButtonDescription", Translator.Instance[$"Help_{Topic}_Title"]));
        }
    }
}
