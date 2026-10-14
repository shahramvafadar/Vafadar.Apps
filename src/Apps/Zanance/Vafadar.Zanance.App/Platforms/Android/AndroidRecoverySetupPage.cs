using Vafadar.Localization;

namespace Vafadar.Zanance.App.Security;

/// <summary>Displays an ephemeral recovery draft until its recorded-code acknowledgment is explicitly saved.</summary>
internal sealed class AndroidRecoverySetupPage : ContentPage
{
    private readonly AndroidProtectedStorageHost host;
    private readonly Translator translator;
    private readonly Label heading = new();
    private readonly Label hint = new();
    private readonly Label codeLabel = new();
    private readonly Editor code = new() { IsReadOnly = true, AutoSize = EditorAutoSizeOption.TextChanges, FlowDirection = FlowDirection.LeftToRight };
    private readonly Label recordedCaption = new();
    private readonly Switch recorded = new();
    private readonly Label failure = new() { IsVisible = false };
    private readonly Button save = new() { MinimumHeightRequest = 44 };
    private readonly Button back = new() { MinimumHeightRequest = 44 };
    private AndroidRecoveryDraft? draft;
    private bool busy;
    private bool loaded;
    private bool disappeared;

    /// <summary>Creates a financial-data-free setup page; native material is prepared only after appearing.</summary>
    internal AndroidRecoverySetupPage(AndroidProtectedStorageHost host, Translator translator)
    {
        this.host = host; this.translator = translator;
        FlowDirection = Application.Current?.Windows.FirstOrDefault()?.Page?.FlowDirection ?? FlowDirection.MatchParent;
        heading.Style = (Style)Application.Current!.Resources["PageTitle"];
        hint.Style = (Style)Application.Current.Resources["Secondary"];
        failure.SetDynamicResource(Label.TextColorProperty, "ExpenseText");
        save.Style = (Style)Application.Current.Resources["PrimaryButton"];
        back.Style = (Style)Application.Current.Resources["TextButton"];
        var acknowledgment = new Grid { ColumnDefinitions = [new(GridLength.Star), new(GridLength.Auto)], ColumnSpacing = 12 };
        acknowledgment.Add(recordedCaption); acknowledgment.Add(recorded, 1);
        var header = new Grid { ColumnDefinitions = [new(GridLength.Star), new(GridLength.Auto)], ColumnSpacing = 8 };
        header.Add(heading); header.Add(new Vafadar.Maui.Controls.HelpButton { Topic = "DatabaseRecovery" }, 1);
        Content = new ScrollView { Content = new VerticalStackLayout
            { Padding = 16, Spacing = 12, Children = { back, header, hint, codeLabel, code, acknowledgment, failure, save } } };
        recorded.Toggled += (_, _) => save.IsEnabled = !busy && draft is not null && recorded.IsToggled;
        save.Clicked += OnSaveClicked;
        back.Clicked += async (_, _) => { if (!busy) await Navigation.PopModalAsync(); };
        RefreshText();
    }

    /// <inheritdoc />
    protected override async void OnAppearing()
    {
        base.OnAppearing(); translator.PropertyChanged -= OnTranslationChanged; translator.PropertyChanged += OnTranslationChanged;
        disappeared = false;
        if (loaded) return;
        loaded = true; SetBusy(true);
        try
        {
            if (host.RecoveryEnrolled)
            {
                hint.Text = translator["Recovery_AlreadyEnabled"];
                codeLabel.IsVisible = code.IsVisible = recorded.IsVisible = recordedCaption.IsVisible = save.IsVisible = false;
            }
            else
            {
                try { draft = await Task.Run(() => host.CreateRecoveryDraftAsync(CancellationToken.None)); }
                catch (RecoveryEnrollmentIncompleteException)
                {
                    var recordedCode = await DisplayPromptAsync(translator["Recovery_Title"], translator["Recovery_CodePrompt"],
                        translator["Recovery_Confirm"], translator["Common_Cancel"], maxLength: 64);
                    if (recordedCode is not null)
                        draft = await Task.Run(() => host.ResumeRecoveryDraftAsync(recordedCode, CancellationToken.None));
                    recordedCode = string.Empty;
                }
                if (draft is not null) code.Text = draft.DisplayCode;
            }
        }
        catch (Exception error) when (error is not OutOfMemoryException) { failure.Text = translator["Recovery_Failed"]; failure.IsVisible = true; }
        finally { SetBusy(false); }
    }

    private async void OnSaveClicked(object? sender, EventArgs e)
    {
        if (busy || draft is null || !recorded.IsToggled) return;
        SetBusy(true); failure.IsVisible = false; draft.RecordedByUser = true;
        try { await Task.Run(() => host.SaveRecoveryDraftAsync(draft, CancellationToken.None)); SetBusy(false); await Navigation.PopModalAsync(); }
        catch (Exception error) when (error is not OutOfMemoryException) { failure.Text = translator["Recovery_Failed"]; failure.IsVisible = true; }
        finally { SetBusy(false); }
    }

    private void SetBusy(bool value)
    {
        busy = value;
        if (!value && disappeared) ClearDraft();
        back.IsEnabled = !value; recorded.IsEnabled = !value;
        save.IsEnabled = !value && draft is not null && recorded.IsToggled;
    }

    private void ClearDraft()
    { code.Text = string.Empty; draft?.Dispose(); draft = null; recorded.IsToggled = false; loaded = false; }

    private void OnTranslationChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e) => RefreshText();
    private void RefreshText()
    {
        Title = heading.Text = translator["Recovery_Title"];
        hint.Text = translator[host.RecoveryEnrolled ? "Recovery_AlreadyEnabled" : "Recovery_SetupHint"];
        codeLabel.Text = translator["Recovery_Code"]; SemanticProperties.SetDescription(code, codeLabel.Text);
        recordedCaption.Text = translator["Recovery_Recorded"]; SemanticProperties.SetDescription(recorded, recordedCaption.Text);
        save.Text = translator["Common_Save"]; back.Text = translator["Common_Back"];
        if (failure.IsVisible) failure.Text = translator["Recovery_Failed"];
    }

    /// <inheritdoc />
    protected override bool OnBackButtonPressed() => busy || base.OnBackButtonPressed();

    /// <inheritdoc />
    protected override void OnDisappearing()
    {
        translator.PropertyChanged -= OnTranslationChanged;
        disappeared = true;
        // The native operation owns this material until its asynchronous Save finishes.
        // Clear the visible string immediately, but do not dispose its RSA/key buffer underneath it.
        code.Text = string.Empty;
        if (!busy) ClearDraft();
        base.OnDisappearing();
    }
}
