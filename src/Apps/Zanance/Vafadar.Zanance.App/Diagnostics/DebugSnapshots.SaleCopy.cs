#if DEBUG && WINDOWS
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Data;
using Vafadar.Localization;
using Vafadar.Maui.Controls;
using Vafadar.Zanance.App.Features.Holdings;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Diagnostics;

internal static partial class DebugSnapshots
{
    /// <summary>Checks only the changed English account wording in original native confirmation dialogs; always cancels.</summary>
    private static async Task ReviewLoanCopyAsync(App app, IServiceProvider services, string folder)
    {
        if (AppInfo.Current.PackageName != "pro.vafadar.zanance.storageacceptance")
            throw new IOException("Loan copy review requires its independent installation.");
        var before = await ProtectedRowsFingerprintAsync(services);
        var translator = services.GetRequiredService<Translator>();
        await SetLanguageAsync(app, services.GetRequiredService<ILocalizationService>(), "en");
        var themes = services.GetRequiredService<Presentation.ThemeService>();
        var interaction = services.GetRequiredService<Interaction.IAppInteraction>();
        var text = services.GetRequiredService<HoldingText>(); var cases = 0;
        foreach (var theme in new[] { Presentation.ThemeChoice.Light, Presentation.ThemeChoice.Dark })
        foreach (var width in new[] { 360, 412, 1280 })
        foreach (var key in new[] { "Loan_ConfirmPay", "Loan_ConfirmReceive" })
        {
            themes.Set(theme); await Task.Delay(1000);
            app.Windows[0].Width = width; app.Windows[0].Height = 820;
            var shown = interaction.ConfirmAsync(translator["Loan_RecordInstallment"],
                translator.Format(key, text.Money(9000, "EUR"), text.Money(1000, "EUR"), "Synthetic household account"),
                translator["Common_Save"], translator["Common_Cancel"]);
            Microsoft.UI.Xaml.Controls.ContentDialog? dialog = null;
            // Native dialogs publish asynchronously; fixed short delays are not evidence of their readiness.
            for (var attempt = 0; attempt < 100 && dialog is null; attempt++)
            {
                await Task.Delay(50);
                if (shown.IsFaulted) await shown;
                if (app.Windows[0].Handler?.PlatformView is Microsoft.UI.Xaml.Window { Content.XamlRoot: { } root })
                    dialog = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetOpenPopupsForXamlRoot(root)
                        .Select(value => value.Child).OfType<Microsoft.UI.Xaml.Controls.ContentDialog>().SingleOrDefault();
            }
            if (dialog is null) throw new IOException("Original loan confirmation did not publish.");
            await CaptureWindowAsync(app.Windows[0], Path.Combine(folder, $"en-{theme}-{width}-{key}.png"));
            var cancel = FindNativeButton(dialog, translator["Common_Cancel"]) ?? throw new IOException("Native Cancel unavailable.");
            if (new Microsoft.UI.Xaml.Automation.Peers.ButtonAutomationPeer(cancel).GetPattern(Microsoft.UI.Xaml.Automation.Peers.PatternInterface.Invoke)
                is not Microsoft.UI.Xaml.Automation.Provider.IInvokeProvider invoke)
                throw new IOException("Native Cancel has no Invoke pattern.");
            invoke.Invoke();
            if (await shown || await ProtectedRowsFingerprintAsync(services) != before)
                throw new IOException("Loan copy review accepted or changed finance.");
            cases++;
        }
        await File.WriteAllTextAsync(Path.Combine(folder, "loan-copy-proof.json"), JsonSerializer.Serialize(new
        { Cases = cases, OriginalNativeDialogAndCancel = true, NoInstallmentPosted = true,
            CompleteFinancialRowsUnchanged = true, FinancialFingerprint = before }));
    }

