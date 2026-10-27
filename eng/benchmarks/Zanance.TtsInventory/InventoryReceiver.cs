using Android.Content;
using Android.Speech.Tts;
using System.Text.Json;
using Vafadar.Zanance.PhysicalReaderGestures;

namespace Vafadar.Zanance.TtsInventory;

/// <summary>D-586: inspect public installed-engine metadata without synthesis, voice selection or settings input.</summary>
[BroadcastReceiver(Name = Component, Exported = true, Permission = Android.Manifest.Permission.Dump)]
[IntentFilter([Action])]
public sealed class InventoryReceiver : BroadcastReceiver
{
    /// <summary>Independent owned package with no Activity or application dependency.</summary>
    public const string Component = "pro.vafadar.zanance.ttsinventoryreview.InventoryReceiver";
    /// <summary>Only the explicit privileged metadata request is accepted.</summary>
    public const string Action = "pro.vafadar.zanance.ttsinventoryreview.INVENTORY";
    /// <summary>Only derived public metadata is emitted under this bounded tag.</summary>
    public const string LogTag = "ZananceTtsInventory";
    private static int _started;

    /// <inheritdoc />
    public override void OnReceive(Context? context, Intent? intent)
    {
        if (context?.ApplicationContext is not { } app || intent?.Action != Action) return;
        var request = PhysicalReaderPolicy.RefusalRequest(intent.GetStringExtra("request"));
        if (request is null || Interlocked.CompareExchange(ref _started, 1, 0) != 0) return;
        var pending = GoAsync();
        _ = Task.Run(async () =>
        {
            try { await InspectAsync(app, request); }
            catch (Exception error) { Emit(new { Kind = "refused", Request = request, ErrorType = error.GetType().FullName }); }
            finally { pending?.Finish(); }
        });
    }

    /// <summary>Check the fixed owned device/candidate before connecting, and reject a replaced install afterward.</summary>
    private static async Task InspectAsync(Context context, string request)
    {
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(18));
        var original = await PhysicalReaderTarget.AdmitAsync(context, deadline.Token);
        if (original is null) throw new InvalidOperationException("Owned candidate admission refused.");
        using var listener = new Initialization();
        using var engine = new TextToSpeech(context, listener);
        try
        {
            var status = await listener.Completion.Task.WaitAsync(TimeSpan.FromSeconds(8), deadline.Token);
            if (status != OperationResult.Success) throw new InvalidOperationException("Installed engine initialization refused.");
            // AOSP connection setup reads defaults. Do not call SetLanguage/SetVoice: they may load/download voice data.
            var advertised = engine.Voices ?? throw new InvalidOperationException("Voice metadata unreadable.");
            if (advertised.Count > 2048) throw new InvalidOperationException("Voice metadata bound exceeded.");
            using var persian = Java.Util.Locale.ForLanguageTag("fa")
                ?? throw new InvalidOperationException("Persian locale metadata unavailable.");
            var availability = engine.IsLanguageAvailable(persian);
            var selected = advertised.Where(voice => voice.Locale?.Language is "fa" or "en" or "de")
                .OrderBy(voice => voice.Locale?.ToLanguageTag(), StringComparer.Ordinal)
                .ThenBy(voice => voice.Name, StringComparer.Ordinal).Select(Describe).ToArray();
            var defaultVoice = engine.DefaultVoice;
            deadline.Token.ThrowIfCancellationRequested();
            if (!PhysicalReaderPolicy.SameInstall(original, PhysicalReaderTarget.ReadInstall(context)))
                throw new InvalidOperationException("Owned installation changed during metadata query.");
            Emit(new
            {
                Kind = "inventory", Request = request, OwnerApk = PhysicalReaderPolicy.OwnerApkSha256,
                ReportedDefaultEngine = Bounded(engine.DefaultEngine),
                ReportedDefaultVoice = defaultVoice is null ? null : Describe(defaultVoice),
                PersianAvailabilityCode = (int)availability, AdvertisedVoiceCount = advertised.Count,
                RelevantAdvertisedVoices = selected, NoVoiceSelectionOrSynthesisRequested = true,
                NoClaimOfInstallationOrIntelligibility = true
            });
        }
        finally { engine.Shutdown(); }
    }

    /// <summary>Preserve network/missing-data flags as metadata, never infer that an advertised voice is installed.</summary>
    private static VoiceMetadata Describe(Voice voice)
    {
        var features = voice.Features?.OrderBy(value => value, StringComparer.Ordinal).Select(Bounded).ToArray() ?? [];
        if (features.Length > 32) throw new InvalidOperationException("Feature metadata bound exceeded.");
        return new(Bounded(voice.Name), Bounded(voice.Locale?.ToLanguageTag()), voice.IsNetworkConnectionRequired, features);
    }

    /// <summary>Reject unbounded or multiline engine metadata; no caller-provided text is echoed.</summary>
    private static string? Bounded(string? value) => value is null || value.Length <= 160
        && value.IndexOfAny(['\0', '\r', '\n']) < 0 ? value : throw new InvalidOperationException("Metadata string bound exceeded.");

    /// <summary>Small immutable public voice facts; no voice files, accounts or device-key material.</summary>
    private sealed record VoiceMetadata(string? Name, string? Locale, bool RequiresNetwork, string?[] Features);

    /// <summary>Keep callback ownership until initialization completes or the finite session shuts down.</summary>
    private sealed class Initialization : Java.Lang.Object, TextToSpeech.IOnInitListener
    {
        /// <summary>Continue inspection off the engine callback thread.</summary>
        internal TaskCompletionSource<OperationResult> Completion { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        /// <inheritdoc />
        public void OnInit(OperationResult status) => Completion.TrySetResult(status);
    }

    /// <summary>Chunk one bounded public record; no screenshots, raw exception messages or speech text.</summary>
    private static void Emit(object value)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(JsonSerializer.Serialize(value));
        if (bytes.Length > 256 * 1024) throw new InvalidOperationException("Inventory output bound exceeded.");
        var id = Guid.NewGuid().ToString("N"); var count = (bytes.Length + 1535) / 1536;
        for (var index = 0; index < count; index++)
        {
            var offset = index * 1536; var length = Math.Min(1536, bytes.Length - offset);
            Android.Util.Log.Info(LogTag, id + "|" + index + "/" + count + "|" + Convert.ToBase64String(bytes.AsSpan(offset, length)));
        }
    }
}
