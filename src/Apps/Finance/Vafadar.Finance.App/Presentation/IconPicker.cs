using FluentIcons.Common;
using FluentIcons.Maui;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Layouts;

namespace Vafadar.Finance.App.Presentation;

/// <summary>
/// Choice of an icon from the bundled set (VIS-03). <see cref="SelectedKey"/> is <see langword="null"/> for "default",
/// e.g. the category's icon for an entry, so a custom icon never disappears silently when the category changes.
/// </summary>
public sealed class IconPicker : ContentView
{
    /// <summary>Icon keys offered for categories, accounts and entries.</summary>
    public static readonly IReadOnlyList<string> Keys =
    [
        "Home", "Food", "FoodPizza", "Cart", "ShoppingBag", "Flash", "Drop", "Fire", "Phone", "Laptop", "Tv", "VehicleCar",
        "Gas", "Airplane", "Beach", "Heart", "Pill", "Stethoscope", "Dumbbell", "Shield", "HatGraduation", "Book", "People",
        "Person", "Games", "MusicNote1", "Sport", "ArrowRepeatAll", "Gift", "Money", "Savings", "BuildingBank", "Briefcase",
        "Handshake", "Receipt", "Wrench", "PaintBrush", "Box", "Balloon", "Umbrella", "Star", "Tag", "Wallet", "Payment",
        "MoreHorizontal", "QuestionCircle",
    ];

    public static readonly BindableProperty SelectedKeyProperty = BindableProperty.Create(
        nameof(SelectedKey), typeof(string), typeof(IconPicker), null, BindingMode.TwoWay,
        propertyChanged: (bindable, _, _) => ((IconPicker)bindable).UpdateStates());

    private static readonly Color Accent = Color.FromArgb("#2E7D32");
    private readonly List<(string? Key, Border Tile)> _tiles = [];

    public IconPicker()
    {
        var panel = new FlexLayout { Wrap = FlexWrap.Wrap };
        foreach (var key in Keys.Prepend(null))
        {
            var tile = new Border
            {
                WidthRequest = 44,
                HeightRequest = 44,
                Margin = new Thickness(2),
                StrokeThickness = 2,
                StrokeShape = new RoundRectangle { CornerRadius = 12 },
                Content = key is null
                    ? new Label { Text = "–", FontSize = 18, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center }
                    : new SymbolIcon { Symbol = Icons.Parse(key, Symbol.Tag), FontSize = 22, ForegroundColor = Accent, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center },
            };
            SemanticProperties.SetDescription(tile, key ?? "default");
            tile.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(() => SelectedKey = key) });
            _tiles.Add((key, tile));
            panel.Children.Add(tile);
        }

        Content = panel;
        UpdateStates();
    }

    /// <summary>Gets or sets the selected icon key; <see langword="null"/> uses the default.</summary>
    public string? SelectedKey
    {
        get => (string?)GetValue(SelectedKeyProperty);
        set => SetValue(SelectedKeyProperty, value);
    }

    private void UpdateStates()
    {
        foreach (var (key, tile) in _tiles)
        {
            var selected = key == SelectedKey;
            tile.Stroke = selected ? Accent : Colors.Transparent;
            tile.BackgroundColor = selected ? Color.FromArgb("#E8F5E9") : Colors.Transparent;
        }
    }
}
