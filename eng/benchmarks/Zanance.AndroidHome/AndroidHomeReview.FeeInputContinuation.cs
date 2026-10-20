using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Data;
using Vafadar.Localization;
using Vafadar.Zanance.App.Diagnostics;
using Vafadar.Zanance.App.Features.Holdings;
using Vafadar.Zanance.App.Interaction;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App;

internal static partial class AndroidHomeReview
{
    /// <summary>Prevents any native financial effect during original accepted-file caption continuation.</summary>
    private sealed class FeeCaptionOnlyInteraction : IAppInteraction
    {
        public Task AlertAsync(string title, string message, string cancel) => throw new ReviewCheckException("Read-only original fee caption.");
        public Task<bool> ConfirmAsync(string title, string message, string accept, string cancel) => throw new ReviewCheckException("Read-only original fee caption.");
        public Task<string?> PromptAsync(string title, string message, string accept, string cancel, string? initialValue = null, int maxLength = -1) => throw new ReviewCheckException("Read-only original fee caption.");
        public Task<string?> ChooseAsync(string title, string cancel, params string[] actions) => throw new ReviewCheckException("Read-only original fee caption.");
        public Task NavigateAsync(string route, IDictionary<string, object>? parameters = null) => throw new ReviewCheckException("Read-only original fee caption.");
        public Task ShowFailureAsync(Exception exception) => throw new ReviewCheckException("Read-only original fee caption.");
    }

    /// <summary>Requires the exact original failed observation and accepted files; never seeds or invokes financial commands.</summary>
    private static async Task<HoldingRefundNativeReview.Prepared> ContinueFeeInputCaptionsAsync(IServiceProvider services, string root, string comparison, CancellationToken ct)
    {
        var baseline = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity?.Intent?.GetStringExtra("review_baseline");
        if (baseline is null || baseline == comparison || !System.Text.RegularExpressions.Regex.IsMatch(baseline, "^[0-9a-f]{32}$")
            || RedirectedAncestors(root).Length != 0) throw new ReviewCheckException("Exact original fee comparison required.");
        var matchingFailure = false;
        foreach (var path in Directory.EnumerateFiles(root, "failure-*.json"))
        {
            using var prior = JsonDocument.Parse(await File.ReadAllTextAsync(path, ct)); var p = prior.RootElement;
            if (p.GetProperty("Failed").GetBoolean() && p.GetProperty("Comparison").GetString() == baseline
                && p.TryGetProperty("ReviewCheck", out var check) && check.GetString() == "Original modal feedback did not finish native publication.") matchingFailure = true;
        }
        if (!matchingFailure) throw new ReviewCheckException("Original terminal caption failure required.");
        var cases = new List<object>(); AssetEventEditorViewModel? model = null;
        foreach (var name in new[] { "malformed", "negative" })
        {
            var financial = Path.Combine(root, "financial");
            using var accepted = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(financial, name + "-accepted.json"), ct));
            using var refused = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(financial, name + "-refused.json"), ct));
            var result = accepted.RootElement;
            if (result.GetProperty("Kind").GetString() != name || result.GetProperty("RecordedBalanceMinor").GetInt64() != 87475
                || result.GetProperty("FeeMinor").GetInt64() != 100 || result.GetProperty("RefundMinor").GetInt64() != 75
                || !result.GetProperty("SameOriginalFormCorrectedAndAcceptedOnce").GetBoolean()
                || !result.GetProperty("RefundIdentityAmountLinkCurrencyAndMetadataPreserved").GetBoolean()
                || !refused.RootElement.GetProperty("CompleteOriginalRowsUnchanged").GetBoolean())
                throw new ReviewCheckException("Original accepted fee receipts differ.");
            var file = Path.Combine(financial, name + ".db");
            if (!File.Exists(file) || RedirectedAncestors(file).Length != 0) throw new ReviewCheckException("Original owned financial file required.");
            await using var fixture = new ServiceCollection().AddSingleton(services.GetRequiredService<TimeProvider>()).AddZananceData(file).BuildServiceProvider();
            var before = await ImportFinancialNativeReview.StoredAsync(fixture, ct);
            var store = fixture.GetRequiredService<ZananceStore>(); var holdings = fixture.GetRequiredService<HoldingStore>();
            var rows = await store.GetEntriesAsync(cancellationToken: ct); var events = await holdings.GetEventsAsync(cancellationToken: ct);
            var cash = (await store.GetAccountsAsync(cancellationToken: ct)).Single(a => a.CurrencyCode == "EUR");
            var fee = rows.Single(e => e.Kind == EntryKind.Expense); var refund = rows.Single(e => e.Kind == EntryKind.Refund);
            if (rows.Count != 3 || events.Count != 1 || fee.Amount != 100 || refund.Amount != 75 || refund.RefundOfId != fee.Id
                || LedgerCalculator.Balance(cash, rows, events.Single().Date) != 87475 || events.Single().Quantity != 1000)
                throw new ReviewCheckException("Original financial state differs.");
            if (name == "malformed")
            {
                model = new(holdings, store, services.GetRequiredService<HoldingText>(), services.GetRequiredService<Translator>(),
                    services.GetRequiredService<ILocalizationService>(), services.GetRequiredService<TimeProvider>(), new FeeCaptionOnlyInteraction());
                await model.ApplyQueryAsync(new Dictionary<string, object> { ["id"] = events.Single().Id });
                using var draft = JsonDocument.Parse(refused.RootElement.GetProperty("OriginalRawDraft").GetString()!); var raw = draft.RootElement;
                model.FeeText = raw.GetProperty("FeeText").GetString()!; model.Note = raw.GetProperty("Note").GetString()!;
                model.RetireReview(); HoldingRefundNativeReview.SetFeedback(model, "Amount_Invalid", services.GetRequiredService<Translator>()["Amount_Invalid"]);
                if (model.CanSave || model.FeeText != "not money") throw new ReviewCheckException("Original raw caption draft differs.");
            }
            if (before != await ImportFinancialNativeReview.StoredAsync(fixture, ct)) throw new ReviewCheckException("Original financial rows changed.");
            cases.Add(result.Clone());
        }
        return new([new("malformed", model!, "Amount_Invalid")], cases, []);
    }
}
