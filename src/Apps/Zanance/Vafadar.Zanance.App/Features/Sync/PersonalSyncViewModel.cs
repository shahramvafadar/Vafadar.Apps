using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vafadar.Localization;
using Vafadar.Localization.Formatting;
using Vafadar.Maui.Mvvm;
using Vafadar.Zanance.App.Interaction;
using Vafadar.Zanance.Core.Sync;
using Vafadar.Zanance.Data.Sync;

namespace Vafadar.Zanance.App.Features.Sync;

/// <summary>An actual service device with a localized activity summary and revocation availability.</summary>
public sealed record PersonalSyncDeviceItem(SyncDevice Source, string Detail)
{
    /// <summary>Gets the owner-chosen device label.</summary>
    public string Name => Source.Name;
    /// <summary>Gets whether the actual service device still has access to revoke.</summary>
    public bool CanRevoke => !Source.Revoked;
}

/// <summary>One original optional personal sync flow; native effects stay behind explicit credential/lifetime ports.</summary>
public sealed partial class PersonalSyncViewModel : ViewModelBase
{
    private readonly PersonalSyncStore store;
    private readonly PersonalSyncCoordinator coordinator;
    private readonly IPersonalSyncEnvironment environment;
    private readonly IAppInteraction interaction;
    private readonly Translator translator;
    private readonly IDateFormatter dates;
    private readonly ILocalizationService localization;
    private CancellationTokenSource? visible;
    private CancellationTokenSource? operation;
    private PersonalSyncTransport? transport;
    private SyncIdentity? identity;
    private SyncDevice? device;
    private PersonalSyncReview? review;
    private PersonalSyncStatus? status;
    private string? failureKey;
    private bool ready;
    private IReadOnlyList<SyncRecord> reviewRemote = [];
    private Func<Task>? closeOnboarding;
    private Func<Task>? finishOnboarding;
    private bool onboardingEligible;

