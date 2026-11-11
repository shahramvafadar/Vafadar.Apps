using System.Collections.ObjectModel;
using System.Collections.Specialized;
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
    private bool catalogReady;
    private bool publishingDeviceItems;
    private IReadOnlyList<SyncRecord> reviewRemote = [];
    private Func<Task>? closeOnboarding;
    private Func<Task>? finishOnboarding;
    private bool onboardingEligible;
    private bool commandActive;
    private bool idleFeedbackPending;
    private bool restoringIdleFeedback;
    private bool readingLocal, rootRetired, closingOnboarding;
    private long disclosureVersion;
    private long activationVersion;
    private Func<bool> ownsRoot = () => true;
    private Action? operationOwner;
    private TaskCompletionSource? commandCompletion;
    private bool acceptedOperation;
    private bool connectionDefaultsRead;

    /// <summary>Creates an empty in-memory screen; no secure state, database or network is read in construction.</summary>
    public PersonalSyncViewModel(PersonalSyncStore store, PersonalSyncCoordinator coordinator, IPersonalSyncEnvironment environment,
        IAppInteraction interaction, Translator translator, IDateFormatter dates, ILocalizationService localization)
    {
        this.store = store; this.coordinator = coordinator; this.environment = environment;
        this.interaction = interaction; this.translator = translator; this.dates = dates; this.localization = localization;
        // Retained native row collections must not retain a retired model through its invalidation observer.
        var owner = new WeakReference<PersonalSyncViewModel>(this);
        NotifyCollectionChangedEventHandler changed = (_, _) =>
        { if (owner.TryGetTarget(out var original)) original.OnCatalogChanged(); };
        Profiles.CollectionChanged += changed; Devices.CollectionChanged += changed; DeviceItems.CollectionChanged += changed;
    }
    /// <summary>Gets whether this edition supports explicit local personal sync.</summary>
    public bool Available => environment.Available;
    /// <summary>Gets whether this original screen can start another reviewed operation.</summary>
    public bool CanInteract => CanRead && ready;
    /// <summary>Gets whether a covered initial read or explicit retry can start.</summary>
    public bool CanRead => Available && OwnsPresentation && (!IsBusy || idleFeedbackPending) && !commandActive && !closingOnboarding;
    /// <summary>Gets actual operation ownership independently of shared status feedback.</summary>
    public bool HasPendingOperation => commandActive;
    /// <summary>Gets whether the complete original local connection snapshot has been published.</summary>
    public bool IsReady => ready;
    /// <summary>Gets whether the authenticated service device and its complete original catalog were published.</summary>
    public bool SignedIn => catalogReady && identity is not null && device is not null;
    /// <summary>Gets whether profile connection actions can start.</summary>
    public bool CanConnect => CanInteract && SignedIn && !IsHouseholdConnection;
    /// <summary>The current actual household transport must retain its encrypted route even when this personal page is opened.</summary>
    public bool IsHouseholdConnection => review?.TransportKind == ProfileSynchronizationKind.Household;
    /// <summary>The original review surface follows the actual bound encrypted household, never a selectable transport override.</summary>
    public string PageTitle => translator[IsHouseholdConnection ? "Household_Review_Title" : "Sync_Title"];
    /// <summary>Household decisions compare already verified local snapshots; service sign-in/connection stays on the encrypted household page.</summary>
    public string IntroText => translator[IsHouseholdConnection ? "Sync_ReviewHint" : "Sync_Intro"];
    /// <summary>The original page's explanations follow its actual financial binding.</summary>
    public string HelpTopic => IsHouseholdConnection ? "HouseholdReview" : "PersonalSync";
    /// <summary>Readable personal-service setup never appears in a household review.</summary>
    public bool ShowPersonalConnection => !IsHouseholdConnection;
    /// <summary>Personal discovery panels remain stable during a submitted operation, while never appearing for a household binding.</summary>
    public bool ShowPersonalSignedIn => SignedIn && ShowPersonalConnection;
    /// <summary>Explicit household empty feedback is separate from a missing original connection.</summary>
    public bool HouseholdReviewEmpty => IsHouseholdConnection && !HasConflicts;
    /// <summary>Gets whether manual synchronization can run for the actual opted-in original.</summary>
    public bool CanSync => CanConnect && status?.Connected == true && SessionMatchesConnection;
    /// <summary>Gets whether native Back may leave outside a running action.</summary>
    public bool CanLeave => !closingOnboarding && (commandActive ? readingLocal : !IsBusy || idleFeedbackPending);
    /// <summary>Gets whether this page is an optional modal over the retained first-run wizard.</summary>
    public bool IsOnboardingRestore => finishOnboarding is not null;
    /// <summary>Gets whether fully applied received accounts can finish first run without another account.</summary>
    public bool CanFinishOnboarding => CanInteract && IsOnboardingRestore && onboardingEligible;
    /// <summary>Gets whether the original wizard can be shown again without discarding its draft.</summary>
    public bool CanCloseOnboarding => CanLeave && visible is not null && IsOnboardingRestore;
    /// <summary>Gets whether cancellation or the explicit first-run actions need their fixed footer.</summary>
    public bool ShowFooter => commandActive || IsBusy || IsOnboardingRestore;
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
    public string StatusText => IsHouseholdConnection ? translator["Household_PersonalRouteHint"] : status?.PausedAfterRestore == true ? translator["Sync_RestorePaused"] : status?.Connected == true
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
        if (rootRetired || !ownsRoot()) return;
        if (!RestoreIdleFeedback()) return;
        if (rootRetired || !ownsRoot()) return;
        if (OwnsPresentation)
        {
            if (commandActive) { if (commandCompletion is { } pending) await pending.Task; return; }
            if (!ready) await ReloadAsync();
            return;
        }
        var activation = ++activationVersion;
        operation?.Cancel();
        if (commandCompletion is { } retiring) await retiring.Task;
        if (activation != activationVersion || rootRetired || !ownsRoot()) return;
        visible?.Dispose(); visible = new();
        translator.PropertyChanged -= OnTranslationChanged; translator.PropertyChanged += OnTranslationChanged;
        environment.StatusChanged -= OnStatusChanged; environment.StatusChanged += OnStatusChanged;
        if (string.IsNullOrEmpty(DeviceName)) DeviceName = translator["Sync_DefaultDevice"];
        ready = false;
        await ReloadAsync();
    }
    /// <summary>Binds publication and native commands to the actual original window or first-run modal.</summary>
    public void BindRootOwner(Func<bool> current) => ownsRoot = current ?? throw new ArgumentNullException(nameof(current));
    /// <summary>Permanently retires an original window; a later appearance cannot revive its pending consent.</summary>
    public void RetireRoot() { if (rootRetired) return; rootRetired = true; Deactivate(); }
    private bool OwnsPresentation => !rootRetired && visible is not null && ownsRoot();
    /// <summary>Retires pending requests and password input; a later page cannot publish this page's old completion.</summary>
    public void Deactivate()
    {
        activationVersion++;
        // D-1047: revoke publication before any cancellation or native feedback can call back.
        var retiringVisibility = visible; visible = null; catalogReady = false;
        var retiringTransport = transport; transport = null; identity = null; device = null;
        translator.PropertyChanged -= OnTranslationChanged;
        environment.StatusChanged -= OnStatusChanged;
        Exception? firstFailure = null;
        void Retire(Action action)
        {
            try { action(); }
            catch (Exception error) when (error is not OutOfMemoryException) { firstFailure ??= error; }
        }
        Retire(() => retiringVisibility?.Cancel());
        Retire(() => retiringVisibility?.Dispose());
        Retire(() => operation?.Cancel());
        Retire(() => retiringTransport?.Dispose());
        Retire(ClearRetiringPassword);
        Retire(ClearAttachmentPreview);
        Retire(RefreshProperties);
        // Keep the first actual failure, after every independent retirement has been attempted.
        if (firstFailure is not null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(firstFailure).Throw();
    }
    /// <summary>Cancels only this screen's pending work; already accepted server work remains durably recoverable.</summary>
    [RelayCommand] private void Cancel() => operation?.Cancel();
    [RelayCommand]
    private async Task CloseOnboardingAsync()
    {
        if (!CanCloseOnboarding || !OwnsPresentation || closeOnboarding is not { } close) return;
        closingOnboarding = true;
        try { Deactivate(); if (!rootRetired && ownsRoot()) await close(); }
        catch (Exception error) when (error is not OutOfMemoryException)
        {
            // If the actual modal remains, restore a covered read opportunity rather than replaying Close.
            if (!rootRetired && ownsRoot())
            {
                visible = new(); ready = false; failureKey = "Sync_ActionFailed";
                translator.PropertyChanged -= OnTranslationChanged; translator.PropertyChanged += OnTranslationChanged;
                environment.StatusChanged -= OnStatusChanged; environment.StatusChanged += OnStatusChanged;
            }
        }
        finally
        {
            closingOnboarding = false;
            try { RefreshProperties(); }
            catch (Exception error) when (error is not OutOfMemoryException) { ReportFeedbackFailure(error); }
        }
    }
    [RelayCommand]
    private Task FinishOnboardingAsync() => RunAsync(async (access, ct) =>
    {
        if (!onboardingEligible || finishOnboarding is null || review is null) return;
        var reviewed = review;
        if (!await ConfirmAsync("Sync_FirstRunConfirm", "")) return;
        access.Validate();
        if (!await store.CompleteOnboardingAsync(reviewed, ct, access.Validate))
        { await RefreshLocalAsync(ct); failureKey = "Sync_FirstRunNotReady"; return; }
        AcceptKnownOperation();
        access.Validate(); await finishOnboarding();
    });

    /// <summary>Retries only the covered local read; it never signs in or opts a profile in.</summary>
    [RelayCommand]
    private Task ReloadAsync() => RunAsync(async (_, ct) =>
    {
        // Native clearing can publish null; only a completely missing first connection borrows public defaults.
        if (!connectionDefaultsRead && string.IsNullOrEmpty(Address) && string.IsNullOrEmpty(Fingerprint))
        { var saved = environment.ReadConnection(); Address = saved.Address; Fingerprint = saved.Fingerprint; }
        connectionDefaultsRead = true;
        await RefreshLocalAsync(ct);
    }, allowCoveredRead: true);
    /// <summary>Opens the original encrypted household flow without changing or relabeling this profile binding.</summary>
    [RelayCommand]
    private Task OpenHouseholdAsync() => CanInteract && IsHouseholdConnection && !IsOnboardingRestore
        ? interaction.NavigateAsync(AppRoutes.HouseholdRoute) : Task.CompletedTask;

    [RelayCommand]
    private Task SignInAsync()
    {
        if (!CanInteract) return Task.CompletedTask;
        // D-1060: busy/access feedback cannot replace the request that invoked this command.
        var inputs = (Address, Fingerprint, Email, DeviceName);
        var password = Password;
        return RunAsync(async (access, ct) =>
        {
            void ValidateInputs()
            {
                access.Validate();
                if (inputs != (Address, Fingerprint, Email, DeviceName))
                    throw new OperationCanceledException("The original sign-in inputs changed.");
            }
            RequirePersonalConnection();
            ValidateInputs();
            if (password != Password) throw new OperationCanceledException("The original request input retired.");
            var peer = new LocalSyncPeer(inputs.Address, inputs.Fingerprint);
            Password = "";
            ValidateInputs();
            catalogReady = false;
            transport?.Dispose(); identity = null; device = null;
            ValidateInputs();
            var client = environment.Open(peer, access.Validate); transport = client;
            ValidateInputs();
            var signedIn = await client.SignInAsync(inputs.Email, password, ct); ValidateInputs();
            identity = signedIn;
            var registered = await client.RegisterDeviceAsync(inputs.DeviceName, ct); ValidateInputs();
            device = registered;
            environment.RememberPeer(peer); await DiscoverCoreAsync(access, ct); await RefreshLocalAsync(ct);
            if (status?.Connected == true) environment.ConnectionChanged();
        });
    }

    /// <summary>Resolves a retained secure session explicitly without submitting another password.</summary>
    [RelayCommand]
    private Task DiscoverAsync()
    {
        if (!CanInteract) return Task.CompletedTask;
        // D-1062: callbacks cannot replace the peer or device label before stored-session discovery.
        var inputs = (Address, Fingerprint, DeviceName);
        return RunAsync(async (access, ct) =>
        {
            void ValidateInputs()
            {
                access.Validate();
                if (inputs != (Address, Fingerprint, DeviceName))
                    throw new OperationCanceledException("The original discovery inputs changed.");
            }
            RequirePersonalConnection();
            ValidateInputs();
            var peer = new LocalSyncPeer(inputs.Address, inputs.Fingerprint);
            catalogReady = false;
            transport?.Dispose(); identity = null; device = null;
            ValidateInputs();
            var client = environment.Open(peer, access.Validate); transport = client;
            ValidateInputs();
            var signedIn = await client.IdentityAsync(ct); ValidateInputs();
            identity = signedIn;
            SyncDevice found;
            try { found = await client.CurrentDeviceAsync(ct); ValidateInputs(); }
            catch (PersonalSyncHttpException error) when (error.Status == System.Net.HttpStatusCode.NotFound)
            {
                ValidateInputs();
                if (!await ConfirmAsync("Sync_RegisterDeviceConfirm", inputs.DeviceName)) return;
                ValidateInputs(); found = await client.RegisterDeviceAsync(inputs.DeviceName, ct); ValidateInputs();
            }
            ValidateInputs();
            device = found; DeviceName = found.Name;
            await DiscoverCoreAsync(access, ct); await RefreshLocalAsync(ct);
        });
    }

    private async Task DiscoverCoreAsync(PersonalSyncAccess access, CancellationToken ct)
    {
        catalogReady = false; RefreshProperties();
        if (transport is null || device is null) throw new InvalidOperationException("The original personal session is unavailable.");
        var profiles = await transport.ProfilesAsync(device.Id, ct); access.Validate();
        var devices = await transport.DevicesAsync(ct); access.Validate();
        PublishCatalog(profiles, devices, access.Validate);
    }

    /// <summary>Grants catalog authority only after complete original rows, captions and ownership survive publication.</summary>
    internal void PublishCatalog(SyncProfile[] profiles, SyncDevice[] devices, Action validate)
    {
        // D-1064: a local-only Reload cannot authorize rows from an interrupted remote publication.
        catalogReady = false;
        try
        {
            validate();
            if (profiles.Length > 100 || devices.Length > 100 || profiles.Any(row => row.Id == Guid.Empty || row.SchemaHash != SyncSchema.Hash) ||
                devices.Any(row => row.Id == Guid.Empty)) throw new InvalidDataException("The personal catalog is incompatible.");
            var items = devices.Select(CreateDeviceItem).ToArray();
            Profiles.Clear(); validate();
            foreach (var row in profiles) { Profiles.Add(row); validate(); }
            Devices.Clear(); validate();
            foreach (var row in devices) { Devices.Add(row); validate(); }
            DeviceItems.Clear(); validate();
            foreach (var item in items) { DeviceItems.Add(item); validate(); }
            void ValidatePublished()
            {
                validate();
                if (!Profiles.SequenceEqual(profiles) || !Devices.SequenceEqual(devices) || !DeviceItems.SequenceEqual(items))
                    throw new InvalidDataException("The original catalog publication changed.");
            }
            ValidatePublished(); catalogReady = true;
            RefreshProperties(); ValidatePublished();
        }
        catch { catalogReady = false; throw; }
    }
    /// <summary>Revokes authority before a retained binding can reuse altered published rows.</summary>
    private void OnCatalogChanged()
    {
        if (!catalogReady) return;
        catalogReady = false;
        RefreshProperties();
    }

    [RelayCommand]
    private Task CreateProfileAsync()
    {
        if (!CanInteract) return Task.CompletedTask;
        var inputs = (Address, Fingerprint, NewProfileName);
        var awaitingRemote = true;
        void ValidateRequest()
        {
            if (!awaitingRemote) return;
            RequireSession();
            if (inputs != (Address, Fingerprint, NewProfileName))
                throw new OperationCanceledException("The original personal creation inputs changed.");
        }
        return RunAsync(async (access, ct) =>
        {
            var name = inputs.NewProfileName.Trim();
            if (IsOnboardingRestore) throw new InvalidOperationException("First-run reception selects an existing personal space.");
            if (name.Length is < 1 or > 100) throw new ArgumentException("An explicit bounded personal space name is required.");
            var connection = await store.ReviewAsync(ct); access.Validate();
            if (!await ConfirmAsync("Sync_CreateConfirm", name)) return;
            access.Validate();
            var digest = Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(
                connection.GenerationPath + "\n" + transport!.Peer.BindingKey(identity!.Id) + "\n" + device!.Id.ToString("N") + "\n" + name)));
            var creation = environment.CreationOperation(digest); access.Validate();
            var remote = await transport!.CreateProfileAsync(new(device!.Id, creation, name, SyncSchema.Hash), ct);
            // D-1067: an acknowledged remote request owns its original intent, not a newly typed name.
            awaitingRemote = false; access.Validate();
            void ValidateConnection() { access.Validate(); RequireSession(); }
            var connectionAccess = access with { Validate = ValidateConnection };
            await ConnectCoreAsync(connection, remote, connectionAccess, ct); environment.CompleteCreation(digest, creation);
            if (NewProfileName == inputs.NewProfileName) NewProfileName = "";
            await DiscoverCoreAsync(access, ct);
        }, validateRequest: ValidateRequest);
    }

    [RelayCommand]
    private Task ConnectAsync(SyncProfile profile) => RunAsync(async (access, ct) =>
    {
        void ValidateSelection()
        {
            access.Validate(); RequireSession();
            if (!Profiles.Any(row => row == profile)) throw new OperationCanceledException("The reviewed personal space changed.");
        }
        // D-1066: the reviewed catalog must still own both the consent answer and the original writer's commit.
        var selectedAccess = access with { Validate = ValidateSelection };
        selectedAccess.Validate();
        var connection = await store.ReviewAsync(ct); selectedAccess.Validate();
        if (!await ConfirmAsync(IsOnboardingRestore ? "Sync_FirstRunConnectConfirm" : "Sync_ConnectConfirm", profile.Name)) return;
        selectedAccess.Validate(); await ConnectCoreAsync(connection, profile, selectedAccess, ct);
    });
    [RelayCommand]
    private Task RecoverInitializationAsync(SyncProfile profile)
    {
        if (!CanInteract) return Task.CompletedTask;
        var selectedDevice = device?.Id;
        var selectedPeer = transport?.Peer;
        var selectedIdentity = identity?.Id;
        var awaitingConnection = true;
        void ValidateSelection()
        {
            if (!awaitingConnection) return;
            RequireSession();
            if (device!.Id != selectedDevice || transport!.Peer != selectedPeer || identity!.Id != selectedIdentity ||
                !Profiles.Contains(profile) || !profile.Initializing || profile.Cursor != 0 || profile.BootstrapDeviceId == Guid.Empty)
                throw new InvalidOperationException("The reviewed initialization authority changed.");
        }
        return RunAsync(async (access, ct) =>
        {
            var connection = await store.ReviewAsync(ct); access.Validate();
            if (!await ConfirmAsync("Sync_InitializationConfirm", profile.Name)) return;
            access.Validate();
            var claimed = await transport!.ClaimInitializationAsync(profile.Id, new(selectedDevice!.Value, profile.BootstrapDeviceId), ct);
            access.Validate();
            // D-1074: claim acknowledgement transfers only bootstrap ownership, never the reviewed space or its contents.
            if (claimed != profile with { BootstrapDeviceId = selectedDevice.Value })
                throw new InvalidDataException("The initialization response differs from its original review.");
            await store.ConnectAsync(connection, selectedPeer!, selectedIdentity!, selectedDevice.Value, claimed, ct,
                restartInitialization: true, validateOriginal: access.Validate);
            // The original transaction accepted this connection; catalog refresh must not invalidate its known receipt.
            awaitingConnection = false; AcceptKnownOperation();
            access.Validate(); environment.ConnectionChanged(); await DiscoverCoreAsync(access, ct); await RefreshLocalAsync(ct);
        }, validateRequest: ValidateSelection);
    }
    private async Task ConnectCoreAsync(PersonalSyncReview connection, SyncProfile profile, PersonalSyncAccess access, CancellationToken ct)
    {
        access.Validate();
        await store.ConnectAsync(connection, transport!.Peer, identity!.Id, device!.Id, profile, ct,
            receiveFirstRun: IsOnboardingRestore, validateOriginal: access.Validate);
        AcceptKnownOperation(); access.Validate();
        environment.ConnectionChanged(); await RefreshLocalAsync(ct);
    }

    [RelayCommand]
    private Task SynchronizeAsync() => RunAsync(async (access, ct) =>
    {
        RequireSession(); access.Validate(); var outcome = await coordinator.SynchronizeAsync(transport!, ct);
        AcceptKnownOperation(); access.Validate();
        if (outcome.Applied) await environment.PublishAppliedAsync(ct);
        await RefreshLocalAsync(ct); environment.ConnectionChanged();
    });

    [RelayCommand]
    private Task PauseAsync() => RunAsync(async (access, ct) =>
    {
        RequirePersonalConnection();
        var connection = review ?? await store.ReviewAsync(ct);
        if (!await ConfirmAsync("Sync_PauseConfirm", "")) return;
        access.Validate(); await store.PauseAsync(connection, ct, access.Validate);
        AcceptKnownOperation(); environment.ConnectionChanged(); await RefreshLocalAsync(ct);
    });

    [RelayCommand]
    private Task SignOutAsync() => RunAsync(async (access, ct) =>
    {
        RequirePersonalConnection();
        var connection = await store.ReviewAsync(ct); access.Validate();
        var peer = transport?.Peer ?? new LocalSyncPeer(Address, Fingerprint);
        if (!await ConfirmAsync("Sync_SignOutConfirm", "")) return;
        access.Validate(); await store.PauseAsync(connection, ct, access.Validate); AcceptKnownOperation();
        using var client = environment.Open(peer, access.Validate); await client.ForgetAsync(ct);
        transport?.Dispose(); transport = null; identity = null; device = null; Password = ""; Email = "";
        Profiles.Clear(); Devices.Clear(); DeviceItems.Clear(); environment.ConnectionChanged(); await RefreshLocalAsync(ct);
    });

    [RelayCommand]
    private Task RevokeAsync(SyncDevice target)
    {
        if (!CanInteract) return Task.CompletedTask;
        var selectedDevice = device?.Id; var selectedPeer = transport?.Peer; var selectedIdentity = identity?.Id;
        var awaitingRevocation = true;
        void ValidateSelection()
        {
            if (!awaitingRevocation) return;
            RequireSession();
            if (device!.Id != selectedDevice || transport!.Peer != selectedPeer || identity!.Id != selectedIdentity ||
                target.Revoked || !Devices.Contains(target) || !DeviceItems.Any(item => item.Source == target && item.CanRevoke))
                throw new OperationCanceledException("The original device review changed.");
        }
        return RunAsync(async (access, ct) =>
        {
            if (!await ConfirmAsync("Sync_RevokeConfirm", target.Name)) return;
            access.Validate();
            await transport!.RevokeDeviceAsync(target.Id, ct);
            // The acknowledged revocation owns its follow-up even when discovery replaces its device catalog.
            awaitingRevocation = false; AcceptKnownOperation(); access.Validate();
            if (selectedDevice == target.Id)
            {
                await transport.ForgetAsync(ct); await store.PauseAsync(await store.ReviewAsync(ct), ct, access.Validate);
                identity = null; device = null; Profiles.Clear(); Devices.Clear(); DeviceItems.Clear(); environment.ConnectionChanged();
            }
            else await DiscoverCoreAsync(access, ct);
            await RefreshLocalAsync(ct);
        }, validateRequest: ValidateSelection);
    }

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
        access.Validate();
        if (!Conflicts.Contains(conflict)) throw new OperationCanceledException("The original conflict row retired.");
        await store.ResolveAsync(conflict, local, ct, access.Validate); AcceptKnownOperation();
        await RefreshLocalAsync(ct); environment.ConnectionChanged();
    });

    [RelayCommand]
    private Task RestartReviewAsync() => RunAsync(async (access, ct) =>
    {
        RequireSession(); if (!await ConfirmAsync("Sync_RestartConfirm", "")) return;
        access.Validate(); await coordinator.RestartReviewAsync(transport!, ct); AcceptKnownOperation();
        await RefreshLocalAsync(ct); environment.ConnectionChanged();
    });

    private async Task RefreshLocalAsync(CancellationToken ct)
    {
        var validate = operationOwner ?? throw new OperationCanceledException("The original sync operation retired.");
        validate(); ready = false; RefreshProperties();
        var workspace = await store.ReadWorkspaceAsync(IsOnboardingRestore, ct);
        ct.ThrowIfCancellationRequested(); validate(); review = workspace.Connection; status = workspace.Status;
        reviewRemote = workspace.Review.RemoteRecords;
        onboardingEligible = workspace.CanCompleteOnboarding;
        Conflicts.Clear(); foreach (var conflict in workspace.Review.Reviews) Conflicts.Add(conflict);
        if (previewReview is { } previous && !Conflicts.Any(item => AttachmentReviewKey(item) == AttachmentReviewKey(previous))) ClearAttachmentPreview();
        RefreshReviewItems();
        ready = true;
    }
    private async Task RunAsync(Func<PersonalSyncAccess, CancellationToken, Task> action, bool allowCoveredRead = false,
        Action? validateRequest = null)
    {
        if (publishingDeviceItems) return;
        if (!(allowCoveredRead ? CanRead : CanInteract)) return;
        if (!RestoreIdleFeedback()) return;
        if (!(allowCoveredRead ? CanRead : CanInteract)) return;
        var originalVisibility = visible!;
        var originalDisclosure = disclosureVersion;
        CancellationTokenSource? ownedOperation = null;
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        commandCompletion = completion; commandActive = true; readingLocal = allowCoveredRead; acceptedOperation = false;
        bool IsCurrent() => !rootRetired && ownsRoot() && ReferenceEquals(visible, originalVisibility)
            && (allowCoveredRead || originalDisclosure == disclosureVersion);
        try
        {
            // D-1040: initial native binding feedback already owns a completion and must share its cleanup.
            IsBusy = true; failureKey = null; RefreshProperties();
            var originalAccess = environment.Capture();
            ownedOperation = CancellationTokenSource.CreateLinkedTokenSource(originalAccess.Cancellation, originalVisibility.Token);
            operation = ownedOperation;
            void ValidateOriginal()
            {
                originalAccess.Validate(); ownedOperation.Token.ThrowIfCancellationRequested();
                if (!IsCurrent())
                    throw new OperationCanceledException("The original personal sync command or disclosure retired.");
                validateRequest?.Invoke();
            }
            var access = new PersonalSyncAccess(ValidateOriginal, ownedOperation.Token, originalAccess.Scope);
            operationOwner = access.Validate;
            access.Validate();
            if (!allowCoveredRead && transport is { } previous)
            {
                transport = environment.Open(previous.Peer, access.Validate); previous.Dispose();
            }
            await action(access, operation.Token); access.Validate();
        }
        catch (OperationCanceledException)
        { if (acceptedOperation && OwnsPresentation) RetainFailure("Sync_ActionFailed"); }
        catch (PersonalSyncHttpException error)
        { if (IsCurrent()) RetainFailure(error.Status == System.Net.HttpStatusCode.Unauthorized ? "Sync_SignInRequired" : "Sync_ServiceUnavailable"); }
        catch (ArgumentException) { if (IsCurrent()) RetainFailure("Sync_CheckInputs"); }
        catch (HttpRequestException) { if (IsCurrent()) RetainFailure("Sync_ServiceUnavailable"); }
        catch (Exception error) when (error is not OutOfMemoryException)
        { if (IsCurrent()) RetainFailure("Sync_ActionFailed"); }
        finally
        {
            try
            {
                try { Password = ""; }
                finally { ownedOperation?.Dispose(); }
            }
            finally
            {
                if (ReferenceEquals(operation, ownedOperation)) operation = null;
                operationOwner = null;
                try
                {
                    // A failed idle notification is feedback, not a still-running original command.
                    // Keep commandActive until publication ends so its callbacks cannot start a competitor.
                    idleFeedbackPending = true;
                    IsBusy = false;
                    idleFeedbackPending = false;
                }
                catch (Exception failure) when (failure is not OutOfMemoryException)
                { ReportFeedbackFailure(failure); }
                finally
                {
                    commandActive = false; readingLocal = false;
                    completion.TrySetResult();
                    try { RefreshProperties(); }
                    catch (Exception failure) when (failure is not OutOfMemoryException)
                    { ReportFeedbackFailure(failure); }
                }
            }
        }
    }
    /// <summary>Retries only failed idle feedback on explicit activation/action, never an accepted original operation.</summary>
    private bool RestoreIdleFeedback()
    {
        // Repair publication itself can call back into a command before that command owns a completion.
        if (restoringIdleFeedback) return false;
        if (commandActive || !idleFeedbackPending) return true;
        restoringIdleFeedback = true;
        try
        {
            IsBusy = false;
            idleFeedbackPending = false;
            return true;
        }
        catch (Exception failure) when (failure is not OutOfMemoryException)
        { ReportFeedbackFailure(failure); return false; }
        finally { restoringIdleFeedback = false; }
    }
    /// <summary>Records only a fixed diagnostic category without connection, credential or exception payloads.</summary>
    private void ReportFeedbackFailure(Exception failure)
    {
        // D-1040: a terminal binding failure is still follow-through failure after a known commit.
        // Keep the original accepted receipt and require a source reread, never primary replay.
        if (OwnsPresentation) RetainFailure("Sync_ActionFailed");
        System.Diagnostics.Debug.WriteLine($"Personal sync feedback failed: {failure.GetType().Name}");
    }
    /// <summary>Retires the old editable review immediately after acknowledged original work, before fallible follow-through.</summary>
    private void AcceptKnownOperation() { acceptedOperation = true; ready = false; RefreshProperties(); }
    /// <summary>Keeps an acknowledged result distinct from a failed primary command; Retry only rereads the source.</summary>
    private void RetainFailure(string key)
    {
        if (acceptedOperation) { ready = false; failureKey = "Sync_AcceptedFollowUpFailed"; }
        else failureKey = key;
    }
    private void RequireSession()
    {
        RequirePersonalConnection();
        if (!catalogReady || transport is null || identity is null || device is null || transport.Peer.Address.AbsoluteUri != new LocalSyncPeer(Address, Fingerprint).Address.AbsoluteUri ||
            transport.Peer.Fingerprint != new LocalSyncPeer(Address, Fingerprint).Fingerprint)
            throw new InvalidOperationException("The reviewed personal peer/session changed.");
    }
    private void RequirePersonalConnection()
    { if (IsHouseholdConnection) throw new InvalidOperationException("The original encrypted household connection owns this operation."); }
    private bool SessionMatchesConnection => transport is not null && identity is not null && device is not null &&
        review?.DeviceId == device.Id && review.IdentityId == identity.Id && review.PeerKey == transport.Peer.BindingKey(identity.Id);
    private async Task<bool> ConfirmAsync(string key, string argument)
    {
        var validate = operationOwner ?? throw new OperationCanceledException("The original sync consent retired.");
        validate();
        var inputs = (Address, Fingerprint, Email, DeviceName, NewProfileName);
        var answer = await interaction.ConfirmAsync(translator["Sync_Title"], translator.Format(key, argument),
            translator["Sync_Confirm"], translator["Common_Cancel"]);
        validate();
        if (inputs != (Address, Fingerprint, Email, DeviceName, NewProfileName))
            throw new OperationCanceledException("The original sync disclosure inputs changed.");
        return answer;
    }
    private void OnTranslationChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        var version = ++disclosureVersion;
        if (publishingDeviceItems) { catalogReady = false; return; }
        publishingDeviceItems = true;
        var authorized = catalogReady; catalogReady = false;
        try
        {
            RefreshDeviceItems();
            var devices = Devices.ToArray(); var items = DeviceItems.ToArray();
            RefreshReviewItems(); catalogReady = authorized;
            RefreshProperties();
            if (version != disclosureVersion || !Devices.SequenceEqual(devices) || !DeviceItems.SequenceEqual(items))
                throw new InvalidDataException("The original translated catalog changed.");
        }
        catch { catalogReady = false; throw; }
        finally { publishingDeviceItems = false; }
    }
    private void RefreshReviewItems()
    {
        var presenter = new PersonalSyncReviewPresenter(translator, dates, localization);
        ReviewItems.Clear();
        var groups = new HashSet<(string Family, Guid Id)>();
        foreach (var conflict in Conflicts)
            if (conflict.Conflict.LinkedGroup is not { } group || groups.Add((SyncConflictGrouping.Family(conflict.Conflict.Proposal.Record), group)))
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
        var version = disclosureVersion;
        var devices = Devices.ToArray();
        var items = devices.Select(CreateDeviceItem).ToArray();
        DeviceItems.Clear(); foreach (var item in items) DeviceItems.Add(item);
        if (version != disclosureVersion || !Devices.SequenceEqual(devices) || !DeviceItems.SequenceEqual(items))
            throw new InvalidDataException("The original device-caption publication changed.");
    }
    /// <summary>Stages the exact original device caption before fallible collection feedback.</summary>
    private PersonalSyncDeviceItem CreateDeviceItem(SyncDevice row) => new(row, row.Revoked ? translator["Sync_DeviceRevoked"] :
        NativeDigits.Apply(translator.Format("Sync_DeviceSeen", dates.Format(row.LastSeenAt)))!);
    private void RefreshProperties()
    {
        RefreshAttachmentProperties();
        foreach (var property in new[] { nameof(PageTitle), nameof(IntroText), nameof(HelpTopic), nameof(ShowPersonalConnection), nameof(ShowPersonalSignedIn), nameof(HouseholdReviewEmpty) }) OnPropertyChanged(property);
        foreach (var property in new[] { nameof(Available), nameof(CanRead), nameof(HasPendingOperation), nameof(IsReady), nameof(CanInteract), nameof(CanConnect), nameof(CanSync), nameof(IsHouseholdConnection), nameof(SignedIn), nameof(CanLeave),
            nameof(Error), nameof(AutomaticError), nameof(StatusText), nameof(DetailText), nameof(NoProfiles), nameof(HasConflicts) }) OnPropertyChanged(property);
        foreach (var property in new[] { nameof(IsOnboardingRestore), nameof(CanFinishOnboarding), nameof(CanCloseOnboarding), nameof(ShowFooter) }) OnPropertyChanged(property);
    }
}
