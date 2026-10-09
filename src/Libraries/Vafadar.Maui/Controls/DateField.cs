using FluentIcons.Common;
using FluentIcons.Maui;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Layouts;
using Syncfusion.Maui.Calendar;
using Vafadar.Core.Text;
using Vafadar.Localization.Formatting;
using Vafadar.Localization;

namespace Vafadar.Maui.Controls;

/// <summary>
/// A date input with three number boxes – day, month and year – in the user's display calendar (Gregorian, Persian or
/// lunar Hijri), a calendar button that opens a month view in the same calendar, and the full date written below
/// ("Tuesday, 6 October 2026"). The value is always a Gregorian <see cref="DateOnly"/>. An unset value (before 1900,
/// e.g. <c>default(DateOnly)</c>) becomes today.
/// </summary>
/// <remarks>
/// <para>
/// The boxes make a date quick to type or change by hand; the calendar shows the month around it (D-59). The calendar
/// is a Syncfusion calendar in dialog mode with an explicit popup size, shown in the app's colors. Its own view is
/// 1 × 1 behind the calendar button: at 0 × 0 (an earlier version) the dialog opened as an empty box on Android.
/// Each box takes digits only (Persian and Arabic digits are read as well), selects its number when focused so that
/// typing replaces it, and moves on to the next box once it is full.
/// </para>
/// <para>
/// A complete, existing date is taken over at once. While the boxes hold a date that does not exist (day 31 of a
/// 30-day month, month 13, a year being typed), the box shows red and the date stays as it was; when the field loses
/// the focus, a day beyond the end of the month becomes the last day, and anything else returns to the stored date.
/// The order of the boxes follows <see cref="CalendarDates.InputOrder"/>. Each complete date part reserves its full
/// digit width at the native font scale and moves to the next row when needed (D-81); the year is never forced into
/// a fixed-width box.
/// </para>
/// </remarks>
public sealed class DateField : ContentView
{
    /// <summary>Identifies the <see cref="Date"/> property.</summary>
    public static readonly BindableProperty DateProperty = BindableProperty.Create(
        nameof(Date), typeof(DateOnly), typeof(DateField), DateOnly.FromDateTime(DateTime.Today), BindingMode.TwoWay,
        propertyChanged: (bindable, _, _) => ((DateField)bindable).OnDateChanged(),
        coerceValue: (_, value) => value is DateOnly date && date < CalendarDates.MinDate ? DateOnly.FromDateTime(DateTime.Today)
            : value is DateOnly late && late > CalendarDates.MaxDate ? CalendarDates.MaxDate : value);

    /// <summary>Identifies an optional calendar for this field without changing the device preference.</summary>
    public static readonly BindableProperty CalendarOverrideProperty = BindableProperty.Create(
        nameof(CalendarOverride), typeof(CalendarSystem?), typeof(DateField), null,
        propertyChanged: (bindable, _, _) =>
        {
            var field = (DateField)bindable;
            field.Arrange();
            field.Write();
        });

    private readonly Dictionary<DatePart, Entry> _boxes = [];
    private readonly Dictionary<DatePart, Label> _separators = [];
    private readonly Dictionary<DatePart, Grid> _partGroups = [];
    private readonly FlexLayout _row;
    private readonly Label _preview;
    private readonly SfCalendar _calendar;
    private readonly SymbolIcon _calendarIcon;
    private readonly Button _calendarButton;
    private bool _typing;
    private bool _writing;

