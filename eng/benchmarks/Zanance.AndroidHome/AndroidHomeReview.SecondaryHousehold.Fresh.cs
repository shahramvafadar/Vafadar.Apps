#if HOUSEHOLD_MONEY_FRESH
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Zanance.App.Features.Sync;
using Vafadar.Zanance.Core.Sharing;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Sharing;
using Vafadar.Zanance.Data.Sync;

namespace Vafadar.Zanance.App;

internal static partial class AndroidHomeReview
{
    /// <summary>Admits one new independent native owner through the real private TLS service; no existing revocation or financial fixture is changed.</summary>
    private static async Task PrepareFreshHouseholdMoneyAsync(IServiceProvider services, string folder, CancellationToken ct)
    {
        if (Package != "pro.vafadar.zanance.householdmoneyreview") throw new ReviewCheckException("Fresh independent package required.");
        var intent = Platform.CurrentActivity?.Intent ?? throw new ReviewCheckException("Explicit native intent required.");
        var port = intent.GetIntExtra("review_household_port", 0); var pin = intent.GetStringExtra("review_household_pin");
        var run = new DirectoryInfo(folder).Name;
        if (port is < 1024 or > 65535 || pin?.Length != 64 || !Guid.TryParseExact(run, "N", out _)) throw new ReviewCheckException("Own loopback configuration required.");
        services.GetRequiredService<PersonalSyncForegroundWorker>().Dispose();
        var peer = new LocalSyncPeer("https://10.0.2.2:" + port.ToString(System.Globalization.CultureInfo.InvariantCulture) + "/", pin);
        var finances = services.GetRequiredService<ZananceStore>();
        if ((await finances.GetAccountsAsync(cancellationToken: ct)).Count != 0 || (await finances.GetEntriesAsync(cancellationToken: ct)).Count != 0)
            throw new ReviewCheckException("Fresh independent household has no cash accounts or entries.");
        var marker = Path.Combine(folder, "fresh-household-admitted.json");
        var journal = services.GetRequiredService<HouseholdJournal>();
        using var transport = new HouseholdServiceTransport(peer, services.GetRequiredService<IPersonalSyncTokenStore>());
        SecondaryHouseholdEvidence["Phase"] = "fresh-native-real-session";
        // Synthetic input is derived only from this public test run, never owner credentials or private fixture keys.
        var retained = await services.GetRequiredService<PersonalSyncStore>().ReviewAsync(ct);
        var identity = retained.RemoteProfile is null
            ? await transport.SignInAsync("household-money-" + run + "@example.invalid", "Fictitious!" + run + "aA9", ct)
            : await transport.IdentityAsync(ct);
        if (!File.Exists(marker))
        {
            SecondaryHouseholdEvidence["Phase"] = "fresh-native-signed-owner-admission";
            var workspace = services.GetRequiredService<HouseholdWorkspace>();
            var state = await workspace.BeginOwnerAsync(transport, ct);
            if (state.MembershipVersion == 0) state = await workspace.CompleteOwnerAsync(transport, state.Authority, "Fictitious secondary monetary review", ct);
            if (!(await journal.ReadAccessAsync(state.Authority, ct)).CanWrite) throw new ReviewCheckException("Actual service-confirmed native owner required.");
            SecondaryHouseholdEvidence["Phase"] = "fresh-native-original-cost";
            var store = services.GetRequiredService<HouseholdCostStore>();
            foreach (var name in new[] { "Fictitious amount payer", "Fictitious amount recipient" })
            {
                var review = await store.ReadAsync(ct);
                if (!review.Contacts.Any(row => row.Name == name)) await store.SaveContactAsync(review, new HouseholdContact { Name = name }, ct);
            }
            var source = await store.ReadAsync(ct);
            if (source.Costs.Count == 0)
            {
                var payer = source.Contacts.Single(row => row.Name == "Fictitious amount payer");
                var recipient = source.Contacts.Single(row => row.Name == "Fictitious amount recipient");
                var cost = new HouseholdCost { Title = "Fictitious original monetary context", Kind = HouseholdCostKind.Expense,
                    Date = new(2026, 10, 21), Amount = 1001, CurrencyCode = "KWD", PaymentKind = HouseholdPaymentKind.Private, PaidByContactId = payer.Id };
                await store.SaveCostAsync(source, cost, [new() { CostId = cost.Id, ContactId = payer.Id, Amount = 333 },
                    new() { CostId = cost.Id, ContactId = recipient.Id, Amount = 668 }], ct);
            }
            // Real local financial setup is performed once; later presentation continuation checks this same source.
            await using var output = new FileStream(marker, FileMode.CreateNew, FileAccess.Write, FileShare.Read);
            await System.Text.Json.JsonSerializer.SerializeAsync(output, new { Admitted = true, Contacts = 2, CostMinor = 1001, Currency = "KWD", CashEntries = 0 }, cancellationToken: ct);
        }
        var authority = await journal.CaptureAsync(peer, identity.Id, ct) ?? throw new ReviewCheckException("Original signed native authority required.");
        if (!(await journal.ReadAccessAsync(authority.Authority, ct)).CanWrite) throw new ReviewCheckException("Original native permission remains mandatory.");
        var final = await services.GetRequiredService<HouseholdCostStore>().ReadAsync(ct);
        if (!final.CanWrite || final.Costs.Count != 1 || final.Contacts.Count != 2 || final.Settlements.Count != 0 || final.Costs.Single().Amount != 1001 ||
            (await finances.GetEntriesAsync(cancellationToken: ct)).Count != 0) throw new ReviewCheckException("Exact independent original financial graph required.");
        SecondaryHouseholdEvidence["FreshRealAdmission"] = true;
    }
}
#endif
