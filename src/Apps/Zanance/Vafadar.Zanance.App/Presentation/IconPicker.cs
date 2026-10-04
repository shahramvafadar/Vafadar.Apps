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

    private readonly List<Tile> _tiles = [];

    public IconPicker()
    {
        var translator = Translator.Instance;
        var panel = new FlexLayout { Wrap = FlexWrap.Wrap };
        var number = 0;
        foreach (var key in Keys.Prepend(null))
        {
            View symbol;
            if (key is null)
            {
                symbol = new Label { Text = "–", FontSize = 18, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center };
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

            // The same names as the icon choice of the category editor ("Icon 3"); the first tile keeps the default.
            var name = key is null ? translator["Icon_Default"] : translator.Format("Category_IconOption", ++number);
            var button = new Button { Text = string.Empty, BackgroundColor = Colors.Transparent, BorderWidth = 0, CornerRadius = 12, Padding = 0 };
            button.Clicked += (_, _) => SelectedKey = key;

            _tiles.Add(new Tile(key, face, button, name));
            panel.Children.Add(new Grid { WidthRequest = 44, HeightRequest = 44, Margin = new Thickness(2), Children = { face, button } });
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

            SemanticProperties.SetDescription(tile.Button, selected ? translator.Format("Common_SelectedItem", tile.Name) : tile.Name);
        }
    }

    private sealed record Tile(string? Key, Border Face, Button Button, string Name);
}