    /// <summary>Creates the control.</summary>
    public DateField()
    {
        foreach (var part in new[] { DatePart.Day, DatePart.Month, DatePart.Year })
        {
            var box = new Entry
            {
                Keyboard = Keyboard.Numeric,
                HorizontalTextAlignment = TextAlignment.Center,
                MaxLength = part == DatePart.Year ? 4 : 2,
                MinimumWidthRequest = part == DatePart.Year ? 84 : 60,
                MinimumHeightRequest = 44,
                FontSize = 17,
                // Digits read left to right in every language; the row itself follows the page direction.
                FlowDirection = FlowDirection.LeftToRight,
            };
            box.TextChanged += (_, _) => OnTyped(part);
            box.Focused += (_, _) => SelectAll(box);
            box.Unfocused += (_, _) => Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(150), OnFocusLeft);
            box.Completed += (_, _) => FocusNext(part);
            _boxes[part] = box;
            // D-81: reserve the full digit count at the actual native font scale, even during partial typing.
            // Native entries include horizontal chrome; the invisible label reserves digits plus that space.
            var reserve = new Label
            {
                Text = part == DatePart.Year ? "8888" : "88",
                Margin = new Thickness(16, 0),
                Opacity = 0,
                InputTransparent = true,
                LineBreakMode = LineBreakMode.NoWrap,
            };
            reserve.SetBinding(Label.FontSizeProperty, new Binding(nameof(Entry.FontSize), source: box));
            reserve.SetBinding(Label.FontFamilyProperty, new Binding(nameof(Entry.FontFamily), source: box));
            reserve.SetBinding(Label.FontAttributesProperty, new Binding(nameof(Entry.FontAttributes), source: box));
            reserve.SetBinding(Label.FontAutoScalingEnabledProperty, new Binding(nameof(Entry.FontAutoScalingEnabled), source: box));
            AutomationProperties.SetIsInAccessibleTree(reserve, false);
            var input = new Grid { MinimumWidthRequest = part == DatePart.Year ? 84 : 60, Children = { reserve, box } };
            var separator = Separator();
            _separators[part] = separator;
            var group = new Grid { ColumnDefinitions = [new(GridLength.Auto), new(GridLength.Auto)], ColumnSpacing = 2,
                Margin = new Thickness(0, 0, 2, 4), Children = { input, separator } };
            Grid.SetColumn(separator, 1);
            FlexLayout.SetShrink(group, 0);
            _partGroups[part] = group;
        }
        _preview = new Label { FontSize = 13, Margin = new Thickness(2, 4, 0, 0), TextColor = ThemeColors.SecondaryText };

        _calendar = new SfCalendar
        {
            Mode = CalendarMode.Dialog,
            SelectionMode = CalendarSelectionMode.Single,
            FooterView = new CalendarFooterView { ShowActionButtons = true, ShowTodayButton = true },
            // The dialog's content is drawn at the size of the calendar view itself: at 0 × 0 it opened as an empty box
            // on Android. 1 × 1 behind the calendar button takes no room and keeps the dialog filled.
            WidthRequest = 1,
            HeightRequest = 1,
            PopupWidth = 330,
            PopupHeight = 420,
        };
        _calendar.AcceptCommand = new Command(AcceptCalendar);
        _calendar.DeclineCommand = new Command(() => _calendar.IsOpen = false);

        // The calendar button: an icon under a transparent button that takes taps, keyboard focus and the spoken name.
        _calendarIcon = new SymbolIcon
        {
            Symbol = Symbol.Calendar,
            FontSize = 24,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            InputTransparent = true,
            ForegroundColor = ThemeColors.Primary,
        };
        AutomationProperties.SetIsInAccessibleTree(_calendarIcon, false);
        _calendarButton = new Button
        {
            Text = string.Empty,
            WidthRequest = 44,
            HeightRequest = 44,
            MinimumWidthRequest = 44,
            MinimumHeightRequest = 44,
            Padding = 0,
            BorderWidth = 0,
            CornerRadius = 22,
            BackgroundColor = Colors.Transparent,
        };
        _calendarButton.Clicked += (_, _) => OpenCalendar();
        var calendarArea = new Grid
        {
            WidthRequest = 44,
            HeightRequest = 44,
            Margin = new Thickness(6, 0, 0, 0),
            VerticalOptions = LayoutOptions.Center,
            Children = { _calendar, _calendarIcon, _calendarButton },
        };
        FlexLayout.SetOrder(calendarArea, 3);
        FlexLayout.SetShrink(calendarArea, 0);

