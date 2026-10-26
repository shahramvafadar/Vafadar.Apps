using Vafadar.Zanance.App.Features.Transactions;

namespace Vafadar.Zanance.App;

internal static partial class AndroidHomeReview
{
    /// <summary>Retains only fixed predicate/geometry evidence when the original shared guard refuses the new Undo scene.</summary>
    private static async Task<object> CheckOriginalUndoGeometryAsync(TransactionsPage page, CancellationToken ct)
    {
        try { return await Diagnostics.TransactionsAddHeaderReview.CheckAsync(page, ct); }
        catch (IOException error) when (error.Message is "Original Transactions title, contextual actions or result viewport differs."
            or "Original Transactions Add target or unobstructed result extent differs."
            or "Original contextual bulk actions overlap transaction rows.")
        {
            var model = (TransactionsViewModel)page.BindingContext; var root = (Grid)page.Content;
            var header = page.FindByName<Grid>("TransactionsHeader"); var add = page.FindByName<Button>("AddAction");
            var list = page.FindByName<CollectionView>("ContentViewport"); var filters = page.FindByName<ScrollView>("FiltersViewport");
            var bulk = page.FindByName<Border>("BulkActions"); var undo = page.FindByName<Border>("UndoNotice");
            throw new ReviewCheckException(System.Text.Json.JsonSerializer.Serialize(new { FixedPredicate = error.Message,
                Root = root.Bounds, Header = header.Bounds, Add = add.Bounds, List = list.Bounds, Filters = filters.Bounds,
                Bulk = bulk.Bounds, Undo = undo.Bounds, model.IsSelecting, model.ShowUndo, Ready = model.Loading.IsReady,
                AddVisible = add.IsVisible, AddEnabled = add.IsEnabled, BulkVisible = bulk.IsVisible, UndoVisible = undo.IsVisible,
                NativeNavVisible = Shell.GetNavBarIsVisible(page), OriginalCommand = ReferenceEquals(add.Command, model.AddCommand) }));
        }
    }

    /// <summary>Checks only the original visible Undo row and viewport geometry without creating or executing an Undo.</summary>
    private static async Task<object> ReviewVisibleUndoRowAsync(TransactionsPage page, float density, CancellationToken ct)
    {
        var model = (TransactionsViewModel)page.BindingContext;
        var notice = page.FindByName<Border>("UndoNotice");
        var caption = page.FindByName<Label>("UndoCaption");
        if (notice.Content is not Grid content) throw new ReviewCheckException("Original Undo content grid unavailable.");
        var button = content.Children.OfType<Button>().Single();
        var list = page.FindByName<CollectionView>("ContentViewport");
        var bulk = page.FindByName<Border>("BulkActions");
        if (!model.ShowUndo || !notice.IsVisible || !ReferenceEquals(button.Command, model.UndoDeleteCommand))
            throw new ReviewCheckException("Original visible Undo row and command required.");
        for (var attempt = 0; attempt < 100; attempt++)
        {
            if (caption.Handler?.PlatformView is Android.Widget.TextView { Layout.LineCount: > 0, IsShown: true, HasWindowFocus: true }
                && button.Handler?.PlatformView is Android.Views.View { IsShown: true, HasWindowFocus: true }) break;
            await Task.Delay(50, ct);
        }
        using var row = Visible(notice); using var viewport = Visible(list); using var action = Visible(button);
        using var label = Visible(caption);
        var text = (Android.Widget.TextView)caption.Handler!.PlatformView!;
        var layout = text.Layout ?? throw new ReviewCheckException("Original Undo caption layout unavailable.");
        if (text.Text != model.UndoText || text.Text != caption.Text
            || Enumerable.Range(0, layout.LineCount).Any(line => layout.GetEllipsisCount(line) != 0)
            || viewport.Bottom > row.Top + 2 || action.Width() / density < 44 || action.Height() / density < 44
            || action.Left < row.Left || action.Right > row.Right || action.Top < row.Top || action.Bottom > row.Bottom
            || label.Left < row.Left || label.Right > row.Right || label.Bottom > action.Top + 2)
            throw new ReviewCheckException("Original scaled Undo caption, target or transaction viewport overlaps or clips.");
        MonetaryOsScope.RequireCompleteHeight(text, layout);
        object? bulkBounds = null;
        if (model.IsSelecting)
        {
            using var selected = Visible(bulk);
            if (viewport.Bottom > selected.Top + 2 || selected.Bottom > row.Top + 2)
                throw new ReviewCheckException("Original selection/Undo rows overlap each other or transactions.");
            bulkBounds = new { selected.Left, selected.Top, selected.Right, selected.Bottom };
        }
        return new { FullCaption = text.Text, CaptionLines = layout.LineCount,
            UndoTop = row.Top, UndoBottom = row.Bottom, ListBottom = viewport.Bottom,
            ButtonWidthDp = action.Width() / density, ButtonHeightDp = action.Height() / density,
            BulkBounds = bulkBounds, OriginalCommand = true, ViewOnlyVisibility = true,
            NoDeleteOrUndoCommand = true, NoNativeUndoOrJournalAcceptance = true };

        static Android.Graphics.Rect Visible(VisualElement element)
        {
            if (element.Handler?.PlatformView is not Android.Views.View { IsShown: true, HasWindowFocus: true } native)
                throw new ReviewCheckException("Original Undo native element is unavailable or unfocused.");
            var rect = new Android.Graphics.Rect();
            if (!native.GetGlobalVisibleRect(rect) || rect.Width() != native.Width || rect.Height() != native.Height)
            { rect.Dispose(); throw new ReviewCheckException("Original Undo native element is clipped."); }
            return rect;
        }
    }
}
