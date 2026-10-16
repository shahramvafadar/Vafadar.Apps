using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vafadar.Localization;
using Vafadar.Maui.Mvvm;
using Vafadar.Zanance.App.Interaction;

namespace Vafadar.Zanance.App.Features.Commerce.Billing;

/// <summary>Explicit account form; secrets exist only in current inputs/request, never preferences, billing cache or logs.</summary>
public sealed partial class BillingIdentityViewModel(IBillingIdentityEnvironment environment, Translator translator, IAppInteraction interaction) : ViewModelBase
{
    private CancellationTokenSource? visible;
    private long epoch;
    private bool changing;
    private bool reviewing;
    private string? statusKey;
    /// <summary>Current explicit account action; changing it never sends a request.</summary>
    [ObservableProperty] public partial int ModeIndex { get; set; }
    /// <summary>Current request email draft.</summary>
    [ObservableProperty] public partial string Email { get; set; } = "";
    /// <summary>Request-only password, cleared before awaiting network work and on retirement.</summary>
    [ObservableProperty] public partial string Password { get; set; } = "";
    /// <summary>User-supplied one-use public challenge id.</summary>
    [ObservableProperty] public partial string Challenge { get; set; } = "";
    /// <summary>User-supplied one-use secret, never remembered or logged.</summary>
    [ObservableProperty] public partial string Token { get; set; } = "";
    /// <summary>All current validation feedback is shown beside the relevant input.</summary>
    [ObservableProperty] public partial string? EmailError { get; private set; }
    /// <summary>Current password validation feedback.</summary>
    [ObservableProperty] public partial string? PasswordError { get; private set; }
    /// <summary>Current challenge validation feedback.</summary>
    [ObservableProperty] public partial string? ChallengeError { get; private set; }
    /// <summary>Current one-use token validation feedback.</summary>
    [ObservableProperty] public partial string? TokenError { get; private set; }
    /// <summary>Content-free translated outcome; never an exception or recipient-existence claim.</summary>
    [ObservableProperty] public partial string? Status { get; private set; }
    /// <summary>Current actual protected session presence.</summary>
    [ObservableProperty] public partial bool Connected { get; private set; }
    /// <summary>Complete action names, refreshed without reinterpreting typed drafts.</summary>
    public string[] Modes => [translator["BillingIdentity_SignIn"], translator["BillingIdentity_Register"], translator["BillingIdentity_Recover"],
        translator["BillingIdentity_Confirm"], translator["BillingIdentity_Reset"]];
    /// <summary>Email applies to sign-in, registration and recovery only.</summary>
    public bool ShowEmail => ModeIndex < 3;
    /// <summary>Password applies to sign-in, registration and explicit reset only.</summary>
    public bool ShowPassword => ModeIndex is 0 or 1 or 4;
    /// <summary>One-use input is required only for explicit challenge completion.</summary>
    public bool ShowChallenge => ModeIndex >= 3;
    /// <summary>No request can run from an inactive form or an unsupported edition.</summary>
    public bool CanSubmit => visible is not null && environment.Available && !reviewing && !IsBusy;
    /// <summary>Only this current actual session can be explicitly retired.</summary>
    public bool CanSignOut => CanSubmit && Connected;
    /// <summary>The main action always names the actual effect.</summary>
    public string SubmitText => Modes[Math.Clamp(ModeIndex, 0, 4)];

