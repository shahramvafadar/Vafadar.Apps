#if DEBUG && WINDOWS
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Backup;
using Vafadar.Backup.Storage;
using Vafadar.Data;
using Vafadar.Localization;
using Vafadar.Zanance.App.Features.Backup;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.App.Security;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Diagnostics;

internal static partial class DebugSnapshots
{
    /// <summary>Qualifies automatic copies against the retained independent protected installation and original page.</summary>
    private static async Task ReviewAutomaticBackupAsync(App app, IServiceProvider services, string folder, bool existingCopiesOnly = false)
    {
        var location = services.GetRequiredService<LocalDatabaseLocation<ZananceDbContext>>();
        if (AppInfo.Current.PackageName != "pro.vafadar.zanance.storageacceptance" || !location.IsProtected
            || !File.Exists(Path.Combine(Path.GetDirectoryName(location.Path)!, "protected-review-fixture.json")))
            throw new IOException("Automatic review requires its independent marked protected installation.");
        var before = await ProtectedRowsFingerprintAsync(services);
        var service = services.GetRequiredService<IAutomaticLocalBackupService>();
        var backup = services.GetRequiredService<IBackupService>();
        var coordinator = services.GetRequiredService<AutomaticBackupCoordinator>();
        var host = services.GetRequiredService<ProtectedStorageHost>();
        var options = services.GetRequiredService<BackupOptions>();
        var safety = new LocalFolderBackupStorage(Path.Combine(FileSystem.AppDataDirectory, "backups-safety"));
        var interval = coordinator.IntervalDays; var keep = options.MaxBackupsToKeep;
        var manualAt = backup.LastBackupAt;
        var set = Profiles.ProfileService.CurrentBackupSet();
        var originalSafety = (await safety.ListAsync()).Where(file => !file.FileName.StartsWith("automatic-", StringComparison.Ordinal))
            .Select(file => file.Id).Order().ToArray();
        var captured = services.GetRequiredService<Profiles.ProfileService>().CaptureStorage(); var path = location.Path;
        void Validate() { captured.Validate(); if (location.Path != path) throw new IOException("Independent generation changed."); }
        try
        {
            options.MaxBackupsToKeep = 2;
            // Three real copies exercise key-aware native retention; only this fictitious installation is owned here.
            for (var index = 0; index < (existingCopiesOnly ? 1 : 3); index++)
                await Task.Run(() => service.CreateWhenDueAsync(safety, TimeSpan.FromTicks(1), Validate));
            var files = (await safety.ListAsync()).Where(file => AutomaticBackupFileName.Matches(file.FileName, set)).ToArray();
            if (files.Length != 2 || backup.LastBackupAt != manualAt || service.LastAutomaticBackupAt is null)
                throw new IOException("Native automatic retention or independent timestamps differ.");
            if (!originalSafety.SequenceEqual((await safety.ListAsync()).Where(file => !file.FileName.StartsWith("automatic-", StringComparison.Ordinal))
                .Select(file => file.Id).Order())) throw new IOException("Automatic retention changed pre-restore safety copies.");
            foreach (var file in files)
            {
                await using var stream = await safety.OpenReadAsync(file.Id);
                using var buffer = new MemoryStream(); await stream.CopyToAsync(buffer); var envelope = buffer.ToArray();
                byte[]? clear = null;
                try
                {
                    clear = await host.OpenAsync(envelope, CancellationToken.None);
                    await services.GetRequiredService<IBackupPackageReader>().ReadValidatedPortableAsync(clear, null,
                        Vafadar.Backup.Security.DeviceSafetyEncryption.MaximumPackageBytes, (_, _, _) => Task.CompletedTask);
                }
                finally { CryptographicOperations.ZeroMemory(envelope); if (clear is not null) CryptographicOperations.ZeroMemory(clear); }
            }
            var local = services.GetRequiredService<ILocalizationService>();
            var themes = services.GetRequiredService<ThemeService>(); var observations = new List<object>(); var contexts = 0;
            foreach (var theme in new[] { ThemeChoice.Light, ThemeChoice.Dark })
            foreach (var language in new[] { "en", "fa", "de" })
            foreach (var width in new[] { 360, 412, 1280 })
            {
                themes.Set(theme); await SetLanguageAsync(app, local, language);
                app.Windows[0].Width = width; app.Windows[0].Height = 820;
                var vm = services.GetRequiredService<BackupViewModel>(); vm.IndependentAutomaticReview = true;
                var page = new BackupPage(vm) { FlowDirection = local.CurrentLanguage.IsRightToLeft ? FlowDirection.RightToLeft : FlowDirection.LeftToRight };
                var shell = app.Windows[0].Page as AppShell ?? throw new IOException("Independent Shell unavailable.");
                await shell.Navigation.PushAsync(page, false);
                try
                {
                    await vm.LoadAsync(); await Task.Delay(200);
                    if (vm.AutomaticBackups.Count != 2 || !vm.CanChooseAutomatic || vm.AutomaticIntervalIndex < 0)
                        throw new IOException("Original automatic page did not publish its current profile.");
                    var picker = VisualDescendants(page).OfType<Picker>().Single(p => ReferenceEquals(p.ItemsSource, vm.AutomaticIntervals));
                    await ScrollToViewIfNeededAsync(VisualDescendants(page).OfType<ScrollView>().First(), picker);
                    DebugPickerText.Check(picker, folder, $"{language}-{theme}-{width}-automatic");
                    if (contexts == 0)
                    {
                        var publishedCaptions = vm.AutomaticIntervals;
                        foreach (var index in new[] { 0, 2, interval == 0 ? 0 : interval == 7 ? 2 : 1 })
                        {
                            if (picker.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.ComboBox combo
                                || Microsoft.UI.Xaml.Automation.Peers.FrameworkElementAutomationPeer.CreatePeerForElement(combo)
                                    is not Microsoft.UI.Xaml.Automation.Peers.ComboBoxAutomationPeer peer
                                || peer.GetPattern(Microsoft.UI.Xaml.Automation.Peers.PatternInterface.ExpandCollapse)
                                    is not Microsoft.UI.Xaml.Automation.Provider.IExpandCollapseProvider expand)
                                throw new IOException("Automatic picker has no native expansion pattern.");
                            expand.Expand(); await Task.Delay(100);
                            try
                            {
                                var row = peer.GetChildren()?.OfType<Microsoft.UI.Xaml.Automation.Peers.ComboBoxItemDataAutomationPeer>()
                                    .SingleOrDefault(child => Equals(child.Item, combo.Items[index]));
                                if (row?.GetPattern(Microsoft.UI.Xaml.Automation.Peers.PatternInterface.SelectionItem)
                                    is not Microsoft.UI.Xaml.Automation.Provider.ISelectionItemProvider select)
                                    throw new IOException("Automatic interval lacks a native selection pattern.");
                                select.Select();
                            }
                            finally { expand.Collapse(); }
                            await Task.Delay(150);
                            if (picker.SelectedIndex != index || coordinator.IntervalDays != (index == 0 ? 0 : index == 1 ? 1 : 7)
                                || !ReferenceEquals(publishedCaptions, vm.AutomaticIntervals))
                                throw new IOException("Native interval/status publication rebuilt its choices or changed the selected interval.");
                        }
                    }
                    foreach (var label in VisualDescendants(picker.Parent).OfType<Label>()) CheckCaption(label, observations);
                    await CaptureWindowAsync(app.Windows[0], Path.Combine(folder, $"{language}-{theme}-{width}-automatic.png"));
                    var button = VisualDescendants(page).OfType<Button>().First(value => value.CommandParameter is LocalBackup { DeviceOnly: true });
                    await ScrollToViewIfNeededAsync(VisualDescendants(page).OfType<ScrollView>().First(), button);
                    InvokeChoiceReview(button);
                    if (vm.OpenLocalCommand.ExecutionTask is not { } opened) throw new IOException("Native automatic row was not invoked.");
                    await opened;
                    if (!vm.Restore.HasPreview || vm.Restore.HasCompleted || vm.Restore.Error is not null)
                        throw new IOException("Automatic copy did not enter the original explicit preview flow.");
                    vm.Restore.CancelCommand.Execute(null);
                    if (vm.Restore.HasPreview || vm.Restore.HasFeedback) throw new IOException("Original cancellation retained its automatic input.");
                    contexts++;
                }
                finally { vm.WatchAutomatic(false); await shell.Navigation.PopAsync(false); }
            }
            if (before != await ProtectedRowsFingerprintAsync(services)) throw new IOException("Automatic snapshot/preview/cancel changed finance.");
            await File.WriteAllTextAsync(Path.Combine(folder, "automatic-backup-proof.json"), JsonSerializer.Serialize(new
            { Contexts = contexts, NativeEncryptedCopies = existingCopiesOnly ? 1 : 3, RetainedAutomaticCopies = files.Length, RetainedCopiesAuthenticated = true,
                SafetyCohortUnchanged = true, PortableTimestampUnchanged = true, NativeRowPreviewAndCancel = contexts,
                NativeIntervalSelections = 3, StatusRetainsChoicePublication = true,
                FinancialRowsUnchanged = true, FinancialFingerprint = before, NativeCaptionObservations = observations.Count }));
        }
        finally { options.MaxBackupsToKeep = keep; coordinator.IntervalDays = interval; }
    }
}
#endif