        // Whole date parts wrap together; fixed widths must not clip the year or overflow a narrow form.
        _row = new FlexLayout { Direction = FlexDirection.Row, Wrap = FlexWrap.Wrap, AlignItems = FlexAlignItems.Center };
        foreach (var group in _partGroups.Values) { _row.Children.Add(group); }
        _row.Children.Add(calendarArea);
        Content = new VerticalStackLayout { Spacing = 0, Children = { _row, _preview } };
        Arrange();
        Write();

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

            Arrange();
            Write();
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

    /// <summary>Gets or sets the calendar of a recurrence editor; other date fields follow the user's preference.</summary>
    public CalendarSystem? CalendarOverride
    {
        get => (CalendarSystem?)GetValue(CalendarOverrideProperty);
        set => SetValue(CalendarOverrideProperty, value);
    }

    private static IServiceProvider? Services => IPlatformApplication.Current?.Services;

    private static ILocalizationService? Localization => Services?.GetService<ILocalizationService>();

    private CalendarSystem Calendar => CalendarOverride ?? Localization?.CurrentCalendar ?? CalendarSystem.Gregorian;

    private static Label Separator() => new()
    {
        Text = "/",
        FontSize = 17,
        VerticalOptions = LayoutOptions.Center,
        TextColor = ThemeColors.SecondaryText,
        FlowDirection = FlowDirection.LeftToRight,
    };

    private void OnLocalizationChanged(object? sender, EventArgs e) => Dispatcher.Dispatch(() =>
    {
        Arrange();
        Write();
    });

    private void OnThemeChanged(object? sender, AppThemeChangedEventArgs e) => Dispatcher.Dispatch(() =>
    {
        _preview.TextColor = ThemeColors.SecondaryText;
        _calendarIcon.ForegroundColor = ThemeColors.Primary;
        foreach (var separatorLabel in _separators.Values)
        {
            separatorLabel.TextColor = ThemeColors.SecondaryText;
        }

        ShowValidity();
    });

    // A date set from outside (a view model, today's default) is written into the boxes; one typed here is not
    // rewritten while the user is still typing ("1" must not turn into "01" under the cursor).
    private void OnDateChanged()
    {
        if (_typing)
        {
            WritePreview();
        }
        else
        {
            Write();
        }
    }

    // Places boxes and separators in the order of the language and calendar.
    private void Arrange()
    {
        var culture = Localization?.CurrentCulture ?? Translator.Instance.Culture;
        var order = CalendarDates.InputOrder(culture, Calendar, Localization?.FormattingCultureName is not null);
        foreach (var separatorLabel in _separators.Values)
        {
            separatorLabel.Text = Localization?.FormattingCultureName is not null ? culture.DateTimeFormat.DateSeparator : "/";
        }
        for (var i = 0; i < order.Count; i++)
        {
            FlexLayout.SetOrder(_partGroups[order[i]], i);
            _separators[order[i]].IsVisible = i < order.Count - 1;
        }

        var separator = Calendar == CalendarSystem.Gregorian && !culture.TextInfo.IsRightToLeft ? culture.DateTimeFormat.DateSeparator.Trim() : "/";
        foreach (var label in _separators.Values)
        {
            label.Text = string.IsNullOrEmpty(separator) ? "/" : separator;
        }

        var translator = Translator.Instance;
        foreach (var (part, box) in _boxes)
        {
            var name = translator[$"DateField_{part}"];
            box.Placeholder = name;
            SemanticProperties.SetDescription(box, name);
        }

        SemanticProperties.SetDescription(_calendarButton, translator["Common_SelectDate"]);
    }

