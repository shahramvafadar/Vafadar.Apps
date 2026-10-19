using Vafadar.Zanance.App.Features.Accounts;
using Vafadar.Zanance.App.Features.Onboarding;
using Vafadar.Zanance.App.Features.Plans;
using Vafadar.Zanance.App.Presentation;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Localization;

namespace Vafadar.Zanance.App;

internal static partial class AndroidHomeReview
{
    /// <summary>Distinguishes the original scrollable input from a clipped static caption using actual native caret/ink endpoints.</summary>
    private static async Task<EditableViewportEvidence> ReviewEditableViewportAsync(Android.Widget.TextView native, string text, CancellationToken ct)
    {
        if (native is not Android.Widget.EditText edit || edit.Text != text || text.Length < 2 || native.Layout?.LineCount != 1)
            throw new ReviewCheckException("Original single-line editable value required.");
        var initialStart = edit.SelectionStart; var initialEnd = edit.SelectionEnd;
        var originalX = edit.ScrollX; var originalY = edit.ScrollY; var focused = edit.HasFocus;
        InputEndpoint start; InputEndpoint end;
        try
        {
            if (!edit.RequestFocus()) throw new ReviewCheckException("Original editable viewport cannot receive focus.");
            start = await ReachAsync(0, 0);
            end = await ReachAsync(text.Length, text.Length - 1);
        }
        finally
        {
            if (initialStart >= 0 && initialEnd >= 0) edit.SetSelection(initialStart, initialEnd);
            edit.ScrollTo(originalX, originalY);
            if (!focused) edit.ClearFocus();
        }
        if (edit.Text != text) throw new ReviewCheckException("Caret-only review changed original input text.");
        var restored = initialStart == edit.SelectionStart && initialEnd == edit.SelectionEnd;
        return new(start.Complete && end.Complete && start.ScrollX != end.ScrollX && restored, start, end, true, restored);

        async Task<InputEndpoint> ReachAsync(int caret, int glyph)
        {
            edit.SetSelection(caret);
            edit.BringPointIntoView(caret);
            InputEndpoint? endpoint = null;
            for (var attempt = 0; attempt < 16; attempt++)
            {
                await Task.Delay(40, ct); await WaitLayoutAsync(edit, ct);
                if (!edit.HasWindowFocus || edit.Text != text || edit.Layout is not { } layout)
                    throw new ReviewCheckException("Original focused editable value was retired.");
                using var ink = new Android.Graphics.Rect(); edit.Paint!.GetTextBounds(text, glyph, glyph + 1, ink);
                var origin = Math.Min(layout.GetPrimaryHorizontal(glyph), layout.GetPrimaryHorizontal(glyph + 1));
                var left = origin + ink.Left + edit.TotalPaddingLeft - edit.ScrollX;
                var right = origin + ink.Right + edit.TotalPaddingLeft - edit.ScrollX;
                var top = layout.GetLineBaseline(0) + ink.Top + edit.TotalPaddingTop - edit.ScrollY;
                var bottom = layout.GetLineBaseline(0) + ink.Bottom + edit.TotalPaddingTop - edit.ScrollY;
                var cursor = layout.GetPrimaryHorizontal(caret) + edit.TotalPaddingLeft - edit.ScrollX;
                var complete = left >= edit.TotalPaddingLeft - 1 && right <= edit.Width - edit.TotalPaddingRight + 1
                    && top >= edit.TotalPaddingTop - 1 && bottom <= edit.Height - edit.TotalPaddingBottom + 1
                    && cursor >= edit.TotalPaddingLeft - 1 && cursor <= edit.Width - edit.TotalPaddingRight + 1;
                endpoint = new(caret, edit.ScrollX, left, right, top, bottom, cursor, complete);
                if (complete) return endpoint;
            }
            return endpoint ?? throw new ReviewCheckException("No native editable endpoint observation.");
        }
    }

    /// <summary>Identifies only the two changed original commands, including shared account-form consumers.</summary>
    private static System.Windows.Input.ICommand? ChangedEditorCommand(ContentPage page) => page.BindingContext switch
    {
        AccountEditorViewModel account => account.Form.ToggleIconPickerCommand,
        OnboardingViewModel onboarding => onboarding.Account.ToggleIconPickerCommand,
        PlanEditorViewModel plan => plan.ToggleRuleOptionsCommand,
        _ => null
    };

