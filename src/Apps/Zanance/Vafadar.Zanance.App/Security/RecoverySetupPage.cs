using Vafadar.Localization;

namespace Vafadar.Zanance.App.Security;

/// <summary>Displays an ephemeral recovery draft until its recorded-code acknowledgment is explicitly saved.</summary>
internal sealed class RecoverySetupPage : ContentPage
{
    private readonly ProtectedStorageHost host;
    private readonly Translator translator;
    private readonly Label heading = new();
    private readonly Label hint = new();
    private readonly Label codeLabel = new();
    private readonly Editor code = new() { IsReadOnly = true, AutoSize = EditorAutoSizeOption.TextChanges, FlowDirection = FlowDirection.LeftToRight };
    private readonly Label recordedCaption = new();
    private readonly Switch recorded = new();
    private readonly Label failure = new() { IsVisible = false };
    private readonly Button save = new() { MinimumHeightRequest = 44, IsEnabled = false };
    private readonly Button back = new() { MinimumHeightRequest = 44 };
    private ProtectedRecoveryDraft? draft;
    private bool busy;
    private bool loaded;
    private bool disappeared;
    private bool closing;
    private bool saved;
    private string? failureKey;
    private RecoverySetupAuthority? authority;
    private Task work = Task.CompletedTask;
    private readonly Window? originalWindow;
    private readonly Page? originalRoot;

    /// <summary>Creates a financial-data-free setup page; native material is prepared only after appearing.</summary>
    internal RecoverySetupPage(ProtectedStorageHost host, Translator translator)
    {
        this.host = host; this.translator = translator;
        originalWindow = Application.Current?.Windows.FirstOrDefault(); originalRoot = originalWindow?.Page;
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
        recorded.Toggled += (_, _) => RefreshButtons();
        save.Clicked += OnSaveClicked;
        back.Clicked += async (_, _) => { if (!busy && !closing) { work = CloseOriginalAsync(); await work; } };
        RefreshText();
    }

    private bool OwnsPage => !disappeared && originalWindow is not null && originalRoot is not null
        && ReferenceEquals(originalWindow.Page, originalRoot)
        && ReferenceEquals(originalRoot.Navigation.ModalStack.LastOrDefault(), this);

    /// <inheritdoc />
    protected override async void OnAppearing()
    {
        base.OnAppearing(); disappeared = false;
        translator.PropertyChanged -= OnTranslationChanged; translator.PropertyChanged += OnTranslationChanged;
        if (originalWindow is not null)
        { originalWindow.PropertyChanged -= OnWindowChanged; originalWindow.PropertyChanged += OnWindowChanged; }
        var previous = work;
        try { await previous; }
        catch (Exception error) when (error is not OutOfMemoryException)
        { if (OwnsPage) ShowFailure("Recovery_Failed"); return; }
        // The native push first establishes its actual modal stack; construction performs no key work.
        await Task.Yield();
        if (!OwnsPage || loaded || busy || closing) return;
        work = PrepareOriginalAsync(); await work;
    }

    private async Task PrepareOriginalAsync()
    {
        loaded = true; SetBusy(true);
        RecoverySetupAuthority? original = null;
        try
        {
            authority?.Dispose(); authority = original = host.CaptureRecoverySetupAuthority();
            original.RequireCurrent();
            if (host.RecoveryEnrolled)
            {
                hint.Text = translator["Recovery_AlreadyEnabled"];
                codeLabel.IsVisible = code.IsVisible = recorded.IsVisible = recordedCaption.IsVisible = save.IsVisible = false;
                return;
            }
            ProtectedRecoveryDraft? prepared = null;
            try
            {
                try { prepared = await Task.Run(() => host.CreateRecoveryDraftAsync(original.Token, original)); }
                catch (RecoveryEnrollmentIncompleteException)
                {
                    if (!OwnsPage) return;
                    original.RequireCurrent();
                    var recordedCode = await DisplayPromptAsync(translator["Recovery_Title"], translator["Recovery_CodePrompt"],
                        translator["Recovery_Confirm"], translator["Common_Cancel"], maxLength: 64);
                    try
                    {
                        if (!OwnsPage) return;
                        original.RequireCurrent();
                        if (recordedCode is not null)
                            prepared = await Task.Run(() => host.ResumeRecoveryDraftAsync(recordedCode, original.Token, original));
                    }
                    finally { recordedCode = string.Empty; }
                }
                if (!OwnsPage || !ReferenceEquals(authority, original)) return;
                original.RequireCurrent();
                draft = prepared; prepared = null;
                if (draft is not null) code.Text = draft.DisplayCode;
            }
            finally { prepared?.Dispose(); }
        }
        catch (Exception error) when (error is not OutOfMemoryException)
        { if (OwnsPage && original?.Token.IsCancellationRequested != true) ShowFailure("Recovery_Failed"); }
        finally { SetBusy(false); }
    }

