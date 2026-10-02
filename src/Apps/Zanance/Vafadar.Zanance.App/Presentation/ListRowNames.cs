#if WINDOWS
using System.Collections;
using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml.Controls;
using WinUIAutomation = Microsoft.UI.Xaml.Automation.AutomationProperties;

namespace Vafadar.Zanance.App.Presentation;

/// <summary>
/// Names the rows of a CollectionView on Windows after their item (D-45). WinUI names a row after the text of what it
/// holds, which is MAUI's wrapper, so screen readers read "Microsoft.Maui.Controls.Platform.ItemTemplateContext" for every
/// row. Row types name themselves through <see cref="object.ToString"/> (EntryRow, PlanRow, ...); the type name or the
/// generated text of a record is never read out.
/// </summary>
internal static class ListRowNames
{
    private static readonly ConditionalWeakTable<ListViewBase, object> Attached = [];

    public static void Attach(ListViewBase list, Microsoft.Maui.Controls.ItemsView view)
    {
        ArgumentNullException.ThrowIfNull(list);
        ArgumentNullException.ThrowIfNull(view);
        if (Attached.TryGetValue(list, out _))
        {
            return;
        }

        Attached.Add(list, view);
        list.ContainerContentChanging += (_, args) =>
        {
            var item = args.Item is Microsoft.Maui.Controls.Platform.ItemTemplateContext context ? context.Item : args.Item;
            WinUIAutomation.SetName(args.ItemContainer, NameOf(item));
        };

        // Group headers (the days of the transaction list) are named after their group, e.g. "Friday, October 2, 2026,
        // +2,468.20 EUR". MAUI's group wrapper is internal, so the group is taken from the view's own list.
        list.ChoosingGroupHeaderContainer += (sender, args) =>
        {
            if (view.ItemsSource is not IList groups || args.GroupIndex < 0 || args.GroupIndex >= groups.Count)
            {
                return;
            }

            args.GroupHeaderContainer ??= sender is GridView ? new GridViewHeaderItem() : new ListViewHeaderItem();
            WinUIAutomation.SetName(args.GroupHeaderContainer, NameOf(groups[args.GroupIndex]));
        };
    }

    private static string NameOf(object? item)
    {
        var text = item?.ToString();
        if (item is null || string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        var type = item.GetType();
        return text == type.FullName || text.StartsWith(type.Name + " {", StringComparison.Ordinal) ? string.Empty : text;
    }
}
#endif
