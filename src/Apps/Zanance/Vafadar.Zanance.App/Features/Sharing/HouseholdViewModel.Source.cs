using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Profiles;
using Vafadar.Zanance.Core.Sharing;
using Vafadar.Zanance.Data.Sharing;

namespace Vafadar.Zanance.App.Features.Sharing;

/// <summary>One explicitly selectable actual source account; selection has no financial effect.</summary>
public sealed partial class HouseholdSourceAccount(Guid id, string caption, Action changed) : ObservableObject
{
    /// <summary>Original local identity, never a service member or device identity.</summary>
    public Guid Id { get; } = id;
    /// <summary>Actual account label and its ISO currency, without a guessed converted balance.</summary>
    public string Caption { get; } = caption;
    /// <summary>Off by default; changing the choice retires any previous reviewed copy.</summary>
    [ObservableProperty] public partial bool Selected { get; set; }
    partial void OnSelectedChanged(bool value) => changed();
}

public sealed partial class HouseholdViewModel
{
    private HouseholdProjectionReview? sourceReview;
    private HouseholdProjectionReview? pendingSource;
    private HouseholdProjectionReceipt? sourceReceipt;
    /// <summary>Content-free native action failure location for independent runtime qualification; never a message, path or credential.</summary>
    internal string? SourceFailureKind { get; private set; }

    /// <summary>Current explicit choices, loaded only when requested in a personal source.</summary>
    public ObservableCollection<HouseholdSourceAccount> SourceAccounts { get; } = [];
    /// <summary>History is an independent opt-in; balances alone are not an implicit full-history export.</summary>
    [ObservableProperty] public partial bool SourceHistory { get; set; }
    /// <summary>Selected free text and files become shared content; files are independently off by default.</summary>
    [ObservableProperty] public partial bool SourceAttachments { get; set; }
    partial void OnSourceHistoryChanged(bool value) { if (!value) SourceAttachments = false; ClearSourceReview(); }
    partial void OnSourceAttachmentsChanged(bool value) { if (value && !SourceHistory) SourceHistory = true; ClearSourceReview(); }
    /// <summary>A shared ledger cannot be silently re-shared through the personal-source shortcut.</summary>
    public bool CanChooseSource => CanInteract && state is null && pendingSource is null;
    /// <summary>The last coherent detached source review, distinct from current editable checkboxes.</summary>
    public bool HasSourceReview => sourceReview is not null;
    /// <summary>Only this actual destination/identity/service can continue the explicit profile handoff.</summary>
    public bool HasSourceHandoff => pendingSource is not null;
    /// <summary>A pending durable request blocks remote reservation even after the process lost its memory-only source view.</summary>
    public bool HasPendingSourceReceipt => sourceReceipt is { Completed: false };
    /// <summary>Complete selected counts, neutral private transfer treatment and explicit shared-content warning.</summary>
    public string SourceSummary => DescribeSource(sourceReview ?? pendingSource);

    private string DescribeSource(HouseholdProjectionReview? review) => review is null ? "" :
        translator.Format("HouseholdSource_Summary", string.Join(", ", review.Accounts.Select(account => account.Name + " (" + account.CurrencyCode + ")")),
            review.Entries, review.Attachments, review.PrivateTransfers, review.DetachedRefunds);

    [RelayCommand] private Task LoadSourceAccountsAsync() => RunAsync(async (access, ct) =>
    {
        if (state is not null || pendingSource is not null) throw new InvalidOperationException();
        ClearSourceReview(); var accounts = await finances.GetAccountsAsync(cancellationToken: ct); access.Validate();
        SourceAccounts.Clear(); foreach (var account in accounts) SourceAccounts.Add(new(account.Id, account.Name + " (" + account.CurrencyCode + ")", ClearSourceReview));
    });

    [RelayCommand] private Task ReviewSourceAsync() => RunAsync(async (access, ct) =>
    {
        if (state is not null || pendingSource is not null) throw new InvalidOperationException();
        ClearSourceReview();
        var selection = new HouseholdProjectionSelection(SourceAccounts.Where(account => account.Selected).Select(account => account.Id).ToArray(),
            SourceHistory, SourceAttachments, translator["HouseholdSource_PrivateCounterpart"]);
        if (selection.AccountIds.Count == 0) throw new InvalidOperationException();
        var reviewed = await sourceProjection.ReviewAsync(selection, ct);
        try { access.Validate(); ct.ThrowIfCancellationRequested(); sourceReview = reviewed; }
        catch { reviewed.Dispose(); throw; }
    });