    /// <summary>Finds the unchanged command's growing caption without rechecking unrelated original controls.</summary>
    private static bool IsChangedEditorCaption(VisualElement control, ContentPage page)
    {
        var command = ChangedEditorCommand(page);
        for (Element? item = control; item is not null; item = item.Parent)
            if (item is WrappingAction action) return command is not null && ReferenceEquals(action.Command, command);
        return false;
    }

    /// <summary>Invokes the original native command twice and checks complete names, growing captions and unchanged drafts.</summary>
    private static async Task<object> ReviewChangedEditorActionAsync(ContentPage page, IServiceProvider services, CancellationToken ct)
    {
        var command = ChangedEditorCommand(page) ?? throw new ReviewCheckException("Changed original action unavailable.");
        var action = ReimbursementViews(page).OfType<WrappingAction>().Single(value => ReferenceEquals(value.Command, command));
        var button = ReimbursementViews(action).OfType<Button>().Single();
        var caption = ReimbursementViews(action).OfType<Label>().Single();
        var form = page.BindingContext switch { AccountEditorViewModel account => account.Form, OnboardingViewModel onboarding => onboarding.Account, _ => null };
        var plan = page.BindingContext as PlanEditorViewModel;
        var initial = form?.ShowIconPicker ?? plan!.ShowRuleOptions;
        var translator = services.GetRequiredService<Translator>();
        string Snapshot() => form is not null ? form.Snapshot() : (string)(typeof(PlanEditorViewModel)
            .GetMethod("Snapshot", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.Invoke(plan, null)!);
        var before = Snapshot(); var states = new List<object>();
        for (var step = 0; step < 3; step++)
        {
            await AwaitEditorWindowFocusAsync(page, ct);
            if (button.Handler?.PlatformView is not Android.Views.View target || caption.Handler?.PlatformView is not Android.Widget.TextView native)
                throw new ReviewCheckException("Original action handlers are unavailable.");
            target.RequestRectangleOnScreen(new Android.Graphics.Rect(0, 0, target.Width, target.Height), true);
            await Task.Delay(100, ct); await WaitLayoutAsync(target, ct);
            var opened = form?.ShowIconPicker ?? plan!.ShowRuleOptions;
            var expected = translator[form is not null ? opened ? "Account_HideIcons" : "Account_ChooseIcon" : "Plan_ChangePattern"];
            if (target is not Android.Widget.Button || !target.HasWindowFocus || !target.Enabled || target.Width < 44 || target.Height < 44
                || EditorNativeDescription(target) != expected || caption.Text != expected || SettingsTextIssue(native) is not null)
                throw new ReviewCheckException("Changed action caption, original name, target or enablement differs.");
            states.Add(new { Opened = opened, CompleteCaption = expected, CaptionWidth = native.Width, CaptionHeight = native.Height,
                NativeCaptionSize = native.TextSize, TargetWidth = target.Width, TargetHeight = target.Height, OriginalNativeName = EditorNativeDescription(target) });
            if (step == 2) break;
            if (!target.PerformClick()) throw new ReviewCheckException("Original growing action refused its click.");
            await Task.Delay(100, ct);
            if ((form?.ShowIconPicker ?? plan!.ShowRuleOptions) == opened) throw new ReviewCheckException("Original disclosure did not change state.");
        }
        if ((form?.ShowIconPicker ?? plan!.ShowRuleOptions) != initial || before != Snapshot())
            throw new ReviewCheckException("Original disclosure changed input values or lost its initial state.");
        return new { States = states, OriginalNativeCommandClickedTwice = true, OriginalDraftUnchanged = true, OriginalStateRestored = true };
    }

    /// <summary>Actual glyph and caret positions after native selection scrolling, with no altered input/scroll policy.</summary>
    private sealed record InputEndpoint(int Caret, int ScrollX, float InkLeft, float InkRight, float InkTop, float InkBottom, float CaretX, bool Complete);
    /// <summary>Original single-line value endpoint evidence, not an assertion that all text fits simultaneously.</summary>
    private sealed record EditableViewportEvidence(bool Complete, InputEndpoint Start, InputEndpoint End, bool OriginalTextPreserved, bool OriginalSelectionRestored);
}