    private async void OnSaveClicked(object? sender, EventArgs e)
    {
        if (busy || closing || !OwnsPage) return;
        if (saved) { work = CloseOriginalAsync(); await work; return; }
        if (draft is null || authority is null || !recorded.IsToggled) return;
        work = SaveOriginalAsync(draft, authority); await work;
    }

    private async Task SaveOriginalAsync(ProtectedRecoveryDraft originalDraft, RecoverySetupAuthority original)
    {
        SetBusy(true); failure.IsVisible = false; failureKey = null;
        var accepted = false;
        try
        {
            original.RequireCurrent();
            originalDraft.RecordedByUser = true;
            // The owner has recorded it; do not retain the visible secret while protected publication awaits.
            code.Text = string.Empty;
            await Task.Run(() => host.SaveRecoveryDraftAsync(originalDraft, original.Token, original));
            // Only the original returned publication is known accepted; closing cannot replay it.
            accepted = saved = true;
            originalDraft.Dispose(); if (ReferenceEquals(draft, originalDraft)) draft = null;
            recorded.IsToggled = false;
        }
        catch (Exception error) when (error is not OutOfMemoryException)
        { if (OwnsPage && !original.Token.IsCancellationRequested) ShowFailure("Recovery_Failed"); }
        finally { SetBusy(false); }
        if (accepted && OwnsPage && ReferenceEquals(authority, original) && !original.Token.IsCancellationRequested)
            await CloseOriginalAsync();
    }

    private async Task CloseOriginalAsync()
    {
        if (busy || closing || !OwnsPage) return;
        closing = true; RefreshButtons();
        try { await Navigation.PopModalAsync(); }
        catch (Exception error) when (error is not OutOfMemoryException)
        { if (OwnsPage) ShowFailure(saved ? "ActiveItems_SavedReturnFailed" : "Error_Unexpected"); }
        finally { closing = false; if (disappeared) ClearDraft(); RefreshButtons(); }
    }

    private void SetBusy(bool value)
    {
        busy = value;
        if (!value && (disappeared || authority?.Token.IsCancellationRequested == true)) ClearDraft();
        RefreshButtons();
    }

    private void RefreshButtons()
    {
        if (!OwnsPage) return;
        back.IsEnabled = !busy && !closing;
        recorded.IsEnabled = !busy && !closing && !saved;
        save.IsEnabled = !busy && !closing && OwnsPage && (saved || (draft is not null && recorded.IsToggled));
        save.Text = translator[saved ? "Common_Close" : "Common_Save"];
    }

    private void ShowFailure(string key)
    { failureKey = key; failure.Text = translator[key]; failure.IsVisible = true; }

    private void ClearDraft()
    {
        code.Text = string.Empty; draft?.Dispose(); draft = null; recorded.IsToggled = false;
        authority?.Dispose(); authority = null; loaded = false; saved = false;
    }

    private void OnWindowChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Window.Page) && !ReferenceEquals(originalWindow?.Page, originalRoot))
        {
            // Window replacement may bypass a normal modal disappearance; do not retain this old page in global events.
            translator.PropertyChanged -= OnTranslationChanged;
            if (originalWindow is not null) originalWindow.PropertyChanged -= OnWindowChanged;
            disappeared = true; loaded = false; authority?.Retire(); code.Text = string.Empty;
            if (!busy && !closing) ClearDraft();
        }
    }

    private void OnTranslationChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (!OwnsPage) return;
        if (!busy && !closing && !saved) recorded.IsToggled = false;
        RefreshText();
    }
    private void RefreshText()
    {
        Title = heading.Text = translator["Recovery_Title"];
        hint.Text = translator[host.RecoveryEnrolled ? "Recovery_AlreadyEnabled" : "Recovery_SetupHint"];
        codeLabel.Text = translator["Recovery_Code"]; SemanticProperties.SetDescription(code, codeLabel.Text);
        recordedCaption.Text = translator["Recovery_Recorded"]; SemanticProperties.SetDescription(recorded, recordedCaption.Text);
        save.Text = translator[saved ? "Common_Close" : "Common_Save"]; back.Text = translator["Common_Back"];
        if (failure.IsVisible) failure.Text = translator[failureKey ?? "Recovery_Failed"];
    }

    /// <inheritdoc />
    protected override bool OnBackButtonPressed()
    {
        if (!busy && !closing) work = CloseOriginalAsync();
        return true;
    }

    /// <inheritdoc />
    protected override void OnDisappearing()
    {
        translator.PropertyChanged -= OnTranslationChanged;
        if (originalWindow is not null) originalWindow.PropertyChanged -= OnWindowChanged;
        disappeared = true; loaded = false; authority?.Retire();
        // The native operation owns this material until its asynchronous Save finishes.
        // Clear the visible string immediately, but do not dispose its RSA/key buffer underneath it.
        code.Text = string.Empty; recorded.IsToggled = false;
        if (!busy && !closing) ClearDraft();
        base.OnDisappearing();
    }
}