    [RelayCommand] private async Task OpenSourceDestinationAsync()
    {
        if (!CanInteract || sourceReview is null || identity is null) return;
        IsBusy = true; errorKey = null; SourceFailureKind = null; Refresh();
        var stage = "source-consent";
        HouseholdProjectionReview? retained = null;
        try
        {
            var access = environment.Capture(); var sourceScope = access.Scope; var selected = sourceReview; var actualIdentity = identity;
            var peer = new Vafadar.Zanance.Data.Sync.LocalSyncPeer(Address, Fingerprint);
            var catalogue = profiles.ReadWorkReview(); var currentId = profiles.Current.Id;
            if (!await interaction.ConfirmAsync(translator["HouseholdSource_Title"], DescribeSource(selected) + "\n\n" + translator["HouseholdSource_Consent"],
                translator["Sync_Confirm"], translator["Common_Cancel"])) return;
            var targets = catalogue.Profiles.Where(profile => profile.Id != currentId).ToArray();
            var labels = targets.Select((profile, index) => "«" + profiles.NameOf(profile) + "» · " + NativeDigits.Apply((index + 1).ToString(System.Globalization.CultureInfo.InvariantCulture))).ToArray();
            stage = "destination-choice";
            var choice = await interaction.ChooseAsync(translator["HouseholdSource_Destination"], translator["Common_Cancel"],
                new[] { translator["Household_NewProfile"] }.Concat(labels).ToArray());
            if (choice is null || choice == translator["Common_Cancel"]) return;
            access.Validate(); visible!.Token.ThrowIfCancellationRequested();
            LocalProfile destination;
            if (choice == translator["Household_NewProfile"])
            {
                stage = "destination-name";
                var name = await interaction.PromptAsync(translator["Household_NewProfile"], translator["Profile_NamePrompt"], translator["Common_Save"],
                    translator["Common_Cancel"], initialValue: Name.Trim() == profiles.NameOf(profiles.Current) ? null : Name.Trim(),
                    maxLength: ProfileService.MaxNameLength);
                if (string.IsNullOrWhiteSpace(name)) return;
                stage = "native-profile-create";
                access.Validate(); destination = await profiles.CreateReviewedAsync(catalogue, name, visible!.Token);
                // Catalogue publication deliberately retires the previous network lifetime. Capture
                // fresh unlocked access, but only for the same source generation before switching.
                access = environment.Capture();
                if (access.Scope != sourceScope) throw new OperationCanceledException("The selected household source profile changed.");
            }
            else
            {
                var index = Array.IndexOf(labels, choice); if (index < 0) throw new InvalidOperationException(); destination = targets[index];
            }
            access.Validate();
            stage = "native-profile-switch";
            var opened = await profiles.SwitchHouseholdSourceAsync(profiles.ReadWorkReview(), destination, sourceProjection, selected, () =>
            {
                access.Validate(); sourceHandoff.Retain(destination.Id, peer, actualIdentity, selected);
                sourceReview = null; retained = selected;
            });
            if (!opened) { if (retained is not null) { sourceHandoff.Finish(retained); retained = null; } return; }
            host.ShowCurrentProfile();
        }
        catch (OperationCanceledException) { if (retained is not null) sourceHandoff.Finish(retained); }
        catch (Exception error) when (error is not OutOfMemoryException)
        {
            // An accepted profile transition may have retired this page. Retain its exact target
            // handoff for a fresh unlocked visit rather than replacing keys or silently publishing.
            errorKey = "Household_ActionFailed";
            SourceFailureKind = stage + ":" + error.GetType().Name + ":" + error.HResult.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }
        finally { IsBusy = false; Refresh(); }
    }

    [RelayCommand] private Task CompleteSourceCopyAsync() => RunAsync(async (access, ct) =>
    {
        var selected = pendingSource ?? throw new InvalidOperationException();
        if (transport is null || identity is null || sourceHandoff.Read(profiles.Current.Id, transport.Peer, identity) != selected ||
            !await ConfirmAsync("HouseholdSource_Consent")) return;
        access.Validate(); var draft = await workspace.BeginOwnerAsync(transport, ct, selected); access.Validate();
        var prior = await journal.ReadProjectionReceiptAsync(draft.Authority, ct);
        if (prior is { Completed: false } && prior.ReviewId != selected.Id)
        {
            if (!await ConfirmAsync("HouseholdSource_ReplacePending")) return;
            access.Validate(); await journal.ReplacePendingProjectionAsync(draft.Authority, prior.ReviewId, selected, ct);
        }
        else await journal.StageProjectionReviewAsync(draft.Authority, selected, ct);
        await journal.ApplyProjectionAsync(draft.Authority, selected, ct); access.Validate();
        sourceHandoff.Finish(selected); pendingSource = null; await ReadLocalAsync(ct);
    });

    [RelayCommand] private Task CancelSourceCopyAsync() => RunAsync(async (access, ct) =>
    {
        if (pendingSource is not { } selected || !await ConfirmAsync("HouseholdSource_CancelMemory")) return;
        access.Validate(); sourceHandoff.Finish(selected); pendingSource = null; await ReadLocalAsync(ct);
    });

    private async Task ReadSourceSelectionAsync(HouseholdJournalState? next, CancellationToken ct)
    {
        pendingSource = identity is not null && transport is not null ? sourceHandoff.Read(profiles.Current.Id, transport.Peer, identity) : null;
        sourceReceipt = next is null ? null : await journal.ReadProjectionReceiptAsync(next.Authority, ct);
    }
    private void ClearSourceReview() { sourceReview?.Dispose(); sourceReview = null; RefreshSourceSelection(); }
    private void RetireSourceSelection() { ClearSourceReview(); pendingSource = null; sourceReceipt = null; SourceAccounts.Clear(); }
    private void RefreshSourceSelection()
    {
        foreach (var property in new[] { nameof(CanChooseSource), nameof(HasSourceReview), nameof(HasSourceHandoff), nameof(HasPendingSourceReceipt), nameof(SourceSummary) })
            OnPropertyChanged(property);
    }
}
