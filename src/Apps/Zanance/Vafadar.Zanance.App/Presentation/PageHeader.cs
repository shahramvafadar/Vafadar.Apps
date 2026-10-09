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
    /// <summary>Adds a growing header once, retaining the same page, body, bindings and back handling.</summary>
    internal static void Attach(ContentPage page, bool rightToLeft)
    {
        // D-98: modal forms already retain their own growing title/Cancel row. The underlying child stack can have
        // several pages, so its depth cannot tell a modal apart from an ordinary child page on Windows.
        if (Shell.GetPresentationMode(page) is PresentationMode.Modal or PresentationMode.ModalAnimated or PresentationMode.ModalNotAnimated)
        { return; }
        if (page.Content is not { } body || body is Grid grid && grid.Children.OfType<PageHeader>().Any()) { return; }
        // D-83: Shell's Windows TitleView has a fixed bar height; scalable multiline titles need their own Auto row.
        // The body is retained, and returning from a nested page does not detach it or rebuild the user's draft.
        if (!body.IsSet(BindingContextProperty))
        {
            // Keep inherited form bindings stable through reparenting; later page-context changes still propagate.
            body.SetBinding(BindingContextProperty, new Binding(nameof(Page.BindingContext), source: page));
        }
        page.Content = null;
        body.WidthRequest = -1; body.HorizontalOptions = LayoutOptions.Fill;
        var host = new Grid { RowDefinitions = [new(GridLength.Auto), new(GridLength.Star)] };
        host.Add(new PageHeader(page, rightToLeft) { Margin = new Thickness(16, 8) }, 0, 0);
        host.Add(body, 0, 1);
        page.Content = host;
        Shell.SetNavBarIsVisible(page, false);
        ReadableWidth.Refresh(page);
    }

    /// <summary>Creates a scalable title with a real back target and live translated accessible description.</summary>
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
        back.SetBinding(SemanticProperties.DescriptionProperty, new Binding("[Common_Back]", source: Translator.Instance));
        back.SetBinding(ToolTipProperties.TextProperty, new Binding("[Common_Back]", source: Translator.Instance));
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
            if (page is ContentPage { Content: Grid host } && host.Children.OfType<ScrollView>().FirstOrDefault() is { } scroll)
            {
                back.Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(150), () => _ = scroll.ScrollToAsync(0, 0, animated: false));
            }
        };

        var title = new Label { VerticalOptions = LayoutOptions.Center, LineBreakMode = LineBreakMode.WordWrap };
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
