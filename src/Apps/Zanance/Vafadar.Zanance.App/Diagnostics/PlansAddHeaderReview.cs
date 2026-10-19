#if (DEBUG && WINDOWS) || ZANANCE_INDEPENDENT_HOME_REVIEW
using Vafadar.Zanance.App.Features.Plans;

namespace Vafadar.Zanance.App.Diagnostics;

/// <summary>Checks original Plans title/Add geometry without invoking a financial writer.</summary>
internal static class PlansAddHeaderReview
{
    /// <summary>Requires the same right-side action as Home and a full remaining list/empty viewport.</summary>
    internal static async Task<object> CheckAsync(PlansPage page, CancellationToken ct)
    {
        await page.PendingPublication.WaitAsync(ct);
        await Task.Delay(250, ct);
        var root = (Grid)page.Content;
        var header = page.FindByName<Grid>("PlansHeader");
        var add = page.FindByName<Button>("AddAction");
        var list = page.FindByName<CollectionView>("ContentViewport");
        var empty = page.FindByName<ScrollView>("EmptyViewport");
        var model = (PlansViewModel)page.BindingContext;
        if (Shell.GetNavBarIsVisible(page) || root.RowDefinitions.Count != 3 || add.Parent != header
            || Grid.GetRow(header) != 0 || Grid.GetColumn(add) != 2 || header.FlowDirection != FlowDirection.LeftToRight
            || add.Width < 44 || add.Height < 44 || add.Width > 50 || add.Height > 50
            || !add.IsEnabled || !add.IsVisible || !ReferenceEquals(add.Command, model.AddCommand)
            || string.IsNullOrWhiteSpace(SemanticProperties.GetDescription(add))
            || Grid.GetRow(list) != 2 || Grid.GetRow(empty) != 2
            || Math.Abs(list.Y + list.Height - root.Height) > 2
            || model.IsEmpty && Math.Abs(empty.Y + empty.Height - root.Height) > 2)
            throw new IOException("Original Plans title action or unobstructed viewport differs.");
        return new { Header = header.Bounds, Add = add.Bounds, List = list.Bounds, Empty = empty.Bounds,
            Root = root.Bounds, model.IsEmpty, Rows = model.Rows.Count, FullSpokenName = SemanticProperties.GetDescription(add),
            OriginalCommand = true, NoBottomActionBand = true };
    }
}
#endif
