using System.Text.Json;
using Android.Views.InputMethods;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Localization;
using Vafadar.Zanance.App.Diagnostics;
using Vafadar.Zanance.App.Features.Home;

namespace Vafadar.Zanance.App;

internal static partial class AndroidHomeReview
{
    /// <summary>Tests actual IME acceptance on original controls, separately from model-to-native text publication.</summary>
    private static async Task<object> ReviewNumericImeAsync(App app, IServiceProvider services, string folder, string comparison, CancellationToken ct)
    {
        if (Microsoft.Maui.ApplicationModel.Platform.CurrentActivity?.Intent?.GetBooleanExtra("review_plan_resume_actions", false) != true)
            throw new ReviewCheckException("Original completed numeric source required; never prepare another fixture.");
        await AwaitOriginalMoneyWindowAsync(ct); app.OpenReviewShell();
        while (Shell.Current.CurrentPage is not HomePage { IsLoaded: true } home || home.Handler?.PlatformView is not Android.Views.View { IsShown: true, HasWindowFocus: true })
            await Task.Delay(100, ct);
        await ((HomePage)Shell.Current.CurrentPage).PendingPublication.WaitAsync(ct);
        var baseline = await ImportFinancialNativeReview.StoredAsync(services, ct);
        using var original = await PlanEditorNativeReview.ContinuePhysicalRateOutcomesAsync(services, Path.Combine(folder, "original-physical-rates"), ct);
        var observations = new List<object>(); var matches = true;
        async Task Input(Page page, string stage, string description, string requested, Func<string> model)
        {
            var entry = PlanEditorNativeReview.Views(page).OfType<Entry>().Single(item => SemanticProperties.GetDescription(item) == description);
            for (Element? parent = entry.Parent; parent is not null; parent = parent.Parent)
                if (parent is ScrollView viewport) { await viewport.ScrollToAsync(entry, ScrollToPosition.MakeVisible, false); break; }
            await PlanEditorNativeReview.WaitAsync(() => entry.Handler?.PlatformView is Android.Widget.EditText
                { IsShown: true, IsAttachedToWindow: true, HasWindowFocus: true, Enabled: true }, ct);
            var native = (Android.Widget.EditText)entry.Handler!.PlatformView!;
            if (native.Context?.PackageName != Package || !native.RequestFocus()) throw new ReviewCheckException("Actual owned numeric editor focus required.");
            await Task.Delay(100, ct);
            using var info = new EditorInfo();
            using var connection = native.OnCreateInputConnection(info) ?? throw new ReviewCheckException("Actual numeric editor has no input connection.");
            connection.BeginBatchEdit(); connection.FinishComposingText(); connection.SetSelection(0, native.Text?.Length ?? 0);
            using (var empty = new Java.Lang.String(string.Empty)) connection.CommitText(empty, 1);
            connection.EndBatchEdit(); await Task.Delay(50, ct);
            if (native.Text != string.Empty || model() != string.Empty) throw new ReviewCheckException("Original IME selection did not clear its actual native/model input.");
            connection.BeginBatchEdit();
            bool committed;
            using (var value = new Java.Lang.String(requested)) committed = connection.CommitText(value, 1);
            connection.EndBatchEdit(); await Task.Delay(100, ct);
            var equal = native.Text == requested && model() == requested; matches &= equal;
            var row = new { Stage = stage, ProcessId = Environment.ProcessId, Requested = requested, ActualNative = native.Text,
                ActualModel = model(), NativeAcceptedCommit = committed, RequestedTextPreserved = equal, NativeInputType = native.InputType.ToString(), NativeInputFlags = (int)native.InputType,
                EditorInputType = info.InputType.ToString(), EditorInputFlags = (int)info.InputType, KeyListenerType = native.KeyListener?.GetType().FullName,
                NumericCulture = services.GetRequiredService<ILocalizationService>().FormattingCultureName,
                ActualOwnedEditorFocused = native.IsFocused && native.HasWindowFocus };
            await File.WriteAllTextAsync(Path.Combine(folder, "ime-" + comparison + "-" + stage + ".json"), JsonSerializer.Serialize(row), ct);
            observations.Add(row); LogReport("numeric-ime-observation", comparison, row);
        }
        var followup = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity?.Intent?.GetBooleanExtra("review_numeric_ime_followup", false) == true;
        var guardsOnly = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity?.Intent?.GetBooleanExtra("review_numeric_ime_guards_only", false) == true;
        var source = await original.NumericImeSourceAsync(ct);
        if (!guardsOnly) await original.NumericImeInputsAsync(Input, followup, ct);
        var adapterChecks = new List<object>();
        if (guardsOnly)
        {
            var field = new Entry { Keyboard = Keyboard.Numeric, IsReadOnly = true, MaxLength = 12 };
            SemanticProperties.SetDescription(field, "Independent raw input");
            var page = new ContentPage { Content = new VerticalStackLayout { Padding = 24, Children = { field } } };
            await Shell.Current.Navigation.PushModalAsync(page);
            try
            {
                await PlanEditorNativeReview.WaitAsync(() => field.Handler?.PlatformView is Android.Widget.EditText
                    { IsShown: true, IsAttachedToWindow: true, HasWindowFocus: true }, ct);
                var native = (Android.Widget.EditText)field.Handler!.PlatformView!;
                LogReport("numeric-ime-adapter-observation", comparison, new { Stage = "initial-read-only", ModelReadOnly = field.IsReadOnly,
                    native.Focusable, native.FocusableInTouchMode, Empty = field.Text is null or "", InputFlags = (int)native.InputType });
                if (native.Focusable || native.FocusableInTouchMode || field.Text is not (null or ""))
                    throw new ReviewCheckException("Original numeric read-only control became editable.");
                adapterChecks.Add(new { Stage = "initial-read-only", Focusable = false, Empty = true });
                field.IsReadOnly = false;
                LogReport("numeric-ime-adapter-observation", comparison, new { Stage = "read-only-disabled-before-input", ModelReadOnly = field.IsReadOnly,
                    native.Focusable, native.FocusableInTouchMode, InputFlags = (int)native.InputType });
                await Input(page, "read-only-disabled", "Independent raw input", "1,25x", () => field.Text ?? string.Empty);
                field.Text = string.Empty; field.IsReadOnly = true;
                if (native.Focusable || native.FocusableInTouchMode) throw new ReviewCheckException("Numeric read-only transition became editable.");
                adapterChecks.Add(new { Stage = "read-only-enabled", Focusable = false });
                field.IsReadOnly = false; field.IsTextPredictionEnabled = false;
                await Input(page, "prediction-changed", "Independent raw input", "1,25x", () => field.Text ?? string.Empty);
                field.Text = string.Empty; field.IsSpellCheckEnabled = false;
                await Input(page, "spelling-changed", "Independent raw input", "-1,25x", () => field.Text ?? string.Empty);
            }
            finally { await Shell.Current.Navigation.PopModalAsync(); }
        }
        else if (followup)
        {
            // Real MAUI controls use the shipping mapper; no security service or owner PIN/password is accessed.
            var field = new Entry { Keyboard = Keyboard.Text, MaxLength = 12 };
            SemanticProperties.SetDescription(field, "Independent raw input");
            var page = new ContentPage { Content = new VerticalStackLayout { Padding = 24, Children = { field } } };
            await Shell.Current.Navigation.PushModalAsync(page);
            try
            {
                await PlanEditorNativeReview.WaitAsync(() => field.Handler?.PlatformView is Android.Widget.EditText
                    { IsShown: true, IsAttachedToWindow: true, HasWindowFocus: true }, ct);
                await Input(page, "ordinary-text", "Independent raw input", "draft,1", () => field.Text ?? string.Empty);
                field.Text = string.Empty; field.Keyboard = Keyboard.Numeric;
                await Input(page, "changed-keyboard", "Independent raw input", "1,25x", () => field.Text ?? string.Empty);
                field.Text = string.Empty; field.IsPassword = true;
                var native = (Android.Widget.EditText)field.Handler!.PlatformView!;
                await Task.Delay(100, ct);
                var masked = native.TransformationMethod is Android.Text.Method.PasswordTransformationMethod;
                if (!masked || native.KeyListener is not Android.Text.Method.DigitsKeyListener || field.MaxLength != 12)
                    throw new ReviewCheckException("Native numeric password masking/listener/length changed.");
                adapterChecks.Add(new { Stage = "empty-numeric-password", Masked = masked, NumericFilteringRetained = true,
                    MaxLength = field.MaxLength, NoPasswordEntered = true, InputFlags = (int)native.InputType });
                field.IsPassword = false;
                await Input(page, "password-disabled", "Independent raw input", "1,25x", () => field.Text ?? string.Empty);
                field.Text = string.Empty; field.MaxLength = 4;
                await Input(page, "native-length-filter", "Independent raw input", "1,25", () => field.Text ?? string.Empty);
                using var info = new EditorInfo();
                using var connection = native.OnCreateInputConnection(info) ?? throw new ReviewCheckException("Original native length editor required.");
                connection.SetSelection(native.Text!.Length, native.Text.Length);
                using (var extra = new Java.Lang.String("6")) connection.CommitText(extra, 1);
                await Task.Delay(50, ct);
                if (field.Text != "1,25" || native.Text != "1,25") throw new ReviewCheckException("Native maximum length filter lost.");
                adapterChecks.Add(new { Stage = "maximum-length-refusal", Retained = true, MaxLength = 4 });
            }
            finally { await Shell.Current.Navigation.PopModalAsync(); }
        }
        if (source != await original.NumericImeSourceAsync(ct)) throw new ReviewCheckException("Adapter-only native input changed original source rows.");
        if (baseline != await ImportFinancialNativeReview.StoredAsync(services, ct)) throw new ReviewCheckException("Native IME probe changed original independent base rows.");
        return new { Comparison = comparison, Scope = "native-numeric-ime", AllRequestedTextPreserved = matches,
            OriginalNumericSourceUnchanged = true, OriginalBaseStoreUnchanged = true, NativeAcceptedSaves = 0,
            NativeLedgerEntries = 0, Followup = followup, GuardsOnly = guardsOnly, AdapterChecks = adapterChecks, Observations = observations, Device = DeviceEvidence() };
    }
}
