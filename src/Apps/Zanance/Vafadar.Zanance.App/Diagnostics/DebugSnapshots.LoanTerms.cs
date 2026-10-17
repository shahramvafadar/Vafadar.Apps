#if DEBUG && WINDOWS
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Data;
using Vafadar.Localization;
using Vafadar.Zanance.App.Features.Accounts;
using Vafadar.Zanance.App.Interaction;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Diagnostics;

internal static partial class DebugSnapshots
{
    /// <summary>Checks original zero-balance loan/lent captions and genuine missing terms without recording payments.</summary>
    private static async Task ReviewLoanTermsAsync(App app, IServiceProvider services, string folder)
    {
        if (AppInfo.Current.PackageName != "pro.vafadar.zanance.loanterms" || !Path.GetFullPath(folder).StartsWith(
            Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "artifacts", "loan-terms-d319")) + Path.DirectorySeparatorChar,
            StringComparison.OrdinalIgnoreCase)) throw new IOException("Independent loan terms qualification required.");
        if (File.Exists(Path.Combine(folder, "complete.json"))) throw new IOException("Completed loan terms checks must not be repeated.");
        var path = Path.Combine(folder, "loan-terms.db"); var continuing = File.Exists(path);
        using var fixture = new ServiceCollection().AddZananceData(path).BuildServiceProvider();
        fixture.MigrateLocalDatabase<ZananceDbContext>();
        var store = fixture.GetRequiredService<ZananceStore>(); var reader = fixture.GetRequiredService<LoanWorkStore>();
        if (!continuing)
        {
            foreach (var (name, type, opening, installment) in new (string, AccountType, long, long?)[] {
                ("D319 zero loan", AccountType.Loan, 0, 10000), ("D319 zero lent", AccountType.Lent, 0, 10000),
                ("D319 active configured", AccountType.Loan, -100000, 10000), ("D319 active missing", AccountType.Loan, -100000, null) })
                await store.SaveAccountAsync(new Account { Name = name, Type = type, CurrencyCode = "EUR", OpeningBalance = opening,
                    OpeningDate = DateOnly.FromDateTime(services.GetRequiredService<TimeProvider>().GetLocalNow().DateTime), InterestRate = 12, Installment = installment });
        }
        var accounts = await store.GetAccountsAsync();
        if (accounts.Count != 4 || (await store.GetEntriesAsync()).Count != 0 || accounts.Any(account => !account.Name.StartsWith("D319 ", StringComparison.Ordinal)))
            throw new IOException("Preserve the original isolated loan fixture.");
        async Task<string> PacketAsync() => JsonSerializer.Serialize(new { Accounts = await store.GetAccountsAsync(),
            Entries = await store.GetEntriesAsync(), Settings = await store.GetSettingsAsync() });
        var before = await PacketAsync(); var local = services.GetRequiredService<ILocalizationService>();
        var translator = services.GetRequiredService<Translator>(); var themes = services.GetRequiredService<ThemeService>();
        var effects = services.GetRequiredService<IAppInteraction>(); var window = app.Windows[0];
        var progress = Path.Combine(folder, "contexts.json");
        var observations = File.Exists(progress) ? JsonSerializer.Deserialize<List<JsonElement>>(await File.ReadAllTextAsync(progress))
            ?? throw new IOException("Invalid retained loan progress.") : new List<JsonElement>();
        foreach (var language in new[] { "en", "fa", "de" })
        foreach (var theme in new[] { ThemeChoice.Light, ThemeChoice.Dark })
        foreach (var width in new[] { 360, 412, 1280 })
        {
            themes.Set(theme); await SetLanguageAsync(app, local, language); window.Width = width; window.Height = 820;
            foreach (var account in accounts.Where(account => account.OpeningBalance == 0 || language == "en" && theme == ThemeChoice.Light && width == 360))
            {
                if (observations.Any(row => row.GetProperty("Name").GetString() == account.Name && row.GetProperty("Language").GetString() == language
                    && row.GetProperty("Theme").GetString() == theme.ToString() && row.GetProperty("Width").GetInt32() == width)) continue;
                var session = new LoanInstallmentSession(reader, store, translator, local, effects);
                var vm = ActivatorUtilities.CreateInstance<AccountDetailViewModel>(services, store, fixture.GetRequiredService<GoalStore>(),
                    fixture.GetRequiredService<HoldingStore>(), reader, session, effects);
                vm.ApplyQueryAttributes(new Dictionary<string, object> { ["id"] = account.Id });
                var page = new AccountDetailPage(vm); var root = window.Page ?? throw new IOException("Original review root missing.");
                await root.Navigation.PushModalAsync(new NavigationPage(page), false);
                for (var attempt = 0; attempt < 200 && !vm.Loading.IsReady && !vm.Loading.HasFailed; attempt++) await Task.Delay(50);
                await Task.Delay(150);
                if (!vm.Loading.IsReady) throw new IOException("Original loan publication failed.");
                var missing = account.Name == "D319 active missing";
                var label = VisualDescendants(page).OfType<Label>().Single(value => value.Text == translator["Loan_AddTerms"]);
                if (vm.MissingLoanTerms != missing || label.IsVisible != missing || vm.HasLoanTerms != (account.Name == "D319 active configured"))
                    throw new IOException("Stored terms and outstanding balance were conflated.");
                if (label.Handler?.PlatformView is not Microsoft.UI.Xaml.FrameworkElement native
                    || (native.Visibility == Microsoft.UI.Xaml.Visibility.Visible) != missing)
                    throw new IOException("Native missing-term caption visibility differs.");
                if (missing) { await ScrollToViewIfNeededAsync(VisualDescendants(page).OfType<ScrollView>().Single(value => value.Orientation != ScrollOrientation.Horizontal), label); CheckCaption(label); }
                await CaptureWindowAsync(window, Path.Combine(folder, $"{language}-{theme}-{width}-{account.Name.Replace(' ', '-')}.png"));
                await root.Navigation.PopModalAsync(false);
                if (before != await PacketAsync()) throw new IOException("Loan caption qualification changed financial inputs.");
                observations.Add(JsonSerializer.SerializeToElement(new { Name = account.Name, Language = language, Theme = theme.ToString(), Width = width,
                    MissingTermsVisible = missing, StoredTermsRetained = true, NoPayment = true }));
                await File.WriteAllTextAsync(progress, JsonSerializer.Serialize(observations));
            }
        }
        await File.WriteAllTextAsync(Path.Combine(folder, "complete.json"), JsonSerializer.Serialize(new { Cases = observations.Count, OriginalDataPreserved = true }));
    }
}
#endif
