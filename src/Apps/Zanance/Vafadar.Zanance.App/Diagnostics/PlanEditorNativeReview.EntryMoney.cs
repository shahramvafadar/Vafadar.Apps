#if DEBUG || ZANANCE_INDEPENDENT_HOME_REVIEW
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Localization;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Reminders;
using Vafadar.Zanance.App.Features.Entries;
using Vafadar.Zanance.App.Features.Plans;
using Vafadar.Zanance.App.Interaction;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Money;
using Vafadar.Zanance.Core.Receipts;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Diagnostics;

internal sealed partial class PlanEditorNativeReview
{
    private sealed record EntryMoneyWorld(Guid Euro, Guid Dollar, Guid Split, Guid Template);
    private EntryMoneyWorld? _entryMoneyWorld;
    private sealed class EntryMoneyInteraction(Translator translator, Func<Page?> discardFrame, bool navigate = false) : IAppInteraction
    {
        /// <inheritdoc />
        public Task NavigateAsync(string route, IDictionary<string, object>? parameters = null) => route == ".." ? navigate ? Shell.Current.GoToAsync("..") : Task.CompletedTask : throw new IOException("Unexpected monetary navigation.");
        /// <inheritdoc />
        public Task ShowFailureAsync(Exception exception) => throw new IOException("Original monetary source failed.", exception);
        /// <inheritdoc />
        public Task<bool> ConfirmAsync(string title, string message, string accept, string cancel) => discardFrame() is { } frame
            && ReferenceEquals(Shell.Current.CurrentPage, frame) && title == translator["Common_DiscardTitle"] && accept == translator["Common_Discard"]
            ? Task.FromResult(true) : throw new IOException("Unexpected monetary confirmation.");
        /// <inheritdoc />
        public Task AlertAsync(string title, string message, string cancel) => throw new IOException("Unexpected monetary alert.");
        /// <inheritdoc />
        public Task<string?> PromptAsync(string title, string message, string accept, string cancel, string? initialValue = null, int maxLength = -1) => throw new IOException("Unexpected monetary prompt.");
        /// <inheritdoc />
        public Task<string?> ChooseAsync(string title, string cancel, params string[] actions) => throw new IOException("Unexpected monetary choice.");
    }
    /// <summary>Seeds only a fresh independently owned original monetary fixture, before any native financial action.</summary>
    internal async Task PrepareEntryMoneyAsync(CancellationToken ct)
    {
        await using var db = await Factory.CreateDbContextAsync(ct);
        if (await db.Accounts.AnyAsync(ct)) throw new IOException("Fresh original transaction money fixture required.");
        var euro = new Account { Name = "Original EUR", CurrencyCode = "EUR", OpeningDate = new(2020, 1, 1) };
        var dollar = new Account { Name = "Original USD", CurrencyCode = "USD", OpeningDate = new(2020, 1, 1) };
        var split = new LedgerEntry { AccountId = euro.Id, Date = DateOnly.FromDateTime(Time.GetLocalNow().DateTime), Kind = EntryKind.Expense,
            Amount = 10000, Title = "Original monetary split" };
        var template = new EntryTemplate { Name = "Original monetary template", Kind = EntryKind.Expense, AccountId = euro.Id, Amount = 10000, Title = "Original template amount" };
        db.Accounts.AddRange(euro, dollar); db.Entries.Add(split); db.Templates.Add(template);
        var settings = await db.Settings.SingleAsync(ct); settings.DefaultAccountId = euro.Id; settings.DefaultCurrencyCode = "EUR";
        await db.SaveChangesAsync(ct); _entryMoneyWorld = new(euro.Id, dollar.Id, split.Id, template.Id);
        await File.WriteAllTextAsync(Path.Combine(_folder, "entry-money-world.json"), JsonSerializer.Serialize(_entryMoneyWorld), ct);
        await WriteEntryMoneyRowsAsync("entry-money-prepared", ct);
    }
    /// <summary>Continues only the last exact original checkpoint, never another native Save.</summary>
    internal static async Task<PlanEditorNativeReview> ContinueEntryMoneyAsync(IServiceProvider host, string folder, CancellationToken ct)
    {
        var result = new PlanEditorNativeReview(host, folder);
        result._entryMoneyWorld = JsonSerializer.Deserialize<EntryMoneyWorld>(await File.ReadAllTextAsync(Path.Combine(folder, "entry-money-world.json"), ct))
            ?? throw new IOException("Original monetary world required.");
        var checkpoint = new[] { "entry-money-actions-complete", "entry-money-split-source", "entry-money-expense-source", "entry-money-transfer-source", "entry-money-prepared" }
            .First(name => File.Exists(Path.Combine(folder, name + ".json")));
        if (await File.ReadAllTextAsync(Path.Combine(folder, checkpoint + ".json"), ct) != await result.AllMoneyRowsAsync(ct)) throw new IOException("Original monetary source rows differ.");
        return result;
    }
    private Task WriteEntryMoneyRowsAsync(string name, CancellationToken ct) => WriteEntryMoneyRowsCoreAsync(name, ct);
    private async Task WriteEntryMoneyRowsCoreAsync(string name, CancellationToken ct) =>
        await File.WriteAllTextAsync(Path.Combine(_folder, name + ".json"), await AllMoneyRowsAsync(ct), ct);
    private EntryEditorViewModel EntryMoneyEditor(IAppInteraction interaction) => new(Store, Translator, Localization, Time,
        new UndoService(Store, Time), new AssetEntryConfirmation(Translator, interaction), interaction, new ReceiptWorkStore(Factory, new ReviewAccess()));
    /// <summary>Gets only the changed monetary labels and raw values for actual native inspection.</summary>
    internal static HashSet<string> EntryMoneyCaptions(Page page)
    {
        IEnumerable<string?> values = page.BindingContext switch
        {
            EntryEditorViewModel vm => [vm.AmountUnitName, vm.DestinationAmountUnitName, vm.FeeUnitName, vm.DestinationFeeUnitName,
                vm.ForeignUnitName, vm.ReimbursableUnitName, vm.AmountFormatNote, vm.DestinationAmountFormatNote, vm.FeeFormatNote,
                vm.DestinationFeeFormatNote, vm.ForeignFormatNote, vm.ReimbursableFormatNote, vm.AmountError, vm.ToAmountError, vm.FeeError,
                vm.DestinationFeeError, vm.ForeignAmountError, vm.ReimbursableError, vm.SaveError],
            SplitEditorViewModel vm => new[] { vm.Error, vm.UnitNote }.Concat(vm.Parts.SelectMany(row => new[] { row.UnitName, row.FormatNote, row.Error })),
            PlanEditorViewModel vm => [vm.AmountUnitName, vm.DestinationAmountUnitName, vm.AmountFormatNote, vm.DestinationAmountFormatNote, vm.AmountError, vm.ToAmountError],
            _ => [],
        };
        return values.Where(value => !string.IsNullOrEmpty(value)).Select(value => value!).ToHashSet(StringComparer.Ordinal);
    }
    /// <summary>Gets current monetary raw text only, excluding unrelated date/search/text editors.</summary>
    internal static HashSet<string> EntryMoneyRaw(Page page)
    {
        IEnumerable<string?> values = page.BindingContext switch
        {
            EntryEditorViewModel vm => [vm.AmountText, vm.ToAmountText, vm.FeeText, vm.DestinationFeeText, vm.ForeignAmountText, vm.ReimbursableText],
            SplitEditorViewModel vm => vm.Parts.Select(row => row.AmountText),
            PlanEditorViewModel vm => [vm.AmountText, vm.ToAmountText],
            _ => [],
        };
        return values.Where(value => !string.IsNullOrEmpty(value)).Select(value => value!).ToHashSet(StringComparer.Ordinal);
    }
    /// <summary>Qualifies the original transfer, expense detail and split inputs; accepted commands are invoked only once.</summary>
    internal async Task EntryMoneyContextAsync(string context, Func<Page, System.Windows.Input.ICommand, Task> invoke,
        Func<Page, string, Task> observe, CancellationToken ct, Func<string, bool>? completed = null)
    {
        var world = _entryMoneyWorld ?? throw new IOException("Original monetary fixture required.");
        var units = DisplayUnits.All; var culture = Localization.FormattingCultureName;
        var before = await AllMoneyRowsAsync(ct); string? route = null; var pendingDisplay = false;
        var interaction = new EntryMoneyInteraction(Translator, () => null);
        try
        {
            if (completed?.Invoke(context + "-transfer-reentry") != true)
            {
                DisplayUnits.Set([]); await SetTabMoneyCultureAsync("en-US", ct);
                var vm = EntryMoneyEditor(interaction); var page = new EntryEditorPage(vm);
                route = await ShowAsync(page, new() { ["kind"] = EntryKind.Transfer, ["from"] = world.Euro, ["to"] = world.Dollar });
                await WaitAsync(() => vm.IsFormEditable && page.Width > 0, ct); await Task.Delay(200, ct);
                vm.Account = vm.Accounts.Single(row => row.Id == world.Euro); vm.ToAccount = vm.ToAccounts.Single(row => row.Id == world.Dollar);
                vm.AmountText = "1,234"; vm.FeeText = "1.23";
                Localization.SetFormattingCulture("de-DE"); pendingDisplay = true; vm.ToAmountText = "1.234"; vm.DestinationFeeText = "1,23";
                Localization.SetFormattingCulture("fr-FR"); await ((IThemeAware)vm).RefreshThemeAsync();
                if (!vm.CanSave || vm.AmountFormatNote is null || vm.DestinationAmountFormatNote is null || vm.FeeFormatNote is null)
                    throw new IOException("Independent original transaction formats were not retained.");
                await observe(page, context + "-transfer-original");
                DisplayUnits.Set([new("EUR", "Hundred EUR", 2), new("USD", "Hundred USD", 2)]);
                await ((IThemeAware)vm).RefreshThemeAsync(); vm.AmountText += "0"; vm.ToAmountText += "0"; vm.FeeText += "0"; vm.DestinationFeeText += "0";
                if (vm.CanSave || vm.AmountError is null || vm.FeeError is null || vm.ToAmountError is null || vm.DestinationFeeError is null
                    || vm.AmountUnitName != "EUR" || vm.DestinationFeeUnitName != "USD") throw new IOException("Partial transfer edits renewed monetary meaning.");
                await observe(page, context + "-transfer-reentry");
                if (before != await AllMoneyRowsAsync(ct)) throw new IOException("Read-only transfer observations changed source rows.");
                if (!File.Exists(Path.Combine(_folder, "entry-money-transfer-source.json")))
                {
                    if (File.Exists(Path.Combine(_folder, "entry-money-transfer-accepted.json")))
                        throw new IOException("Preserve the original accepted transfer receipt; never invoke another Save for an incomplete observer.");
                    vm.AmountText = vm.ToAmountText = vm.FeeText = vm.DestinationFeeText = string.Empty;
                    vm.AmountText = vm.ToAmountText = "12,34"; vm.FeeText = vm.DestinationFeeText = "0,0123";
                    _phase = "entry-money-transfer"; var id = vm.FormDraftId; await invoke(page, vm.SaveCommand);
                    await WaitAsync(() => vm.FormAccepted && !vm.IsBusy, ct);
                    var saved = (await Store.GetEntriesAsync(cancellationToken: ct)).Single(row => row.Id == id);
                    if (saved.Amount != 123400 || saved.ToAmount != 123400 || vm.CanSave) throw new IOException("Native transfer changed original monetary amounts.");
                    var fees = (await Store.GetEntriesAsync(cancellationToken: ct)).Where(row => row.GroupId == saved.GroupId && row.Id != id).ToArray();
                    if (fees.Length != 2 || fees.Any(row => row.Amount != 123)) throw new IOException("Independent native fees were not retained.");
                    await WriteEntryMoneyRowsAsync("entry-money-transfer-source", ct); before = await AllMoneyRowsAsync(ct);
                }
                await ReturnMoneyTabAsync(pendingDisplay, ct); pendingDisplay = false; Routing.UnRegisterRoute(route); _routes.Remove(route); route = null;
            }
            if (completed?.Invoke(context + "-expense-reentry") != true)
            {
                DisplayUnits.Set([]); await SetTabMoneyCultureAsync("en-US", ct);
                var receipt = ReceiptParser.Parse("MONETARY SHOP\nTOTAL 12.34 EUR", DateOnly.FromDateTime(Time.GetLocalNow().DateTime));
                if (receipt.Candidates.Count != 1) throw new IOException("One explicit fictitious receipt total required.");
                var vm = EntryMoneyEditor(interaction); var page = new EntryEditorPage(vm);
                route = await ShowAsync(page, new() { ["kind"] = EntryKind.Expense, ["from"] = world.Euro, ["receipt"] = receipt });
                await WaitAsync(() => vm.IsFormEditable && page.Width > 0, ct); await Task.Delay(200, ct);
                if (!File.Exists(Path.Combine(_folder, "entry-money-adoption-complete.json")))
                {
                    await invoke(page, vm.ApplyTemplateCommand);
                    await WaitAsync(() => !vm.IsBusy, ct); await observe(page, context + "-template-original-amount");
                    DisplayUnits.Set([new("EUR", "Hundred EUR", 2)]); await ((IThemeAware)vm).RefreshThemeAsync();
                    await invoke(page, vm.UseReceiptAmountCommand); await observe(page, context + "-receipt-renewed-unit");
                    if (!MoneyText.TryParse(vm.AmountText, "EUR", Localization.CurrentCulture, out var parsed) || parsed != 1234 || vm.AmountUnitName != "Hundred EUR")
                        throw new IOException("Explicit receipt adoption did not renew its authoritative unit.");
                    await WriteEntryMoneyRowsAsync("entry-money-adoption-complete", ct);
                }
                DisplayUnits.Set([]); vm.AmountText = string.Empty; vm.AmountText = "100"; vm.ShowDetails = true;
                vm.ForeignEnabled = true; vm.ForeignCurrency = "USD"; vm.ForeignAmountText = "1,234";
                vm.ReimbursableEnabled = true; vm.ReimbursableText = "25";
                Localization.SetFormattingCulture("de-DE"); pendingDisplay = true; await ((IThemeAware)vm).RefreshThemeAsync();
                await observe(page, context + "-expense-original");
                DisplayUnits.Set([new("EUR", "Hundred EUR", 2), new("USD", "Hundred USD", 2)]); vm.ForeignCurrency = "JPY";
                await ((IThemeAware)vm).RefreshThemeAsync(); vm.AmountText += "0"; vm.ReimbursableText += "0"; vm.ForeignAmountText += "0";
                if (vm.CanSave || vm.AmountError is null || vm.ReimbursableError is null || vm.ForeignAmountError is null || vm.ForeignUnitName != "USD")
                    throw new IOException("Expense detail input silently adopted a different unit/currency.");
                await observe(page, context + "-expense-reentry");
                if (before != await AllMoneyRowsAsync(ct)) throw new IOException("Read-only expense work changed original rows.");
                if (!File.Exists(Path.Combine(_folder, "entry-money-expense-source.json")))
                {
                    if (File.Exists(Path.Combine(_folder, "entry-money-expense-accepted.json")))
                        throw new IOException("Preserve the original accepted expense receipt; never invoke another Save for an incomplete observer.");
                    vm.AmountText = vm.ReimbursableText = vm.ForeignAmountText = string.Empty;
                    vm.AmountText = "1"; vm.ReimbursableText = "0,25"; vm.ForeignAmountText = "1234";
                    _phase = "entry-money-expense"; var id = vm.FormDraftId; await invoke(page, vm.SaveCommand);
                    await WaitAsync(() => vm.FormAccepted && !vm.IsBusy, ct); var saved = (await Store.GetEntriesAsync(cancellationToken: ct)).Single(row => row.Id == id);
                    if (saved.Amount != 10000 || saved.ReimbursableAmount != 2500 || saved.OriginalAmount != 1234 || saved.OriginalCurrencyCode != "JPY")
                        throw new IOException("Native expense details changed monetary meaning.");
                    await WriteEntryMoneyRowsAsync("entry-money-expense-source", ct); before = await AllMoneyRowsAsync(ct);
                }
                await ReturnMoneyTabAsync(pendingDisplay, ct); pendingDisplay = false; Routing.UnRegisterRoute(route); _routes.Remove(route); route = null;
            }
            if (completed?.Invoke(context + "-split-reentry") != true)
            {
                DisplayUnits.Set([]); await SetTabMoneyCultureAsync("en-US", ct);
                var vm = new SplitEditorViewModel(Store, Translator, Localization, interaction); var page = new SplitEditorPage(vm);
                route = await ShowAsync(page, new() { ["id"] = world.Split }); await WaitAsync(() => vm.CanEdit && vm.Parts.Count >= 2 && page.Width > 0, ct);
                await Task.Delay(200, ct); vm.Parts[0].AmountText = "60"; vm.Parts[1].AmountText = "40";
                Localization.SetFormattingCulture("de-DE"); pendingDisplay = true; await ((IThemeAware)vm).RefreshThemeAsync();
                await observe(page, context + "-split-original");
                DisplayUnits.Set([new("EUR", "Hundred EUR", 2)]); await ((IThemeAware)vm).RefreshThemeAsync();
                vm.Parts[0].AmountText = "0,6"; vm.Parts[1].AmountText = "0,4";
                if (vm.CanSave || vm.Parts.Any(row => row.Error is null || row.UnitName != "EUR")) throw new IOException("Rebalanced split digits adopted a new unit without clearing.");
                await observe(page, context + "-split-reentry");
                if (before != await AllMoneyRowsAsync(ct)) throw new IOException("Read-only split observations changed source rows.");
                if (!File.Exists(Path.Combine(_folder, "entry-money-split-source.json")))
                {
                    if (File.Exists(Path.Combine(_folder, "entry-money-split-accepted.json")))
                        throw new IOException("Preserve the original accepted split receipt; never invoke another Save for an incomplete observer.");
                    vm.Parts[0].AmountText = vm.Parts[1].AmountText = string.Empty; vm.Parts[0].AmountText = "0,6"; vm.Parts[1].AmountText = "0,4";
                    _phase = "entry-money-split"; await invoke(page, vm.SaveCommand); await WaitAsync(() => !vm.IsBusy && !vm.IsDirty, ct);
                    var parts = (await Store.GetEntriesAsync(cancellationToken: ct)).Where(row => row.Title == "Original monetary split").ToArray();
                    if (parts.Length != 2 || parts.Sum(row => row.Amount) != 10000 || vm.CanSave) throw new IOException("Native split changed its original financial total.");
                    await WriteEntryMoneyRowsAsync("entry-money-split-source", ct); await WriteEntryMoneyRowsAsync("entry-money-actions-complete", ct);
                }
            }
            if (route is not null) { await ReturnMoneyTabAsync(pendingDisplay, ct); pendingDisplay = false; Routing.UnRegisterRoute(route); _routes.Remove(route); route = null; }
            if (!File.Exists(Path.Combine(_folder, "entry-money-repeat-complete.json")))
            {
                DisplayUnits.Set([]); await SetTabMoneyCultureAsync("en-US", ct);
                var vm = EntryMoneyEditor(interaction); var page = new EntryEditorPage(vm);
                route = await ShowAsync(page, new() { ["kind"] = EntryKind.Transfer, ["from"] = world.Euro, ["to"] = world.Dollar });
                await WaitAsync(() => vm.IsFormEditable && page.Width > 0, ct); await Task.Delay(200, ct);
                vm.Account = vm.Accounts.Single(row => row.Id == world.Euro); vm.ToAccount = vm.ToAccounts.Single(row => row.Id == world.Dollar);
                vm.AmountText = "1,234"; vm.FeeText = "1.23";
                Localization.SetFormattingCulture("de-DE"); pendingDisplay = true; vm.ToAmountText = "1.234";
                DisplayUnits.Set([new("EUR", "Hundred EUR", 2)]); await ((IThemeAware)vm).RefreshThemeAsync();
                vm.AmountText = string.Empty; vm.AmountText = "12,34";
                if (vm.CanSave || !vm.CanRepeat) throw new IOException("Unused fee review incorrectly blocked original plan handoff.");
                var unchanged = await AllMoneyRowsAsync(ct); PlanEditorPage? childPage = null; PlanEditorViewModel? child = null;
                Routing.UnRegisterRoute(AppShell.PlanEditorRoute);
                Routing.RegisterRoute(AppShell.PlanEditorRoute, new PageFactory(() =>
                {
                    var effects = new EntryMoneyInteraction(Translator, () => childPage, true);
                    child = new(Store, Plans, Translator, Localization, _host.GetRequiredService<IDateFormatter>(), Time, _host.GetRequiredService<ReminderService>(), effects);
                    childPage = new(child); return childPage;
                }));
                try
                {
                    await invoke(page, vm.RepeatCommand);
                    await WaitAsync(() => childPage is not null && ReferenceEquals(Shell.Current.CurrentPage, childPage) && child?.CanInteract == true, ct);
                    if (!MoneyText.TryParse(child!.AmountText, "EUR", Localization.CurrentCulture, out var primary) || primary != 123400
                        || !MoneyText.TryParse(child.ToAmountText, "USD", Localization.CurrentCulture, out var destination) || destination != 123400)
                        throw new IOException("Original plan handoff changed monetary meaning.");
                    await observe(childPage!, context + "-repeat-handoff"); await invoke(childPage!, child.CancelCommand);
                    await WaitAsync(() => ReferenceEquals(Shell.Current.CurrentPage, page) && vm.IsFormEditable, ct);
                    if (vm.CanSave || !vm.CanRepeat || vm.FeeUnitName != "EUR" || vm.FeeText != "1.23" || unchanged != await AllMoneyRowsAsync(ct))
                        {
                        await File.WriteAllTextAsync(Path.Combine(_folder, "entry-money-repeat-return-failed.json"), JsonSerializer.Serialize(new { vm.CanSave, vm.CanRepeat, vm.IsFormEditable, vm.FeeUnitName, vm.FeeText, vm.LoadError, vm.SaveError, RowsUnchanged = unchanged == await AllMoneyRowsAsync(ct) }), ct);
                        throw new IOException("Plan return changed its original parent money or stored rows.");
                    }
                    await observe(page, context + "-repeat-return"); await WriteEntryMoneyRowsAsync("entry-money-repeat-complete", ct);
                }
                finally { Routing.UnRegisterRoute(AppShell.PlanEditorRoute); Routing.RegisterRoute(AppShell.PlanEditorRoute, typeof(PlanEditorPage)); }
            }
        }
        catch (Exception error)
        {
            await File.WriteAllTextAsync(Path.Combine(_folder, "entry-money-context-failed-" + Environment.ProcessId + ".json"),
                JsonSerializer.Serialize(new { Context = context, Error = error.ToString(), ProcessId = Environment.ProcessId }), CancellationToken.None);
            throw;
        }
        finally
        {
            await ReturnMoneyTabAsync(pendingDisplay, ct);
            if (route is not null) { Routing.UnRegisterRoute(route); _routes.Remove(route); }
            DisplayUnits.Set(units); await SetTabMoneyCultureAsync(culture, ct);
        }
    }
}
#endif