    // Writes the stored date into the boxes and the line below.
    private void Write()
    {
        var (year, month, day) = CalendarDates.Parts(Date, Calendar);
        _writing = true;
        _boxes[DatePart.Day].Text = day.ToString("00", System.Globalization.CultureInfo.InvariantCulture);
        _boxes[DatePart.Month].Text = month.ToString("00", System.Globalization.CultureInfo.InvariantCulture);
        _boxes[DatePart.Year].Text = year.ToString(System.Globalization.CultureInfo.InvariantCulture);
        _writing = false;
        WritePreview();
        ShowValidity();
    }

    private void WritePreview()
    {
        var formatter = Services?.GetService<IDateFormatter>();
        _preview.Text = formatter?.Format(Date, DateFormatStyle.Long, Calendar) ?? Date.ToString("D", Translator.Instance.Culture);
    }

    private void OnTyped(DatePart part)
    {
        if (_writing)
        {
            return;
        }

        var box = _boxes[part];
        // Digits only; Persian and Arabic digits from the keyboard are stored as Latin ones.
        var digits = new string([.. Digits.ToAscii(box.Text ?? string.Empty).Where(char.IsAsciiDigit)]);
        if (digits != box.Text)
        {
            _writing = true;
            box.Text = digits;
            _writing = false;
        }

        if (TryRead(out var date))
        {
            _typing = true;
            Date = date;
            _typing = false;
        }

        ShowValidity();

        // A full box, or a first digit that cannot start a two-digit day or month, moves on to the next box.
        var full = digits.Length >= box.MaxLength
            || (part == DatePart.Month && digits is [>= '2'])
            || (part == DatePart.Day && digits is [>= '4']);
        if (full && box.IsFocused)
        {
            FocusNext(part);
        }
    }

    private bool TryRead(out DateOnly date)
    {
        date = default;
        return Number(DatePart.Year) is { } year && Number(DatePart.Month) is { } month && Number(DatePart.Day) is { } day
            && CalendarDates.TryCreate(year, month, day, Calendar, out date);
    }

    private int? Number(DatePart part) =>
        int.TryParse(_boxes[part].Text, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out var value) ? value : null;

    // A box whose number cannot belong to a real date shows red; the others keep the normal text color.
    private void ShowValidity()
    {
        var calendar = Calendar;
        var year = Number(DatePart.Year);
        var month = Number(DatePart.Month);
        var day = Number(DatePart.Day);
        var yearOk = year is { } y && CalendarDates.DaysInMonth(y, 1, calendar) > 0;
        var monthOk = month is >= 1 and <= 12;
        var days = yearOk && monthOk ? CalendarDates.DaysInMonth(year!.Value, month!.Value, calendar) : 31;
        Mark(DatePart.Year, yearOk);
        Mark(DatePart.Month, monthOk);
        Mark(DatePart.Day, day is { } d && d >= 1 && d <= days);
    }

    private void Mark(DatePart part, bool valid) => _boxes[part].TextColor = valid ? ThemeColors.Text : ThemeColors.Danger;

    private void SelectAll(Entry box) => Dispatcher.Dispatch(() =>
    {
        box.CursorPosition = 0;
        box.SelectionLength = box.Text?.Length ?? 0;
    });

    private void FocusNext(DatePart part)
    {
        var culture = Localization?.CurrentCulture ?? Translator.Instance.Culture;
        var order = CalendarDates.InputOrder(culture, Calendar, Localization?.FormattingCultureName is not null);
        var index = order.ToList().IndexOf(part);
        if (index + 1 < order.Count)
        {
            _boxes[order[index + 1]].Focus();
        }
        else
        {
            _boxes[part].Unfocus();
        }
    }

