using System.Globalization;
using System.Resources;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Interaction;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;
namespace Vafadar.Zanance.App.Tests;

/// <summary>Short native action captions preserve the complete disclosure and its exact acceptance fingerprint.</summary>
public sealed class AiNativeConfirmationCaptionTests
{
    /// <summary>All six actual resources use a short positive action without changing destination or cost disclosure.</summary>
    [Theory]
    [InlineData("en", "Send")] [InlineData("fa", "ارسال")]
    [InlineData("es", "Enviar")] [InlineData("fr", "Envoyer")] [InlineData("it", "Invia")]
    public async Task Native_action_is_short_localized_and_keeps_complete_disclosure(string language, string expected)
    {
        using var f = new FlowFixture(); var port = new NativeCaptionPort(false);
        var prepared = await PrepareAsync(f, port, language);
        var before = await GoalChooserFlowTests.SnapshotAsync(f);
        try
        {
            await prepared.Model.SendAsync();
            Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
            Assert.Equal(1, port.Confirmations); Assert.Equal(expected, port.Accept);
            Assert.NotEqual(f.Translator["Ai_Send"], port.Accept);
            Assert.Equal(f.Translator["Ai_Title"], port.Title);
            Assert.Equal(f.Translator["Ai_ConfirmCancelAction"], port.Cancel);
            Assert.Equal(f.Translator.Format("Ai_SendConfirm", prepared.Runtime.Destination, "declared", "input-only",
                NativeDigits.Apply(7L.ToString(f.Localization.CurrentCulture))), port.Message);
            Assert.Equal(0, prepared.Runtime.Primary); Assert.False(prepared.Model.HasPendingOperation);
            Assert.True(prepared.Model.CanSend); Assert.Null(prepared.Model.Error);
        }
        finally { prepared.Model.RetireRoot(); }
    }

    /// <summary>A synthetic response cannot authorize the next boundary after either visible action caption changes.</summary>
    [Theory]
    [InlineData("Ai_ConfirmSendAction")] [InlineData("Ai_ConfirmCancelAction")]
    public async Task Changed_native_confirmation_caption_refuses_before_authorization(string resourceKey)
    {
        using var f = new FlowFixture(); var port = new NativeCaptionPort(true);
        var prepared = await PrepareAsync(f, port, "en");
        var overlay = new CaptionResource(resourceKey) { Value = f.Translator[resourceKey] };
        f.Translator.AddResources(overlay); port.BeforeReturn = () => overlay.Value = "Changed action";
        var before = await GoalChooserFlowTests.SnapshotAsync(f);
        try
        {
            await prepared.Model.SendAsync();
            Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
            Assert.Equal(1, port.Confirmations); Assert.Equal("Send", port.Accept);
            Assert.Equal(0, prepared.Runtime.Primary); Assert.NotNull(prepared.Model.Error);
            Assert.False(prepared.Model.HasPendingOperation); Assert.False(prepared.Model.CanSend);
        }
        finally { prepared.Model.RetireRoot(); }
    }

    private static async Task<AiQuotePrepared> PrepareAsync(FlowFixture f, IAppInteraction port, string language)
    {
        if (Environment.GetEnvironmentVariable("ZANANCE_D984_HOST_TMP") is { } owned)
            Assert.StartsWith(Path.GetFullPath(owned), Path.GetFullPath(f.Directory.Path), StringComparison.OrdinalIgnoreCase);
        f.Localization.SetLanguage(f.Localization.SupportedLanguages.Single(x => x.CultureName == language));
        var factory = f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>();
        await using (var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken))
        { db.Accounts.Add(new Account { Name = "D984 zero metadata", Type = AccountType.Checking, CurrencyCode = "EUR", OpeningDate = new(2026, 1, 1), OpeningBalance = 0 }); await db.SaveChangesAsync(TestContext.Current.CancellationToken); }
        var prepared = await AiQuoteAuthorityProbes.CreateAsync(factory, f.Services.GetRequiredService<ICommercialWriteAccessSource>(), f.Translator, f.Localization, f.Time, port);
        await prepared.Model.QuoteAsync(); Assert.True(prepared.Model.CanSend); return prepared;
    }

    /// <summary>Overrides only the declared action resource; no language event hides a missing literal fingerprint.</summary>
    private sealed class CaptionResource(string key) : ResourceManager
    {
        internal required string Value;
        public override string? GetString(string name, CultureInfo? culture) => name == key ? Value : null;
    }

    /// <summary>Captures literal confirmation arguments; all other interaction effects refuse.</summary>
    private sealed class NativeCaptionPort(bool response) : IAppInteraction
    {
        internal int Confirmations;
        internal string? Title, Message, Accept, Cancel;
        internal Action? BeforeReturn;
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