    /// <summary>Creates an empty in-memory screen; no secure state, database or network is read in construction.</summary>
    public PersonalSyncViewModel(PersonalSyncStore store, PersonalSyncCoordinator coordinator, IPersonalSyncEnvironment environment,
        IAppInteraction interaction, Translator translator, IDateFormatter dates, ILocalizationService localization)
    {
        this.store = store; this.coordinator = coordinator; this.environment = environment;
        this.interaction = interaction; this.translator = translator; this.dates = dates; this.localization = localization;
    }
    /// <summary>Gets whether this edition supports explicit local personal sync.</summary>
    public bool Available => environment.Available;
    /// <summary>Gets whether this original screen can start another reviewed operation.</summary>
    public bool CanInteract => CanRead && ready;
    /// <summary>Gets whether a covered initial read or explicit retry can start.</summary>
    public bool CanRead => Available && visible is not null && !IsBusy;
    /// <summary>Gets whether the complete original local connection snapshot has been published.</summary>
    public bool IsReady => ready;
    /// <summary>Gets whether a current actual authenticated service device was resolved.</summary>
    public bool SignedIn => identity is not null && device is not null;
    /// <summary>Gets whether profile connection actions can start.</summary>
    public bool CanConnect => CanInteract && SignedIn;
    /// <summary>Gets whether manual synchronization can run for the actual opted-in original.</summary>
    public bool CanSync => CanConnect && status?.Connected == true && SessionMatchesConnection;
    /// <summary>Gets whether native Back may leave outside a running action.</summary>
    public bool CanLeave => !IsBusy;
    /// <summary>Gets whether this page is an optional modal over the retained first-run wizard.</summary>
    public bool IsOnboardingRestore => finishOnboarding is not null;
    /// <summary>Gets whether fully applied received accounts can finish first run without another account.</summary>
    public bool CanFinishOnboarding => CanInteract && IsOnboardingRestore && onboardingEligible;
    /// <summary>Gets whether the original wizard can be shown again without discarding its draft.</summary>
    public bool CanCloseOnboarding => CanLeave && visible is not null && IsOnboardingRestore;
    /// <summary>Gets whether cancellation or the explicit first-run actions need their fixed footer.</summary>
    public bool ShowFooter => IsBusy || IsOnboardingRestore;
    /// <summary>Attaches native window effects before the first visible read, without touching a profile.</summary>
    public void BeginOnboarding(Func<Task> close, Func<Task> finish)
    {
        if (visible is not null) throw new InvalidOperationException("The first-run mode must be selected before page activation.");
        closeOnboarding = close; finishOnboarding = finish; onboardingEligible = false; RefreshProperties();
    }
    /// <summary>Gets translated content-free failure feedback without exposing service response bodies.</summary>
    public string? Error => failureKey is null ? null : translator[failureKey];
    /// <summary>Gets retained automatic failure feedback without disclosing a service body or clearing local work.</summary>
    public string? AutomaticError => status?.Connected != true ? null : SignedIn && !SessionMatchesConnection
        ? translator["Sync_ConnectionReviewRequired"] : status.Failure switch
    {
        "sign-in-required" => translator["Sync_SignInRequired"],
        "service-unavailable" => translator["Sync_ServiceUnavailable"],
        "connection-review-required" => translator["Sync_ConnectionReviewRequired"],
        _ => null,
    };
    /// <summary>Gets translated complete connection/restore status.</summary>
    public string StatusText => status?.PausedAfterRestore == true ? translator["Sync_RestorePaused"] : status?.Connected == true
        ? translator.Format("Sync_Connected", status.RemoteName ?? "") : translator["Sync_Paused"];
    /// <summary>Gets the actual queue count and last successful local application using the chosen display calendar.</summary>
    public string DetailText => status is null ? "" : NativeDigits.Apply(translator.Format("Sync_StatusDetails", status.PendingChanges,
        status.LastSuccessAt is { } at ? dates.Format(at) : translator["Sync_Never"]))!;
    /// <summary>Gets whether there are no listed compatible personal spaces after a successful discovery.</summary>
    public bool NoProfiles => SignedIn && Profiles.Count == 0;
    /// <summary>Gets unresolved review presence separately from a network outage.</summary>
    public bool HasConflicts => Conflicts.Count != 0;
    /// <summary>Gets the current visible status refresh, so its actual completion can be observed without polling.</summary>
    public Task PendingStatusRefresh { get; private set; } = Task.CompletedTask;
    /// <summary>Gets actual owner-authorized personal spaces, never another user's guessed identifiers.</summary>
    public ObservableCollection<SyncProfile> Profiles { get; } = [];
    /// <summary>Gets actual owner-authorized revocable devices.</summary>
    public ObservableCollection<SyncDevice> Devices { get; } = [];
    /// <summary>Gets localized original device rows without exposing service session identifiers.</summary>
    public ObservableCollection<PersonalSyncDeviceItem> DeviceItems { get; } = [];
    /// <summary>Gets exact retained conflict snapshots; choice commands always revalidate them.</summary>
    public ObservableCollection<PersonalSyncConflictReview> Conflicts { get; } = [];
    /// <summary>Gets complete typed version comparisons; an ambiguous presentation disables its native choice.</summary>
    public ObservableCollection<PersonalSyncConflictItem> ReviewItems { get; } = [];
    /// <summary>Gets or sets the explicit public local service address.</summary>
    [ObservableProperty] public partial string Address { get; set; } = "";
    /// <summary>Gets or sets the manually reviewed public certificate fingerprint.</summary>
    [ObservableProperty] public partial string Fingerprint { get; set; } = "";
    /// <summary>Gets or sets request-only sign-in input; it is not remembered.</summary>
    [ObservableProperty] public partial string Email { get; set; } = "";
    /// <summary>Gets or sets the ephemeral password, cleared before waiting for its request to finish.</summary>
    [ObservableProperty] public partial string Password { get; set; } = "";
    /// <summary>Gets or sets an explicit owner-visible device label without a hardware identifier.</summary>
    [ObservableProperty] public partial string DeviceName { get; set; } = "";
    /// <summary>Gets or sets the name of a new separate remote copy.</summary>
    [ObservableProperty] public partial string NewProfileName { get; set; } = "";

