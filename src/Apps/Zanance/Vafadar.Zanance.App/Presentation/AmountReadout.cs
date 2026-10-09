using Vafadar.Localization;

namespace Vafadar.Zanance.App.Presentation;

/// <summary>
/// Displays a complete, already formatted large amount at its native font scale. Oversized values scroll horizontally
/// instead of splitting decimal digits or dropping the currency/sign; a translated hint appears only on overflow (D-81).
/// </summary>
public sealed class AmountReadout : ContentView
{
    /// <summary>Identifies the canonical formatted amount; no financial parsing or calculation occurs here.</summary>
    public static readonly BindableProperty AmountTextProperty = BindableProperty.Create(nameof(AmountText), typeof(string),
        typeof(AmountReadout), null, propertyChanged: (bindable, _, value) =>
        {
            var readout = (AmountReadout)bindable;
            readout._caption.Text = (string?)value;
            SemanticProperties.SetDescription(readout._caption, (string?)value ?? string.Empty);
            // Missing/covered values must not leave a blank 44 px scroll area or a stale overflow hint.
            readout._scroll.IsVisible = !string.IsNullOrEmpty((string?)value);
            readout.RefreshOverflow();
            // A new amount/language starts at its sign again, rather than keeping an old scrolled unit-only view.
            readout.Dispatcher.Dispatch(() => { if (readout._scroll.IsVisible && readout._scroll.ScrollX > 1) { _ = readout._scroll.ScrollToAsync(0, 0, animated: false); } });
        });

    /// <summary>Identifies an optional existing semantic amount color.</summary>
    public static readonly BindableProperty TextColorProperty = BindableProperty.Create(nameof(TextColor), typeof(Color),
        typeof(AmountReadout), null, propertyChanged: (bindable, _, value) =>
        {
            var caption = ((AmountReadout)bindable)._caption;
            if (value is Color color) { caption.TextColor = color; }
            else { caption.SetDynamicResource(Label.TextColorProperty, "AmountText"); }
        });

    private readonly Label _caption;
    private readonly ScrollView _scroll;
    private readonly Label _hint;

    /// <summary>Creates the existing large scalable caption and a real native horizontal scroll viewport.</summary>
    public AmountReadout()
    {
        _caption = new Label { LineBreakMode = LineBreakMode.NoWrap };
        if (Application.Current?.Resources.TryGetValue("AmountLarge", out var value) == true && value is Style style)
        { _caption.Style = style; }
        _scroll = new ScrollView { Orientation = ScrollOrientation.Horizontal, HorizontalScrollBarVisibility = ScrollBarVisibility.Always,
            VerticalScrollBarVisibility = ScrollBarVisibility.Never, MinimumHeightRequest = 44,
            FlowDirection = FlowDirection.LeftToRight, IsVisible = false, Content = _caption };
        _hint = new Label { FontSize = 12, IsVisible = false, InputTransparent = true };
        _hint.SetDynamicResource(Label.TextColorProperty, "SecondaryText");
        _hint.SetBinding(Label.TextProperty, new Binding("[Amount_ScrollHint]", source: Translator.Instance));
        _caption.SizeChanged += (_, _) => RefreshOverflow();
        _scroll.SizeChanged += (_, _) => RefreshOverflow();
        Content = new VerticalStackLayout { Spacing = 2, Children = { _scroll, _hint } };
        Loaded += (_, _) => { RefreshAlignment(); RefreshOverflow(); };
    }

    /// <summary>Gets or sets the full signed amount/digit/unit packet, including its original direction marks.</summary>
    public string? AmountText { get => (string?)GetValue(AmountTextProperty); set => SetValue(AmountTextProperty, value); }

    /// <summary>Gets or sets the original semantic amount color.</summary>
    public Color? TextColor { get => (Color?)GetValue(TextColorProperty); set => SetValue(TextColorProperty, value); }

    /// <inheritdoc />
    protected override void OnPropertyChanged(string? propertyName = null)
    {
        base.OnPropertyChanged(propertyName);
        if (propertyName == nameof(FlowDirection) && _caption is not null) { RefreshAlignment(); }
    }

    private void RefreshAlignment() => _caption.HorizontalTextAlignment = FlowDirection == FlowDirection.RightToLeft
        ? TextAlignment.End : TextAlignment.Start;

    private void RefreshOverflow() => _hint.IsVisible = _scroll.IsVisible && _scroll.Width > 0 && _caption.Width > _scroll.Width + 1;
}
