using System.Globalization;
using System.Resources;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Features.Analysis;
using Vafadar.Zanance.App.Interaction;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Analysis;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Analysis;
using Vafadar.Zanance.Data.Commerce;
namespace Vafadar.Zanance.App.Tests;

/// <summary>Related native share/continue/close actions keep full disclosures and current literal captions.</summary>
public sealed class AiOtherNativeConfirmationCaptionTests
{
    /// <summary>Each remaining original confirmation uses a translated concise action and no primary effect.</summary>
    [Theory]
    [InlineData("share", "en", "Share")] [InlineData("share", "fa", "اشتراک‌گذاری")]
    [InlineData("share", "es", "Compartir")] [InlineData("share", "fr", "Partager")] [InlineData("share", "it", "Condividi")]
    [InlineData("continue", "en", "Continue")] [InlineData("continue", "fa", "ادامه")]
    [InlineData("continue", "es", "Continuar")] [InlineData("continue", "fr", "Continuer")] [InlineData("continue", "it", "Continua")]
    [InlineData("close", "en", "Close")] [InlineData("close", "fa", "بستن")]
    [InlineData("close", "es", "Cerrar")] [InlineData("close", "fr", "Fermer")] [InlineData("close", "it", "Chiudi")]
    public async Task Related_action_is_short_and_retains_complete_literal_disclosure(string operation, string language, string expected)
    {
        using var f = new FlowFixture(); var port = new CaptionPort(false); var prepared = await PrepareAsync(f, port, operation, language);
        var before = await GoalChooserFlowTests.SnapshotAsync(f);
        try
        {
            await InvokeAsync(prepared.Model, operation);
            Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
            Assert.Equal(1, port.Confirmations); Assert.Equal(expected, port.Accept);
            Assert.Equal(f.Translator["Ai_Title"], port.Title); Assert.Equal(f.Translator["Ai_ConfirmCancelAction"], port.Cancel);
            Assert.Equal(f.Translator[operation switch { "share" => "Ai_ManualConfirm", "continue" => "Ai_ContinueConfirm", _ => "Ai_CloseConfirm" }], port.Message);
            Assert.Equal(0, prepared.Runtime.Primary); Assert.Equal(operation == "share" ? 0 : 1, prepared.Runtime.Opens);
            Assert.False(prepared.Model.HasPendingOperation); Assert.Null(prepared.Model.Error);
            if (operation == "share") Assert.True(prepared.Model.CanShare); else Assert.True(prepared.Model.CanContinue);
        }
        finally { prepared.Model.RetireRoot(); }
    }

