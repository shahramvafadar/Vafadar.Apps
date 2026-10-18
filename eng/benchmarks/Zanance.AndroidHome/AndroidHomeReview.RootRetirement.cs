using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Localization;
using Vafadar.Zanance.App.Features.Home;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App;

internal static partial class AndroidHomeReview
{
    /// <summary>Checks actual retired Shell ownership without repeating financial or caption checks.</summary>
    private static async Task<object> ReviewRootRetirementAsync(App app, IServiceProvider services, string run, string comparison, CancellationToken ct)
    {
        var theme = services.GetRequiredService<ThemeService>();
        var originalTheme = theme.Choice;
        var local = services.GetRequiredService<ILocalizationService>();
        var originalLanguage = local.CurrentLanguage;
        var store = services.GetRequiredService<ZananceStore>();
        async Task<string> StoredAsync() => JsonSerializer.Serialize(new { Accounts = await store.GetAccountsAsync(cancellationToken: ct),
            Entries = await store.GetEntriesAsync(cancellationToken: ct), Settings = await store.GetSettingsAsync(ct) });
        var before = await StoredAsync();
        var shells = new List<WeakReference<AppShell>>();
        var observations = new List<object>();
        var backChecks = new List<object>();
        await AwaitReimbursementFocusAsync(ct);
        app.OpenReviewShell();
        await AwaitRootHomeAsync(ct);
        var first = CountToolbarSubscriptions();
        try
        {
            var requested = Platform.CurrentActivity?.Intent?.GetIntExtra("review_roots", 8) ?? 8;
            if (requested is < 2 or > 8) throw new ReviewCheckException("Bounded root count required.");
            for (var group = 0; group < requested; group++)
            {
                CaptureCurrentRoot(shells);
                var rebuilt = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                void Rebuilt(object? sender, EventArgs args) => rebuilt.TrySetResult();
                app.ShellRebuilt += Rebuilt;
                try
                {
                    theme.Set(group % 2 == 0 ? ThemeChoice.Dark : ThemeChoice.Light);
                    await rebuilt.Task.WaitAsync(TimeSpan.FromSeconds(30), ct);
                }
                finally { app.ShellRebuilt -= Rebuilt; }
                await AwaitRootHomeAsync(ct);
                // Independent diagnosis only: wait for the same detached roots, not a new fixture.
                await Task.Delay(250, ct);
                var retirement = new List<int>();
                for (var sample = 0; sample < 10; sample++)
                {
                    await Task.Run(() => { GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect(); }, ct);
                    await Task.Delay(100, ct);
                    retirement.Add(CountLiveRoots(shells));
                    if (retirement[^1] == 0) break;
                }
                var result = new { Comparison = comparison, Group = group, RetiredRoots = shells.Count,
                    LiveRetiredRoots = CountLiveRoots(shells), ToolbarSubscriptions = CountToolbarSubscriptions(),
                    NativeRootFragments = CountNativeRootFragments(),
                    RetirementSamples = retirement,
                    InitialToolbarSubscriptions = first, SourceMvid = typeof(AndroidHomeReview).Module.ModuleVersionId,
                    TranslationTargets = CountTranslationTargets(), DisplayTargets = CountDisplayTargets(local),
                    FinancialCommandsInvoked = false, SystemSettingsChanged = false };
                observations.Add(result); LogReport("root-retirement-" + group, run, result);
                if (Platform.CurrentActivity?.Intent?.GetBooleanExtra("review_assert_roots", false) == true
                    && (result.LiveRetiredRoots != 0 || result.ToolbarSubscriptions != first || result.NativeRootFragments != 1))
                    throw new ReviewCheckException("Retired native navigation ownership is not released.");
            }
            if (Platform.CurrentActivity?.Intent?.GetBooleanExtra("review_back_names", false) == true)
            {
                foreach (var choice in new[] { ThemeChoice.Light, ThemeChoice.Dark })
                foreach (var language in new[] { "en", "fa", "de" })
                {
                    await AwaitReimbursementFocusAsync(ct);
                    await Shell.Current.GoToAsync(AppShell.AboutRoute, false);
                    var page = Shell.Current.CurrentPage;
                    theme.Set(choice);
                    local.SetLanguage(local.SupportedLanguages.Single(value => value.CultureName == language));
                    await Task.Delay(150, ct);
                    await AwaitReimbursementFocusAsync(ct);
                    if (!ReferenceEquals(page, Shell.Current.CurrentPage))
                        throw new ReviewCheckException("Display changes unexpectedly replaced the open original detail.");
                    var expected = Translator.Instance["Common_Back"];
                    LogReport("root-retirement-back-start", run, new { Comparison = comparison, Language = language, Theme = choice.ToString(), NativeBackName = expected });
                    var rebuilt = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                    void Rebuilt(object? sender, EventArgs args) => rebuilt.TrySetResult();
                    app.ShellRebuilt += Rebuilt;
                    try
                    {
                        ClickOriginalNativeBack(expected);
                        await rebuilt.Task.WaitAsync(TimeSpan.FromSeconds(30), ct);
                    }
                    finally { app.ShellRebuilt -= Rebuilt; }
                    await AwaitRootHomeAsync(ct);
                    var back = new { Comparison = comparison, Language = language, Theme = choice.ToString(), NativeBackName = expected,
                        OriginalNativeBackClicked = true, NativeRootFragments = CountNativeRootFragments(), ToolbarSubscriptions = CountToolbarSubscriptions() };
                    backChecks.Add(back);
                    LogReport("root-retirement-back-" + choice + "-" + language, run, back);
                }
            }
            if (before != await StoredAsync()) throw new ReviewCheckException("Native root replacement changed stored fictitious inputs.");
        }
        finally { local.SetLanguage(originalLanguage); theme.Set(originalTheme); }
        return new { Comparison = comparison, Observations = observations, OriginalThemeRestored = theme.Choice == originalTheme,
            OriginalLanguageRestored = local.CurrentLanguage == originalLanguage, NativeBackChecks = backChecks, StoredInputsUnchanged = before == await StoredAsync(),
            FinancialCommandsInvoked = false, SystemSettingsChanged = false, MainPackageTouched = false };
    }

