#if SYNC_PUBLIC_REVIEW
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Android.Views;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Backup;
using Vafadar.Data;
using Vafadar.Localization;
using Vafadar.Localization.Formatting;
using Vafadar.Maui.Controls;
using Vafadar.Zanance.App.Features.Sync;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App;

internal static partial class AndroidSyncReview
{
    /// <summary>Reviews only changed connection/default/help surfaces through original controls in a fresh owned package.</summary>
    private static async Task RunPublicServiceAsync(App app, IServiceProvider services, string run, bool resume)
    {
        var checkpoint = "identity";
        using var deadline = new CancellationTokenSource(TimeSpan.FromMinutes(5)); var ct = deadline.Token;
        try
        {
            Check(!resume && AppInfo.Current.PackageName == Package, "Fresh independent public-service review required.");
            var path = new Java.IO.File(FileSystem.AppDataDirectory).CanonicalPath;
            Check(path.Contains("/" + Package + "/", StringComparison.Ordinal), "Independent native data root required.");
            var root = Path.Combine(path, "public-sync-review", run); Directory.CreateDirectory(root);
            Check(!File.Exists(Path.Combine(root, "started")), "Never reset a previous owned review.");
            await File.WriteAllTextAsync(Path.Combine(root, "started"), "Fictitious original hosted connection review", ct);
            var finances = services.GetRequiredService<ZananceStore>();
            Check((await finances.GetAccountsAsync(cancellationToken: ct)).Count == 0, "Fresh account-free installation required.");
            using var beforeBytes = new MemoryStream(); await services.GetServices<IBackupSource>().Single(source => source.Name == SqliteDatabaseBackupSource<ZananceDbContext>.EntryName).WriteAsync(beforeBytes, ct); beforeBytes.Position = 0;
            await using var before = await SqliteMemorySnapshot.OpenAsync(beforeBytes, 32 * 1024 * 1024, ct);
            var local = services.GetRequiredService<ILocalizationService>(); var translator = services.GetRequiredService<Translator>();
            var results = new List<object>();
            foreach (var language in new[] { "en", "fa", "de" })
            foreach (var theme in new[] { AppTheme.Light, AppTheme.Dark })
            foreach (var hosted in new[] { false, true })
            {
                checkpoint = language + "-" + theme + "-" + hosted;
                local.SetLanguage(local.SupportedLanguages.Single(item => item.CultureName == language)); app.UserAppTheme = theme;
                await Task.Delay(200, ct);
                var vm = services.GetRequiredService<PersonalSyncViewModel>();
                Check(!vm.IsReady && !vm.CanInteract, "Construction must stay covered.");
                if (!hosted) vm.Address = "https://10.0.2.2:5048/";
                var back = 0;
                var page = new PersonalSyncPage(vm) { FlowDirection = local.IsRightToLeft ? FlowDirection.RightToLeft : FlowDirection.LeftToRight };
                page.BeginOnboarding(() => { back++; return Task.CompletedTask; }, () => throw new IOException("No received account can finish this fixture."));
                app.ShowSyncReview(page);
                for (var frame = 0; frame < 100 && !vm.IsReady && vm.Error is null; frame++) await Task.Delay(100, ct);
                Check(vm.IsReady && vm.Error is null && !vm.CanFinishOnboarding && vm.ReviewItems.Count == 0, "Original first-run read failed.");
                Check(!hosted || vm.Address == "https://api.zanance.vafadar.pro/" && vm.Fingerprint.Length == 0, "Actual native hosted default or mandatory trust changed.");
                await Task.Delay(300, ct);
                var native = (Android.Views.View)page.Handler!.PlatformView!;
                var nodes = NativeViews(native).OfType<Android.Widget.TextView>().ToArray();
                foreach (var key in new[] { "Sync_Address", "Sync_Fingerprint", "Sync_Intro" })
                {
                    var expected = NativeDigits.Apply(translator[key]);
                    var caption = nodes.FirstOrDefault(node => node.Text == expected);
                    Check(caption?.Layout is not null && caption.Width > 0 && caption.Height > 0, "Original native changed caption is missing.");
                    for (var line = 0; line < caption!.Layout!.LineCount; line++)
                        Check(caption.Layout.GetEllipsisCount(line) == 0 && caption.Layout.GetLineRight(line) - caption.Layout.GetLineLeft(line)
                            <= caption.Width - caption.TotalPaddingLeft - caption.TotalPaddingRight + 2, "Original connection caption is clipped.");
                    Check(caption.Layout.Height <= caption.Height - caption.TotalPaddingTop - caption.TotalPaddingBottom + 2, "Original connection caption height is clipped.");
                }
                var surface = page.GetVisualTreeDescendants().OfType<WrappingAction>().Single(item => ReferenceEquals(item.Command, vm.CloseOnboardingCommand));
                var button = ((Grid)surface.Content!).Children.OfType<Button>().Single();
                var action = (Android.Views.View)button.Handler!.PlatformView!;
                using var accessible = action.CreateAccessibilityNodeInfo();
                Check(accessible!.ContentDescription == translator["Sync_FirstRunBack"] && action.Width / action.Resources!.DisplayMetrics!.Density >= 44
                    && action.Height / action.Resources.DisplayMetrics.Density >= 44, "Original Back target or full native name missing.");
                Check(action.PerformClick() && back == 1, "Original native Back did not invoke its original callback.");
                Check((Platform.CurrentActivity!.Window!.Attributes!.Flags & WindowManagerFlags.Secure) != 0, "Secure flag must remain enabled.");
                if (hosted)
                {
                    var text = translator["Help_PersonalSync_Text"] + Environment.NewLine + Environment.NewLine
                        + translator.Format("Help_Example", translator["Help_PersonalSync_Example"]);
                    var shown = HelpButton.ShowAsync("PersonalSync"); await Task.Delay(300, ct);
                    Android.Util.Log.Info("ZananceSyncHelp", run + "|" + JsonSerializer.Serialize(new
                    { Language = language, Theme = theme.ToString(), ExpectedSha256 = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(text))) }));
                    // The observer reads the actual native dialog and invokes only its actual OK target via adb.
                    await shown.WaitAsync(ct);
                }
                results.Add(new { Language = language, Theme = theme.ToString(), Hosted = hosted, DefaultSystemTrust = hosted,
                    OriginalGrowingCaptions = true, NativeBackInvoked = true, SecureFlag = true, HelpShown = hosted });
                vm.Deactivate();
                await File.WriteAllTextAsync(Path.Combine(root, "progress.json"), JsonSerializer.Serialize(results), ct);
            }
            using var afterBytes = new MemoryStream(); await services.GetServices<IBackupSource>().Single(source => source.Name == SqliteDatabaseBackupSource<ZananceDbContext>.EntryName).WriteAsync(afterBytes, ct); afterBytes.Position = 0;
            await using var after = await SqliteMemorySnapshot.OpenAsync(afterBytes, 32 * 1024 * 1024, ct);
            await SqliteSnapshotVerifier.VerifyAsync(before.Connection, after.Connection, ct);
            Check(!services.GetRequiredService<MauiPersonalSyncEnvironment>().HasConnectionInterest, "Review must never opt a profile into network work.");
            var json = JsonSerializer.Serialize(new { Passed = true, Fictitious = true, MainPackageTouched = false, Cases = results,
                CompletePortableFinancialSnapshotUnchanged = true, NativeHelpDialogs = 6, NoServiceOptIn = true });
            await File.WriteAllTextAsync(Path.Combine(root, "result.json"), json, ct); Emit(run, json);
        }
        catch (Exception error)
        {
            Emit(run, JsonSerializer.Serialize(new { Passed = false, Checkpoint = checkpoint, ErrorType = error.GetType().FullName, Stack = error.StackTrace }));
        }
    }
}
#endif