    // Opens the month view in the display calendar at the stored date.
    private void OpenCalendar()
    {
        _calendar.Identifier = Calendar switch
        {
            CalendarSystem.Persian => CalendarIdentifier.Persian,
            // The dialog's Umm al-Qura calendar covers 30 April 1900 to 16 November 2077 (1318–1500 AH); a date outside
            // opens in the Gregorian dialog.
            CalendarSystem.Hijri when Date >= new DateOnly(1900, 4, 30) && Date <= new DateOnly(2077, 11, 16) => CalendarIdentifier.UmAlQura,
            _ => CalendarIdentifier.Gregorian,
        };
        _calendar.FlowDirection = FlowDirection;
        StyleCalendar();
        _calendar.MonthView.FirstDayOfWeek = Localization?.FirstDayOfWeek ?? DayOfWeek.Monday;
        _calendar.SelectedDate = Date.ToDateTime(TimeOnly.MinValue);
        _calendar.DisplayDate = Date.ToDateTime(TimeOnly.MinValue);
        _calendar.IsOpen = true;
    }

    // The dialog in the app's colors and font for the current theme (Syncfusion's own default is a violet light theme).
    private void StyleCalendar()
    {
        var card = ThemeColors.Card;
        var text = ThemeColors.Text;
        var muted = ThemeColors.SecondaryText;
        var primary = ThemeColors.Primary;
        var font = Application.Current?.Resources.TryGetValue("AppFont", out var value) == true && value is string family ? family : null;
        CalendarTextStyle Style(Color color, double size, FontAttributes attributes = FontAttributes.None)
        {
            var style = new CalendarTextStyle { TextColor = color, FontSize = size, FontAttributes = attributes };
            if (font is not null)
            {
                style.FontFamily = font;
            }

            return style;
        }

        _calendar.Background = card;
        // The area behind the days has no property of its own; it comes from Syncfusion's theme key.
        _calendar.Resources["SfCalendarTheme"] = "CommonTheme";
        _calendar.Resources["SfCalendarNormalBackground"] = card;
        _calendar.SelectionBackground = primary;
        _calendar.TodayHighlightBrush = primary;
        _calendar.HeaderView.Background = card;
        _calendar.HeaderView.TextStyle = Style(text, 16, FontAttributes.Bold);
        // Day cells stay transparent: a cell background draws a grid between the days.
        _calendar.MonthView.TrailingLeadingDatesBackground = Colors.Transparent;
        _calendar.MonthView.TodayBackground = Colors.Transparent;
        _calendar.MonthView.TextStyle = Style(text, 14);
        _calendar.MonthView.TodayTextStyle = Style(primary, 14, FontAttributes.Bold);
        _calendar.MonthView.SelectionTextStyle = Style(ThemeColors.OnPrimary, 14, FontAttributes.Bold);
        _calendar.MonthView.TrailingLeadingDatesTextStyle = Style(muted, 14);
        _calendar.MonthView.HeaderView.Background = card;
        _calendar.MonthView.HeaderView.TextStyle = Style(muted, 12);
        _calendar.FooterView.Background = card;
        _calendar.FooterView.DividerColor = ThemeColors.Outline;
        _calendar.FooterView.TextStyle = Style(primary, 14, FontAttributes.Bold);
    }

    private void AcceptCalendar()
    {
        if (_calendar.SelectedDate is DateTime selected)
        {
            Date = DateOnly.FromDateTime(selected);
        }

        _calendar.IsOpen = false;
    }

    // When no box has the focus any more: a day beyond the end of the month becomes its last day ("31" in a 30-day
    // month), anything else that is not a date returns to the stored date. The boxes then show the stored date again.
    private void OnFocusLeft()
    {
        if (_boxes.Values.Any(b => b.IsFocused))
        {
            return;
        }

        if (Number(DatePart.Year) is { } year && Number(DatePart.Month) is { } month && Number(DatePart.Day) is { } day && day >= 1
            && CalendarDates.DaysInMonth(year, month, Calendar) is var days and > 0
            && CalendarDates.TryCreate(year, month, Math.Min(day, days), Calendar, out var date))
        {
            Date = date;
        }

        Write();
    }
}