    /// <summary>Checks and invokes the original Android toolbar arrow, not a substituted navigation command.</summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ClickOriginalNativeBack(string expected)
    {
        if (Shell.Current.Handler?.PlatformView is not Android.Views.View native)
            throw new ReviewCheckException("Original native Shell unavailable.");
        var toolbar = NavigationRootNativeViews(native).OfType<AndroidX.AppCompat.Widget.Toolbar>()
            .Single(value => value.IsShown && value.NavigationIcon is not null && value.NavigationContentDescription == expected);
        var button = NavigationRootNativeViews(toolbar).Single(value => value.IsShown && value.Clickable && value.ContentDescription == expected);
        if (!button.PerformClick()) throw new ReviewCheckException("Original native back action refused.");
    }

    /// <summary>Visits only the current independent navigation view hierarchy.</summary>
    private static IEnumerable<Android.Views.View> NavigationRootNativeViews(Android.Views.View view)
    {
        yield return view;
        if (view is Android.Views.ViewGroup group)
            for (var index = 0; index < group.ChildCount; index++)
                if (group.GetChildAt(index) is { } child)
                    foreach (var descendant in NavigationRootNativeViews(child)) yield return descendant;
    }

    /// <summary>Does not carry the previous native root into the caller's collection await.</summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void CaptureCurrentRoot(List<WeakReference<AppShell>> shells)
    {
        if (Shell.Current is not AppShell shell) throw new ReviewCheckException("Original active Shell required.");
        shells.Add(new WeakReference<AppShell>(shell));
    }

    /// <summary>Counts only public type identity of the known source event; never records delegate objects or data.</summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int CountToolbarSubscriptions()
    {
        var field = typeof(Translator).GetField("PropertyChanged", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new ReviewCheckException("Original publisher event field unavailable.");
        return (field.GetValue(Translator.Instance) as Delegate)?.GetInvocationList()
            .Count(handler => handler.Target?.GetType().FullName?.EndsWith("AccessibleShellRenderer+AccessibleToolbarTracker", StringComparison.Ordinal) == true) ?? 0;
    }

    /// <summary>Ends strong TryGetTarget locals before the following independent collection.</summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int CountLiveRoots(List<WeakReference<AppShell>> shells) => shells.Count(reference => reference.TryGetTarget(out _));

    /// <summary>Counts the Activity's actual root fragments without inspecting arguments, views or private data.</summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int CountNativeRootFragments()
    {
        if (Platform.CurrentActivity is not AndroidX.Fragment.App.FragmentActivity activity
            || activity.SupportFragmentManager?.Fragments is not { } fragments)
            throw new ReviewCheckException("Original fragment Activity required.");
        return fragments.Count(fragment => fragment is Microsoft.Maui.Controls.Platform.Compatibility.ShellItemRendererBase && fragment.IsAdded);
    }

    /// <summary>Records source type counts only, never subscriber objects, state or financial values.</summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Dictionary<string, int> CountTranslationTargets()
    {
        var field = typeof(Translator).GetField("PropertyChanged", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new ReviewCheckException("Original publisher event field unavailable.");
        return GroupSubscriberTypes(field.GetValue(Translator.Instance) as Delegate);
    }

    /// <summary>Counts only the known original display publisher's subscription types.</summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Dictionary<string, int> CountDisplayTargets(ILocalizationService local)
    {
        var field = typeof(LocalizationService).GetField("Changed", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new ReviewCheckException("Original display event field unavailable.");
        return GroupSubscriberTypes(field.GetValue(local) as Delegate);
    }

    /// <summary>Lets every temporary strong invocation-list reference expire before another collection.</summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Dictionary<string, int> GroupSubscriberTypes(Delegate? callbacks) => (callbacks?.GetInvocationList() ?? [])
        .GroupBy(handler => handler.Target?.GetType().FullName ?? "static")
        .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);

    /// <summary>Waits for the original current Home publication and unobscured native root.</summary>
    private static async Task AwaitRootHomeAsync(CancellationToken ct)
    {
        // Window.Page publication precedes asynchronous Android fragment attachment. Do not
        // classify that intermediate null CurrentPage as a product or ownership failure.
        while (true)
        {
            await AwaitReimbursementFocusAsync(ct);
            if (Shell.Current.CurrentPage is HomePage { IsLoaded: true } home
                && home.Handler?.PlatformView is Android.Views.View { IsAttachedToWindow: true } native)
            {
                await home.PendingPublication.WaitAsync(ct);
                if (ReferenceEquals(home, Shell.Current.CurrentPage))
                {
                    await WaitLayoutAsync(native, ct);
                    return;
                }
            }
            await Task.Delay(50, ct);
        }
    }
}
