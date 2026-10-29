using FluentIcons.Common;
using FluentIcons.Maui;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Layouts;
using Vafadar.Localization;

namespace Vafadar.Zanance.App.Presentation;

/// <summary>
/// Choice of an icon from the bundled set (VIS-03). <see cref="SelectedKey"/> is <see langword="null"/> for "default",
/// e.g. the category's icon for an entry, so a custom icon never disappears silently when the category changes.
/// </summary>
/// <remarks>
/// Each tile is a drawn face under a transparent button, so it works with touch, the keyboard and screen readers, and
/// screen readers announce the selected tile. The colours are dynamic resources and follow a theme change.
/// </remarks>
public sealed class IconPicker : ContentView
{
    /// <summary>Icon keys offered for categories, accounts and entries.</summary>
    public static readonly IReadOnlyList<string> Keys = IconCatalog.Keys;

    /// <summary>Identifies the two-way original icon selection.</summary>
    public static readonly BindableProperty SelectedKeyProperty = BindableProperty.Create(
        nameof(SelectedKey), typeof(string), typeof(IconPicker), null, BindingMode.TwoWay,
        propertyChanged: (bindable, _, _) => ((IconPicker)bindable).UpdateStates());

    private readonly List<Tile> _tiles = [];
    private bool _isLoaded;

    /// <summary>Creates the original icon buttons and refreshes their names while attached.</summary>
    public IconPicker()
    {
        var panel = new FlexLayout { Wrap = FlexWrap.Wrap };
        foreach (var key in Keys.Prepend(null))
        {
            View symbol;
            if (key is null)
            {
                // D-356: this decorative default glyph shares the fixed icon tile; its real button retains the translated scalable name.
                symbol = new Label { Text = "–", FontSize = 18, FontAutoScalingEnabled = false, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center };
            }
            else
            {
                var icon = new SymbolIcon { Symbol = Icons.Parse(key, Symbol.Tag), FontSize = 22, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center };
                icon.SetDynamicResource(SymbolIcon.ForegroundColorProperty, "Primary");
                symbol = icon;
            }

            var face = new Border
            {
                StrokeThickness = 2,
                StrokeShape = new RoundRectangle { CornerRadius = 12 },
                InputTransparent = true,
                Content = symbol,
            };
            AutomationProperties.SetIsInAccessibleTree(face, false);

            // What the icon shows ("Car", "Savings"), the same names as in the category editor; the first tile keeps the default.
            var button = new Button { Text = string.Empty, BackgroundColor = Colors.Transparent, BorderWidth = 0, CornerRadius = 12, Padding = 0 };
            button.Clicked += (_, _) => SelectedKey = key;

            _tiles.Add(new Tile(key, face, button));
            panel.Children.Add(new Grid { WidthRequest = 44, HeightRequest = 44, Margin = new Thickness(2), Children = { face, button } });
        }

        Content = panel;
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        UpdateStates();
    }

    /// <summary>Gets or sets the selected icon key; <see langword="null"/> uses the default.</summary>
    public string? SelectedKey
    {
        get => (string?)GetValue(SelectedKeyProperty);
        set => SetValue(SelectedKeyProperty, value);
    }

    private void OnLoaded(object? sender, EventArgs e)
    {
        if (!_isLoaded)
        {
            _isLoaded = true;
            Translator.Instance.PropertyChanged += OnTranslatorChanged;
        }
        // D-179: a retained form can return after its language changed while this picker was detached.
        UpdateStates();
    }

    private void OnUnloaded(object? sender, EventArgs e)
    {
        _isLoaded = false;
        Translator.Instance.PropertyChanged -= OnTranslatorChanged;
    }

    private void OnTranslatorChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e) =>
        Dispatcher.Dispatch(() => { if (_isLoaded) UpdateStates(); });

    // The selection is marked like every other selection in the app: the soft action blue with a blue outline.
    private void UpdateStates()
    {
        var translator = Translator.Instance;
        foreach (var tile in _tiles)
        {
            var selected = tile.Key == SelectedKey;
            if (selected)
            {
                // A value set in code outranks a dynamic resource set later, so the transparent one is removed first.
                tile.Face.ClearValue(Border.StrokeProperty);
                tile.Face.ClearValue(BackgroundColorProperty);
                tile.Face.SetDynamicResource(Border.StrokeProperty, "Primary");
                tile.Face.SetDynamicResource(BackgroundColorProperty, "PrimarySoft");
            }
            else
            {
                tile.Face.Stroke = Colors.Transparent;
                tile.Face.BackgroundColor = Colors.Transparent;
            }

            // Resolve the unchanged key in the current language; do not retain a constructor-time caption.
            var name = translator[tile.Key is null ? "Icon_Default" : $"Icon_{tile.Key}"];
            SemanticProperties.SetDescription(tile.Button, selected ? translator.Format("Common_SelectedItem", name) : name);
        }
    }

    private sealed record Tile(string? Key, Border Face, Button Button);
}
