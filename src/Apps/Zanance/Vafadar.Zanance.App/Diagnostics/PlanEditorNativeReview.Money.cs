#if DEBUG || ZANANCE_INDEPENDENT_HOME_REVIEW
using Vafadar.Zanance.App.Features.Plans;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Money;

namespace Vafadar.Zanance.App.Diagnostics;

internal sealed partial class PlanEditorNativeReview
{
    /// <summary>Recovers only a failed immutable report after both original no-Save debt observations completed; no UI action is repeated.</summary>
    internal static async Task<object> VerifyDebtMoneyReceiptAsync(IServiceProvider host, string folder, CancellationToken ct)
    {
        using var original = new PlanEditorNativeReview(host, Path.Combine(folder, "money-debt-fixture"));
        using var expected = System.Text.Json.JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(original._folder, "debt-money-prepared.json"), ct));
        using var actual = System.Text.Json.JsonDocument.Parse(await original.RowsAsync(ct));
        foreach (var name in new[] { "Accounts", "Settings", "Schedules", "States", "Entries" })
            if (expected.RootElement.GetProperty(name).GetRawText() != actual.RootElement.GetProperty(name).GetRawText())
                throw new IOException("Original completed debt rows changed: " + name);
        var observations = new List<System.Text.Json.JsonElement>();
        foreach (var stage in new[] { "native-debt-money-original-format", "native-debt-money-reentry-required" })
        {
            using var receipt = System.Text.Json.JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(folder, stage + ".json"), ct));
            var row = receipt.RootElement;
            if (row.GetProperty("Stage").GetString() != stage || !row.GetProperty("NativeReadings").EnumerateArray()
                .Any(value => value.TryGetProperty("Text", out var text) && text.GetString() == "1,234"))
                throw new IOException("Complete original debt monetary observation required.");
            observations.Add(row.Clone());
        }
        if (observations.Select(row => row.GetProperty("ProcessId").GetInt32()).Distinct().Count() != 1)
            throw new IOException("One original debt observation process required.");
        return new { OriginalDebtRowsUnchanged = true, NoAcceptedCommandReplayed = true, NativePlanSaves = 0,
            NativeLedgerPosts = 0, ReportOnlyContinuation = true, Observations = observations };
    }
    /// <summary>Continues the exact seeded monetary fixture; accepted Save is read-only evidence and is never repeated.</summary>
    internal static async Task<PlanEditorNativeReview> ContinueMoneyAsync(IServiceProvider host, string folder, CancellationToken ct)
    {
        var result = new PlanEditorNativeReview(host, folder);
        var receipt = Path.Combine(folder, File.Exists(Path.Combine(folder, "money-save-verified.json")) ? "money-save-verified.json" : "money-prepared.json");
        using var expected = System.Text.Json.JsonDocument.Parse(await File.ReadAllTextAsync(receipt, ct));
        using var actual = System.Text.Json.JsonDocument.Parse(await result.RowsAsync(ct));
        foreach (var name in new[] { "Accounts", "Settings", "Schedules", "States", "Entries" })
            if (expected.RootElement.GetProperty(name).GetRawText() != actual.RootElement.GetProperty(name).GetRawText())
                throw new IOException("Original monetary continuation differs: " + name);
        result.Store.Changed += result.Accepted; result.Plans.Changed += result.Accepted; return result;
    }
    /// <summary>Seeds only new independent monetary-context accounts without replaying original account UI acceptance.</summary>
    internal async Task PrepareMoneyAsync(CancellationToken ct)
    {
        await using var db = await Factory.CreateDbContextAsync(ct);
        if (db.Accounts.Any()) throw new IOException("Fresh monetary fixture required.");
        db.Accounts.AddRange(new Account { Name = "Original EUR", CurrencyCode = "EUR", OpeningDate = new(2020, 1, 1) },
            new Account { Name = "Original USD", CurrencyCode = "USD", OpeningDate = new(2020, 1, 1) });
        await db.SaveChangesAsync(ct); await WriteRowsAsync("money-prepared", ct);
    }

    /// <summary>Invokes only two refused native submissions while validation changes raw money; accepted source rows are never replayed.</summary>
    internal async Task SubmissionMoneyAsync(Func<Page, System.Windows.Input.ICommand, Task> invoke,
        Func<Page, string, Task> observe, CancellationToken ct)
    {
        var units = DisplayUnits.All; var culture = Localization.FormattingCultureName; var before = await RowsAsync(ct);
        var vm = Editor(); var page = new PlanEditorPage(vm); string? route = null;
        try
        {
            DisplayUnits.Set([]); Localization.SetFormattingCulture("en-US"); route = await ShowAsync(page);
            await WaitAsync(() => vm.CanInteract && page.Width > 0, ct); await Task.Delay(200, ct);
            vm.AmountText = "12.34"; await invoke(page, vm.SaveCommand); await WaitAsync(() => vm.NameError is not null, ct);
            await observe(page, "native-money-invalid-name"); vm.Name = "D430 validation boundary"; var changed = false;
            vm.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(vm.NameError) && vm.NameError is null && !changed)
                { changed = true; vm.AmountText = "99.00"; }
            };
            await invoke(page, vm.SaveCommand); await Task.Delay(200, ct);
            if (!changed || vm.AmountText != "99.00" || vm.FormAccepted || vm.IsBusy || before != await RowsAsync(ct))
                throw new IOException("Validation publication borrowed newer raw money or changed accepted source rows.");
            await observe(page, "native-money-new-explicit-save-required");
            await WriteRowsAsync("money-submission-refused", ct);
        }
        finally
        {
            await Shell.Current.GoToAsync("//home", false); if (route is not null) { Routing.UnRegisterRoute(route); _routes.Remove(route); }
            DisplayUnits.Set(units); Localization.SetFormattingCulture(culture);
        }
    }

    /// <summary>Checks original debt repayment money on a separate no-Save fixture, without changing an accepted transfer's references.</summary>
    internal async Task DebtMoneyAsync(Func<Page, string, Task> observe, CancellationToken ct)
    {
        var units = DisplayUnits.All; var culture = Localization.FormattingCultureName;
        var debt = new Account { Name = "D430 original debt", Type = AccountType.Loan, CurrencyCode = "USD", OpeningDate = new(2020, 1, 1), OpeningBalanceKnown = false };
        var cash = (await Store.GetAccountsAsync(cancellationToken: ct)).Single(a => a.CurrencyCode == "USD");
        var plan = DebtSetup.Reminder(debt, cash.Id, DateOnly.FromDateTime(Time.GetLocalNow().DateTime), Calendars.ToPeriod(Localization.CurrentCalendar));
        // This monetary-only fixture exercises an existing debt draft, without introducing a notification permission scenario.
        plan.ReminderEnabled = false;
        await using (var db = await Factory.CreateDbContextAsync(ct)) { db.Accounts.Add(debt); db.Schedules.Add(plan); await db.SaveChangesAsync(ct); }
        await WriteRowsAsync("debt-money-prepared", ct); var before = await RowsAsync(ct);
        var vm = Editor(); var page = new PlanEditorPage(vm); string? route = null;
        try
        {
            DisplayUnits.Set([]); Localization.SetFormattingCulture("en-US"); route = await ShowAsync(page, new() { ["id"] = plan.Id });
            await WaitAsync(() => vm.CanInteract && page.Width > 0, ct); await Task.Delay(200, ct);
            if (!vm.IsDebtTransfer || vm.Account?.CurrencyCode != "USD" || vm.ToAccount?.Id != debt.Id) throw new IOException("Original debt payment currency/direction differs.");
            vm.AmountModeIndex = 0; vm.AmountText = "1,234"; Localization.SetFormattingCulture("de-DE"); await ((IThemeAware)vm).RefreshThemeAsync();
            await observe(page, "native-debt-money-original-format");
            DisplayUnits.Set([new("USD", "Hundred dollars", 2)]); await ((IThemeAware)vm).RefreshThemeAsync();
            if (vm.CanSave || vm.AmountError is null || vm.AmountText != "1,234" || vm.AmountUnitName != "USD") throw new IOException("Original debt amount was reinterpreted.");
            await observe(page, "native-debt-money-reentry-required");
            if (before != await RowsAsync(ct)) throw new IOException("Debt monetary review wrote stored rows.");
        }
        finally
        {
            await Shell.Current.GoToAsync("//home", false); if (route is not null) { Routing.UnRegisterRoute(route); _routes.Remove(route); }
            DisplayUnits.Set(units); Localization.SetFormattingCulture(culture);
        }
    }

    /// <summary>Observes only changed original monetary surfaces; the first context may save one plan, never ledger money.</summary>
    internal async Task MoneyContextAsync(string context, Func<Page, System.Windows.Input.ICommand, Task> invoke,
        Func<Page, string, Task> observe, bool save, CancellationToken ct)
    {
        var previousUnits = DisplayUnits.All; var previousCulture = Localization.FormattingCultureName;
        var vm = Editor(); _current = vm; var page = new PlanEditorPage(vm); string? route = null;
        try
        {
            DisplayUnits.Set([]); route = await ShowAsync(page); await WaitAsync(() => vm.CanInteract, ct);
            // Bind and realize the original controls before introducing the fictitious unsaved draft.
            await WaitAsync(() => page.FindByName<Picker>("ToAccountInput").Handler is not null && page.Width > 0, ct);
            await Task.Delay(200, ct);
            vm.Name = "D430 original monetary context"; vm.KindIndex = 2;
            vm.Account = vm.Accounts.Single(a => a.CurrencyCode == "EUR"); vm.ToAccount = vm.Accounts.Single(a => a.CurrencyCode == "USD");
            await Task.Delay(200, ct);
            if (vm.Account?.CurrencyCode != "EUR" || vm.ToAccount?.CurrencyCode != "USD")
                throw new IOException("Original native monetary accounts did not finish publication before input.");
            Localization.SetFormattingCulture("en-US"); vm.AmountText = "1,234";
            Localization.SetFormattingCulture("de-DE"); vm.ToAmountText = "1.234";
            Localization.SetFormattingCulture("fr-FR"); await ((IThemeAware)vm).RefreshThemeAsync();
            if (vm.AmountFormatNote is null || vm.DestinationAmountFormatNote is null || !vm.CanSave)
                throw new IOException("Original independent monetary formats were lost.");
            await observe(page, context + "-money-format");
            await File.WriteAllTextAsync(Path.Combine(_folder, context + "-before-unit.json"), System.Text.Json.JsonSerializer.Serialize(new
            { vm.KindIndex, vm.AmountModeIndex, vm.AmountText, vm.ToAmountText, vm.AmountUnitName, vm.DestinationAmountUnitName,
                vm.ShowAmount, vm.ShowToAmount, vm.CanSave, PageOwned = ReferenceEquals(Shell.Current.CurrentPage, page) }), ct);
            DisplayUnits.Set([new("EUR", "Hundred euros", 2)]); await ((IThemeAware)vm).RefreshThemeAsync();
            vm.AmountText += "0";
            if (vm.CanSave || vm.AmountError is null || vm.AmountUnitName != "EUR" || vm.DestinationAmountUnitName != "USD")
                throw new IOException($"Original monetary unit changed without complete reentry: save={vm.CanSave}, error={vm.AmountError is not null}, source={vm.AmountUnitName}, destination={vm.DestinationAmountUnitName}, raw={vm.AmountText}, shown={vm.ShowAmount}/{vm.ShowToAmount}.");
            await observe(page, context + "-money-reentry-required");
            vm.AmountText = string.Empty; vm.AmountText = "12,34";
            if (!vm.CanSave || vm.AmountUnitName != "Hundred euros" || vm.AmountError is not null)
                throw new IOException("Explicit monetary reentry remained unavailable.");
            await observe(page, context + "-money-reentered");
            if (save)
            {
                _phase = "money-save"; await WriteRowsAsync("money-save-intention", ct);
                await invoke(page, vm.SaveCommand); await WaitAsync(() => vm.FormAccepted && !vm.IsBusy, ct);
                var stored = (await Plans.GetSchedulesAsync(ct)).Single();
                if (stored.Amount != 123400 || stored.ToAmount != 123400 || vm.CanSave || (await Store.GetEntriesAsync(cancellationToken: ct)).Count != 0)
                    throw new IOException("Original monetary writer or acceptance differed.");
                await WriteRowsAsync("money-save-verified", ct);
            }
        }
        catch (Exception error)
        {
            await File.WriteAllTextAsync(Path.Combine(_folder, context + "-money-failure.txt"), error.ToString(), CancellationToken.None); throw;
        }
        finally
        {
            // This fixture does not supply the ordinary root reader. Do not await an unrelated Home publication to release its own page.
            await Shell.Current.GoToAsync("//home", false);
            if (route is not null) { Routing.UnRegisterRoute(route); _routes.Remove(route); }
            DisplayUnits.Set(previousUnits); Localization.SetFormattingCulture(previousCulture);
        }
    }
}
#endif
