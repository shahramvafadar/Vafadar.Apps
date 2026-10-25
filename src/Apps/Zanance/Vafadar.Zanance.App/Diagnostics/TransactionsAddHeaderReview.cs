#if (DEBUG && WINDOWS) || ZANANCE_INDEPENDENT_HOME_REVIEW
using Vafadar.Zanance.App.Features.Transactions;

namespace Vafadar.Zanance.App.Diagnostics;

/// <summary>Checks the original title action and retained filter/list/contextual-action viewports without financial writes.</summary>
internal static class TransactionsAddHeaderReview
{
    /// <summary>Requires the Home/Plans title action and no persistent bottom Add row.</summary>
    internal static async Task<object> CheckAsync(TransactionsPage page, CancellationToken ct)
    {
        await page.PendingPublication.WaitAsync(ct);
        await Task.Delay(300, ct);
        var root = (Grid)page.Content;
        var header = page.FindByName<Grid>("TransactionsHeader");
        var add = page.FindByName<Button>("AddAction");
        var list = page.FindByName<CollectionView>("ContentViewport");
        var filters = page.FindByName<ScrollView>("FiltersViewport");
        var bulk = page.FindByName<Border>("BulkActions");
        var undo = page.FindByName<Border>("UndoNotice");
        var model = (TransactionsViewModel)page.BindingContext;
        if (Shell.GetNavBarIsVisible(page) || root.RowDefinitions.Count != 5 || add.Parent != header
            || Grid.GetRow(header) != 0 || Grid.GetRow(filters) != 1 || Grid.GetRow(list) != 2
            || Grid.GetRow(bulk) != 3 || Grid.GetRow(undo) != 4
            || Grid.GetColumn(add) != 2 || header.FlowDirection != FlowDirection.LeftToRight
            || !ReferenceEquals(add.Command, model.AddCommand) || !model.Loading.IsReady
            || string.IsNullOrWhiteSpace(SemanticProperties.GetDescription(add))
            || add.IsVisible == model.IsSelecting || bulk.IsVisible != model.IsSelecting
            || list.Height < Math.Min(144, root.Height / 3) - 2)
            throw new IOException("Original Transactions title, contextual actions or result viewport differs.");
        if (!model.IsSelecting && (add.Width < 44 || add.Height < 44 || add.Width > 50 || add.Height > 50
            || !add.IsEnabled || add.X < header.Width / 2
            || !undo.IsVisible && Math.Abs(list.Y + list.Height - root.Height) > 2))
            throw new IOException("Original Transactions Add target or unobstructed result extent differs.");
        if (model.IsSelecting && list.Y + list.Height > bulk.Y + 2)
            throw new IOException("Original contextual bulk actions overlap transaction rows.");
        return new { Header = header.Bounds, Add = add.Bounds, List = list.Bounds, Filters = filters.Bounds,
            Bulk = bulk.Bounds, Undo = undo.Bounds, Root = root.Bounds, model.IsSelecting,
            Groups = model.Days.Count, FullSpokenName = SemanticProperties.GetDescription(add),
            OriginalCommand = true, NoBottomAddBand = true };
    }
}
#endif
