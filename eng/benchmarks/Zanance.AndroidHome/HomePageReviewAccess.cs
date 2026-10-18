using Vafadar.Zanance.Core.Dashboard;

namespace Vafadar.Zanance.App.Features.Home;

public partial class HomePage
{
    /// <summary>Exercises original optional category attachment only in this independent caller, without saved preferences.</summary>
    internal void PreviewCategorySectionForReview(bool visible)
    {
        if (AppInfo.Current.PackageName != global::Vafadar.Zanance.App.AndroidHomeReview.Package)
            throw new IOException("Independent Home review package required.");
        _layout.SetVisible(HomeSection.Categories, visible);
        UpdateVisibility();
    }
}
