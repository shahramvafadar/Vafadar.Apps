using System.Collections;
using System.Collections.Specialized;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Layouts;

namespace Vafadar.Maui.Controls;

/// <summary>
/// A single-choice group of chips that wraps onto several lines, so long labels (e.g. in German) never scroll or
/// get cut off like a fixed-width segmented control would. Items are shown with their <c>ToString()</c> text.
/// </summary>
public sealed class ChoiceChips : ContentView
{
    /// <summary>Identifies the <see cref="ItemsSource"/> property.</summary>
    public static readonly BindableProperty ItemsSourceProperty = BindableProperty.Create(
        nameof(ItemsSource), typeof(IList), typeof(ChoiceChips), null,
        propertyChanged: (bindable, oldValue, newValue) => ((ChoiceChips)bindable).OnItemsSourceChanged(oldValue as IList, newValue as IList));

    /// <summary>Identifies the <see cref="SelectedIndex"/> property.</summary>
    public static readonly BindableProperty SelectedIndexProperty = BindableProperty.Create(
        nameof(SelectedIndex), typeof(int), typeof(ChoiceChips), -1, BindingMode.TwoWay,
        propertyChanged: (bindable, _, _) => ((ChoiceChips)bindable).UpdateStates());

    /// <summary>Identifies the <see cref="AccentColor"/> property.</summary>
    public static readonly BindableProperty AccentColorProperty = BindableProperty.Create(
        nameof(AccentColor), typeof(Color), typeof(ChoiceChips), Color.FromArgb("#2E7D32"),
        propertyChanged: (bindable, _, _) => ((ChoiceChips)bindable).UpdateStates());

    /// <summary>Identifies the <see cref="IsCompact"/> property.</summary>
    public static readonly BindableProperty IsCompactProperty = BindableProperty.Create(
        nameof(IsCompact), typeof(bool), typeof(ChoiceChips), false,
        propertyChanged: (bindable, _, _) => ((ChoiceChips)bindable).CreatePanel());

    private static readonly Color Outline = Color.FromArgb("#C8C8C8");
    private static readonly Color Text = Color.FromArgb("#1F1F1F");

    private readonly List<Border> _chips = [];
    private Layout _panel = null!;

    /// <summary>Creates the control.</summary>
    public ChoiceChips()
    {
        CreatePanel();
    }

    /// <summary>
    /// Gets or sets a value indicating whether the chips are smaller and stay on one line that scrolls horizontally
    /// (filter bars) instead of wrapping (form fields).
    /// </summary>
    public bool IsCompact
    {
        get => (bool)GetValue(IsCompactProperty);
        set => SetValue(IsCompactProperty, value);
    }

    /// <summary>Gets or sets the items to choose from.</summary>
    public IList? ItemsSource
    {
        get => (IList?)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    /// <summary>Gets or sets the index of the selected item, or -1.</summary>
    public int SelectedIndex
    {
        get => (int)GetValue(SelectedIndexProperty);
        set => SetValue(SelectedIndexProperty, value);
    }

    /// <summary>Gets or sets the fill colour of the selected chip.</summary>
    public Color AccentColor
    {
        get => (Color)GetValue(AccentColorProperty);
        set => SetValue(AccentColorProperty, value);
    }

    private void OnItemsSourceChanged(IList? oldItems, IList? newItems)
    {
        if (oldItems is INotifyCollectionChanged oldObservable)
        {
            oldObservable.CollectionChanged -= OnItemsChanged;
        }

        if (newItems is INotifyCollectionChanged newObservable)
        {
            newObservable.CollectionChanged += OnItemsChanged;
        }

        Rebuild();
    }

    private void OnItemsChanged(object? sender, NotifyCollectionChangedEventArgs e) => Rebuild();

    private void CreatePanel()
    {
        if (IsCompact)
        {
            _panel = new HorizontalStackLayout();
            Content = new ScrollView { Orientation = ScrollOrientation.Horizontal, HorizontalScrollBarVisibility = ScrollBarVisibility.Never, Content = _panel };
        }
        else
        {
            _panel = new FlexLayout { Wrap = FlexWrap.Wrap, JustifyContent = FlexJustify.Start, AlignItems = FlexAlignItems.Start };
            Content = _panel;
        }

        Rebuild();
    }

    private void Rebuild()
    {
        _panel.Children.Clear();
        _chips.Clear();
        if (ItemsSource is null)
        {
            return;
        }

        for (var i = 0; i < ItemsSource.Count; i++)
        {
            var index = i;
            var chip = new Border
            {
                Padding = IsCompact ? new Thickness(14, 7) : new Thickness(16, 10),
                Margin = IsCompact ? new Thickness(0, 0, 8, 0) : new Thickness(0, 0, 8, 8),
                StrokeThickness = 1,
                StrokeShape = new RoundRectangle { CornerRadius = 20 },
                MinimumHeightRequest = IsCompact ? 36 : 44,
                Content = new Label
                {
                    Text = ItemsSource[i]?.ToString(),
                    FontSize = IsCompact ? 14 : 15,
                    VerticalOptions = LayoutOptions.Center,
                },
            };
            chip.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(() => SelectedIndex = index) });
            SemanticProperties.SetDescription(chip, ItemsSource[i]?.ToString());
            _chips.Add(chip);
            _panel.Children.Add(chip);
        }

        UpdateStates();
    }

    private void UpdateStates()
    {
        for (var i = 0; i < _chips.Count; i++)
        {
            var selected = i == SelectedIndex;
            var chip = _chips[i];
            chip.BackgroundColor = selected ? AccentColor : Colors.White;
            chip.Stroke = selected ? AccentColor : Outline;
            ((Label)chip.Content!).TextColor = selected ? Colors.White : Text;
        }
    }
}
