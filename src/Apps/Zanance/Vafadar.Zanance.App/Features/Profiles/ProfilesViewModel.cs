using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vafadar.Localization;
using Vafadar.Maui.Mvvm;
using Vafadar.Zanance.App.Profiles;

namespace Vafadar.Zanance.App.Features.Profiles;

/// <summary>A profile in the list.</summary>
public sealed record ProfileRow(LocalProfile Profile, string Name, bool IsCurrent, string? Detail);

/// <summary>
/// Local profiles (§3): each one keeps its own data, settings, app lock and backups on this device. Opening a profile
/// with the app lock asks for the device owner first.
/// </summary>
public sealed partial class ProfilesViewModel(ProfileService profiles, Translator translator) : ViewModelBase
{
    public ObservableCollection<ProfileRow> Profiles { get; } = [];

    [ObservableProperty]
    public partial string? Error { get; set; }

    public Task LoadAsync()
    {
        Error = null;
        Profiles.Clear();
        var current = profiles.Current;
        foreach (var profile in profiles.All)
        {
            var isCurrent = profile.Id == current.Id;
            Profiles.Add(new ProfileRow(profile, profiles.NameOf(profile), isCurrent, isCurrent ? translator["Profile_Open"] : null));
        }

        return Task.CompletedTask;
    }

    [RelayCommand]
    private async Task AddAsync()
    {
        var name = await Shell.Current.DisplayPromptAsync(translator["Profile_Add"], translator["Profile_NamePrompt"], translator["Common_Save"], translator["Common_Cancel"], maxLength: ProfileService.MaxNameLength);
        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        if (IsTaken(name, null))
        {
            Error = translator["Profile_NameTaken"];
            return;
        }

        var profile = profiles.Create(name);
        await LoadAsync();
        if (await Shell.Current.DisplayAlertAsync(translator["Profile_Add"], translator.Format("Profile_OpenNew", profiles.NameOf(profile)), translator["Profile_OpenAction"], translator["Common_Cancel"]))
        {
            await OpenProfileAsync(profile);
        }
    }

    [RelayCommand]
    private async Task ChooseAsync(ProfileRow row)
    {
        var open = translator["Profile_OpenAction"];
        var rename = translator["Profile_Rename"];
        var delete = translator["Common_Delete"];
        string[] actions = row.IsCurrent ? [rename] : row.Profile.IsMain ? [open, rename] : [open, rename, delete];
        var choice = await Shell.Current.DisplayActionSheetAsync(row.Name, translator["Common_Cancel"], null, actions);
        if (choice == open)
        {
            await OpenProfileAsync(row.Profile);
        }
        else if (choice == rename)
        {
            var name = await Shell.Current.DisplayPromptAsync(rename, translator["Profile_NamePrompt"], translator["Common_Save"], translator["Common_Cancel"], initialValue: row.Name, maxLength: ProfileService.MaxNameLength);
            if (!string.IsNullOrWhiteSpace(name))
            {
                if (IsTaken(name, row.Profile.Id))
                {
                    Error = translator["Profile_NameTaken"];
                    return;
                }

                profiles.Rename(row.Profile, name);
                await LoadAsync();
            }
        }
        else if (choice == delete)
        {
            // Two confirmations: the profile's data cannot be restored without a backup of it.
            if (await Shell.Current.DisplayAlertAsync(translator["Profile_Delete"], translator.Format("Profile_DeleteMessage", row.Name), delete, translator["Common_Cancel"])
                && await Shell.Current.DisplayAlertAsync(translator["Profile_Delete"], translator["Profile_DeleteFinal"], delete, translator["Common_Cancel"]))
            {
                profiles.Delete(row.Profile);
                await LoadAsync();
            }
        }
    }

    // Two profiles with one name cannot be told apart in the profile list and the switcher.
    private bool IsTaken(string name, string? exceptId)
    {
        var wanted = Vafadar.Core.Text.SearchText.Normalize(name);
        return profiles.All.Any(p => p.Id != exceptId
            && string.Equals(Vafadar.Core.Text.SearchText.Normalize(profiles.NameOf(p)), wanted, StringComparison.CurrentCultureIgnoreCase));
    }

    private async Task OpenProfileAsync(LocalProfile profile)
    {
        Error = null;
        if (!await profiles.SwitchToAsync(profile))
        {
            Error = translator["Profile_Locked"];
            return;
        }

        (Application.Current as App)?.ShowCurrentProfile();
    }
}
