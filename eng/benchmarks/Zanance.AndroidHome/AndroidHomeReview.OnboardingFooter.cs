using Vafadar.Zanance.App.Features.Onboarding;
using Vafadar.Zanance.App.Presentation;

namespace Vafadar.Zanance.App;

internal static partial class AndroidHomeReview
{
    /// <summary>Selects only original translated footer captions; no unchanged wizard body matrix is replayed.</summary>
    private static bool IsOnboardingFooterCaption(VisualElement control, ContentPage page)
    {
        if (page.BindingContext is not OnboardingViewModel vm) return false;
        for (Element? item = control; item is not null; item = item.Parent)
            if (item is WrappingAction action) return ReferenceEquals(action.Command, vm.BackCommand) || ReferenceEquals(action.Command, vm.NextCommand);
        return false;
    }

    /// <summary>Verifies real original command ownership, complete single-word names/targets and busy disablement without Finish/Save.</summary>
    private static async Task<object> ReviewOnboardingFooterAsync(ContentPage page, CancellationToken ct)
    {
        if (page.BindingContext is not OnboardingViewModel vm) throw new ReviewCheckException("Original onboarding model required.");
        var footer = page.FindByName<FlexLayout>("Footer") ?? throw new ReviewCheckException("Original responsive footer unavailable.");
        var actions = footer.Children.OfType<WrappingAction>().Where(ReimbursementEffectivelyVisible).ToArray();
        if (actions.Length != (vm.CanGoBack ? 2 : 1)) throw new ReviewCheckException("Original footer visibility differs.");
        var observations = new List<object>();
        foreach (var action in actions)
        {
            await AwaitEditorWindowFocusAsync(page, ct);
            var button = ReimbursementViews(action).OfType<Button>().Single(); var label = ReimbursementViews(action).OfType<Label>().Single();
            if (button.Handler?.PlatformView is not Android.Widget.Button target || label.Handler?.PlatformView is not Android.Widget.TextView native)
                throw new ReviewCheckException("Original footer native controls unavailable.");
            await WaitLayoutAsync(target, ct);
            if (!target.HasWindowFocus || !target.Enabled || target.Width < 44 || target.Height < 44
                || EditorNativeDescription(target) != action.Text || SettingsTextIssue(native) is not null || native.Layout?.LineCount != 1
                || (!ReferenceEquals(button.Command, vm.BackCommand) && !ReferenceEquals(button.Command, vm.NextCommand)))
                throw new ReviewCheckException("Original footer word, target or command is incomplete.");
            observations.Add(new { Caption = action.Text, NativeName = EditorNativeDescription(target), TargetWidth = target.Width,
                TargetHeight = target.Height, NativeLines = native.Layout.LineCount, WholeWordPreserved = true, OriginalCommandBound = true });
        }
        var next = actions.Single(action => ReferenceEquals(action.Command, vm.NextCommand));
        var nextButton = ReimbursementViews(next).OfType<Button>().Single();
        var initialBusy = vm.IsBusy;
        try
        {
            vm.IsBusy = true; await Task.Delay(100, ct);
            if (nextButton.Handler?.PlatformView is not Android.Widget.Button { Enabled: false })
                throw new ReviewCheckException("Busy original onboarding still enables its action.");
        }
        finally { vm.IsBusy = initialBusy; }
        await Task.Delay(100, ct);
        if (nextButton.Handler?.PlatformView is not Android.Widget.Button { Enabled: true })
            throw new ReviewCheckException("Original onboarding action did not restore enablement.");
        return new { Step = vm.Step, Actions = observations, BusyDisablesNext = true, OriginalBusyStateRestored = true,
            NavigationOrFinancialSaveInvoked = false };
    }
}
