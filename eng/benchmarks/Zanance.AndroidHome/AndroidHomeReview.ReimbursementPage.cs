using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Data;
using Vafadar.Localization;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Features.Entries;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App;

internal static partial class AndroidHomeReview
{
    /// <summary>Qualifies the original large reimbursement page, real worker reads, coverage and retry.</summary>
    private static async Task<object> ReviewReimbursementPageAsync(App app, IServiceProvider services, string root, string comparison, CancellationToken ct, bool visibilityOnly = false, bool retainedFixture = false)
    {
        var qualifyVisibility = visibilityOnly || retainedFixture;
        var path = Path.Combine(root, "reimbursement-page.db");
        if (File.Exists(path) && !visibilityOnly && !retainedFixture) throw new ReviewCheckException("Never reseed retained reimbursement page inputs.");
        using var fixture = new ServiceCollection().AddZananceData(path).BuildServiceProvider();
        if (!visibilityOnly && !retainedFixture) await Task.Run(() => fixture.MigrateLocalDatabase<ZananceDbContext>(), ct);
        var originalStore = fixture.GetRequiredService<ZananceStore>();
        var account = new Account { Name = "Fictitious large receivable account", CurrencyCode = "EUR", OpeningDate = new DateOnly(2026, 10, 1) };
        if (!visibilityOnly && !retainedFixture)
        {
        await originalStore.SaveAccountAsync(account, ct);
        var entries = new List<LedgerEntry>();
        for (var index = 0; index < 4096; index++)
        {
            var expense = new LedgerEntry { AccountId = account.Id, Kind = EntryKind.Expense, Amount = 1000,
                ReimbursableAmount = 1000, Date = account.OpeningDate, Title = "Fictitious original expense " + index, Tags = ["retained"] };
            entries.Add(expense); entries.Add(EntryActions.CreateReimbursement(expense, 200, account.Id, expense.Date));
        }
        await Task.Run(() => originalStore.SaveEntriesAsync(entries, [], ct), ct);
        }
        async Task<string> StoredAsync() => JsonSerializer.Serialize(new { Accounts = await originalStore.GetAccountsAsync(cancellationToken: ct),
            Entries = await originalStore.GetEntriesAsync(cancellationToken: ct), Settings = await originalStore.GetSettingsAsync(ct) });
        var before = await StoredAsync();
        var expected = EntryActions.OpenReimbursements(await originalStore.GetEntriesAsync(cancellationToken: ct));
        if (expected.Count != 4096 || expected.Any(item => item.Open != 800)) throw new ReviewCheckException("Large original fixture incomplete.");
        var reads = new ReimbursementReadFactory(fixture.GetRequiredService<IDbContextFactory<ZananceDbContext>>());
        var store = new ZananceStore(reads, services.GetRequiredService<TimeProvider>(), fixture.GetRequiredService<ICommercialWriteAccessSource>());
        var local = services.GetRequiredService<ILocalizationService>(); var translator = services.GetRequiredService<Translator>();
        var theme = services.GetRequiredService<ThemeService>(); var observations = new List<object>();
        await AwaitReimbursementFocusAsync(ct);
        app.OpenReviewShell();
        foreach (var choice in new[] { ThemeChoice.Light, ThemeChoice.Dark })
        {
            var rebuilt = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            void Rebuilt(object? sender, EventArgs args) => rebuilt.TrySetResult();
            app.ShellRebuilt += Rebuilt;
            try { theme.Set(choice); await rebuilt.Task.WaitAsync(TimeSpan.FromSeconds(30), ct); }
            finally { app.ShellRebuilt -= Rebuilt; }
            foreach (var language in new[] { "en", "fa", "de" })
            {
                var receipt = Path.Combine(root, "reimbursement-fixed-" + comparison + "-" + language + "-" + choice + ".json");
                var inputHash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(before)));
                if (retainedFixture && !visibilityOnly && File.Exists(receipt))
                {
                    using var saved = JsonDocument.Parse(await File.ReadAllTextAsync(receipt, ct));
                    var item = saved.RootElement;
                    if (item.GetProperty("SourceMvid").GetString() != typeof(AndroidHomeReview).Assembly.ManifestModule.ModuleVersionId.ToString()
                        || item.GetProperty("InputHash").GetString() != inputHash
                        || !item.GetProperty("SeparateEmptyViewportAndNoOverlap").GetBoolean())
                        throw new ReviewCheckException("Retained reimbursement case differs; never replay or replace it.");
                    observations.Add(item.Clone()); continue;
                }
                await SetLanguageAsync(app, local, language, ct);
                for (var wait = 0; Shell.Current?.CurrentPage?.Handler?.PlatformView is not Android.Views.View { IsAttachedToWindow: true }; wait++)
                { if (wait >= 300) throw new ReviewCheckException("Original review root did not attach."); await Task.Delay(50, ct); }
                await AwaitReimbursementFocusAsync(ct);
                var vm = new ReimbursementsViewModel(store, translator, local, new DateFormatter(local));
                var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                void Published(object? sender, System.ComponentModel.PropertyChangedEventArgs args)
                { if (args.PropertyName == nameof(vm.Loading.IsReady) && vm.Loading.IsReady) ready.TrySetResult(); }
                vm.Loading.PropertyChanged += Published;
                var page = new ReimbursementsPage(vm);
                try { await Shell.Current.Navigation.PushAsync(page); await ready.Task.WaitAsync(TimeSpan.FromSeconds(45), ct); }
                finally { vm.Loading.PropertyChanged -= Published; }
                if (vm.Items.Count != expected.Count || !vm.Items.Select(row => row.Id).SequenceEqual(expected.Select(item => item.Expense.Id)))
                    throw new ReviewCheckException("Original large reimbursement order or count differs.");
                if (qualifyVisibility)
                {
                    var expectedAmounts = expected.ToDictionary(item => item.Expense.Id,
                        item => Core.Money.MoneyText.Format(item.Open, "EUR", local.CurrentCulture));
                    var expectedTitles = expected.ToDictionary(item => item.Expense.Id, item => item.Expense.Title);
                    if (vm.Items.Any(row => row.AmountText != expectedAmounts[row.Id] || row.Title != expectedTitles[row.Id])
                        || vm.TotalText != Core.Money.MoneyText.Format(3276800, "EUR", local.CurrentCulture))
                        throw new ReviewCheckException("Original reimbursement displayed amounts, titles or complete total differ.");
                }
                var rows = vm.Items.ToArray(); var packet = Core.Money.MoneyText.Format(long.MaxValue, "EUR", local.CurrentCulture);
                var changed = rows.Select(row => row with { AmountText = packet, Title = row.Title + " · " + new string('W', 30) }).ToArray();
                vm.Items.ReplaceAll(changed);
                var list = page.FindByName<CollectionView>("ReimbursementList");
                var realizedCounts = new List<int>();
                foreach (var edge in new[] { 0, changed.Length - 1 })
                {
                    list.ScrollTo(changed[edge], position: edge == 0 ? ScrollToPosition.Start : ScrollToPosition.End, animate: false);
                    Grid? row = null;
                    for (var wait = 0; wait < 200 && row is null; wait++)
                    {
                        await Task.Delay(50, ct);
                        row = ReimbursementViews(page).OfType<Grid>().FirstOrDefault(value => ReferenceEquals(value.BindingContext, changed[edge])
                            && value.Children.OfType<AmountReadout>().Any());
                    }
                    if (row is null) throw new ReviewCheckException("Original reimbursement edge did not realize.");
                    await AwaitReimbursementFocusAsync(ct);
                    var caption = ReimbursementViews(row).OfType<Label>().Single(value => value.Text == changed[edge].Title);
                    if (caption.Handler?.PlatformView is not Android.Widget.TextView native || native.Layout is not { } layout)
                        throw new ReviewCheckException("Original reimbursement caption missing.");
                    if (qualifyVisibility) await RevealNativeReimbursementAsync(native, ct);
                    for (var line = 0; line < layout.LineCount; line++)
                        if (layout.GetEllipsisCount(line) != 0 || layout.GetLineLeft(line) < -1 || layout.GetLineRight(line) > native.Width - native.TotalPaddingRight + 1)
                            throw new ReviewCheckException("Original reimbursement caption is clipped.");
                    var button = ReimbursementViews(row).OfType<Button>().Single();
                    if (button.Width < 44 || button.Height < 44 || !ReferenceEquals(button.Command, vm.OpenCommand)
                        || !ReferenceEquals(button.CommandParameter, changed[edge]) || SemanticProperties.GetDescription(button) != changed[edge].Title)
                        throw new ReviewCheckException("Original reimbursement target changed.");
                    if (qualifyVisibility)
                    {
                        if (button.Handler?.PlatformView is not Android.Views.View target)
                            throw new ReviewCheckException("Original reimbursement native target missing.");
                        using var node = target.CreateAccessibilityNodeInfo();
                        var name = node?.ContentDescription?.ToString() ?? node?.Text?.ToString() ?? string.Empty;
                        if (!name.Contains(changed[edge].Title, StringComparison.Ordinal))
                            throw new ReviewCheckException("Original reimbursement native accessible title incomplete.");
                    }
                    var amount = row.Children.OfType<AmountReadout>().Single();
                    var scroll = ReimbursementViews(amount).OfType<ScrollView>().Single();
                    var text = ReimbursementViews(amount).OfType<Label>().Single(value => value.Text == packet);
                    if (qualifyVisibility)
                    {
                        if (scroll.Handler?.PlatformView is not Android.Views.View viewport)
                            throw new ReviewCheckException("Original amount visibility target missing.");
                        await RevealNativeReimbursementAsync(viewport, ct);
                    }
                    var end = Math.Max(0, text.Width - scroll.Width);
                    await scroll.ScrollToAsync(end, 0, false); await Task.Delay(100, ct);
                    if (Math.Abs(scroll.ScrollX - end) > 2) throw new ReviewCheckException("Original reimbursement currency end unreachable.");
                    await scroll.ScrollToAsync(0, 0, false); await Task.Delay(100, ct);
                    await AwaitReimbursementFocusAsync(ct);
                    if (scroll.ScrollX > 1 || amount.Width > page.Width + 1) throw new ReviewCheckException("Original reimbursement monetary viewport failed.");
                    var count = ReimbursementViews(page).OfType<AmountReadout>().Count(value => value.Handler is not null);
                    if (count >= 200) throw new ReviewCheckException("Large reimbursement collection is not virtualized.");
                    realizedCounts.Add(count);
                }
                if (!visibilityOnly)
                {
                reads.Hold = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                var pending = vm.LoadAsync();
                if (!ReferenceEquals(pending, vm.LoadAsync()) || !vm.Loading.IsLoading || ReimbursementEffectivelyVisible(list))
                    throw new ReviewCheckException("Original covered reimbursement read was not shared.");
                vm.OpenCommand.Execute(changed[0]);
                if (!ReferenceEquals(Shell.Current.CurrentPage, page)) throw new ReviewCheckException("Covered reimbursement navigated.");
                reads.Hold.SetResult(); await pending;
                reads.FailNext();
                try { await vm.LoadAsync(); throw new ReviewCheckException("Expected original read refusal absent."); }
                catch (ReimbursementReadFailure) { }
                if (!vm.Loading.HasFailed || ReimbursementEffectivelyVisible(list)) throw new ReviewCheckException("Failed original read was exposed.");
                await Task.Delay(100, ct);
                await AwaitReimbursementFocusAsync(ct);
                if (page.FindByName<Button>("RetryLoadButton").Handler?.PlatformView is not Android.Views.View retry || !retry.PerformClick())
                    throw new ReviewCheckException("Original native retry unavailable.");
                for (var wait = 0; !vm.Loading.IsReady && wait < 300; wait++) await Task.Delay(50, ct);
                if (!vm.Loading.IsReady || vm.Items.Count != 4096 || !ReimbursementEffectivelyVisible(list)) throw new ReviewCheckException("Original native retry incomplete.");
                vm.OpenCommand.Execute(changed[0]);
                if (!ReferenceEquals(Shell.Current.CurrentPage, page)) throw new ReviewCheckException("Retired reimbursement navigated.");
                }
                if (qualifyVisibility)
                {
                    await vm.Loading.RunAsync(() => Task.CompletedTask, () => { vm.Items.ReplaceAll([]); vm.TotalText = string.Empty; vm.IsEmpty = true; });
                    await Task.Delay(200, ct); await AwaitReimbursementFocusAsync(ct);
                    if (page.Handler?.PlatformView is not Android.Views.View rootView)
                        throw new ReviewCheckException("Original empty reimbursement native page missing.");
                    var empty = ReimbursementViews(page).OfType<Label>().Single(value => value.Text == translator["Reimbursements_Empty"]);
                    if (empty.Handler?.PlatformView is not Android.Widget.TextView nativeEmpty)
                        throw new ReviewCheckException("Original empty reimbursement caption missing.");
                    await RevealNativeReimbursementAsync(nativeEmpty, ct);
                    var intro = ReimbursementViews(page).OfType<Label>().Single(value => value.Text == translator["Reimbursements_Intro"] && ReimbursementEffectivelyVisible(value));
                    if (intro.Handler?.PlatformView is not Android.Views.View introView)
                        throw new ReviewCheckException("Original empty reimbursement intro missing.");
                    var introPoint = new int[2]; var emptyPoint = new int[2];
                    introView.GetLocationOnScreen(introPoint); nativeEmpty.GetLocationOnScreen(emptyPoint);
                    if (introPoint[1] + introView.Height > emptyPoint[1] + 1)
                        throw new ReviewCheckException("Original reimbursement intro overlaps the empty caption.");
                    if (!ReimbursementEffectivelyVisible(page.FindByName<ScrollView>("EmptyReimbursementViewport")) || !vm.Loading.IsReady || !NativeTexts(rootView).Contains(translator["Reimbursements_Empty"]))
                        throw new ReviewCheckException("Presented empty reimbursement state missing.");
                }
                if (before != await StoredAsync()) throw new ReviewCheckException("Original reimbursement inputs changed.");
                var observation = new { Language = language, Theme = choice.ToString(), OriginalRows = expected.Count, Rows = vm.Items.Count, RealizedReadouts = realizedCounts,
                    CompleteOriginalOrder = true, CoveredCoalescedRead = !visibilityOnly, NativeRetryAfterRealFactoryFailure = !visibilityOnly, RetiredTargetRefused = !visibilityOnly,
                    NativeVerticalVisibilityRequired = qualifyVisibility, PresentedEmptyStateOnly = qualifyVisibility,
                    SeparateEmptyViewportAndNoOverlap = qualifyVisibility, OriginalTitlesAmountsAndTotal = qualifyVisibility,
                    SourceMvid = typeof(AndroidHomeReview).Assembly.ManifestModule.ModuleVersionId.ToString(), InputHash = inputHash };
                if (retainedFixture && !visibilityOnly)
                {
                    await using var output = new FileStream(receipt, FileMode.CreateNew, FileAccess.Write, FileShare.Read);
                    await JsonSerializer.SerializeAsync(output, observation, cancellationToken: ct);
                }
                observations.Add(observation);
                await Shell.Current.Navigation.PopAsync();
            }
        }
        return new { Comparison = comparison, OriginalWindowFocusRequired = true, WorkerContextConstructions = reads.Reads, RetainedCases = observations.OfType<JsonElement>().Count(),
            CompleteStoredInputsUnchanged = true, MainPackageTouched = false, SystemSettingsChanged = false, Observations = observations };
    }

    /// <summary>Requests original vertical visibility and refuses partially offscreen native captions or amounts.</summary>
    private static async Task RevealNativeReimbursementAsync(Android.Views.View target, CancellationToken ct)
    {
        target.RequestRectangleOnScreen(new Android.Graphics.Rect(0, 0, target.Width, target.Height), true);
        await Task.Delay(150, ct); await WaitLayoutAsync(target, ct); await AwaitReimbursementFocusAsync(ct);
        var visible = new Android.Graphics.Rect();
        if (!target.GetGlobalVisibleRect(visible) || visible.Width() + 1 < target.Width || visible.Height() + 1 < target.Height)
            throw new ReviewCheckException("Original reimbursement caption or amount is outside its visible native viewport.");
    }

    /// <summary>Preserves the same process while a real system dialog covers it; acceptance still requires actual focus.</summary>
    private static async Task AwaitReimbursementFocusAsync(CancellationToken ct)
    {
        if (Platform.CurrentActivity?.HasWindowFocus != true)
        {
            Android.Util.Log.Info("ZananceHomeStage", "reimbursement-await-original-window-focus");
            while (Platform.CurrentActivity?.HasWindowFocus != true) await Task.Delay(100, ct);
        }
        RequireHomeWindowFocus();
    }

    /// <summary>Checks the original page's complete visible ancestor chain.</summary>
    private static bool ReimbursementEffectivelyVisible(VisualElement value)
    {
        for (Element? current = value; current is not null; current = current.Parent)
            if (current is VisualElement { IsVisible: false }) return false;
        return true;
    }

    /// <summary>Visits only realized original MAUI views, not the collection's source objects.</summary>
    private static IEnumerable<Microsoft.Maui.Controls.View> ReimbursementViews(IVisualTreeElement node)
    {
        if (node is Microsoft.Maui.Controls.View view) yield return view;
        foreach (var child in node.GetVisualChildren()) foreach (var childView in ReimbursementViews(child)) yield return childView;
    }

    /// <summary>Fails one actual independent context construction, without altering SQLite inputs.</summary>
    private sealed class ReimbursementReadFailure : IOException;

    /// <summary>Requires real store reads on a worker and permits an explicit retained read/failure.</summary>
    private sealed class ReimbursementReadFactory(IDbContextFactory<ZananceDbContext> inner) : IDbContextFactory<ZananceDbContext>
    {
        private int _fail;
        /// <summary>Gets the observed worker context count.</summary>
        internal int Reads { get; private set; }
        /// <summary>Gets or sets an explicit independent read gate.</summary>
        internal TaskCompletionSource? Hold { get; set; }
        /// <summary>Fails only the next actual construction.</summary>
        internal void FailNext() => Interlocked.Exchange(ref _fail, 1);
        /// <inheritdoc />
        public ZananceDbContext CreateDbContext() { Check(); return inner.CreateDbContext(); }
        /// <inheritdoc />
        public async Task<ZananceDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
        { Check(); if (Hold is { } gate) await gate.Task.WaitAsync(cancellationToken); return await inner.CreateDbContextAsync(cancellationToken); }
        /// <summary>Rejects presentation-thread storage and consumes only an explicitly requested failure.</summary>
        private void Check()
        {
            if (MainThread.IsMainThread) throw new ReviewCheckException("Original reimbursement storage ran on the presentation thread.");
            Reads++;
            if (Interlocked.Exchange(ref _fail, 0) != 0) throw new ReimbursementReadFailure();
        }
    }
}