    /// <summary>Attaches only while the actual page is visible; drafts stay in memory across help dialogs.</summary>
    public async Task ActivateAsync()
    {
        visible?.Cancel(); visible?.Dispose(); visible = new();
        translator.PropertyChanged -= OnTranslationChanged; translator.PropertyChanged += OnTranslationChanged;
        environment.StatusChanged -= OnStatusChanged; environment.StatusChanged += OnStatusChanged;
        if (Address.Length == 0)
        { var saved = environment.ReadConnection(); Address = saved.Address; Fingerprint = saved.Fingerprint; }
        if (DeviceName.Length == 0) DeviceName = translator["Sync_DefaultDevice"];
        ready = false;
        await ReloadAsync();
    }
    /// <summary>Retires pending requests and password input; a later page cannot publish this page's old completion.</summary>
    public void Deactivate()
    {
        ClearAttachmentPreview();
        visible?.Cancel(); visible?.Dispose(); visible = null; operation?.Cancel();
        Password = ""; translator.PropertyChanged -= OnTranslationChanged;
        environment.StatusChanged -= OnStatusChanged;
        transport?.Dispose(); transport = null; identity = null; device = null; RefreshProperties();
    }
    /// <summary>Cancels only this screen's pending work; already accepted server work remains durably recoverable.</summary>
    [RelayCommand] private void Cancel() => operation?.Cancel();
    [RelayCommand]
    private Task CloseOnboardingAsync() => CanCloseOnboarding && closeOnboarding is { } close ? close() : Task.CompletedTask;
    [RelayCommand]
    private Task FinishOnboardingAsync() => RunAsync(async (access, ct) =>
    {
        if (!onboardingEligible || finishOnboarding is null || review is null) return;
        var reviewed = review;
        if (!await ConfirmAsync("Sync_FirstRunConfirm", "")) return;
        access.Validate();
        if (!await store.CompleteOnboardingAsync(reviewed, ct))
        { await RefreshLocalAsync(ct); failureKey = "Sync_FirstRunNotReady"; return; }
        access.Validate(); await finishOnboarding();
    });

    /// <summary>Retries only the covered local read; it never signs in or opts a profile in.</summary>
    [RelayCommand]
    private Task ReloadAsync() => RunAsync(async (_, ct) => await RefreshLocalAsync(ct), allowCoveredRead: true);

    [RelayCommand]
    private Task SignInAsync() => RunAsync(async (access, ct) =>
    {
        var peer = new LocalSyncPeer(Address, Fingerprint);
        var password = Password; Password = "";
        transport?.Dispose(); transport = environment.Open(peer, access.Validate);
        identity = await transport.SignInAsync(Email, password, ct); access.Validate();
        device = await transport.RegisterDeviceAsync(DeviceName, ct); access.Validate();
        environment.RememberPeer(peer); await DiscoverCoreAsync(access, ct); await RefreshLocalAsync(ct);
        if (status?.Connected == true) environment.ConnectionChanged();
    });

    /// <summary>Resolves a retained secure session explicitly without submitting another password.</summary>
    [RelayCommand]
    private Task DiscoverAsync() => RunAsync(async (access, ct) =>
    {
        var peer = new LocalSyncPeer(Address, Fingerprint);
        transport?.Dispose(); transport = environment.Open(peer, access.Validate);
        identity = await transport.IdentityAsync(ct); access.Validate();
        try { device = await transport.CurrentDeviceAsync(ct); }
        catch (PersonalSyncHttpException error) when (error.Status == System.Net.HttpStatusCode.NotFound)
        {
            if (!await ConfirmAsync("Sync_RegisterDeviceConfirm", DeviceName)) return;
            access.Validate(); device = await transport.RegisterDeviceAsync(DeviceName, ct);
        }
        access.Validate(); DeviceName = device.Name;
        await DiscoverCoreAsync(access, ct); await RefreshLocalAsync(ct);
    });

    private async Task DiscoverCoreAsync(PersonalSyncAccess access, CancellationToken ct)
    {
        if (transport is null || device is null) throw new InvalidOperationException("The original personal session is unavailable.");
        var profiles = await transport.ProfilesAsync(device.Id, ct); var devices = await transport.DevicesAsync(ct); access.Validate();
        if (profiles.Length > 100 || devices.Length > 100 || profiles.Any(row => row.Id == Guid.Empty || row.SchemaHash != SyncSchema.Hash) ||
            devices.Any(row => row.Id == Guid.Empty)) throw new InvalidDataException("The personal catalog is incompatible.");
        Profiles.Clear(); foreach (var row in profiles) Profiles.Add(row);
        Devices.Clear(); foreach (var row in devices) Devices.Add(row);
        RefreshDeviceItems();
    }

