using UIKit;

namespace Vafadar.Zanance.App;

/// <summary>
/// Covers the app while it is inactive, so the app switcher snapshot never shows financial data (SEC-02, D-23). iOS
/// takes that snapshot right after the app resigns active; the cover is removed when it becomes active again.
/// </summary>
internal static class PrivacyCover
{
    private const nint Tag = 0x5A414E;

    public static void Show()
    {
        foreach (var window in Windows())
        {
            if (window.ViewWithTag(Tag) is not null)
            {
                continue;
            }

            var cover = new UIView(window.Bounds)
            {
                Tag = Tag,
                BackgroundColor = UIColor.SystemBackground,
                AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight,
            };
            window.AddSubview(cover);
        }
    }

    public static void Hide()
    {
        foreach (var window in Windows())
        {
            window.ViewWithTag(Tag)?.RemoveFromSuperview();
        }
    }

    private static IEnumerable<UIWindow> Windows() =>
        UIApplication.SharedApplication.ConnectedScenes.OfType<UIWindowScene>().SelectMany(scene => scene.Windows);
}