    /// <summary>Captures only original session presence; opening the form never signs in or creates an account.</summary>
    public async Task ActivateAsync()
    {
        epoch++; visible?.Cancel(); visible?.Dispose(); visible = new(); IsBusy = false; RefreshDisplay();
        await RunAsync(async ct => (await environment.ConnectedAsync(ct), statusKey), false);
    }
    /// <summary>Retires observations and clears request-only secrets, preserving all purchases and local financial files.</summary>
    public void Deactivate()
    { epoch++; visible?.Cancel(); visible?.Dispose(); visible = null; Password = ""; Token = ""; IsBusy = false; RefreshDisplay(); }
    /// <summary>Updates action captions without changing input values or mode.</summary>
    public void RefreshDisplay()
    {
        if (statusKey is not null) Status = translator[statusKey];
        if (EmailError is not null) EmailError = translator["BillingIdentity_EmailError"];
        if (PasswordError is not null) PasswordError = translator["BillingIdentity_PasswordError"];
        if (ChallengeError is not null) ChallengeError = translator["BillingIdentity_ChallengeError"];
        if (TokenError is not null) TokenError = translator["BillingIdentity_TokenError"];
        foreach (var name in new[] { nameof(Modes), nameof(ShowEmail), nameof(ShowPassword), nameof(ShowChallenge), nameof(CanSubmit), nameof(CanSignOut), nameof(SubmitText) }) OnPropertyChanged(name);
        SubmitCommand.NotifyCanExecuteChanged(); SignOutCommand.NotifyCanExecuteChanged();
    }
    partial void OnModeIndexChanged(int oldValue, int newValue)
    {
        if (changing) return;
        if (!CanSubmit || newValue is < 0 or > 4)
        { changing = true; try { ModeIndex = oldValue; } finally { changing = false; } return; }
        Password = ""; Token = ""; EmailError = PasswordError = ChallengeError = TokenError = Status = null; statusKey = null; RefreshDisplay();
    }
    [RelayCommand(CanExecute = nameof(CanSubmit))]
    private async Task SubmitAsync()
    {
        var captured = epoch; var mode = ModeIndex; var email = Email.Trim(); var challengeText = Challenge.Trim(); var token = Token.Trim(); var password = Password;
        EmailError = ShowEmail && (email.Length is < 4 or > 254 || !email.Contains('@', StringComparison.Ordinal)) ? translator["BillingIdentity_EmailError"] : null;
        PasswordError = ShowPassword && password.Length is < 12 or > 256 ? translator["BillingIdentity_PasswordError"] : null;
        var validChallenge = Guid.TryParseExact(challengeText, "D", out var challenge) && challenge != Guid.Empty;
        ChallengeError = ShowChallenge && !validChallenge ? translator["BillingIdentity_ChallengeError"] : null;
        TokenError = ShowChallenge && (token.Length is < 1 or > 8192 || token.Any(char.IsControl)) ? translator["BillingIdentity_TokenError"] : null;
        if (EmailError is not null || PasswordError is not null || ChallengeError is not null || TokenError is not null) return;
        if (mode == 1)
        {
            reviewing = true; RefreshDisplay(); bool accepted;
            try { accepted = await interaction.ConfirmAsync(translator["BillingIdentity_Register"], translator["BillingIdentity_Explanation"],
                translator["BillingIdentity_Register"], translator["Common_Cancel"]); }
            finally { reviewing = false; RefreshDisplay(); }
            if (!accepted) return;
        }
        if (captured != epoch || !CanSubmit || mode != ModeIndex) return;
        Password = ""; Token = "";
        await RunAsync(async ct =>
        {
            switch (mode)
            {
                case 0: await environment.SignInAsync(email, password, ct); break;
                case 1: await environment.RegisterAsync(email, password, ct); break;
                case 2: await environment.RecoverAsync(email, ct); break;
                default: await environment.CompleteAsync(challenge, token, mode == 4 ? password : null, ct); break;
            }
            return (await environment.ConnectedAsync(ct), mode is 1 or 2 ? "BillingIdentity_Instructions"
                : mode is 3 or 4 ? "BillingIdentity_Completed" : "BillingIdentity_SignedIn");
        });
    }
    [RelayCommand(CanExecute = nameof(CanSignOut))]
    private async Task SignOutAsync()
    {
        var captured = epoch; reviewing = true; RefreshDisplay(); bool accepted;
        try { accepted = await interaction.ConfirmAsync(translator["BillingIdentity_SignOut"], translator["BillingIdentity_SignOutExplanation"],
            translator["BillingIdentity_SignOut"], translator["Common_Cancel"]); }
        finally { reviewing = false; RefreshDisplay(); }
        if (!accepted || captured != epoch || !CanSignOut) return;
        await RunAsync(async ct =>
        {
            var confirmed = await environment.SignOutAsync(ct);
            return (await environment.ConnectedAsync(ct), confirmed ? "BillingIdentity_SignedOut" : "BillingIdentity_LocalSignOut");
        });
    }
    private async Task RunAsync(Func<CancellationToken, Task<(bool Connected, string? Status)>> work, bool clearStatus = true)
    {
        if (visible is null || IsBusy) return;
        var current = epoch; var ct = visible.Token; IsBusy = true; if (clearStatus) { Status = null; statusKey = null; } RefreshDisplay();
        try
        {
            var result = await work(ct);
            if (current != epoch || ct.IsCancellationRequested) return;
            Connected = result.Connected; statusKey = result.Status; Status = statusKey is null ? null : translator[statusKey];
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { }
        catch (Exception) { if (current == epoch && !ct.IsCancellationRequested) { statusKey = "BillingIdentity_Unavailable"; Status = translator[statusKey]; } }
        finally { if (current == epoch) { IsBusy = false; RefreshDisplay(); } }
    }
}
