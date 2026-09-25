using Microsoft.Extensions.DependencyInjection;
using Syncfusion.Maui.Calendar;
using Vafadar.Localization;
using Vafadar.Localization.Formatting;

namespace Vafadar.Maui.Controls;

/// <summary>
/// A date input that shows the date in the user's display calendar and opens a calendar dialog in the same calendar
/// (Gregorian or Persian). The value is always a Gregorian <see cref="DateOnly"/>.
/// </summary>
public sealed class DateField : ContentView
{
    /// <summary>Identifies the <see cref="Date"/> property.</summary>
    public static readonly BindableProperty DateProperty = BindableProperty.Create(
        nameof(Date), typeof(DateOnly), typeof(DateField), DateOnly.FromDateTime(DateTime.Today), BindingMode.TwoWay,
        propertyChanged: (bindable, _, _) => ((DateField)bindable).UpdateText());

    private readonly Label _text;
    private readonly SfCalendar _calendar;

    /// <summary>Creates the control.</summary>
    public DateField()
    {
        _text = new Label { VerticalOptions = LayoutOptions.Center, FontSize = 16 };
        _calendar = new SfCalendar
        {
            Mode = CalendarMode.Dialog,
            SelectionMode = CalendarSelectionMode.Single,
            FooterView = new CalendarFooterView { ShowActionButtons = true, ShowTodayButton = true },
            HeightRequest = 0,
            WidthRequest = 0,
        };
        _calendar.AcceptCommand = new Command(Accept);
        _calendar.DeclineCommand = new Command(() => _calendar.IsOpen = false);

        var frame = new Border
        {
            Padding = new Thickness(12, 10),
            StrokeThickness = 1,
            Stroke = Color.FromArgb("#C8C8C8"),
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 8 },
            Content = _text,
            MinimumHeightRequest = 48,
        };
        frame.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(Open) });
        SemanticProperties.SetHint(frame, Translator.Instance["Common_SelectDate"]);

        Content = new Grid { Children = { frame, _calendar } };
        UpdateText();

        // Language or calendar may change while the page is alive (e.g. during onboarding).
        Loaded += (_, _) =>
        {
            if (Localization is { } localization)
            {
                localization.Changed += OnLocalizationChanged;
            }

            UpdateText();
        };
        Unloaded += (_, _) =>
        {
            if (Localization is { } localization)
            {
                localization.Changed -= OnLocalizationChanged;
            }
        };
    }

    /// <summary>Gets or sets the selected date.</summary>
    public DateOnly Date
    {
        get => (DateOnly)GetValue(DateProperty);
        set => SetValue(DateProperty, value);
    }

    private static IServiceProvider? Services => IPlatformApplication.Current?.Services;

    private static ILocalizationService? Localization => Services?.GetService<ILocalizationService>();

    private void OnLocalizationChanged(object? sender, EventArgs e) => Dispatcher.Dispatch(UpdateText);

    private void Open()
    {
        var calendar = Localization?.CurrentCalendar ?? CalendarSystem.Gregorian;
        _calendar.Identifier = calendar == CalendarSystem.Persian ? CalendarIdentifier.Persian : CalendarIdentifier.Gregorian;
        _calendar.FlowDirection = FlowDirection;
        _calendar.SelectedDate = Date.ToDateTime(TimeOnly.MinValue);
        _calendar.DisplayDate = Date.ToDateTime(TimeOnly.MinValue);
        _calendar.IsOpen = true;
    }

    private void Accept()
    {
        if (_calendar.SelectedDate is DateTime selected)
        {
            Date = DateOnly.FromDateTime(selected);
        }

        _calendar.IsOpen = false;
    }

    private void UpdateText()
    {
        var formatter = Services?.GetService<IDateFormatter>();
        _text.Text = formatter?.Format(Date, DateFormatStyle.Long) ?? Date.ToString("d", Translator.Instance.Culture);
        SemanticProperties.SetDescription(this, _text.Text);
    }
}