    /// <summary>Invokes the original sale-result help using fictitious presentation only and no financial write.</summary>
    private static async Task ReviewSaleCopyAsync(App app, IServiceProvider services, string folder)
    {
        var location = services.GetRequiredService<LocalDatabaseLocation<ZananceDbContext>>();
        if (AppInfo.Current.PackageName != "pro.vafadar.zanance.storageacceptance"
            || !File.Exists(Path.Combine(Path.GetDirectoryName(location.Path)!, "protected-review-fixture.json")))
            throw new IOException("Sale help review requires its independent marked installation.");
        var type = (await services.GetRequiredService<HoldingStore>().GetTypesAsync())
            .Single(value => value.Name == "Synthetic long-caption holding review");
        var before = await ProtectedRowsFingerprintAsync(services);
        var localization = services.GetRequiredService<ILocalizationService>();
        var translator = services.GetRequiredService<Translator>();
        var themes = services.GetRequiredService<Presentation.ThemeService>();
        var observations = new List<object>(); var cases = 0;
        foreach (var theme in new[] { Presentation.ThemeChoice.Light, Presentation.ThemeChoice.Dark })
        foreach (var language in new[] { "en", "fa", "de" })
        foreach (var width in new[] { 360, 412, 1280 })
        {
            themes.Set(theme); await SetLanguageAsync(app, localization, language);
            app.Windows[0].Width = width; app.Windows[0].Height = 820;
            var shell = app.Windows[0].Page as AppShell ?? throw new IOException("Original Shell unavailable.");
            await shell.GoToAsync(AppShell.HoldingDetailRoute, new Dictionary<string, object> { ["id"] = type.Id });
            await Task.Delay(400);
            var page = shell.CurrentPage as HoldingDetailPage ?? throw new IOException("Original holding detail unavailable.");
            var vm = (HoldingDetailViewModel)page.BindingContext;
            var original = vm.RealisedText;
            // Presentation-only result; neither a sale nor a tax/ledger algorithm is fabricated or saved.
            vm.RealisedText = translator.Format("Holding_Realised", services.GetRequiredService<HoldingText>().Money(2000, type.PriceCurrencyCode, true));
            var help = VisualDescendants(page).OfType<HelpButton>().Single(value => value.Topic == "HoldingSaleResult");
            var target = VisualDescendants(help).OfType<Button>().Single();
            await ScrollToViewIfNeededAsync(FindScrollView(page)!, target);
            InvokeChoiceReview(target, observations); await Task.Delay(300);
            if (app.Windows[0].Handler?.PlatformView is not Microsoft.UI.Xaml.Window { Content.XamlRoot: { } root })
                throw new IOException("Help native root unavailable.");
            var dialog = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetOpenPopupsForXamlRoot(root)
                .Select(value => value.Child).OfType<Microsoft.UI.Xaml.Controls.ContentDialog>().Single();
            if (dialog.Title?.ToString() != translator["Help_HoldingSaleResult_Title"])
                throw new IOException("Original help has the wrong title.");
            await CaptureWindowAsync(app.Windows[0], Path.Combine(folder, $"{language}-{theme}-{width}-sale-help.png"));
            var close = FindNativeButton(dialog, translator["Common_Ok"]) ?? throw new IOException("Native help close unavailable.");
            if (new Microsoft.UI.Xaml.Automation.Peers.ButtonAutomationPeer(close).GetPattern(Microsoft.UI.Xaml.Automation.Peers.PatternInterface.Invoke)
                is not Microsoft.UI.Xaml.Automation.Provider.IInvokeProvider invoke)
                throw new IOException("Native close has no Invoke pattern.");
            invoke.Invoke(); await Task.Delay(100);
            vm.RealisedText = original;
            await shell.GoToAsync("..");
            if (await ProtectedRowsFingerprintAsync(services) != before) throw new IOException("Help changed financial data.");
            cases++;
        }
        await File.WriteAllTextAsync(Path.Combine(folder, "sale-copy-proof.json"), JsonSerializer.Serialize(new
        { Cases = cases, OriginalHelpNativeInvokeAndClose = true, NoSaleOrFinancialWrite = true,
            CompleteFinancialRowsUnchanged = true, FinancialFingerprint = before, NativeObservations = observations.Count }));
    }
}
#endif
