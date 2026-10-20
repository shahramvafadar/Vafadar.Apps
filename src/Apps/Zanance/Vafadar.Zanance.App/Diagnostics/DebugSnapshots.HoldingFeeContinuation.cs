#if DEBUG && WINDOWS
using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Data;
using Vafadar.Localization;
using Vafadar.Zanance.App.Features.Holdings;
using Vafadar.Zanance.App.Interaction;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Diagnostics;

internal static partial class DebugSnapshots
{
    /// <summary>Refuses all native financial effects while original accepted files supply only retained captions.</summary>
    private sealed class FeeCaptionOnlyInteraction : IAppInteraction
    {
        public Task AlertAsync(string title, string message, string cancel) => throw new IOException("Read-only fee continuation.");
        public Task<bool> ConfirmAsync(string title, string message, string accept, string cancel) => throw new IOException("Read-only fee continuation.");
        public Task<string?> PromptAsync(string title, string message, string accept, string cancel, string? initialValue = null, int maxLength = -1) => throw new IOException("Read-only fee continuation.");
        public Task<string?> ChooseAsync(string title, string cancel, params string[] actions) => throw new IOException("Read-only fee continuation.");
        public Task NavigateAsync(string route, IDictionary<string, object>? parameters = null) => throw new IOException("Read-only fee continuation.");
        public Task ShowFailureAsync(Exception exception) => throw new IOException("Read-only fee continuation.", exception);
    }