    /// <summary>New visible-action keys participate in the same original literal acceptance guard.</summary>
    [Theory] [InlineData("share")] [InlineData("continue")] [InlineData("close")]
    public async Task Related_changed_positive_caption_refuses_the_next_original_boundary(string operation)
    {
        using var f = new FlowFixture(); var port = new CaptionPort(true); var prepared = await PrepareAsync(f, port, operation, "en");
        var key = operation switch { "share" => "Ai_ConfirmShareAction", "continue" => "Ai_ConfirmContinueAction", _ => "Ai_ConfirmCloseAction" };
        var overlay = new CaptionResource(key, f.Translator[key]); f.Translator.AddResources(overlay); port.BeforeReturn = () => overlay.Value = "Changed action";
        var before = await GoalChooserFlowTests.SnapshotAsync(f);
        try
        {
            await InvokeAsync(prepared.Model, operation);
            Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f)); Assert.Equal(1, port.Confirmations);
            Assert.Equal(0, prepared.Runtime.Primary); Assert.NotNull(prepared.Model.Error); Assert.False(prepared.Model.HasPendingOperation);
        }
        finally { prepared.Model.RetireRoot(); }
    }

    private static Task InvokeAsync(AiAnalysisViewModel model, string operation) => operation switch
    { "share" => model.ShareAsync(), "continue" => model.ContinueAsync(), "close" => model.CloseAsync(), _ => throw new ArgumentOutOfRangeException(nameof(operation)) };
    private static async Task<Prepared> PrepareAsync(FlowFixture f, IAppInteraction port, string operation, string language)
    {
        if (Environment.GetEnvironmentVariable("ZANANCE_D984_HOST_TMP") is { } owned)
            Assert.StartsWith(Path.GetFullPath(owned), Path.GetFullPath(f.Directory.Path), StringComparison.OrdinalIgnoreCase);
        f.Localization.SetLanguage(f.Localization.SupportedLanguages.Single(x => x.CultureName == language));
        var factory = f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>();
        await using (var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken))
        { db.Accounts.Add(new Account { Name = "D984 other zero metadata", Type = AccountType.Checking, CurrencyCode = "EUR", OpeningDate = new(2026, 1, 1), OpeningBalance = 0 }); await db.SaveChangesAsync(TestContext.Current.CancellationToken); }
        var runtime = new CaptionRuntime(f.Time, language, operation != "share");
        var model = new AiAnalysisViewModel(new AiSnapshotStore(factory, f.Services.GetRequiredService<ICommercialWriteAccessSource>()), f.Translator, f.Localization, new DateFormatter(f.Localization), f.Time, port, runtime);
        await model.LoadAsync(); Assert.True(model.Loaded);
        if (operation == "share") { model.Accounts[0].Selected = true; await model.ReviewAsync(); Assert.True(model.CanShare); }
        else { await model.CheckProviderAsync(); Assert.True(model.CanContinue); }
        return new(model, runtime);
    }
    private sealed record Prepared(AiAnalysisViewModel Model, CaptionRuntime Runtime);
    /// <summary>Declared retained pending input only; no original request is submitted or persisted.</summary>
    private sealed class CaptionRuntime(TimeProvider time, string language, bool pending) : IAiRuntime, IAiRemote
    {
        internal int Opens, Primary;
        public bool NetworkAvailable => pending;
        public string Destination => "https://localhost/declared-caption-only";
        public AiUiAccess Capture(string file) => new(() => { }, CancellationToken.None);
        public Task<IAiRemote> OpenAsync(string file, CancellationToken ct) { Opens++; return Task.FromResult<IAiRemote>(this); }
        public Task<AiAvailability> AvailabilityAsync(CancellationToken ct) => Task.FromResult(new AiAvailability(true, "declared", "input-only", new(20, 0, 7, 0)));
        public Task<AiClientRecord?> ReadAsync(CancellationToken ct)
        {
            var payload = new AiPayload(1, AiPurpose.PeriodExplanation, language, new(new(2026, 10, 1), new(2026, 10, 9), 1, true, [new("EUR", 0, 0, 0, 0)], 0), null);
            var offer = new AiOffer(Guid.NewGuid(), "declared", "input-only", payload.Purpose, 7, time.GetUtcNow().AddDays(1));
            return Task.FromResult<AiClientRecord?>(new(1, new(Guid.NewGuid(), offer, payload), null));
        }
        public Task<bool> ConfirmDisclosureAsync() { Primary++; throw new InvalidOperationException("No genuine authorization."); }
        public Task ShareAsync(string text) { Primary++; throw new InvalidOperationException("No recipient."); }
        public Task<AiOffer> OfferAsync(AiPurpose purpose, CancellationToken ct) { Primary++; throw new InvalidOperationException(); }
        public Task<AiRequestResult> SubmitAsync(AiOffer offer, AiPayload payload, Func<Task> check, CancellationToken ct) { Primary++; throw new InvalidOperationException(); }
        public Task<AiRequestResult> ContinueAsync(CancellationToken ct) { Primary++; throw new InvalidOperationException(); }
        public Task<AiRequestResult> CloseAsync(CancellationToken ct) { Primary++; throw new InvalidOperationException(); }
        public void Dispose() { }
    }
    /// <summary>Mutates one literal resource without a language event or native input.</summary>
    private sealed class CaptionResource(string key, string initial) : ResourceManager
    {
        internal string Value = initial;
        public override string? GetString(string name, CultureInfo? culture) => name == key ? Value : null;
    }
    /// <summary>Only synthetic confirmation responses exist; every other interaction refuses.</summary>
    private sealed class CaptionPort(bool response) : IAppInteraction
    {
        internal int Confirmations; internal string? Title, Message, Accept, Cancel; internal Action? BeforeReturn;
        public Task<bool> ConfirmAsync(string title, string message, string accept, string cancel)
        { Confirmations++; Title = title; Message = message; Accept = accept; Cancel = cancel; BeforeReturn?.Invoke(); return Task.FromResult(response); }
        public Task AlertAsync(string title, string message, string cancel) => throw new InvalidOperationException();
        public Task<string?> PromptAsync(string title, string message, string accept, string cancel, string? initialValue = null, int maxLength = -1) => throw new InvalidOperationException();
        public Task<string?> ChooseAsync(string title, string cancel, params string[] choices) => throw new InvalidOperationException();
        public Task NavigateAsync(string route, IDictionary<string, object>? parameters = null) => throw new InvalidOperationException();
        public Task ShowFailureAsync(Exception exception) => throw new InvalidOperationException();
        public Task RestoreOnboardingAsync() => throw new InvalidOperationException();
        public void CompleteOnboarding() => throw new InvalidOperationException();
    }
}