    [RelayCommand]
    private Task CreateProfileAsync() => RunAsync(async (access, ct) =>
    {
        RequireSession(); var name = NewProfileName.Trim();
        if (IsOnboardingRestore) throw new InvalidOperationException("First-run reception selects an existing personal space.");
        if (name.Length is < 1 or > 100) throw new ArgumentException("An explicit bounded personal space name is required.");
        var connection = await store.ReviewAsync(ct); access.Validate();
        if (!await ConfirmAsync("Sync_CreateConfirm", name)) return;
        access.Validate();
        var digest = Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(
            connection.GenerationPath + "\n" + transport!.Peer.BindingKey(identity!.Id) + "\n" + device!.Id.ToString("N") + "\n" + name)));
        var creation = environment.CreationOperation(digest);
        var remote = await transport.CreateProfileAsync(new(device.Id, creation, name, SyncSchema.Hash), ct); access.Validate();
        await ConnectCoreAsync(connection, remote, access, ct); environment.CompleteCreation(digest, creation);
        NewProfileName = ""; await DiscoverCoreAsync(access, ct);
    });

    [RelayCommand]
    private Task ConnectAsync(SyncProfile profile) => RunAsync(async (access, ct) =>
    {
        RequireSession();
        if (!Profiles.Any(row => row == profile)) throw new InvalidOperationException("The reviewed personal space changed.");
        var connection = await store.ReviewAsync(ct); access.Validate();
        if (!await ConfirmAsync(IsOnboardingRestore ? "Sync_FirstRunConnectConfirm" : "Sync_ConnectConfirm", profile.Name)) return;
        access.Validate(); await ConnectCoreAsync(connection, profile, access, ct);
    });
    [RelayCommand]
    private Task RecoverInitializationAsync(SyncProfile profile) => RunAsync(async (access, ct) =>
    {
        RequireSession();
        if (!Profiles.Contains(profile) || !profile.Initializing || profile.Cursor != 0 || profile.BootstrapDeviceId == Guid.Empty)
            throw new InvalidOperationException("A current unpublished initialization must be explicitly reviewed.");
        var connection = await store.ReviewAsync(ct); var selectedDevice = device!.Id; var selectedPeer = transport!.Peer;
        access.Validate();
        if (!await ConfirmAsync("Sync_InitializationConfirm", profile.Name)) return;
        access.Validate(); RequireSession();
        if (device!.Id != selectedDevice || transport!.Peer != selectedPeer || !Profiles.Contains(profile))
            throw new InvalidOperationException("The reviewed initialization authority changed.");
        var claimed = await transport.ClaimInitializationAsync(profile.Id, new(selectedDevice, profile.BootstrapDeviceId), ct);
        access.Validate();
        await store.ConnectAsync(connection, selectedPeer, identity!.Id, selectedDevice, claimed, ct, restartInitialization: true);
        access.Validate(); environment.ConnectionChanged(); await DiscoverCoreAsync(access, ct); await RefreshLocalAsync(ct);
    });
    private async Task ConnectCoreAsync(PersonalSyncReview connection, SyncProfile profile, PersonalSyncAccess access, CancellationToken ct)
    {
        access.Validate();
        await store.ConnectAsync(connection, transport!.Peer, identity!.Id, device!.Id, profile, ct, receiveFirstRun: IsOnboardingRestore); access.Validate();
        environment.ConnectionChanged(); await RefreshLocalAsync(ct);
    }

    [RelayCommand]
    private Task SynchronizeAsync() => RunAsync(async (access, ct) =>
    {
        RequireSession(); access.Validate(); var outcome = await coordinator.SynchronizeAsync(transport!, ct); access.Validate();
        if (outcome.Applied) await environment.PublishAppliedAsync(ct);
        await RefreshLocalAsync(ct); environment.ConnectionChanged();
    });

    [RelayCommand]
    private Task PauseAsync() => RunAsync(async (access, ct) =>
    {
        var connection = review ?? await store.ReviewAsync(ct);
        if (!await ConfirmAsync("Sync_PauseConfirm", "")) return;
        access.Validate(); await store.PauseAsync(connection, ct); environment.ConnectionChanged(); await RefreshLocalAsync(ct);
    });

    [RelayCommand]
    private Task SignOutAsync() => RunAsync(async (access, ct) =>
    {
        var connection = await store.ReviewAsync(ct); access.Validate();
        var peer = transport?.Peer ?? new LocalSyncPeer(Address, Fingerprint);
        if (!await ConfirmAsync("Sync_SignOutConfirm", "")) return;
        access.Validate(); await store.PauseAsync(connection, ct);
        using var client = environment.Open(peer, access.Validate); await client.ForgetAsync(ct);
        transport?.Dispose(); transport = null; identity = null; device = null; Password = ""; Email = "";
        Profiles.Clear(); Devices.Clear(); DeviceItems.Clear(); environment.ConnectionChanged(); await RefreshLocalAsync(ct);
    });

    [RelayCommand]
    private Task RevokeAsync(SyncDevice target) => RunAsync(async (access, ct) =>
    {
        RequireSession(); if (target.Revoked || !Devices.Any(row => row == target)) return;
        if (!await ConfirmAsync("Sync_RevokeConfirm", target.Name)) return;
        access.Validate(); await transport!.RevokeDeviceAsync(target.Id, ct);
        if (device!.Id == target.Id)
        {
            await transport.ForgetAsync(ct); await store.PauseAsync(await store.ReviewAsync(ct), ct);
            identity = null; device = null; Profiles.Clear(); Devices.Clear(); DeviceItems.Clear(); environment.ConnectionChanged();
        }
        else await DiscoverCoreAsync(access, ct);
        await RefreshLocalAsync(ct);
    });

    [RelayCommand] private Task KeepLocalAsync(PersonalSyncConflictReview conflict) => ChooseAsync(conflict, true);
    [RelayCommand] private Task KeepRemoteAsync(PersonalSyncConflictReview conflict) => ChooseAsync(conflict, false);
    private Task ChooseAsync(PersonalSyncConflictReview conflict, bool local) => RunAsync(async (access, ct) =>
    {
        if (!Conflicts.Contains(conflict)) throw new InvalidOperationException("The personal review was retired.");
        var displayed = ReviewItems.SingleOrDefault(item => item.Review == conflict);
        if (displayed is null || !(local ? displayed.CanChooseLocal : displayed.CanChooseRemote))
            throw new InvalidOperationException("The personal record cannot be completely reviewed.");
        var confirmation = conflict.Conflict.LinkedGroup is not null
            ? local ? "Sync_KeepLocalGroupConfirm" : "Sync_KeepRemoteGroupConfirm"
            : local ? "Sync_KeepLocalConfirm" : "Sync_KeepRemoteConfirm";
        if (!await ConfirmAsync(confirmation, "")) return;
        access.Validate(); await store.ResolveAsync(conflict, local, ct); await RefreshLocalAsync(ct); environment.ConnectionChanged();
    });

    [RelayCommand]
    private Task RestartReviewAsync() => RunAsync(async (access, ct) =>
    {
        RequireSession(); if (!await ConfirmAsync("Sync_RestartConfirm", "")) return;
        access.Validate(); await coordinator.RestartReviewAsync(transport!, ct); await RefreshLocalAsync(ct); environment.ConnectionChanged();
    });

    private async Task RefreshLocalAsync(CancellationToken ct)
    {
        var nextReview = await store.ReviewAsync(ct); var nextStatus = await store.StatusAsync(ct); var conflicts = await store.ConflictsAsync(ct);
        var prepared = await store.PrepareReviewsAsync(conflicts, ct);
        var eligible = IsOnboardingRestore && await store.CanCompleteOnboardingAsync(nextReview, ct);
        ct.ThrowIfCancellationRequested(); review = nextReview; status = nextStatus;
        reviewRemote = prepared.RemoteRecords;
        onboardingEligible = eligible;
        Conflicts.Clear(); foreach (var conflict in prepared.Reviews) Conflicts.Add(conflict);
        if (previewReview is { } previous && !Conflicts.Any(item => AttachmentReviewKey(item) == AttachmentReviewKey(previous))) ClearAttachmentPreview();
        RefreshReviewItems();
        ready = true;
    }
    private async Task RunAsync(Func<PersonalSyncAccess, CancellationToken, Task> action, bool allowCoveredRead = false)
    {
        if (!(allowCoveredRead ? CanRead : CanInteract)) return;
        IsBusy = true; failureKey = null; RefreshProperties();
        try
        {
            var access = environment.Capture();
            operation = CancellationTokenSource.CreateLinkedTokenSource(access.Cancellation, visible!.Token);
            if (transport is { } previous)
            {
                transport = environment.Open(previous.Peer, access.Validate); previous.Dispose();
            }
            await action(access, operation.Token); access.Validate();
        }
        catch (OperationCanceledException) { }
        catch (PersonalSyncHttpException error)
        { failureKey = error.Status == System.Net.HttpStatusCode.Unauthorized ? "Sync_SignInRequired" : "Sync_ServiceUnavailable"; }
        catch (ArgumentException) { failureKey = "Sync_CheckInputs"; }
        catch (HttpRequestException) { failureKey = "Sync_ServiceUnavailable"; }
        catch (Exception error) when (error is not OutOfMemoryException)
        { failureKey = "Sync_ActionFailed"; }
        finally
        {
            Password = ""; operation?.Dispose(); operation = null; IsBusy = false; RefreshProperties();
        }
    }
    private void RequireSession()
    {
        if (transport is null || identity is null || device is null || transport.Peer.Address.AbsoluteUri != new LocalSyncPeer(Address, Fingerprint).Address.AbsoluteUri ||
            transport.Peer.Fingerprint != new LocalSyncPeer(Address, Fingerprint).Fingerprint)
            throw new InvalidOperationException("The reviewed personal peer/session changed.");
    }
    private bool SessionMatchesConnection => transport is not null && identity is not null && device is not null &&
        review?.DeviceId == device.Id && review.IdentityId == identity.Id && review.PeerKey == transport.Peer.BindingKey(identity.Id);
    private Task<bool> ConfirmAsync(string key, string argument) => interaction.ConfirmAsync(translator["Sync_Title"],
        translator.Format(key, argument), translator["Sync_Confirm"], translator["Common_Cancel"]);
    private void OnTranslationChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    { RefreshDeviceItems(); RefreshReviewItems(); RefreshProperties(); }
    private void RefreshReviewItems()
    {
        var presenter = new PersonalSyncReviewPresenter(translator, dates, localization);
        ReviewItems.Clear();
        var groups = new HashSet<Guid>();
        foreach (var conflict in Conflicts)
            if (conflict.Conflict.LinkedGroup is not { } group || groups.Add(group))
            {
                var item = presenter.Create(conflict, reviewRemote);
                ReviewItems.Add(item);
            }
    }
    private void OnStatusChanged(object? sender, EventArgs e)
    {
        if (CanRead) PendingStatusRefresh = ReloadAsync();
    }
    private void RefreshDeviceItems()
    {
        DeviceItems.Clear();
        foreach (var row in Devices)
            DeviceItems.Add(new(row, row.Revoked ? translator["Sync_DeviceRevoked"] :
                NativeDigits.Apply(translator.Format("Sync_DeviceSeen", dates.Format(row.LastSeenAt)))!));
    }
    private void RefreshProperties()
    {
        RefreshAttachmentProperties();
        foreach (var property in new[] { nameof(Available), nameof(CanRead), nameof(IsReady), nameof(CanInteract), nameof(CanConnect), nameof(CanSync), nameof(SignedIn), nameof(CanLeave),
            nameof(Error), nameof(AutomaticError), nameof(StatusText), nameof(DetailText), nameof(NoProfiles), nameof(HasConflicts) }) OnPropertyChanged(property);
        foreach (var property in new[] { nameof(IsOnboardingRestore), nameof(CanFinishOnboarding), nameof(CanCloseOnboarding), nameof(ShowFooter) }) OnPropertyChanged(property);
    }
}
