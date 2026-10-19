using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using Android.Views;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Backup;
using Vafadar.Data;
using Vafadar.Localization;
using Vafadar.Zanance.App.Features.Home;
using Vafadar.Zanance.App.Security;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;
using Vafadar.Zanance.Performance;

namespace Vafadar.Zanance.App;

/// <summary>Measures original Release/AOT Home publication against independently owned protected financial workloads.</summary>
internal static partial class AndroidHomeReview
{
    /// <summary>The independent install allowed to own these fictitious workloads.</summary>
    internal const string Package = "pro.vafadar.zanance.androidhomereview";
    /// <summary>Public derived-result channel scoped by the host to the exact review process.</summary>
    internal const string LogTag = "ZananceHomeReview";
    private static object? _balanceEvidence;

    /// <summary>Prepares a fresh workload or measures its existing protected generations.</summary>
    internal static async Task RunAsync(App app, IServiceProvider services, string phase, string run, string? comparison, string? baseline)
    {
        if (AppInfo.Current.PackageName != Package) throw new ReviewCheckException("Independent compiled identity required.");
        // Android's /data/user/0 is an OS alias. Resolve it before applying the unchanged no-redirect fixture guard.
        var parent = Path.Combine(new Java.IO.File(FileSystem.AppDataDirectory).CanonicalPath, "android-home");
        Directory.CreateDirectory(parent);
        var root = Path.Combine(parent, run);
        _balanceEvidence = null;
        using var deadline = new CancellationTokenSource(TimeSpan.FromMinutes(12)); var ct = deadline.Token;
        try
        {
            if (phase is "prepare" or "prepare-unreviewed")
            {
                var resumeUnreviewed = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity?.Intent?.GetBooleanExtra("review_resume_unreviewed", false) == true;
                await Task.Run(() => PrepareAsync(root, ct, unreviewed: phase == "prepare-unreviewed", resumeUnreviewed), ct);
                await WriteReportAsync(root, phase, new { Prepared = true, Fictitious = true, UnreviewedCorpus = phase == "prepare-unreviewed", NativeOverflowRefusals = phase == "prepare-unreviewed" ? CheckUnreviewedOverflow() : 0, CoreModuleVersionId = typeof(LedgerCalculator).Module.ModuleVersionId, MainPackageTouched = false, Device = DeviceEvidence() }, ct);
            }
            else if (phase is "home-review-count" or "home-review-count-fixed" or "home-add-header" or "home-add-header-fixed" or "getting-started")
            {
                Directory.CreateDirectory(root);
                if (RedirectedAncestors(root).Length != 0) throw new ReviewCheckException("Nonredirected review root required.");
                app.OpenReviewShell();
                await AwaitReimbursementFocusAsync(ct);
                var local = services.GetRequiredService<ILocalizationService>();
                var display = DeviceDisplay.Current.MainDisplayInfo;
                var logicalWidth = Math.Round(display.Width / display.Density);
                var result = await Diagnostics.HomeReviewCountReview.RunAsync(app, services, root, [logicalWidth],
                    language => SetLanguageAsync(app, local, language, ct), async (page, context) =>
                    {
                        if (phase is "home-add-header" or "home-add-header-fixed")
                            await Diagnostics.HomeAddHeaderReview.CheckAsync(page, button =>
                            {
                                // A native modal owns focus instead of the covered Activity window.
                                // Require the actual original button's own shown/focused window on both surfaces.
                                if (button.Handler?.PlatformView is not Android.Widget.Button native
                                    || native.Context?.PackageName != Package || !native.IsShown || !native.HasWindowFocus
                                    || !native.Enabled || !native.PerformClick())
                                    throw new ReviewCheckException("Original native header action refused.");
                                return Task.CompletedTask;
                            }, ct);
                        if (phase == "getting-started")
                        {
                            foreach (var key in new[] { "Home_StartEntry", "Home_StartPlan", "Home_StartBudget" })
                            {
                                var text = services.GetRequiredService<Translator>()[key];
                                var label = ReimbursementViews(page).OfType<Label>().Single(value => value.Text == text);
                                await page.FindByName<ScrollView>("ContentViewport").ScrollToAsync(label, ScrollToPosition.Center, false);
                                if (label.Handler?.PlatformView is not Android.Widget.TextView native)
                                    throw new ReviewCheckException("Original guidance caption unavailable.");
                                await WaitLayoutAsync(native, ct); RequireHomeWindowFocus();
                                var button = ReimbursementViews(page).OfType<Button>().Single(value => SemanticProperties.GetDescription(value) == text);
                                if (!button.IsVisible || !button.IsEnabled || button.Command is null || label.TextDecorations != TextDecorations.None
                                    || (native.PaintFlags & Android.Graphics.PaintFlags.StrikeThruText) != 0)
                                    throw new ReviewCheckException("Original unfinished guidance action differs.");
                            }
                        }
                        var model = (HomeViewModel)page.BindingContext;
                        var viewport = page.FindByName<ScrollView>("ContentViewport");
                        foreach (var text in new[] { model.ConfirmedBalanceText, model.UnreviewedText })
                        {
                            var label = ReimbursementViews(page).OfType<Label>().Single(value => value.Text == text);
                            await viewport.ScrollToAsync(label, ScrollToPosition.Center, false);
                            if (label.Handler?.PlatformView is not Android.Widget.TextView native)
                                throw new ReviewCheckException("Original native review caption unavailable.");
                            // The original label mapper changes display digits without changing the bound value.
                            var expectedNative = label.FlowDirection == FlowDirection.LeftToRight
                                ? text : Vafadar.Localization.Formatting.NativeDigits.Apply(text);
                            await WaitLayoutAsync(native, ct); RequireHomeWindowFocus();
                            using var visible = new Android.Graphics.Rect();
                            var samples = new List<object>(); var complete = false;
                            for (var attempt = 0; attempt < 40; attempt++)
                            {
                                var layout = native.Layout;
                                var shown = native.GetGlobalVisibleRect(visible);
                                var ellipsis = layout is null ? -1 : Enumerable.Range(0, layout.LineCount).Sum(line => layout.GetEllipsisCount(line));
                                complete = label.Text == text && native.Text == expectedNative && shown && layout is not null && ellipsis == 0
                                    && native.Height - native.TotalPaddingTop - native.TotalPaddingBottom >= layout.Height;
                                samples.Add(new { Attempt = attempt, Context = context, CaptionMatched = native.Text == expectedNative, BoundTextMatched = label.Text == text, Shown = shown,
                                    native.Width, native.Height, native.TotalPaddingTop, native.TotalPaddingBottom, LayoutHeight = layout?.Height,
                                    Ellipsis = ellipsis, VisibleHeight = visible.Height(), Complete = complete });
                                if (complete) break;
                                await Task.Delay(50, ct);
                            }
                            _balanceEvidence = samples;
                            if (!complete)
                                throw new ReviewCheckException("Original review caption differs.");
                        }
                    }, ct, retainedFixture: phase is "home-review-count-fixed" or "home-add-header-fixed", gettingStarted: phase == "getting-started");
                await WriteReportAsync(root, phase, new { Comparison = comparison, Qualification = result, LogicalWidth = logicalWidth,
                    Device = DeviceEvidence(), MainPackageTouched = false }, ct);
            }
            else if (phase == "plan-publication-fixed")
            {
                if (!Directory.Exists(root) || RedirectedAncestors(root).Length != 0)
                    throw new ReviewCheckException("Original nonredirected plan review root required.");
                await WriteReportAsync(root, phase, await ReviewFirstPlanPublicationAsync(app, services, root, comparison!, ct, retainedFixture: true), ct);
            }
            else if (phase == "plan-publication")
            {
                Directory.CreateDirectory(root);
                if (RedirectedAncestors(root).Length != 0) throw new ReviewCheckException("Redirected plan review root refused.");
                await WriteReportAsync(root, phase, await ReviewFirstPlanPublicationAsync(app, services, root, comparison!, ct), ct);
            }
            else if (phase == "ecb-reference-report")
                await WriteReportAsync(root, phase, await ReadEcbReportAsync(root, comparison!, baseline!, ct), ct);
            else if (phase == "retained-aggregate")
                await WriteReportAsync(root, phase, await ReviewRetainedAggregateAsync(app, services, root, comparison!, ct), ct);
            else if (phase == "ecb-reference")
                await WriteReportAsync(root, phase, await ReviewEcbAsync(app, services, root, comparison!, ct), ct);
            else if (phase is "csv-input" or "csv-input-empty" or "csv-quotation")
                await WriteReportAsync(root, phase, await ReviewCsvInputAsync(app, services, root, comparison!, phase == "csv-input-empty", phase == "csv-quotation", ct), ct);
            else if (phase == "root-retirement")
            {
                if (!Directory.Exists(root) || RedirectedAncestors(root).Length != 0)
                    throw new ReviewCheckException("Retained independent diagnostic root required.");
                await WriteReportAsync(root, phase, await ReviewRootRetirementAsync(app, services, run, comparison!, ct), ct);
            }
            else if (phase is "home-refresh" or "home-refresh-references")
            {
                Directory.CreateDirectory(root);
                if (RedirectedAncestors(root).Length != 0 || File.Exists(Path.Combine(root, phase + "-" + comparison + "-result.json")))
                    throw new ReviewCheckException("Fresh independent Home refresh review required.");
                await WriteReportAsync(root, phase, await ReviewHomeRefreshAsync(app, services, root, comparison!, ct, referencesOnly: phase == "home-refresh-references"), ct);
            }
            else if (phase is "settlement-commit" or "settlement-commit-fixed" or "settlement-commit-observed")
            {
                Directory.CreateDirectory(root);
                if (RedirectedAncestors(root).Length != 0 || File.Exists(Path.Combine(root, phase + "-" + comparison + "-result.json")))
                    throw new ReviewCheckException("Fresh independent settlement review required.");
                await WriteReportAsync(root, phase, await ReviewSettlementCommitAsync(app, services, root, comparison!, ct, resume: phase != "settlement-commit", acceptedFirst: phase == "settlement-commit-observed"), ct);
            }
            else if (phase == "plans-add-header")
            {
                Directory.CreateDirectory(root);
                if (RedirectedAncestors(root).Length != 0 || File.Exists(Path.Combine(root, phase + "-" + comparison + "-result.json")))
                    throw new ReviewCheckException("Fresh independent Plans header review required.");
                await WriteReportAsync(root, phase, await ReviewPlansHeaderAsync(app, services, root, comparison!, ct), ct);
            }
            else if (phase == "editor-text")
            {
                Directory.CreateDirectory(root);
                if (RedirectedAncestors(root).Length != 0 || File.Exists(Path.Combine(root, phase + "-" + comparison + "-result.json")))
                    throw new ReviewCheckException("Fresh original editor text review required.");
                await WriteReportAsync(root, phase, await ReviewEditorTextAsync(app, services, root, comparison!, ct), ct);
            }
            else if (phase == "settings-text")
            {
                Directory.CreateDirectory(root);
                if (RedirectedAncestors(root).Length != 0 || File.Exists(Path.Combine(root, phase + "-" + comparison + "-result.json")))
                    throw new ReviewCheckException("Fresh original Settings text review required.");
                await WriteReportAsync(root, phase, await ReviewSettingsTextAsync(app, services, root, comparison!, ct), ct);
            }
            else if (phase == "read-profile")
            {
                RequireRoot(root);
                await WriteReportAsync(root, phase, await ReviewReadStagesAsync(root, comparison!, baseline!, ct), ct);
            }
            else if (phase is "ledger" or "ledger-read")
            {
                RequireRoot(root);
                await WriteReportAsync(root, phase, await ReviewPhoneLedgerAsync(app, services, root, comparison!, ct, includeWrites: phase == "ledger"), ct);
            }
            else if (phase is "destination-fee" or "destination-fee-fixed" or "destination-fee-native" or "destination-fee-lifecycle" or "destination-fee-owned" or "destination-fee-resume" or "destination-fee-inspect" or "destination-fee-prepared" or "destination-fee-migrations" or "destination-fee-effect" or "destination-fee-unicode")
            {
                Directory.CreateDirectory(root);
                if (RedirectedAncestors(root).Length != 0) throw new ReviewCheckException("Nonredirected transfer fixture required.");
                var intent = Path.Combine(root, "destination-fee-intent.json");
                var expected = JsonSerializer.Serialize(new { Comparison = comparison, SourceMvid = typeof(AndroidHomeReview).Module.ModuleVersionId });
                if (File.Exists(intent))
                {
                    if (await File.ReadAllTextAsync(intent, ct) != expected)
                    {
                        using var prior = JsonDocument.Parse(await File.ReadAllTextAsync(intent, ct));
                        if (phase is not ("destination-fee-fixed" or "destination-fee-native" or "destination-fee-lifecycle" or "destination-fee-owned" or "destination-fee-resume" or "destination-fee-inspect" or "destination-fee-prepared" or "destination-fee-migrations" or "destination-fee-effect" or "destination-fee-unicode") || prior.RootElement.GetProperty("Comparison").GetString() != comparison
                            || !Guid.TryParse(Platform.CurrentActivity?.Intent?.GetStringExtra("review_fee_baseline_mvid"), out var baselineMvid)
                            || prior.RootElement.GetProperty("SourceMvid").GetGuid() != baselineMvid)
                            throw new ReviewCheckException("Retained transfer source/comparison changed.");
                    }
                }
                else await File.WriteAllTextAsync(intent, expected, ct);
                await WriteReportAsync(root, phase, await ReviewDestinationFeeAsync(app, services, root, comparison!, ct), ct);
            }
            else if (phase == "narrow-surfaces")
            {
                if (File.Exists(Path.Combine(root, phase + "-" + comparison + "-result.json")))
                    throw new ReviewCheckException("Completed narrow-surface qualification must not be repeated.");
                await WriteReportAsync(root, phase, await ReviewNarrowSurfacesAsync(app, services, root, comparison!, ct), ct);
            }
            else if (phase == "font-scale")
            {
                // Reuse the retained financial presentation fixture, not a performance workload/reseed.
                if (!File.Exists(Path.Combine(root, "financial-tables.db")) || RedirectedAncestors(root).Length != 0)
                    throw new ReviewCheckException("Retained independent financial presentation fixture required.");
                if (File.Exists(Path.Combine(root, phase + "-" + comparison + "-result.json")))
                    throw new ReviewCheckException("Completed OS font-scale qualification must not be repeated.");
                await WriteReportAsync(root, phase, await ReviewOsFontScaleAsync(app, services, root, comparison!, ct), ct);
            }
            else if (phase is "tables" or "legends" or "budget-titles" or "home-legends" or "plan-rows" or "ledger-rows")
            {
                if (File.Exists(Path.Combine(root, phase + "-" + comparison + "-result.json")))
                    throw new ReviewCheckException("Completed financial table review must not be repeated.");
                Directory.CreateDirectory(root);
                await WriteReportAsync(root, phase, await ReviewFinancialTablesAsync(app, services, root, comparison!, ct,
                    legendsOnly: phase == "legends", budgetTitlesOnly: phase == "budget-titles", homeLegendsOnly: phase == "home-legends", planRowsOnly: phase == "plan-rows", ledgerRowsOnly: phase == "ledger-rows"), ct);
            }
            else if (phase == "billing-owned")
            {
                if (File.Exists(Path.Combine(root, phase + "-" + comparison + "-result.json")))
                    throw new ReviewCheckException("Completed billing review must not be repeated.");
                Directory.CreateDirectory(root);
                await WriteReportAsync(root, phase, await ReviewOwnedBillingAsync(app, services, root, comparison!, ct), ct);
            }
            else if (phase == "settings-read")
            {
                RequireRoot(root);
                if (File.Exists(Path.Combine(root, phase + "-" + comparison + "-result.json")))
                    throw new ReviewCheckException("Completed original settings review must not be repeated.");
                await WriteReportAsync(root, phase, await ReviewSettingsReadAsync(root, comparison!, ct), ct);
            }
            else if (phase == "schema")
            {
                RequireRoot(root);
                await WriteReportAsync(root, phase, await ReviewSchemaConnectionAsync(root, comparison!, ct), ct);
            }
            else if (phase is "reimbursement-visibility" or "reimbursement-page-fixed")
            {
                if (baseline is null || !System.Text.RegularExpressions.Regex.IsMatch(baseline, "^[0-9a-f]{32}$")
                    || !File.Exists(Path.Combine(root, "reimbursement-page-" + baseline + "-result.json"))
                    || RedirectedAncestors(root).Length != 0)
                    throw new ReviewCheckException("Original completed large reimbursement fixture required.");
                using var previous = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(root, "reimbursement-page-" + baseline + "-result.json"), ct));
                if (!previous.RootElement.GetProperty("CompleteStoredInputsUnchanged").GetBoolean()
                    || previous.RootElement.GetProperty("Observations").GetArrayLength() != 6)
                    throw new ReviewCheckException("Original reimbursement component acceptance incomplete.");
                await WriteReportAsync(root, phase, await ReviewReimbursementPageAsync(app, services, root, comparison!, ct, visibilityOnly: phase == "reimbursement-visibility", retainedFixture: true), ct);
            }
            else if (phase == "home-template-reload")
            {
                Directory.CreateDirectory(root);
                if (RedirectedAncestors(root).Length != 0 || (File.Exists(Path.Combine(root, phase + "-" + comparison + "-result.json")) || File.Exists(Path.Combine(root, phase + "-result.json"))))
                    throw new ReviewCheckException("Fresh nonredirected template review required.");
                await WriteReportAsync(root, phase, await ReviewHomeTemplatesAsync(app, services, root, comparison!, ct), ct);
            }
            else if (phase == "reimbursement-page")
            {
                Directory.CreateDirectory(root);
                if (RedirectedAncestors(root).Length != 0) throw new ReviewCheckException("Redirected reimbursement page root refused.");
                await WriteReportAsync(root, phase, await ReviewReimbursementPageAsync(app, services, root, comparison!, ct), ct);
            }
            else if (phase == "reimbursements")
            {
                Directory.CreateDirectory(root);
                if (RedirectedAncestors(root).Length != 0) throw new ReviewCheckException("Redirected reimbursement review root refused.");
                await WriteReportAsync(root, phase, await ReviewReimbursementsAsync(app, services, root, comparison!, ct), ct);
            }
            else if (phase == "filters")
            {
                if (File.Exists(Path.Combine(root, "filters-" + comparison + "-result.json")))
                    throw new ReviewCheckException("Completed filter review must not be repeated.");
                Directory.CreateDirectory(root);
                await WriteReportAsync(root, phase, await ReviewFilterActionsAsync(app, services, root, comparison!, ct), ct);
            }
            else
            {
                RequireRoot(root);
                if (comparison is null || !System.Text.RegularExpressions.Regex.IsMatch(comparison, "^[0-9a-f]{32}$")
                    || phase is not ("measure" or "compare" or "guard" or "home-stages")) throw new ReviewCheckException("Explicit original comparison required.");
                if (baseline is null || !System.Text.RegularExpressions.Regex.IsMatch(baseline, "^[0-9a-f]{32}$"))
                    throw new ReviewCheckException("Original baseline presentation identity required.");
                await MeasureAsync(app, services, root, phase, comparison, baseline, ct);
            }
        }
        catch (Exception error) when (error is not OutOfMemoryException)
        {
            // Never serialize exception messages: native command errors can contain key-bearing connection text.
            var report = new { Failed = true, Comparison = comparison, ErrorType = error.GetType().FullName, Stack = error.StackTrace,
                ReviewCheck = error is ReviewCheckException check ? check.Message : null,
                WindowPageType = app.Windows.FirstOrDefault()?.Page?.GetType().Name,
                ShellPageType = (app.Windows.FirstOrDefault()?.Page as AppShell)?.CurrentPage?.GetType().Name,
                BalanceEvidence = _balanceEvidence,
                HeaderEvidence = Diagnostics.HomeAddHeaderReview.LastObservation,
                HeaderProgress = File.Exists(Path.Combine(root, "review-count-progress.json")) ? await File.ReadAllTextAsync(Path.Combine(root, "review-count-progress.json")) : null,
                CompletedCases = File.Exists(Path.Combine(root, "progress-" + comparison + "-" + phase + ".json")) ? await File.ReadAllTextAsync(Path.Combine(root, "progress-" + comparison + "-" + phase + ".json")) : null,
                RootExists = Directory.Exists(root), MarkerExists = File.Exists(Path.Combine(root, "fictitious-performance.marker")),
                RedirectedAncestors = RedirectedAncestors(root) };
            var path = Directory.Exists(root) ? Path.Combine(root, "failure-" + Guid.NewGuid().ToString("N") + ".json")
                : Path.Combine(parent, run + "-failure-" + Guid.NewGuid().ToString("N") + ".json");
            await File.WriteAllTextAsync(path, JsonSerializer.Serialize(report), CancellationToken.None);
            LogReport(phase, run, report);
        }
    }

    private static string[] RedirectedAncestors(string root)
    {
        var names = new List<string>();
        for (var item = new DirectoryInfo(root); item is not null; item = item.Parent)
            if (item.Exists && (item.Attributes & FileAttributes.ReparsePoint) != 0) names.Add(item.Name);
        return names.ToArray();
    }

    private static void RequireRoot(string root)
    {
        if (!File.Exists(Path.Combine(root, "fictitious-performance.marker"))) throw new ReviewCheckException("Owned workload marker missing.");
        for (var item = new DirectoryInfo(root); item is not null; item = item.Parent)
            if ((item.Attributes & FileAttributes.ReparsePoint) != 0) throw new ReviewCheckException("Redirected workload root refused.");
    }

    private static async Task PrepareAsync(string root, CancellationToken ct, bool unreviewed = false, bool resumeUnreviewed = false)
    {
        if (resumeUnreviewed)
        {
            // Only the observed terminal pre-encryption reference failure can use this continuation.
            if (!unreviewed || Path.GetFileName(root) != "25b9dab64bf849fcb24e39246d773215"
                || !Directory.Exists(Path.Combine(root, "reference")) || Directory.Exists(Path.Combine(root, "tenfold")))
                throw new ReviewCheckException("Original failed unreviewed preparation required.");
            RequireRoot(root);
            var failures = Directory.GetFiles(root, "failure-*.json");
            if (failures.Length != 1) throw new ReviewCheckException("Exact retained preparation failure required.");
            using var failure = JsonDocument.Parse(await File.ReadAllTextAsync(failures[0], ct));
            if (failure.RootElement.GetProperty("ErrorType").GetString() != typeof(SqliteException).FullName
                || !failure.RootElement.GetProperty("Stack").GetString()!.Contains("PrepareAsync", StringComparison.Ordinal))
                throw new ReviewCheckException("Retained failure is not the original pre-encryption update.");
        }
        else FixtureDirectory.Create(root);
        foreach (var (name, shape) in new[] { ("reference", Workload.Reference), ("tenfold", Workload.Tenfold) })
        {
            var folder = Path.Combine(root, name);
            if (!(resumeUnreviewed && name == "reference")) await PerformanceFixture.CreateAsync(folder, shape, ct);
            var original = Path.Combine(folder, "fixture.db");
            if (unreviewed)
            {
                await PrepareUnreviewedRowsAsync(folder, shape, ct);
            }
            using var data = new ServiceCollection().AddZananceData(original, protectionRequired: true).BuildServiceProvider();
            var location = data.GetRequiredService<LocalDatabaseLocation<ZananceDbContext>>();
            var keyFolder = Path.Combine(folder, "keys"); Directory.CreateDirectory(keyFolder);
            var keys = new AndroidGenerationKeyStore(keyFolder);
            using var owner = await location.Access.AcquireExclusiveAsync(ct);
            await SqlCipherProfileBootstrap.PrepareAsync(location, owner, Path.Combine(folder, "identities.sqlite"),
                new Dictionary<string, string> { [""] = original }, true, keys, new AndroidGenerationFileDurability(folder),
                (_, _) => throw new ReviewCheckException("No restored intent belongs to a new workload."), () => RequireRoot(folder),
                256 * 1024 * 1024, "4.19.0", ct);
        }
    }

    /// <summary>Marks only a validated pre-encryption corpus through the original context, preserving all other typed rows.</summary>
    private static async Task PrepareUnreviewedRowsAsync(string folder, Workload shape, CancellationToken ct)
    {
        RequireRoot(folder);
        var original = Path.Combine(folder, "fixture.db");
        if (!File.Exists(original) || Directory.Exists(Path.Combine(folder, "keys")) || File.Exists(Path.Combine(folder, "identities.sqlite")))
            throw new ReviewCheckException("Unprotected original fixture required before first bootstrap.");
        // Observe the original raw-connection failure without executing another financial update.
        var missingFunction = false;
        await using (var rawConnection = new SqliteConnection(new SqliteConnectionStringBuilder
            { DataSource = original, Mode = SqliteOpenMode.ReadWrite, Pooling = false }.ToString()))
        {
            await rawConnection.OpenAsync(ct);
            using var explain = rawConnection.CreateCommand(); explain.CommandText = "EXPLAIN UPDATE Entries SET Review = 1";
            try { await using var unused = await explain.ExecuteReaderAsync(ct); }
            catch (SqliteException error) when (error.SqliteErrorCode == 1
                && error.Message.Contains("no such function: zanance_household_write", StringComparison.Ordinal)) { missingFunction = true; }
        }
        if (!missingFunction) throw new ReviewCheckException("Original raw write-function refusal not established.");
        using var data = new ServiceCollection().AddZananceData(original).BuildServiceProvider();
        await using var db = await data.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(ct);
        var applied = (await db.Database.GetAppliedMigrationsAsync(ct)).ToArray();
        if (!applied.SequenceEqual(db.Database.GetMigrations()) || await db.Entries.CountAsync(ct) != shape.Entries
            || await db.Entries.AnyAsync(entry => entry.Review != ReviewState.Confirmed, ct)
            || await db.Accounts.CountAsync(ct) != shape.Accounts || await db.Schedules.CountAsync(ct) != shape.Schedules
            || await db.Settings.CountAsync(ct) != 1)
            throw new ReviewCheckException("Original completed confirmed corpus differs; preserve its files.");
        await db.Database.OpenConnectionAsync(ct);
        var actual = (SqliteConnection)db.Database.GetDbConnection();
        using (var check = actual.CreateCommand())
        {
            check.CommandText = "SELECT (SELECT count(*) FROM HouseholdLocalBinding) + (SELECT count(*) FROM SyncBinding)";
            if (Convert.ToInt64(await check.ExecuteScalarAsync(ct), System.Globalization.CultureInfo.InvariantCulture) != 0)
                throw new ReviewCheckException("Only unbound independent fixtures can be prepared.");
        }
        await using var expected = new SqliteConnection("Data Source=:memory:;Pooling=False");
        await expected.OpenAsync(ct); actual.BackupDatabase(expected);
        // Keep the original native guard function on both connections; never replace or disable a trigger.
        var options = new DbContextOptionsBuilder<ZananceDbContext>().UseSqlite(expected)
            .UseModel(Vafadar.Zanance.Data.CompiledModel.ZananceDbContextModel.Instance).Options;
        await using var expectedContext = new ZananceDbContext(options);
        if (await expectedContext.Database.ExecuteSqlRawAsync("UPDATE Entries SET Review = 1", ct) != shape.Entries
            || await db.Database.ExecuteSqlRawAsync("UPDATE Entries SET Review = 1", ct) != shape.Entries)
            throw new ReviewCheckException("Exact fresh review transition required.");
        await SqliteSnapshotVerifier.VerifyAsync(expected, actual, ct);
        LogReport("unreviewed-preparation-rows", Path.GetFileName(Path.GetDirectoryName(folder)!), new
        {
            Shape = Path.GetFileName(folder), shape.Entries, RawMissingFunctionConfirmed = true,
            OriginalContextGuardPreserved = true, CompleteSchemaAndTypedRowsExceptIntendedReviewMatched = true,
            BeforeFirstEncryption = true,
        });
    }

    /// <summary>Checks the original AOT calculator's two total boundaries without any stored or financial write.</summary>
    private static int CheckUnreviewedOverflow()
    {
        var refused = 0;
        foreach (var kind in new[] { EntryKind.Income, EntryKind.Expense })
        {
            var account = new Core.Accounts.Account { Name = "Independent review boundary", CurrencyCode = "EUR" };
            var first = new LedgerEntry { AccountId = account.Id, Amount = long.MaxValue, Kind = kind, Review = ReviewState.Unreviewed };
            var second = new LedgerEntry { AccountId = account.Id, Amount = 2, Kind = kind, Review = ReviewState.Unreviewed };
            try
            {
                _ = LedgerCalculator.Unreviewed([account], [first, second]);
                throw new ReviewCheckException("Unreviewed total overflow was not refused.");
            }
            catch (OverflowException) { refused++; }
        }
        return refused;
    }

    /// <summary>Refuses a rendered Home measured behind a native dialog or another foreground window.</summary>
    private static void RequireHomeWindowFocus()
    {
        if (Microsoft.Maui.ApplicationModel.Platform.CurrentActivity?.HasWindowFocus != true)
            throw new ReviewCheckException("Original Home Activity must own actual window focus.");
    }

    private static async Task MeasureAsync(App app, IServiceProvider services, string root, string phase, string comparison, string baseline, CancellationToken ct)
    {
        var local = services.GetRequiredService<ILocalizationService>();
        app.OpenReviewShell();
        await Task.Delay(1000, ct); // Let original empty-install foreground work settle outside the measurement.
        var results = new List<object>();
        var nativeReadProfiles = new List<object>();
        var tagReaderContracts = new List<object>();
        var reviewedInputGuards = new List<object>();
        var completed = new HashSet<string>(StringComparer.Ordinal);
        if (File.Exists(Path.Combine(root, "progress-" + comparison + "-" + phase + ".json")))
        {
            using var previous = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(root, "progress-" + comparison + "-" + phase + ".json"), ct));
            foreach (var item in previous.RootElement.EnumerateArray())
            {
                var name = item.GetProperty("Shape").GetString(); var language = item.GetProperty("Language").GetString();
                if (name is not ("reference" or "tenfold") || language is not ("en" or "fa" or "de") ||
                    !item.GetProperty("OriginalBalancesAndNativeTextMatched").GetBoolean() ||
                    !item.GetProperty("OriginalFinancialPresentationMatched").GetBoolean() ||
                    !item.TryGetProperty("OriginalWindowFocusRequired", out var focused) || !focused.GetBoolean() ||
                    item.GetProperty("FirstThenSecondPublicationMilliseconds").GetArrayLength() != (phase == "guard" ? 1 : 2) ||
                    !completed.Add(name + ":" + language)) throw new ReviewCheckException("Invalid retained completed case.");
                results.Add(item.Clone());
            }
        }
        foreach (var (name, shape) in new[] { ("reference", Workload.Reference), ("tenfold", Workload.Tenfold) })
        {
            var folder = Path.Combine(root, name); RequireRoot(folder);
            var original = Path.Combine(folder, "fixture.db");
            var identityPath = Path.Combine(folder, "identities.sqlite");
            if (File.Exists(original) || !File.Exists(identityPath) || !Directory.Exists(Path.Combine(folder, "keys")))
                throw new ReviewCheckException("Only the retained protected workload can be measured; never regenerate keys or finance.");
            using var data = new ServiceCollection().AddZananceData(original, protectionRequired: true).BuildServiceProvider();
            var location = data.GetRequiredService<LocalDatabaseLocation<ZananceDbContext>>();
            var keys = new AndroidGenerationKeyStore(Path.Combine(folder, "keys"));
            SqlCipherProfileGeneration generation;
            using (var owner = await location.Access.AcquireExclusiveAsync(ct))
            {
                var identities = new SqliteDatabaseIdentityCatalogue(identityPath, () => RequireRoot(folder));
                var identity = (await identities.ReadAsync(ct)).Single();
                var history = new SqliteProfileRestoreHistory(original + ".restore-history.sqlite", identity.InitialCandidateKey, () => RequireRoot(folder));
                generation = await SqlCipherProfileGeneration.OpenCurrentAsync(location, identities, "", original, history,
                    _ => throw new ReviewCheckException("No restored workload is expected."), keys, new AndroidGenerationFileDurability(folder),
                    owner, () => RequireRoot(folder), 256 * 1024 * 1024, "4.19.0", ct);
                generation.Publish(location, owner);
            }
            using (generation)
            {
                if (phase is not ("guard" or "home-stages")) nativeReadProfiles.Add(await ProfileNativeReadAsync(data, shape.Entries, ct));
                var today = DateOnly.FromDateTime(services.GetRequiredService<TimeProvider>().GetLocalNow().DateTime);
                var expected = await data.GetRequiredService<ZananceStore>().GetHomeSnapshotAsync(today, 0, ct);
                if (expected.Entries.Count != shape.Entries || expected.QuickResources.Accounts.Count != shape.Accounts || expected.Plans.Schedules.Count != shape.Schedules)
                    throw new ReviewCheckException("Retained complete workload shape differs.");
                await using (var metadata = await data.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(ct))
                {
                    var mapping = metadata.Model.FindEntityType(typeof(LedgerEntry))!.FindProperty(nameof(LedgerEntry.Tags))!.GetTypeMapping();
                    var collectionReader = mapping.Converter is null ? mapping.JsonValueReaderWriter
                        : mapping.Converter.GetType() == typeof(Microsoft.EntityFrameworkCore.Storage.ValueConversion.CollectionToJsonStringConverter<string>)
                            ? ((Microsoft.EntityFrameworkCore.Storage.ValueConversion.CollectionToJsonStringConverter<string>)mapping.Converter).JsonReaderWriter : null;
                    tagReaderContracts.Add(new { Shape = name, Converter = mapping.Converter?.GetType().Name,
                        Reader = collectionReader?.GetType().Name, CanonicalEmptyArrayReader = collectionReader?.GetType()
                            == typeof(Microsoft.EntityFrameworkCore.Storage.Json.JsonCollectionOfReferencesReaderWriter<List<string>, string>) });
                }
                var balances = LedgerCalculator.TotalBalances(expected.QuickResources.Accounts, expected.Entries, today);
                // D-383: preserve an independent old-order oracle outside every timed publication.
                var expectedRecent = expected.Entries.Where(entry => entry.Date <= today)
                    .OrderByDescending(entry => entry.Date).ThenByDescending(entry => entry.CreatedAt).Take(3).ToArray();
                foreach (var language in phase == "guard" ? new[] { "en" } : new[] { "en", "fa", "de" })
                {
                    if (completed.Contains(name + ":" + language)) continue;
                    await SetLanguageAsync(app, local, language, ct);
                    if (app.Windows[0].Page is not AppShell shell || shell.CurrentPage is not HomePage originalHome)
                        throw new ReviewCheckException("Original native Shell/Home is unavailable.");
                    await originalHome.PendingPublication;
                    var samples = new List<double>(); var gaps = new List<double>(); var notificationSamples = new List<object>();
                    for (var sample = 0; sample < (phase == "guard" ? 1 : 2); sample++)
                    {
                        RequireHomeWindowFocus();
                        var reads = new WindowReadFactory(data.GetRequiredService<IDbContextFactory<ZananceDbContext>>());
                        var store = new ZananceStore(reads, data.GetRequiredService<TimeProvider>(), data.GetRequiredService<ICommercialWriteAccessSource>());
                        var vm = ActivatorUtilities.CreateInstance<HomeViewModel>(services, store);
                        // D-320: observe existing public publication boundaries before native bindings subscribe.
                        // No timing hook, financial algorithm or observer is introduced into the production app.
                        var clock = new Stopwatch(); var marks = new List<object>();
                        void Mark(string stage)
                        {
                            if (phase != "home-stages") return;
                            lock (marks) marks.Add(new { Stage = stage, Milliseconds = clock.Elapsed.TotalMilliseconds });
                            if (stage is "navigation-start" or "property:QuickAddTargetText" or "final-model-publication" or "native-layout-ready")
                                Android.Util.Log.Info("ZananceHomeStage", comparison + "|" + name + "|" + language + "|" + sample + "|" + stage);
                        }
                        System.ComponentModel.PropertyChangedEventHandler changed = (_, args) =>
                        {
                            if (args.PropertyName is "QuickAddTargetText" or "HasAccounts" or "ScopeText" or "ForecastEndText" or "HasSlices" or "HoldingsText" or "HasPinnedGoals" or "HasAttention") Mark("property:" + args.PropertyName);
                        };
                        System.Collections.Specialized.NotifyCollectionChangedEventHandler balancesChanged = (_, _) => Mark("balances-publish");
                        System.Collections.Specialized.NotifyCollectionChangedEventHandler periodsChanged = (_, args) => Mark("periods:" + args.Action);
                        System.Collections.Specialized.NotifyCollectionChangedEventHandler plansChanged = (_, args) => Mark("plans:" + args.Action);
                        System.Collections.Specialized.NotifyCollectionChangedEventHandler slicesChanged = (_, args) => Mark("slices:" + args.Action);
                        System.Collections.Specialized.NotifyCollectionChangedEventHandler accountsChanged = (_, _) => Mark("accounts-publish");
                        System.Collections.Specialized.NotifyCollectionChangedEventHandler recentChanged = (_, args) => Mark("recent:" + args.Action);
                        System.Collections.Specialized.NotifyCollectionChangedEventHandler goalsChanged = (_, args) => Mark("goals:" + args.Action);
                        System.Collections.Specialized.NotifyCollectionChangedEventHandler templatesChanged = (_, args) => Mark("templates:" + args.Action);
                        if (phase == "home-stages")
                        {
                            vm.PropertyChanged += changed; vm.Balances.CollectionChanged += balancesChanged;
                            vm.Periods.CollectionChanged += periodsChanged; vm.Upcoming.CollectionChanged += plansChanged;
                            vm.Slices.CollectionChanged += slicesChanged; vm.Accounts.CollectionChanged += accountsChanged;
                            vm.RecentEntries.CollectionChanged += recentChanged; vm.PinnedGoals.CollectionChanged += goalsChanged;
                            vm.QuickTemplates.CollectionChanged += templatesChanged;
                        }
                        var page = new HomePage(vm) { FlowDirection = local.CurrentLanguage.IsRightToLeft ? FlowDirection.RightToLeft : FlowDirection.LeftToRight };
                        var presentations = 0;
                        vm.PresentingSnapshot += () => { if (vm.Loading.IsReady) throw new ReviewCheckException("Layout published after uncovering."); presentations++; Mark("final-model-publication"); };
                        var heartbeat = Stopwatch.StartNew(); var last = 0d; var largest = 0d;
                        var timer = app.Dispatcher.CreateTimer(); timer.Interval = TimeSpan.FromMilliseconds(50);
                        timer.Tick += (_, _) => { var now = heartbeat.Elapsed.TotalMilliseconds; largest = Math.Max(largest, now - last); last = now; };
                        clock.Start(); Mark("navigation-start"); timer.Start();
                        await shell.Navigation.PushAsync(page, false);
                        try
                        {
                            await page.PendingPublication;
                            if (!vm.Loading.IsReady || vm.Accounts.Count != shape.Accounts || !vm.HasEntries ||
                                reads.SynchronousCaptures != 0 || presentations != 1 || !page.FindByName<ScrollView>("ContentViewport").IsVisible)
                                throw new ReviewCheckException("Complete original Home publication differs.");
                            if (page.Handler?.PlatformView is not Android.Views.View native) throw new ReviewCheckException("Android native Home tree unavailable.");
                            await WaitLayoutAsync(native, ct); Mark("native-layout-ready"); clock.Stop();
                            RequireHomeWindowFocus();
                            var texts = NativeTexts(native).ToHashSet(StringComparer.Ordinal);
                            foreach (var balance in balances)
                            {
                                var packet = Core.Money.MoneyText.Format(balance.Value, balance.Key, local.CurrentCulture);
                                // Native label mapping applies the original digit/separator choice; canonical VM money remains Latin.
                                var displayed = Vafadar.Localization.Formatting.NativeDigits.Apply(packet)!;
                                _balanceEvidence = new { Shape = name, Language = language, Expected = packet,
                                    ViewModel = vm.Balances.Select(value => new { value.CurrencyCode, value.Text }).ToArray(),
                                    NativeTextCount = texts.Count, NativeCurrencyTexts = texts.Where(text => text.Contains(balance.Key, StringComparison.Ordinal)).Take(8).ToArray() };
                                if (!vm.Balances.Any(value => value.CurrencyCode == balance.Key && value.Text == packet) || !texts.Contains(displayed))
                                    throw new ReviewCheckException("Real native balance differs from the complete original ledger.");
                            }
                            await Task.Delay(75, ct); timer.Stop();
                            var recentPresenter = new Presentation.EntryPresenter(expected.QuickResources.Accounts.ToDictionary(account => account.Id),
                                new Presentation.CategoryLookup(expected.QuickResources.Categories, services.GetRequiredService<Translator>()),
                                services.GetRequiredService<Translator>(), local.CurrentCulture);
                            var recentPackets = expectedRecent.Select(entry => recentPresenter.Row(entry)).ToArray();
                            if (!vm.RecentEntries.Select(row => row.Id).SequenceEqual(expectedRecent.Select(entry => entry.Id))
                                || vm.HasRecent != (expectedRecent.Length > 0)
                                || vm.RecentEntries.Where((row, index) => row.Title != recentPackets[index].Title
                                    || row.AmountText != recentPackets[index].AmountText || row.IsUnreviewed != recentPackets[index].IsUnreviewed
                                    || !row.Subtitle.EndsWith(" · " + recentPackets[index].Subtitle, StringComparison.Ordinal)).Any())
                                throw new ReviewCheckException("Original recent entry identities or formatted packets differ.");
                            var presentation = CapturePresentation(vm);
                            var originalPath = Path.Combine(folder, "presentation-" + baseline + "-" + language + ".json");
                            if ((phase == "measure" || phase == "home-stages" && comparison == baseline) && !File.Exists(originalPath))
                                await File.WriteAllTextAsync(originalPath, presentation, ct);
                            if (!File.Exists(originalPath) || presentation != await File.ReadAllTextAsync(originalPath, ct))
                                throw new ReviewCheckException("Original complete financial presentation differs.");
                            samples.Add(clock.Elapsed.TotalMilliseconds); gaps.Add(largest);
                            if (phase == "home-stages") lock (marks) notificationSamples.Add(new { Sample = sample, Marks = marks.ToArray() });
                        }
                        finally
                        {
                            timer.Stop(); vm.PropertyChanged -= changed; vm.Balances.CollectionChanged -= balancesChanged;
                            vm.Periods.CollectionChanged -= periodsChanged; vm.Upcoming.CollectionChanged -= plansChanged;
                            vm.Slices.CollectionChanged -= slicesChanged; vm.Accounts.CollectionChanged -= accountsChanged;
                            vm.RecentEntries.CollectionChanged -= recentChanged; vm.PinnedGoals.CollectionChanged -= goalsChanged;
                            vm.QuickTemplates.CollectionChanged -= templatesChanged;
                            page.RetirePublication(); await shell.Navigation.PopAsync(false); await originalHome.PendingPublication;
                        }
                    }
                    results.Add(new { Shape = name, Language = language, shape.Entries, shape.Accounts, shape.Schedules,
                        FirstThenSecondPublicationMilliseconds = samples, LargestDispatcherGapMilliseconds = gaps, PublicNotificationSamples = notificationSamples,
                        OriginalBalancesAndNativeTextMatched = true, OriginalFinancialPresentationMatched = true, OriginalRecentEntryPacketsMatched = true,
                        ActualNativeLayout = true, OriginalWindowFocusRequired = true, NoSynchronousSettingsRead = true });
                    await File.WriteAllTextAsync(Path.Combine(root, "progress-" + comparison + "-" + phase + ".json"), JsonSerializer.Serialize(results), ct);
                    _balanceEvidence = null;
                }
                if (phase == "guard")
                {
                    var store = data.GetRequiredService<ZananceStore>();
                    await store.ValidateHomeSnapshotAsync(expected, ct);
                    var originalAmount = expected.Plans.Schedules[0].Amount;
                    var planRejected = false;
                    try
                    {
                        expected.Plans.Schedules[0].Amount = (originalAmount ?? 0) + 1;
                        try { await store.ValidateHomeSnapshotAsync(expected, ct); }
                        catch (InvalidOperationException) { planRejected = true; }
                    }
                    finally { expected.Plans.Schedules[0].Amount = originalAmount; }
                    await store.ValidateHomeSnapshotAsync(expected, ct);
                    var added = new Core.Goals.Goal { Name = "Fictitious altered review only", CurrencyCode = "EUR", TargetAmount = 123 };
                    var goalRejected = false;
                    try
                    {
                        expected.Goals.Goals.Add(added);
                        try { await store.ValidateHomeSnapshotAsync(expected, ct); }
                        catch (InvalidOperationException) { goalRejected = true; }
                    }
                    finally { expected.Goals.Goals.Remove(added); }
                    await store.ValidateHomeSnapshotAsync(expected, ct);
                    if (!planRejected || !goalRejected) throw new ReviewCheckException("Altered original input review was accepted.");
                    reviewedInputGuards.Add(new { Shape = name, AlteredPlanInputRefused = true, AlteredGoalInputRefused = true,
                        SameRestoredOriginalReviewAccepted = true, NoDatabaseSave = true });
                }
                var rawKey = await keys.ReadAsync(generation.Binding, ct);
                try
                {
                    await using var retainedRollback = new SqliteConnection(new SqliteConnectionStringBuilder
                    { DataSource = generation.Path + ".rollback.db", Mode = SqliteOpenMode.ReadOnly, Pooling = false,
                        Password = Convert.ToBase64String(rawKey) }.ToString());
                    await retainedRollback.OpenAsync(ct);
                    await using var current = await data.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(ct);
                    await current.Database.OpenConnectionAsync(ct);
                    await SqliteSnapshotVerifier.VerifyAsync(retainedRollback, (SqliteConnection)current.Database.GetDbConnection(), ct);
                }
                finally { CryptographicOperations.ZeroMemory(rawKey); }
            }
        }
        await WriteReportAsync(root, phase, new { Comparison = comparison, BaselineComparison = baseline, Results = results, NativeReadProfiles = nativeReadProfiles, TagReaderContracts = tagReaderContracts, ReviewedInputGuards = reviewedInputGuards,
            DataModuleVersionId = typeof(ZananceStore).Module.ModuleVersionId, CallerModuleVersionId = typeof(HomePage).Module.ModuleVersionId,
            CoreModuleVersionId = typeof(Core.Plans.Occurrences).Module.ModuleVersionId,
            OriginalProductionPage = true, OriginalWindowFocusRequired = true, RetainedCompletedCases = completed.ToArray(),
            ModuleIdentityScope = "Current completion caller; retained case packets preserve their original caller evidence",
            CompleteRawSchemaAndTypedRowsUnchanged = true, NoFinancialSave = true, Device = DeviceEvidence(), WholeAppColdStartupOrFullDeviceAcceptance = false,
            Boundary = "Original navigation/read/projection/publication plus Android layout and frame callbacks; excludes fixture preparation and cold startup" }, ct);
    }

    /// <summary>Separates native stepping from entity materialization, with the original operation-local cache and no financial writes.</summary>
    private static Task<object> ProfileNativeReadAsync(IServiceProvider data, int expected, CancellationToken ct) => Task.Run(async () =>
    {
        await using var db = await data.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(ct);
        await db.Database.OpenConnectionAsync(ct);
        await db.Database.ExecuteSqlRawAsync("PRAGMA cache_size = -32768", ct);
        var connection = (SqliteConnection)db.Database.GetDbConnection();
        using var transaction = connection.BeginTransaction(deferred: true);
        using var command = connection.CreateCommand(); command.Transaction = transaction;
        command.CommandText = "SELECT * FROM Entries NOT INDEXED ORDER BY rowid DESC";
        var clock = Stopwatch.StartNew(); var rows = 0;
        await using (var reader = await command.ExecuteReaderAsync(ct))
            while (await reader.ReadAsync(ct)) rows++;
        clock.Stop();
        if (rows != expected) throw new ReviewCheckException("Native read profile lost original rows.");
        var entity = db.Model.FindEntityType(typeof(LedgerEntry)) as Microsoft.EntityFrameworkCore.Metadata.RuntimeEntityType
            ?? throw new ReviewCheckException("Original compiled runtime model unavailable.");
        var binding = entity.ConstructorBinding as Microsoft.EntityFrameworkCore.Metadata.ConstructorBinding
            ?? throw new ReviewCheckException("Original ledger materialization binding unavailable.");
        return (object)new { Entries = rows, NativeSteppingMilliseconds = clock.Elapsed.TotalMilliseconds,
            ConstructorParameters = binding.Constructor.GetParameters().Select(parameter => parameter.Name).ToArray(),
            Boundary = "native full-row stepping only; excludes key opening, CLR field conversion and managed sorting" };
    }, ct);

    /// <summary>Captures original financial presentation without commands, colours or copied calculation algorithms.</summary>
    private static string CapturePresentation(HomeViewModel vm) => JsonSerializer.Serialize(new
    {
        Balances = vm.Balances.Select(value => new { value.CurrencyCode, value.Text }),
        Periods = vm.Periods.Select(value => new { value.IncomeText, value.ExpenseText, value.ResultText, value.RefundsText }),
        Accounts = vm.Accounts.Select(value => new { value.Id, value.Name, value.TypeName, value.BalanceText, value.IsNegative, value.NotInTotals, value.OpeningUnknown }),
        Upcoming = vm.Upcoming.Select(value => new { value.ScheduleId, value.OriginalDate, value.Title, value.Subtitle, value.AmountText, value.Badge }),
        Slices = vm.Slices.Select(value => new { value.CategoryIds, value.Name, value.Value, value.AmountText, value.PercentText }),
        vm.BudgetText, vm.BudgetDailyText, vm.BudgetProgress, vm.ForecastEndText, vm.ForecastLowText, vm.ForecastNote,
        vm.LowBalanceText, vm.DueCount, vm.DueText, vm.Next30Text, vm.ContractText, vm.ReimbursementText,
        vm.UnreviewedCount, vm.UnreviewedText, vm.ConfirmedBalanceText, vm.CombinedText, vm.CombinedIncomplete,
        vm.HoldingsText, vm.GoalAttentionText, vm.ScopeText,
    });

    private static async Task SetLanguageAsync(App app, ILocalizationService local, string language, CancellationToken ct)
    {
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        void Rebuilt(object? sender, EventArgs args) => completion.TrySetResult();
        app.ShellRebuilt += Rebuilt;
        try
        {
            var changed = local.CurrentLanguage.CultureName != language;
            local.SetLanguage(local.SupportedLanguages.Single(value => value.CultureName == language));
            if (changed) await completion.Task.WaitAsync(TimeSpan.FromSeconds(15), ct);
        }
        finally { app.ShellRebuilt -= Rebuilt; }
        await Task.Delay(500, ct);
    }

    private static async Task WaitLayoutAsync(Android.Views.View view, CancellationToken ct)
    {
        // Keep the original successful frame boundary; allow the same real layout to settle on slower hardware.
        for (var frame = 0; frame < 300; frame++)
        {
            using var callback = new FrameCompletion();
            Choreographer.Instance!.PostFrameCallback(callback);
            await callback.Completion.Task.WaitAsync(ct);
            if (frame > 0 && view.IsLaidOut && !view.IsLayoutRequested && view.Width > 0 && view.Height > 0) return;
        }
        throw new ReviewCheckException("Original native Home did not finish layout.");
    }

    private static IEnumerable<string> NativeTexts(Android.Views.View view)
    {
        if (view is Android.Widget.TextView text && text.Text is { } value) yield return value;
        if (view is ViewGroup group)
            for (var index = 0; index < group.ChildCount; index++)
                foreach (var item in NativeTexts(group.GetChildAt(index)!)) yield return item;
    }

    private static async Task WriteReportAsync(string root, string phase, object report, CancellationToken ct)
    {
        var packet = JsonSerializer.SerializeToElement(report);
        var suffix = packet.TryGetProperty("Comparison", out var comparison) ? "-" + comparison.GetString() : "";
        await using (var output = new FileStream(Path.Combine(root, phase + suffix + "-result.json"), FileMode.CreateNew, FileAccess.Write, FileShare.Read))
            await JsonSerializer.SerializeAsync(output, report, cancellationToken: ct);
        LogReport(phase, Path.GetFileName(root), report);
    }

    private static void LogReport(string phase, string run, object report)
    {
        // Only counts/timings/verification outcomes or type/stack are emitted, never key material or native error messages.
        // Chunk the public JSON rather than enabling debugging or exposing private files in this Release application.
        var bytes = System.Text.Encoding.UTF8.GetBytes(JsonSerializer.Serialize(report));
        var count = (bytes.Length + 1535) / 1536;
        for (var index = 0; index < count; index++)
        {
            var offset = index * 1536; var length = Math.Min(1536, bytes.Length - offset);
            Android.Util.Log.Info(LogTag, phase + "|" + run + "|" + index + "/" + count + "|" + Convert.ToBase64String(bytes.AsSpan(offset, length)));
        }
    }

    private sealed class ReviewCheckException(string check) : IOException(check) { }

    private sealed class FrameCompletion : Java.Lang.Object, Choreographer.IFrameCallback
    {
        /// <summary>Completes on the actual Android frame callback.</summary>
        internal TaskCompletionSource Completion { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public void DoFrame(long frameTimeNanos) => Completion.TrySetResult();
    }

    private sealed class WindowReadFactory(IDbContextFactory<ZananceDbContext> inner) : IDbContextFactory<ZananceDbContext>
    {
        /// <summary>Counts forbidden synchronous context captures during original page publication.</summary>
        internal int SynchronousCaptures;
        public ZananceDbContext CreateDbContext() { SynchronousCaptures++; return inner.CreateDbContext(); }
        public Task<ZananceDbContext> CreateDbContextAsync(CancellationToken ct = default) => inner.CreateDbContextAsync(ct);
    }
}
