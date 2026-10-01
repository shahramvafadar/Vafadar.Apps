#if WINDOWS
using FluentIcons.Common;
using FluentIcons.Maui;
using Vafadar.Localization;

namespace Vafadar.Zanance.App.Presentation;

/// <summary>
/// The title of a page opened from another page on Windows, with a clear back button in front of it. The window's own
/// back arrow is small and sits in the title bar, where it is easily missed. Going back uses the shell's back handling,
/// so pages that ask before discarding changes still do.
/// </summary>
internal sealed class PageHeader : Grid
{
    public PageHeader(Page page, bool rightToLeft)
    {
        ArgumentNullException.ThrowIfNull(page);
        ColumnDefinitions = [new ColumnDefinition(GridLength.Auto), new ColumnDefinition(GridLength.Star)];
        ColumnSpacing = 8;
        VerticalOptions = LayoutOptions.Center;

        var icon = new SymbolImageSource { Symbol = rightToLeft ? Symbol.ArrowRight : Symbol.ArrowLeft, Size = 22 };
        icon.SetDynamicResource(FontImageSource.ColorProperty, "Primary");
        var back = new ImageButton
        {
            Source = icon,
            WidthRequest = 44,
            HeightRequest = 44,
            Padding = 11,
            CornerRadius = 22,
            BorderWidth = 1,
            VerticalOptions = LayoutOptions.Center,
        };
        back.SetDynamicResource(BackgroundProperty, "PrimarySoft");
        back.SetDynamicResource(ImageButton.BorderColorProperty, "PrimaryLine");
        SemanticProperties.SetDescription(back, Translator.Instance["Common_Back"]);
        ToolTipProperties.SetText(back, Translator.Instance["Common_Back"]);
        back.Clicked += (_, _) => Shell.Current?.SendBackButtonPressed();

        // WinUI gives a newly shown page's text box the focus and scrolls to it (Settings opened halfway down at the
        // reminder days). The first time the page is shown, the back button takes the focus and the page starts at the
        // top; coming back from a page opened from here keeps the position.
        var shown = false;
        back.Loaded += (_, _) =>
        {
            if (shown)
            {
                return;
            }

            shown = true;
            back.Focus();
            if (page is ContentPage { Content: ScrollView scroll })
            {
                back.Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(150), () => _ = scroll.ScrollToAsync(0, 0, animated: false));
            }
        };

        var title = new Label { VerticalOptions = LayoutOptions.Center, LineBreakMode = LineBreakMode.TailTruncation };
        if (Application.Current?.Resources.TryGetValue("PageTitle", out var style) == true && style is Style pageTitle)
        {
            title.Style = pageTitle;
        }

        title.SetBinding(Label.TextProperty, new Binding(nameof(Page.Title), source: page));
        this.Add(back, 0, 0);
        this.Add(title, 1, 0);
    }
}
#endif
