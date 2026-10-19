#if DEBUG && WINDOWS
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Localization;
using Vafadar.Zanance.App.Features.Accounts;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Diagnostics;

internal static partial class DebugSnapshots
{
    /// <summary>Checks the original account choices, decorative default icon and debt-options action without saving a draft.</summary>
    private static async Task ReviewEditorTextWindowsAsync(App app, IServiceProvider services, string folder)
    {
        if (Environment.GetEnvironmentVariable("VAFADAR_EDITOR_RETAINED_AGGREGATE_ONLY") == "1")
        {
            await ReviewRetainedAggregateWindowsAsync(app, services, folder);
            return;
        }
        if (Environment.GetEnvironmentVariable("VAFADAR_EDITOR_ONBOARDING_FOOTER_ONLY") == "1")
        {
            await ReviewOnboardingFooterWindowsAsync(app, services, folder);
            return;
        }
        if (Environment.GetEnvironmentVariable("VAFADAR_EDITOR_NARROW_ACTIONS_ONLY") == "1")
        {
            await ReviewNarrowEditorActionsWindowsAsync(app, services, folder);
            return;
        }
        if (Environment.GetEnvironmentVariable("VAFADAR_EDITOR_GOAL_CHOICES_ONLY") == "1")
        {
            await ReviewGoalChoicesWindowsAsync(app, services, folder);
            return;
        }
        var store = services.GetRequiredService<ZananceStore>(); var plans = services.GetRequiredService<PlanStore>();
        async Task<string> StoredAsync() => JsonSerializer.Serialize(new { Accounts = await store.GetAccountsAsync(),
            Entries = await store.GetEntriesAsync(), Settings = await store.GetSettingsAsync(), Plans = await plans.GetSchedulesAsync() });
        var detailsOnly = Environment.GetEnvironmentVariable("VAFADAR_EDITOR_DEBT_DETAILS_ONLY") == "1";
        var before = await StoredAsync(); var local = services.GetRequiredService<ILocalizationService>();
        var translator = services.GetRequiredService<Translator>(); var captures = new List<object>();
        foreach (var language in new[] { "en", "fa", "de" })
        foreach (var theme in new[] { ThemeChoice.Light, ThemeChoice.Dark })
        foreach (var width in new[] { 360d, 412d, 1280d })
        {
            await Shell.Current.GoToAsync("//home"); app.Windows[0].Width = width;
            await SetStoreThemeAsync(app, services.GetRequiredService<ThemeService>(), theme);
            await SetLanguageAsync(app, local, language);
            var vm = ActivatorUtilities.CreateInstance<AccountEditorViewModel>(services);
            await vm.ApplyQueryAsync(new Dictionary<string, object>());
            vm.Form.Name = "D356 fictitious unsaved draft"; vm.Form.OpeningText = "1234.56";
            vm.Form.ShowIconPicker = true;
            var page = new AccountEditorPage(vm) { FlowDirection = local.CurrentLanguage.IsRightToLeft ? FlowDirection.RightToLeft : FlowDirection.LeftToRight };
            await Shell.Current.Navigation.PushModalAsync(page, false); await Task.Delay(200);
            var outputName = language + "-" + theme + "-" + width;
            var output = Path.Combine(folder, outputName); Directory.CreateDirectory(output);
            var scroll = VisualDescendants(page).OfType<ScrollView>().First();
            var checkedCaptions = 0;
            if (!detailsOnly)
            {
                var choices = VisualDescendants(page).OfType<Vafadar.Maui.Controls.ChoiceChips>()
                    .Single(control => ReferenceEquals(control.ItemsSource, vm.Form.TypeNames));
                await ScrollToViewIfNeededAsync(scroll, choices); await Task.Delay(150);
                var captions = VisualDescendants(choices).OfType<Label>().ToArray();
                if (captions.Length != vm.Form.TypeNames.Count) throw new IOException("Original account choices incomplete.");
                foreach (var caption in captions) CheckCaption(caption, ProfileNativeReads);
                checkedCaptions = captions.Length;
                await CaptureWindowAsync(app.Windows[0], Path.Combine(output, "types.png"));
                var icons = VisualDescendants(page).OfType<IconPicker>().Single();
                await ScrollToViewIfNeededAsync(scroll, icons); await Task.Delay(150);
                var glyph = VisualDescendants(icons).OfType<Label>().Single(value => value.Text == "–");
                if (glyph.FontAutoScalingEnabled || glyph.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.TextBlock nativeGlyph
                    || nativeGlyph.IsTextScaleFactorEnabled || nativeGlyph.ActualHeight < 1 || nativeGlyph.IsTextTrimmed)
                    throw new IOException("Decorative default icon lost its fixed glyph geometry.");
                ProfileNativeReads.Add(nativeGlyph);
                for (var offset = 0; offset <= nativeGlyph.ContentEnd.Offset - nativeGlyph.ContentStart.Offset; offset++)
                {
                    var pointer = nativeGlyph.ContentStart.GetPositionAtOffset(offset, Microsoft.UI.Xaml.Documents.LogicalDirection.Forward);
                    if (pointer is null) continue;
                    ProfileNativeReads.Add(pointer);
                    var rect = pointer.GetCharacterRect(Microsoft.UI.Xaml.Documents.LogicalDirection.Forward);
                    if (rect.Top < -1 || rect.Bottom > glyph.Height + 1 || rect.Left < -1 || rect.Right > glyph.Width + 1)
                        throw new IOException("Decorative default glyph exceeds its actual tile allocation.");
                }
                var iconButton = VisualDescendants(icons).OfType<Button>().First();
                var expectedIconName = translator.Format("Common_SelectedItem", translator["Icon_Default"]);
                if (SemanticProperties.GetDescription(iconButton) != expectedIconName)
                    throw new IOException("Default icon lost its original translated selected name.");
                InvokeChoiceReview(iconButton, ProfileNativeReads); await Task.Delay(100);
                if (vm.Form.IconKey is not null) throw new IOException("Default icon action changed the original default choice.");
                await CaptureWindowAsync(app.Windows[0], Path.Combine(output, "default-icon.png"));
            }
            vm.Form.ShowIconPicker = false; vm.Form.TypeIndex = (int)AccountType.Loan; await Task.Delay(150);
            var command = detailsOnly ? vm.Form.ToggleAccountDetailsCommand : vm.Form.ToggleDebtTermsCommand;
            var expectedAction = translator[detailsOnly ? "Debt_AccountDetails" : "Debt_EstimateOptions"];
            bool IsOpen() => detailsOnly ? vm.Form.ShowAccountDetails : vm.Form.ShowDebtTerms;
            var action = VisualDescendants(page).OfType<WrappingAction>().Single(value => ReferenceEquals(value.Command, command));
            await ScrollToViewIfNeededAsync(scroll, action); await Task.Delay(150);
            var captionLabel = VisualDescendants(action).OfType<Label>().Single(); CheckCaption(captionLabel, ProfileNativeReads);
            var button = VisualDescendants(action).OfType<Button>().Single();
            if (action.Height < 44 || SemanticProperties.GetDescription(button) != expectedAction)
                throw new IOException("Growing debt action lost its original complete name or target.");
            var draft = vm.Form.Snapshot();
            InvokeChoiceReview(button, ProfileNativeReads); await Task.Delay(150);
            if (!IsOpen()) throw new IOException("Original debt-options command did not open its existing fields.");
            InvokeChoiceReview(button, ProfileNativeReads); await Task.Delay(150);
            if (IsOpen() || draft != vm.Form.Snapshot() || before != await StoredAsync())
                throw new IOException("Debt-options action changed draft values or stored financial inputs.");
            await CaptureWindowAsync(app.Windows[0], Path.Combine(output, "debt-options.png"));
            await File.WriteAllTextAsync(Path.Combine(output, "observed.json"), JsonSerializer.Serialize(new
            { Surface = detailsOnly ? "account-details" : "types-icon-estimate", CompleteChoiceCaptions = checkedCaptions, OriginalDefaultIconName = !detailsOnly, OriginalDefaultIconAction = !detailsOnly,
                DecorativeGlyphUnscaled = !detailsOnly, CompleteDebtCaption = true, OriginalDebtToggleOpenedAndClosed = true,
                DraftPreserved = true, StoredInputsUnchanged = true, NoEditorSave = true }));
            captures.Add(new { Language = outputName, File = "debt-options.png" });
            await Shell.Current.Navigation.PopModalAsync(false);
        }
        await File.WriteAllTextAsync(Path.Combine(folder, "complete.json"), JsonSerializer.Serialize(new
        { Package = AppInfo.Current.PackageName, ProcessId = Environment.ProcessId, Captures = captures,
            FinancialRowsUnchangedAfterSeeding = before == await StoredAsync(), WindowsLayoutStressOnly = true }));
    }
}
#endif
