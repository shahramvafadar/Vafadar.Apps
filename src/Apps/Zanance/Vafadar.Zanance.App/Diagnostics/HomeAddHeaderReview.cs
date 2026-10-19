#if (DEBUG && WINDOWS) || ZANANCE_INDEPENDENT_HOME_REVIEW
using Vafadar.Zanance.App.Features.Entries;
using Vafadar.Zanance.App.Features.Home;

namespace Vafadar.Zanance.App.Diagnostics;

/// <summary>Checks the actual Home title action, retained viewport and native open/Cancel without financial writes.</summary>
internal static class HomeAddHeaderReview
{
    /// <summary>Retains public derived geometry when an independent observation fails.</summary>
    internal static object? LastObservation { get; private set; }

    /// <summary>Uses original controls and native invocation, preserving the separate fictitious fixture.</summary>
    internal static async Task CheckAsync(HomePage page, Func<Button, Task> invoke, CancellationToken ct, bool exerciseNavigation = true)
    {
        await Task.Delay(250, ct);
        var header = page.FindByName<Grid>("HomeHeader");
        var add = page.FindByName<Button>("AddAction");
        var viewport = page.FindByName<ScrollView>("ContentViewport");
        var root = (Grid)page.Content;
        var model = (HomeViewModel)page.BindingContext;
        LastObservation = new { Stage = "header", Header = header.Bounds, Add = add.Bounds, Viewport = viewport.Bounds,
            Root = root.Bounds, HeaderParent = add.Parent?.GetType().Name, Ready = model.Loading.IsReady };
        if (Shell.GetNavBarIsVisible(page) || root.RowDefinitions.Count != 2
            || Grid.GetRow(header) != 0 || Grid.GetRow(viewport) != 1 || add.Parent != header
            || add.Width < 44 || add.Height < 44 || add.Width > 50 || add.Height > 50
            || !add.IsVisible || !add.IsEnabled || !ReferenceEquals(add.Command, model.AddEntryCommand)
            || header.FlowDirection != FlowDirection.LeftToRight || Grid.GetColumn(add) != 2
            || header.Height < add.Height || viewport.Y < header.Height - 1
            || Math.Abs(viewport.Y + viewport.Height - root.Height) > 2)
            throw new IOException("Original Home header or unobstructed viewport geometry differs.");
        var originalHeaderBounds = header.Bounds;
        var originalAddBounds = add.Bounds;
        // Android converts scroll coordinates to an integer. Use the finite laid-out extent.
        var bottom = Math.Max(0, (viewport.Content?.Height ?? 0) - viewport.Height);
        await viewport.ScrollToAsync(0, bottom, false); await Task.Delay(100, ct);
        LastObservation = new { Stage = "bottom", RequestedY = bottom, viewport.ScrollY, ContentHeight = viewport.Content?.Height, Viewport = viewport.Bounds };
        if (viewport.Content is not VisualElement content || viewport.ScrollY < Math.Max(0, content.Height - viewport.Height) - 2
            || header.Bounds != originalHeaderBounds || add.Bounds != originalAddBounds)
            throw new IOException("Home bottom content or fixed header is unavailable.");
        await viewport.ScrollToAsync(0, 0, false);
        // The separate non-input Windows desktop cannot host this framework modal (0x800F1000).
        // Keep its original geometry checks; actual native open/Cancel is exercised on Android.
        if (!exerciseNavigation) return;
        var shell = Shell.Current ?? throw new IOException("Original Shell required.");
        LastObservation = new { Stage = "native-add" };
        await invoke(add);
        for (var attempt = 0; attempt < 80 && shell.Navigation.ModalStack.LastOrDefault() is not EntryEditorPage; attempt++)
            await Task.Delay(50, ct);
        if (model.AddEntryCommand.ExecutionTask is { } opening) await opening.WaitAsync(ct);
        if (shell.Navigation.ModalStack.LastOrDefault() is not EntryEditorPage editor)
            throw new IOException("Native header Add did not open the original entry editor.");
        LastObservation = new { Stage = "native-cancel", Editor = editor.GetType().Name };
        await Task.Delay(400, ct);
        var cancel = Descendants(editor).OfType<Button>().Single(button =>
            button.Command is not null && editor.BindingContext is EntryEditorViewModel vm && ReferenceEquals(button.Command, vm.CancelCommand));
        await invoke(cancel);
        if (editor.BindingContext is EntryEditorViewModel editorModel && editorModel.CancelCommand.ExecutionTask is { } cancelTask)
            await cancelTask.WaitAsync(ct);
        for (var attempt = 0; attempt < 80 && shell.Navigation.ModalStack.Contains(editor); attempt++) await Task.Delay(50, ct);
        if (shell.Navigation.ModalStack.Contains(editor)) throw new IOException("Native Cancel did not close the unsaved editor.");
        LastObservation = new { Stage = "returned" };
        await page.PendingPublication.WaitAsync(ct);
        if (!model.Loading.IsReady || !add.IsEnabled || !add.IsVisible || page != shell.Navigation.NavigationStack.Last())
            throw new IOException("Original Home did not return after native Cancel.");
    }

    /// <summary>Reads the original owned visual tree; no substitute controls or financial writes.</summary>
    private static IEnumerable<VisualElement> Descendants(IVisualTreeElement parent)
    {
        foreach (var child in parent.GetVisualChildren())
        {
            if (child is VisualElement view) yield return view;
            foreach (var descendant in Descendants(child)) yield return descendant;
        }
    }
}
#endif
