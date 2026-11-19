using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Vafadar.Zanance.Api.Analysis;
using Vafadar.Zanance.Core.Analysis;

namespace Vafadar.Zanance.Api.Tests;

/// <summary>Opt-in live evaluation of the exact fictional locale corpus, isolated from the ordinary CI lanes.</summary>
[Trait("Qualification", "AiLocal")]
public sealed class AiPilotLiveLocaleTests
{
    private sealed class Factory(string connection) : IDbContextFactory<ServiceDbContext>
    {
        public ServiceDbContext CreateDbContext() => new(new DbContextOptionsBuilder<ServiceDbContext>().UseNpgsql(connection).Options);
    }

    private sealed record Case(string Locale, AiPurpose Purpose, string Model, Guid RequestId, AiPayload Payload);

    /// <summary>Dispatches each approved fictional case once and observes each preserved handle to its first accepted result.</summary>
    [Fact]
    public async Task Live_fixed_locale_corpus_records_original_outcomes_and_usage()
    {
        var ct = TestContext.Current.CancellationToken;
        var connection = Environment.GetEnvironmentVariable("ZANANCE_AI_LOCALE_LIVE_CONNECTION");
        if (connection is null) { Assert.Skip("Explicit isolated multilingual live fixture required."); return; }
        var settings = new NpgsqlConnectionStringBuilder(connection);
        Assert.Equal(("127.0.0.1", 55479, "zanance_ai_provider_pilot"), (settings.Host, settings.Port, settings.Database));
        var root = Environment.GetEnvironmentVariable("ZANANCE_AI_LOCALE_LIVE_ROOT")!;
        Assert.Equal("Zanance fixed synthetic multilingual live cohort", await File.ReadAllTextAsync(Path.Combine(root, "fixture.marker"), ct));
        var factory = new Factory(connection);
        var configuration = new ConfigurationBuilder().AddEnvironmentVariables().Build();
        using (var reservation = File.Open(Path.Combine(root, "corpus-dispatch-reserved"), FileMode.CreateNew)) { }
        var time = TimeProvider.System;
        var actors = new Dictionary<string, ClaimsPrincipal>(StringComparer.Ordinal);
        var cases = AiPilotLocaleFixtures.All.SelectMany(fixture => AiPilotProvider.Models(fixture.Payload.Purpose)
                .Select(model => new Case(fixture.Payload.Language, fixture.Payload.Purpose, model, Guid.NewGuid(), fixture.Payload)))
            .OrderBy(item => item.Purpose).ThenBy(item => item.Model, StringComparer.Ordinal).ThenBy(item => item.Locale, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(24, cases.Length);
        // The transport is the original service client, with its reviewed timeouts and log suppression.
        var services = new ServiceCollection();
        AiModule.Add(services, pilotEnabled: true);
        await using var built = services.BuildServiceProvider();
        var pilot = new AiPilotProvider(configuration, built.GetRequiredService<IHttpClientFactory>(), time);
        var protection = DataProtectionProvider.Create(new DirectoryInfo(Path.Combine(root, "private-evaluation-keys")),
            options => options.SetApplicationName("Zanance isolated locale evaluation"));
        var store = new AiRequestStore(factory, new UnconfiguredAiProvider(), protection, time, pilot);
        await using (var db = factory.CreateDbContext())
        {
            await db.Database.MigrateAsync(ct);
            Assert.Empty(await db.Set<AiRequestRow>().ToArrayAsync(ct));
            foreach (var locale in cases.Select(item => item.Locale).Distinct(StringComparer.Ordinal))
            {
                var owner = "fictional-live-locales-" + locale;
                var session = Guid.NewGuid();
                db.Users.Add(new ServiceUser { Id = owner, UserName = owner, SecurityStamp = "fictional-live-locale-stamp" });
                db.Sessions.Add(new DeviceSession { Id = session, UserId = owner, Generation = 1, ExpiresAt = time.GetUtcNow().AddHours(6) });
                actors.Add(locale, new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, owner),
                    new Claim(ServiceHost.SessionClaim, session.ToString("D")), new Claim(ServiceHost.GenerationClaim, "1")], "synthetic")));
            }
            await db.SaveChangesAsync(ct);
        }
        await File.WriteAllTextAsync(Path.Combine(root, "cases.json"),
            JsonSerializer.Serialize(cases.Select(item => new { item.Locale, item.Purpose, item.Model, item.RequestId })), ct);
        foreach (var locale in actors.Keys)
            await store.GrantAsync("fictional-live-locales-" + locale, "fictional-live-locale-credit-" + locale, false, 4, ct);

        var finished = new HashSet<Guid>();
        foreach (var cohort in cases.GroupBy(item => (item.Purpose, item.Model)))
        {
            await using var db = factory.CreateDbContext();
            await pilot.SelectModelAsync(db, cohort.Key.Purpose, cohort.Key.Model, "fictional-locale-evaluation", ct);
            foreach (var item in cohort)
            {
                var actor = actors[item.Locale];
                var offer = await store.OfferAsync(actor, item.Purpose, ct);
                Assert.Equal(item.Model, offer.Model);
                var result = await store.SubmitAsync(actor, new(item.RequestId, offer, item.Payload), ct);
                await ObserveAsync(item, result);
            }
        }
        var deadline = time.GetUtcNow().AddMinutes(9);
        while (time.GetUtcNow() < deadline && finished.Count < cases.Length)
        {
            var checkedAny = false;
            foreach (var item in cases.Where(item => !finished.Contains(item.RequestId)))
            {
                await using var db = factory.CreateDbContext();
                var saved = await db.Set<AiRequestRow>().AsNoTracking().SingleAsync(row => row.Id == item.RequestId, ct);
                if (saved.VendorOperationId is null) continue;
                checkedAny = true;
                var result = await store.StatusAsync(actors[item.Locale], item.RequestId, ct);
                await ObserveAsync(item, result);
            }
            if (!checkedAny || finished.Count == cases.Length) break;
            await Task.Delay(TimeSpan.FromSeconds(10), ct);
        }

        async Task ObserveAsync(Case item, AiRequestResult result)
        {
            await using var db = factory.CreateDbContext();
            var saved = await db.Set<AiRequestRow>().AsNoTracking().SingleAsync(row => row.Id == item.RequestId, ct);
            var seconds = (time.GetUtcNow() - saved.CreatedAt).TotalSeconds;
            var receipt = new
            {
                item.Locale, item.Purpose, item.Model, State = result.State.ToString(), result.Text,
                VendorHandleRecorded = saved.VendorOperationId is not null, saved.VendorInputTokens, saved.VendorOutputTokens,
                saved.VendorCachedInputTokens, saved.VendorThinkingTokens, saved.PilotMeteredCostMicros,
                SecondsSinceAdmission = seconds,
            };
            var terminal = result.State != AiRequestState.Pending;
            var path = Path.Combine(root, $"{item.Locale}-{item.Model}-{(terminal ? "terminal" : "last-observed")}.json");
            await using var output = File.Open(path, terminal ? FileMode.CreateNew : FileMode.Create);
            await JsonSerializer.SerializeAsync(output, receipt, cancellationToken: ct);
            if (terminal) finished.Add(item.RequestId);
        }
    }
}
