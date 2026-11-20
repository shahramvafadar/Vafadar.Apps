
#if AI_NETWORK_REVIEW
using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Zanance.App.Features.Analysis;
using Vafadar.Zanance.App.Features.Sync;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Analysis;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Analysis;
using Vafadar.Zanance.Data.Sync;

namespace Vafadar.Zanance.App;

/// <summary>Exercises the original native analysis transport and private journal against an isolated pinned service.</summary>
internal static class AndroidAnalysisNetworkReview
{
    private const string Tag = "ZananceAnalysisNetwork";

    /// <summary>Runs the one admitted fictional cohort and reports only its current checkpoint.</summary>
    internal static async Task RunAsync(App app, IServiceProvider services, string run, int port, string? fingerprint)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(3));
        var ct = timeout.Token;
        var checkpoint = "identity";
        try
        {
            var directory = new Java.IO.File(FileSystem.AppDataDirectory).CanonicalPath;
            Require(AppInfo.Current.PackageName == AndroidSyncReview.Package &&
                directory.Contains("/" + AndroidSyncReview.Package + "/", StringComparison.Ordinal));
            Require(port is >= 1024 and <= 65535 && fingerprint is { Length: 64 } && fingerprint.All(char.IsAsciiHexDigit));
            var root = Path.Combine(directory, "analysis-network-review", run);
            Directory.CreateDirectory(root);
            using (File.Open(Path.Combine(root, "started"), FileMode.CreateNew)) { }
            app.ShowSyncReview(new ContentPage { Content = new Label { Text = "Independent fictional analysis network review" } });

            checkpoint = "fictional-source";
            var store = services.GetRequiredService<ZananceStore>();
            Require((await store.GetAccountsAsync(cancellationToken: ct)).Count == 0);
            var account = new Account { Name = "Fictitious native analysis account", CurrencyCode = "EUR", OpeningDate = new(2025, 1, 1) };
            Require(await store.SaveAccountAsync(account, cancellationToken: ct));
            var snapshot = services.GetRequiredService<AiSnapshotStore>();
            var source = await snapshot.AccountsAsync(ct);
            Require(source.Accounts.Single().Id == account.Id);
            var review = await snapshot.ReviewAsync(new(source.File, [account.Id], new(2025, 9, 1), new(2025, 9, 30), true, false, "en"), ct);

            checkpoint = "pinned-sign-in";
            var peer = new LocalSyncPeer("https://10.0.2.2:" + port.ToString(CultureInfo.InvariantCulture) + "/", fingerprint!);
            var lifetime = services.GetRequiredService<PersonalSyncLifetime>();
            var original = lifetime.CaptureAnalysis(true);
            Require(original.Scope == source.File);
            var tokens = services.GetRequiredService<IPersonalSyncTokenStore>();
            using var transport = new PersonalSyncTransport(peer, tokens, TimeProvider.System, original.Validate);
            var identity = await transport.SignInAsync("android-analysis-" + run + "@example.invalid", "Fictitious!" + run + "aA9", ct);
            Require(await transport.BillingIdentityAsync(ct) == identity.Id);
            using var remote = new AiConnectedRemote(transport, services.GetRequiredService<IAiPrivateStorage>(),
                identity.Id, source.File, new(original.Validate, original.Cancellation));
            Require(await remote.ReadAsync(ct) is null);

            checkpoint = "native-openai";
            var first = await SendAsync("openai", "gpt-6-luna");
            checkpoint = "original-route-switch";
            var switched = false;
            for (var attempt = 0; attempt < 30 && !switched; attempt++)
            {
                switched = (await remote.AvailabilityAsync(ct)).Model == "gemini-3.5-flash-lite";
                if (!switched) await Task.Delay(TimeSpan.FromSeconds(1), ct);
            }
            Require(switched);
            checkpoint = "native-gemini";
            var second = await SendAsync("gemini", "gemini-3.5-flash-lite");
            Require(first.RequestId != second.RequestId);
            Require((await remote.ReadAsync(ct))?.Result == second);
            checkpoint = "unchanged-source";
            await snapshot.CheckAsync(review, ct);
            Require((await store.GetEntriesAsync(cancellationToken: ct)).Count == 0);
            Require((await snapshot.AccountsAsync(ct)).Accounts.Single() == source.Accounts.Single());
            await File.WriteAllTextAsync(Path.Combine(root, "complete"), "Two isolated original requests settled", ct);
            Android.Util.Log.Info(Tag, run + "|complete|2|zero-ledger");
            async Task<AiRequestResult> SendAsync(string provider, string model)
            {
                var offer = await remote.OfferAsync(AiPurpose.PeriodExplanation, ct);
                Require(offer.Provider == provider && offer.Model == model);
                var result = await remote.SubmitAsync(offer, review.Payload, () => snapshot.CheckAsync(review, ct), ct);
                Require(result.State == AiRequestState.Completed && result.ChargedUnits == 1 &&
                    result.Text == "The selected period has no recorded activity.");
                return result;
            }
        }
        catch (Exception error)
        {
            Android.Util.Log.Error(Tag, run + "|" + checkpoint + "|" + error.GetType().Name);
        }
    }

    private static void Require(bool value)
    {
        if (!value) throw new InvalidDataException("The current independent analysis invariant failed.");
    }
}
#endif
