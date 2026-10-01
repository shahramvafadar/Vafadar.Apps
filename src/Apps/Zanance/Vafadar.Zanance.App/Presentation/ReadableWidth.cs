namespace Vafadar.Zanance.App.Presentation;

/// <summary>
/// Keeps every page readable in wide windows (Windows, tablets in landscape, D-41): wider than <see cref="Max"/>, the
/// page content becomes a centred column of that width instead of cards and rows stretched across the whole window.
/// Phones are narrower and keep the full width. Applied once per page when it first appears.
/// </summary>
internal static class ReadableWidth
{
    /// <summary>The widest column: cards, lists and forms read like on a large phone.</summary>
    public const double Max = 720;

    private static readonly BindableProperty AppliedProperty =
        BindableProperty.CreateAttached("ReadableWidthApplied", typeof(bool), typeof(ReadableWidth), false);

    /// <summary>Applies the column to every page the app shows from now on.</summary>
    public static void Attach(Application app)
    {
        ArgumentNullException.ThrowIfNull(app);
        app.PageAppearing += (_, page) => Apply(page);
    }

    private static void Apply(Page page)
    {
        if (page is not ContentPage { Content: { } content } contentPage || (bool)contentPage.GetValue(AppliedProperty))
        {
            return;
        }

        contentPage.SetValue(AppliedProperty, true);
        void Update()
        {
            // The page itself keeps the full window, so its background still fills it.
            var wide = contentPage.Width > Max + 32;
            content.WidthRequest = wide ? Max : -1;
            content.HorizontalOptions = wide ? LayoutOptions.Center : LayoutOptions.Fill;
        }

        contentPage.SizeChanged += (_, _) => Update();
        Update();
    }
}
