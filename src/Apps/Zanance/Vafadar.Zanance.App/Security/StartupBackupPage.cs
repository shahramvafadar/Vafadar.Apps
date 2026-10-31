using System.Security.Cryptography;
using Vafadar.Localization;

namespace Vafadar.Zanance.App.Security;

/// <summary>Explicit portable recovery input on a covered startup failure; no financial preview or device credential is exposed.</summary>
internal sealed class StartupBackupPage : ContentPage
{
    private readonly ProtectedBackupRecovery recovery;
    private readonly Translator translator;
    private readonly TaskCompletionSource<bool> completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly Label heading = new();
    private readonly Label hint = new();
    private readonly Label fileName = new();
    private readonly Label passwordCaption = new();
    private readonly Entry password = new() { IsPassword = true, FlowDirection = FlowDirection.LeftToRight };
    private readonly Label error = new() { IsVisible = false };
    private readonly Presentation.WrappingAction pick = new() { Appearance = Presentation.ActionAppearance.Secondary };
    private readonly Button save = new() { MinimumHeightRequest = 44, IsEnabled = false };
    private readonly Button back = new() { MinimumHeightRequest = 44 };
    private byte[]? package;
    private bool busy;
    private bool closing;
    private bool retired;
    private bool lifetimeDisposed;
    private bool restored;
    private string? errorKey;
    private readonly CancellationTokenSource lifetime = new();
    private readonly Window? originalWindow;
    private readonly Page? originalRoot;
    private RecoverySetupAuthority? selection;

    /// <summary>Creates only the explicit picker/password/confirmation surface, without touching original data.</summary>
    internal StartupBackupPage(ProtectedBackupRecovery recovery, Translator translator)
    {
        this.recovery = recovery; this.translator = translator;
        originalWindow = Application.Current?.Windows.FirstOrDefault(); originalRoot = originalWindow?.Page;
        heading.Style = (Style)Application.Current!.Resources["PageTitle"];
        hint.Style = (Style)Application.Current.Resources["Secondary"];
        error.SetDynamicResource(Label.TextColorProperty, "ExpenseText");
        back.Style = (Style)Application.Current.Resources["TextButton"];
        // D-80: the file-selection caption must grow with translations and native text scaling.
        save.Style = (Style)Application.Current.Resources["PrimaryButton"];
        Content = new ScrollView { Content = new VerticalStackLayout
            { Padding = 16, Spacing = 12, Children = { back, heading, hint, pick, fileName, passwordCaption, password, error, save } } };
        back.Clicked += async (_, _) => { if (!busy && !closing) await CloseAsync(); };
        pick.Command = new CommunityToolkit.Mvvm.Input.AsyncRelayCommand(OnPickAsync);
        save.Clicked += OnSaveClicked; RefreshText();
    }

    /// <summary>Completes only after explicit recovery staging succeeds or this page is cancelled.</summary>
    internal Task<bool> Completion => completion.Task;

    private bool OwnsPage => !retired && originalWindow is not null && originalRoot is not null
        && ReferenceEquals(originalWindow.Page, originalRoot)
        && ReferenceEquals(originalRoot.Navigation.ModalStack.LastOrDefault(), this);

    private async Task OnPickAsync()
    {
        if (busy || closing || restored || !OwnsPage) return;
        SetBusy(true); error.IsVisible = false; errorKey = null;
        byte[]? loaded = null; RecoverySetupAuthority? original = null;
        try
        {
            original = recovery.CaptureStartupBackupAuthority(lifetime.Token);
            var result = await FilePicker.Default.PickAsync(new PickOptions { PickerTitle = translator["Backup_PickFile"] });
            if (result is null || !OwnsPage) return;
            original.RequireCurrent();
            await using var input = await result.OpenReadAsync();
            original.RequireCurrent();
            loaded = await StartupBackupPackageReader.ReadAsync(input, original.Token);
            if (!OwnsPage) return;
            original.RequireCurrent();
            Clear(); selection = original; original = null;
            package = loaded; loaded = null; fileName.Text = result.FileName;
        }
        catch (Exception failure) when (failure is not OutOfMemoryException)
        { if (OwnsPage) ShowError("Recovery_BackupFailed"); }
        finally
        {
            original?.Dispose();
            if (loaded is not null) CryptographicOperations.ZeroMemory(loaded);
            SetBusy(false);
        }
    }

