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

    /// <summary>Creates only the explicit picker/password/confirmation surface, without touching original data.</summary>
    internal StartupBackupPage(ProtectedBackupRecovery recovery, Translator translator)
    {
        this.recovery = recovery; this.translator = translator;
        heading.Style = (Style)Application.Current!.Resources["PageTitle"];
        hint.Style = (Style)Application.Current.Resources["Secondary"];
        error.SetDynamicResource(Label.TextColorProperty, "ExpenseText");
        back.Style = (Style)Application.Current.Resources["TextButton"];
        // D-80: the file-selection caption must grow with translations and native text scaling.
        save.Style = (Style)Application.Current.Resources["PrimaryButton"];
        Content = new ScrollView { Content = new VerticalStackLayout
            { Padding = 16, Spacing = 12, Children = { back, heading, hint, pick, fileName, passwordCaption, password, error, save } } };
        back.Clicked += async (_, _) => { if (!busy) await CloseAsync(false); };
        pick.Command = new CommunityToolkit.Mvvm.Input.AsyncRelayCommand(OnPickAsync);
        save.Clicked += OnSaveClicked; RefreshText();
    }

    /// <summary>Completes only after explicit recovery staging succeeds or this page is cancelled.</summary>
    internal Task<bool> Completion => completion.Task;

    private async Task OnPickAsync()
    {
        if (busy) return; SetBusy(true); error.IsVisible = false;
        byte[]? loaded = null;
        try
        {
            var result = await FilePicker.Default.PickAsync(new PickOptions { PickerTitle = translator["Backup_PickFile"] });
            if (result is null) return;
            await using var input = await result.OpenReadAsync();
            using var buffer = new MemoryStream(); var chunk = new byte[81920];
            try
            {
                int count;
                while ((count = await input.ReadAsync(chunk)) != 0)
                {
                    if (buffer.Length + count > Vafadar.Backup.Security.DeviceSafetyEncryption.MaximumPackageBytes + 1024L)
                        throw new IOException("Portable recovery file exceeds its bound.");
                    await buffer.WriteAsync(chunk.AsMemory(0, count));
                }
                loaded = buffer.ToArray();
            }
            finally
            {
                CryptographicOperations.ZeroMemory(chunk);
                if (buffer.TryGetBuffer(out var owned)) CryptographicOperations.ZeroMemory(owned.AsSpan());
            }
            if (package is not null) CryptographicOperations.ZeroMemory(package);
            package = loaded; loaded = null; fileName.Text = result.FileName; password.Text = string.Empty;
        }
        catch (Exception failure) when (failure is not OutOfMemoryException) { ShowError(); }
        finally { if (loaded is not null) CryptographicOperations.ZeroMemory(loaded); SetBusy(false); }
    }

    private async void OnSaveClicked(object? sender, EventArgs e)
    {
        if (busy || package is null) return;
        if (!await DisplayAlertAsync(translator["Recovery_FromBackup"], translator["Recovery_BackupWarning"],
            translator["Recovery_Confirm"], translator["Common_Cancel"])) return;
        SetBusy(true); error.IsVisible = false; var entered = password.Text; password.Text = string.Empty;
        try
        {
            await Task.Run(() => recovery.StageAsync(package, string.IsNullOrEmpty(entered) ? null : entered, CancellationToken.None));
            SetBusy(false); await CloseAsync(true);
        }
        catch (Exception failure) when (failure is not OutOfMemoryException) { ShowError(); }
        finally { entered = string.Empty; SetBusy(false); }
    }

    private void ShowError() { error.Text = translator["Recovery_BackupFailed"]; error.IsVisible = true; }
    private void SetBusy(bool value)
    { busy = value; pick.IsEnabled = back.IsEnabled = password.IsEnabled = !value; save.IsEnabled = !value && package is not null; }
    private async Task CloseAsync(bool restored)
    { closing = true; Clear(); await Navigation.PopModalAsync(animated: false); completion.TrySetResult(restored); }
    private void Clear()
    { password.Text = string.Empty; if (package is not null) CryptographicOperations.ZeroMemory(package); package = null; }
    private void OnTranslationChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e) => RefreshText();
    private void RefreshText()
    {
        Title = heading.Text = translator["Recovery_FromBackup"]; hint.Text = translator["Recovery_BackupWarning"];
        pick.Text = translator["Backup_PickFile"]; save.Text = translator["Recovery_Confirm"]; back.Text = translator["Common_Back"];
        passwordCaption.Text = translator["Recovery_BackupPassword"]; SemanticProperties.SetDescription(password, passwordCaption.Text);
        if (error.IsVisible) error.Text = translator["Recovery_BackupFailed"];
    }

    /// <inheritdoc />
    protected override void OnAppearing()
    { base.OnAppearing(); translator.PropertyChanged -= OnTranslationChanged; translator.PropertyChanged += OnTranslationChanged; }
    /// <inheritdoc />
    protected override bool OnBackButtonPressed()
    {
        if (!busy) { Clear(); completion.TrySetResult(false); }
        return busy || base.OnBackButtonPressed();
    }
    /// <inheritdoc />
    protected override void OnDisappearing()
    {
        translator.PropertyChanged -= OnTranslationChanged; password.Text = string.Empty;
        if (!busy && !closing) { Clear(); completion.TrySetResult(false); }
        base.OnDisappearing();
    }
}
