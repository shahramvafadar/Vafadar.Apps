using Microsoft.Extensions.DependencyInjection;
using Syncfusion.Maui.Calendar;
using Vafadar.Localization.Formatting;
using Vafadar.Localization;

namespace Vafadar.Maui.Controls;

/// <summary>
/// A date input that shows the date in the user's display calendar and opens a calendar dialog in the same calendar
/// (Gregorian, Persian or lunar Hijri – Umm al-Qura). The value is always a Gregorian <see cref="DateOnly"/>. An unset value (before 1900, e.g.
/// <c>default(DateOnly)</c>) becomes today: the native calendars cannot show such dates.
/// </summary>
/// <remarks>
/// The field is a real button (a transparent button over the drawn field): it takes keyboard focus and is announced as
/// a button with the date. The outline follows a theme change while the page is open.
/// </remarks>
public sealed class DateField : ContentView
{
    /// <summary>Identifies the <see cref="Date"/> property.</summary>
    public static readonly BindableProperty DateProperty = BindableProperty.Create(
        nameof(Date), typeof(DateOnly), typeof(DateField), DateOnly.FromDateTime(DateTime.Today), BindingMode.TwoWay,
        propertyChanged: (bindable, _, _) => ((DateField)bindable).UpdateText(),
        coerceValue: (_, value) => value is DateOnly { Year: < 1900 } ? DateOnly.FromDateTime(DateTime.Today) : value);

    private readonly Label _text;
    private readonly Border _frame;
    private readonly Button _button;
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

        _frame = new Border
        {
            Padding = new Thickness(12, 10),
            StrokeThickness = 1,
            Stroke = ThemeColors.Outline,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 8 },
            Content = _text,
            MinimumHeightRequest = 48,
            InputTransparent = true,
        };
        AutomationProperties.SetIsInAccessibleTree(_frame, false);

        // The transparent button over the field takes taps, keyboard focus and the spoken date.
        _button = new Button { Text = string.Empty, Padding = 0, BorderWidth = 0, CornerRadius = 8, BackgroundColor = Colors.Transparent };
        _button.Clicked += (_, _) => Open();

        Content = new Grid { Children = { _frame, _button, _calendar } };
        UpdateText();

        // Language or calendar may change while the page is alive (e.g. during onboarding), and so may the theme.
        Loaded += (_, _) =>
        {
            if (Localization is { } localization)
            {
                localization.Changed += OnLocalizationChanged;
            }

            if (Application.Current is { } application)
            {
                application.RequestedThemeChanged += OnThemeChanged;
            }

            _frame.Stroke = ThemeColors.Outline;
            UpdateText();
        };
        Unloaded += (_, _) =>
        {
            if (Localization is { } localization)
            {
                localization.Changed -= OnLocalizationChanged;
            }

            if (Application.Current is { } application)
            {
                application.RequestedThemeChanged -= OnThemeChanged;
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

    private void OnThemeChanged(object? sender, AppThemeChangedEventArgs e) => Dispatcher.Dispatch(() => _frame.Stroke = ThemeColors.Outline);

    private void Open()
    {
        var calendar = Localization?.CurrentCalendar ?? CalendarSystem.Gregorian;
        _calendar.Identifier = calendar switch
        {
            CalendarSystem.Persian => CalendarIdentifier.Persian,
            // The dialog's Umm al-Qura calendar covers 30 April 1900 to 16 November 2077 (1318–1500 AH); a date outside
            // opens in the Gregorian dialog.
            CalendarSystem.Hijri when Date >= new DateOnly(1900, 4, 30) && Date <= new DateOnly(2077, 11, 16) => CalendarIdentifier.UmAlQura,
            _ => CalendarIdentifier.Gregorian,
        };
        _calendar.FlowDirection = FlowDirection;
        _calendar.MonthView.FirstDayOfWeek = Localization?.FirstDayOfWeek ?? DayOfWeek.Monday;
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
        SemanticProperties.SetDescription(_button, _text.Text);
        SemanticProperties.SetHint(_button, Translator.Instance["Common_SelectDate"]);
    }
}