    private async void OnSaveClicked(object? sender, EventArgs e)
    {
        if (busy || closing || !OwnsPage) return;
        if (restored) { await CloseAsync(); return; }
        if (package is null || selection is null) return;
        var originalPackage = package; var original = selection;
        var title = translator["Recovery_FromBackup"]; var warning = translator["Recovery_BackupWarning"];
        var confirm = translator["Recovery_Confirm"]; var cancel = translator["Common_Cancel"];
        SetBusy(true); error.IsVisible = false; errorKey = null;
        var entered = string.Empty;
        try
        {
            original.RequireCurrent();
            var accepted = await DisplayAlertAsync(title, warning, confirm, cancel);
            if (!accepted || !OwnsPage) return;
            original.RequireCurrent();
            if (!ReferenceEquals(package, originalPackage) || !ReferenceEquals(selection, original)
                || title != translator["Recovery_FromBackup"] || warning != translator["Recovery_BackupWarning"]
                || confirm != translator["Recovery_Confirm"] || cancel != translator["Common_Cancel"])
                throw new IOException("The original recovery disclosure or input changed.");
            entered = password.Text ?? string.Empty; password.Text = string.Empty;
            await Task.Run(() => recovery.StageAsync(originalPackage, string.IsNullOrEmpty(entered) ? null : entered,
                original.Token, original));
            // Only the original returned publication is known staged; native closing never stages it again.
            restored = true; Clear();
        }
        catch (Exception failure) when (failure is not OutOfMemoryException)
        { if (OwnsPage) ShowError("Recovery_BackupFailed"); }
        finally { entered = string.Empty; SetBusy(false); }
        if (restored && OwnsPage) await CloseAsync();
    }

    private void ShowError(string key) { errorKey = key; error.Text = translator[key]; error.IsVisible = true; }
    private void SetBusy(bool value)
    {
        busy = value;
        // Android can retire a modal during a failed native root transaction without Disappearing.
        // Recheck actual ownership when its original asynchronous work returns, not just lifecycle events.
        if (!value && !OwnsPage) Retire();
        // Do not erase the borrowed package underneath an in-flight original staging operation.
        if (!value && retired) FinishRetirement();
        RefreshButtons();
    }
    private void RefreshButtons()
    {
        if (!OwnsPage) return;
        pick.IsEnabled = password.IsEnabled = !busy && !closing && !restored;
        back.IsEnabled = !busy && !closing;
        save.IsEnabled = !busy && !closing && (restored || package is not null);
        save.Text = translator[restored ? "Common_Close" : "Recovery_Confirm"];
    }
    private async Task CloseAsync()
    {
        if (busy || closing || !OwnsPage) return;
        closing = true; RefreshButtons();
        try
        {
            password.Text = string.Empty; await Navigation.PopModalAsync(animated: false); Clear();
            completion.TrySetResult(restored);
        }
        catch (Exception failure) when (failure is not OutOfMemoryException)
        {
            if (OwnsPage) ShowError(restored ? "ActiveItems_SavedReturnFailed" : "Error_Unexpected");
            else
            {
                // A native pop may retire this presentation before its continuation throws.
                // Complete the original waiter only from the actual current root and departed modal.
                var returned = ReferenceEquals(originalWindow?.Page, originalRoot)
                    && originalRoot is not null && !originalRoot.Navigation.ModalStack.Contains(this);
                completion.TrySetResult(restored && returned);
            }
        }
        finally { closing = false; RefreshButtons(); }
    }
    private void Clear()
    {
        password.Text = string.Empty;
        if (package is not null) CryptographicOperations.ZeroMemory(package);
        package = null; selection?.Dispose(); selection = null;
    }
    private void Retire()
    {
        translator.PropertyChanged -= OnTranslationChanged;
        if (originalWindow is not null) originalWindow.PropertyChanged -= OnWindowChanged;
        if (retired) return;
        retired = true; lifetime.Cancel(); password.Text = string.Empty;
        if (!busy) FinishRetirement();
        if (!closing) completion.TrySetResult(false);
    }
    private void FinishRetirement()
    {
        Clear();
        if (!lifetimeDisposed) { lifetimeDisposed = true; lifetime.Dispose(); }
    }
    private void OnWindowChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    { if (e.PropertyName == nameof(Window.Page) && !ReferenceEquals(originalWindow?.Page, originalRoot)) Retire(); }
    private void OnTranslationChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    { if (OwnsPage) RefreshText(); }
    private void RefreshText()
    {
        Title = heading.Text = translator["Recovery_FromBackup"]; hint.Text = translator["Recovery_BackupWarning"];
        pick.Text = translator["Backup_PickFile"]; save.Text = translator[restored ? "Common_Close" : "Recovery_Confirm"]; back.Text = translator["Common_Back"];
        passwordCaption.Text = translator["Recovery_BackupPassword"]; SemanticProperties.SetDescription(password, passwordCaption.Text);
        if (error.IsVisible) error.Text = translator[errorKey ?? "Recovery_BackupFailed"];
    }

    /// <inheritdoc />
    protected override async void OnAppearing()
    {
        base.OnAppearing(); if (retired) return;
        translator.PropertyChanged -= OnTranslationChanged; translator.PropertyChanged += OnTranslationChanged;
        if (originalWindow is not null)
        { originalWindow.PropertyChanged -= OnWindowChanged; originalWindow.PropertyChanged += OnWindowChanged; }
        await Task.Yield(); RefreshButtons();
    }
    /// <inheritdoc />
    protected override bool OnBackButtonPressed()
    { if (!busy && !closing) _ = CloseAsync(); return true; }
    /// <inheritdoc />
    protected override void OnDisappearing()
    { Retire(); base.OnDisappearing(); }
}
