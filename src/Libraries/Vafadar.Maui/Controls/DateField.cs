using Microsoft.Extensions.DependencyInjection;
using Vafadar.Core.Text;
using Vafadar.Localization.Formatting;
using Vafadar.Localization;

namespace Vafadar.Maui.Controls;

/// <summary>
/// A date input with three number boxes – day, month and year – in the user's display calendar (Gregorian, Persian or
/// lunar Hijri), with the full date written below ("Tuesday, 6 October 2026"). The value is always a Gregorian
/// <see cref="DateOnly"/>. An unset value (before 1900, e.g. <c>default(DateOnly)</c>) becomes today.
/// </summary>
/// <remarks>
/// <para>
/// The boxes replace a calendar dialog (D-59): the Syncfusion dialog opened empty on Android, and a month view makes
/// a date far away slow to reach. Each box takes digits only (Persian and Arabic digits are read as well), selects
/// its number when focused so that typing replaces it, and moves on to the next box once it is full.
/// </para>
/// <para>
/// A complete, existing date is taken over at once. While the boxes hold a date that does not exist (day 31 of a
/// 30-day month, month 13, a year being typed), the box shows red and the date stays as it was; when the field loses
/// the focus, a day beyond the end of the month becomes the last day, and anything else returns to the stored date.
/// The order of the boxes follows <see cref="CalendarDates.InputOrder"/>.
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

    private readonly Dictionary<DatePart, Entry> _boxes = [];
    private readonly Label[] _separators;
    private readonly Label _preview;
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
                WidthRequest = part == DatePart.Year ? 84 : 60,
                FontSize = 17,
                // Digits read left to right in every language; the row itself follows the page direction.
                FlowDirection = FlowDirection.LeftToRight,
            };
            box.TextChanged += (_, _) => OnTyped(part);
            box.Focused += (_, _) => SelectAll(box);
            box.Unfocused += (_, _) => Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(150), OnFocusLeft);
            box.Completed += (_, _) => FocusNext(part);
            _boxes[part] = box;
        }

        _separators = [Separator(), Separator()];
        _preview = new Label { FontSize = 13, Margin = new Thickness(2, 4, 0, 0), TextColor = ThemeColors.SecondaryText };

        var row = new Grid
        {
            ColumnDefinitions = [new(GridLength.Auto), new(GridLength.Auto), new(GridLength.Auto), new(GridLength.Auto), new(GridLength.Auto)],
            ColumnSpacing = 2,
            HorizontalOptions = LayoutOptions.Start,
        };
        foreach (var view in _boxes.Values.Cast<View>().Concat(_separators))
        {
            row.Children.Add(view);
        }

        Content = new VerticalStackLayout { Spacing = 0, Children = { row, _preview } };
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

    private static IServiceProvider? Services => IPlatformApplication.Current?.Services;

    private static ILocalizationService? Localization => Services?.GetService<ILocalizationService>();

    private static CalendarSystem Calendar => Localization?.CurrentCalendar ?? CalendarSystem.Gregorian;

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
        foreach (var separator in _separators)
        {
            separator.TextColor = ThemeColors.SecondaryText;
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
        var order = CalendarDates.InputOrder(culture, Calendar);
        for (var i = 0; i < order.Count; i++)
        {
            Grid.SetColumn(_boxes[order[i]], i * 2);
        }

        Grid.SetColumn(_separators[0], 1);
        Grid.SetColumn(_separators[1], 3);
        var separator = Calendar == CalendarSystem.Gregorian && !culture.TextInfo.IsRightToLeft ? culture.DateTimeFormat.DateSeparator.Trim() : "/";
        foreach (var label in _separators)
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
        _preview.Text = formatter?.Format(Date, DateFormatStyle.Long) ?? Date.ToString("D", Translator.Instance.Culture);
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
        var order = CalendarDates.InputOrder(culture, Calendar);
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