    /// <summary>Continues only twelve uncompleted captions after the exact original six-context Windows failure.</summary>
    private static async Task<HoldingRefundNativeReview.Prepared> ContinueOriginalFeeCaptionsAsync(IServiceProvider services, string folder, string originalRun)
    {
        if (!System.Text.RegularExpressions.Regex.IsMatch(originalRun, "^[0-9a-f]{32}$")
            || !AppInfo.Current.PackageName.StartsWith("pro.vafadar.zanance.storepreview.", StringComparison.Ordinal))
            throw new IOException("Exact independent original fee run required.");
        var original = Path.Combine(Path.GetDirectoryName(folder)!, originalRun);
        if (Directory.EnumerateFileSystemEntries(original, "complete.json").Any()
            || !File.ReadAllText(Path.Combine(original, "primary-error.txt")).Contains("Original refunded-fee feedback is incomplete", StringComparison.Ordinal))
            throw new IOException("Exact original pending caption failure required.");
        foreach (var theme in new[] { "Light", "Dark" })
        foreach (var width in new[] { 360, 412, 1280 })
            if (!File.Exists(Path.Combine(original, "en-" + theme + "-" + width, "malformed.png")))
                throw new IOException("Original six successful captions required.");
        foreach (var stage in new[] { "malformed-refused", "malformed-accepted", "negative-refused", "negative-accepted" })
            if (!File.Exists(Path.Combine(original, "original-" + stage, "live.png")))
                throw new IOException("Original live command captures required.");
        var cases = new List<object>(); AssetEventEditorViewModel? presentation = null;
        foreach (var name in new[] { "malformed", "negative" })
        {
            var financial = Path.Combine(original, "financial");
            using var refused = JsonDocument.Parse(File.ReadAllText(Path.Combine(financial, name + "-refused.json")));
            using var accepted = JsonDocument.Parse(File.ReadAllText(Path.Combine(financial, name + "-accepted.json")));
            var result = accepted.RootElement;
            if (result.GetProperty("Kind").GetString() != name || result.GetProperty("FeeMinor").GetInt64() != 100
                || result.GetProperty("RefundMinor").GetInt64() != 75 || result.GetProperty("RecordedBalanceMinor").GetInt64() != 87475
                || !result.GetProperty("SameOriginalFormCorrectedAndAcceptedOnce").GetBoolean()
                || !result.GetProperty("RefundIdentityAmountLinkCurrencyAndMetadataPreserved").GetBoolean()
                || !refused.RootElement.GetProperty("CompleteOriginalRowsUnchanged").GetBoolean())
                throw new IOException("Exact original accepted/refused fee receipts required.");
            var file = Path.Combine(financial, name + ".db");
            if (!File.Exists(file) || (File.GetAttributes(file) & FileAttributes.ReparsePoint) != 0)
                throw new IOException("Owned original financial file required.");
            await using var fixture = new ServiceCollection().AddSingleton(services.GetRequiredService<TimeProvider>()).AddZananceData(file).BuildServiceProvider();
            var store = fixture.GetRequiredService<ZananceStore>(); var holdings = fixture.GetRequiredService<HoldingStore>();
            var before = await ImportFinancialNativeReview.StoredAsync(fixture, CancellationToken.None);
            var rows = await store.GetEntriesAsync(); var events = await holdings.GetEventsAsync(); var accounts = await store.GetAccountsAsync();
            var fee = rows.Single(e => e.Kind == EntryKind.Expense); var refund = rows.Single(e => e.Kind == EntryKind.Refund);
            var cash = accounts.Single(a => a.CurrencyCode == "EUR");
            if (rows.Count != 3 || events.Count != 1 || fee.Amount != 100 || refund.Amount != 75 || refund.RefundOfId != fee.Id
                || LedgerCalculator.Balance(cash, rows, events.Single().Date) != 87475 || events.Single().Quantity != 1000)
                throw new IOException("Original accepted financial rows differ.");
            if (name == "malformed")
            {
                using var draft = JsonDocument.Parse(refused.RootElement.GetProperty("OriginalRawDraft").GetString()!);
                var raw = draft.RootElement;
                presentation = new(holdings, store, services.GetRequiredService<HoldingText>(), services.GetRequiredService<Translator>(),
                    services.GetRequiredService<ILocalizationService>(), services.GetRequiredService<TimeProvider>(), new FeeCaptionOnlyInteraction());
                await presentation.ApplyQueryAsync(new Dictionary<string, object> { ["id"] = events.Single().Id });
                presentation.QuantityText = raw.GetProperty("QuantityText").GetString()!;
                presentation.AmountText = raw.GetProperty("AmountText").GetString()!;
                presentation.FeeText = raw.GetProperty("FeeText").GetString()!;
                presentation.Note = raw.GetProperty("Note").GetString()!;
                presentation.SecondAmountText = raw.GetProperty("SecondAmountText").GetString()!;
                presentation.Date = DateOnly.Parse(raw.GetProperty("Date").GetString()!, CultureInfo.InvariantCulture);
                presentation.Account = presentation.Accounts.Single(a => a.Id == raw.GetProperty("Account").GetGuid());
                presentation.Location = presentation.Locations.Single(a => a.Id == raw.GetProperty("Location").GetGuid());
                presentation.PriceModeIndex = raw.GetProperty("PriceModeIndex").GetInt32(); presentation.RetireReview();
                HoldingRefundNativeReview.SetFeedback(presentation, "Amount_Invalid", services.GetRequiredService<Translator>()["Amount_Invalid"]);
                if (presentation.CanSave || presentation.FeeText != "not money") throw new IOException("Original caption draft changed.");
            }
            if (before != await ImportFinancialNativeReview.StoredAsync(fixture, CancellationToken.None))
                throw new IOException("Original financial data changed during read-only continuation.");
            cases.Add(result.Clone());
        }
        await File.WriteAllTextAsync(Path.Combine(folder, "original-fee-continuation.json"), JsonSerializer.Serialize(new
        { OriginalRun = originalRun, OriginalFourLiveCapturesRetained = true, OriginalSixEnglishCapturesRetained = true, FinancialCommandsInvoked = 0,
            OriginalFinancialFilesUnchanged = true, RemainingCaptions = 12 }));
        return new([new("malformed", presentation!, "Amount_Invalid")], cases, []);
    }
}
#endif
