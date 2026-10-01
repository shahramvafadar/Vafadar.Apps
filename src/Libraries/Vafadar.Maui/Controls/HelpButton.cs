using Vafadar.Localization;

namespace Vafadar.Maui.Controls;

/// <summary>
/// The round "?" next to a field or a switch whose effect is not obvious. Tapping it explains the setting in full,
/// with an example. The texts are looked up by <see cref="Topic"/>: <c>Help_{Topic}_Title</c>, <c>Help_{Topic}_Text</c>
/// and the optional <c>Help_{Topic}_Example</c>. Colors come from the app's <c>Primary</c>, <c>PrimarySoft</c> and
/// <c>PrimaryLine</c> resources when it defines them.
/// </summary>
public sealed class HelpButton : Button
{
    /// <summary>Identifies the <see cref="Topic"/> property.</summary>
    public static readonly BindableProperty TopicProperty = BindableProperty.Create(
        nameof(Topic), typeof(string), typeof(HelpButton), string.Empty,
        propertyChanged: (bindable, _, _) => ((HelpButton)bindable).UpdateDescription());

    /// <summary>Creates the button.</summary>
    public HelpButton()
    {
        Text = "?";
        FontAttributes = FontAttributes.Bold;
        FontSize = 15;
        WidthRequest = 30;
        HeightRequest = 30;
        MinimumWidthRequest = 30;
        MinimumHeightRequest = 30;
        Padding = 0;
        CornerRadius = 15;
        BorderWidth = 1;
        VerticalOptions = LayoutOptions.Center;
        HorizontalOptions = LayoutOptions.Start;

        // A value set in code outranks a dynamic resource, so a fallback colour set here would hide the theme colours
        // (the dark theme showed the light ones). The fallback is only for apps that do not define these resources.
        Colour(TextColorProperty, "Primary", "#1D56C9");
        Colour(BackgroundColorProperty, "PrimarySoft", "#EAF1FD");
        Colour(BorderColorProperty, "PrimaryLine", "#C7D8F7");
        Clicked += async (_, _) => await ShowAsync(Topic);

        // The language can change while the page is shown (onboarding); the spoken name follows it.
        Loaded += (_, _) =>
        {
            Translator.Instance.PropertyChanged += OnTranslatorChanged;
            UpdateDescription();
        };
        Unloaded += (_, _) => Translator.Instance.PropertyChanged -= OnTranslatorChanged;
    }

    private void Colour(BindableProperty property, string key, string fallback)
    {
        if (Application.Current?.Resources.TryGetValue(key, out _) == true)
        {
            SetDynamicResource(property, key);
        }
        else
        {
            SetValue(property, Color.FromArgb(fallback));
        }
    }

    private void OnTranslatorChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e) =>
        Dispatcher.Dispatch(UpdateDescription);

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

    // Screen readers announce "Help: <title>", so the button is never just a question mark.
    private void UpdateDescription()
    {
        if (!string.IsNullOrEmpty(Topic))
        {
            SemanticProperties.SetDescription(this, Translator.Instance.Format("Help_ButtonDescription", Translator.Instance[$"Help_{Topic}_Title"]));
        }
    }
}
