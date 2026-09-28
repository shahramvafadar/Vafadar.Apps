using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vafadar.Finance.App.Presentation;
using Vafadar.Finance.Core.Dashboard;
using Vafadar.Localization;
using Vafadar.Maui.Mvvm;

namespace Vafadar.Finance.App.Features.Home;

/// <summary>A Home section in the customisation list.</summary>
public sealed partial class HomeSectionRow(HomeSection section, string name, bool isVisible, Action<HomeSectionRow> changed) : ObservableObject
{
    public HomeSection Section { get; } = section;

    public string Name { get; } = name;

    [ObservableProperty]
    public partial bool IsVisible { get; set; } = isVisible;

    [ObservableProperty]
    public partial bool CanMoveUp { get; set; }

    [ObservableProperty]
    public partial bool CanMoveDown { get; set; }

    partial void OnIsVisibleChanged(bool value) => changed(this);
}

/// <summary>
/// Customising Home (§21.5, REP-08): sections can be hidden and reordered. The balance and "needs attention" always stay
/// on top, so nothing that needs action is hidden (DASH-03). Stored in the local preferences.
/// </summary>
public sealed partial class HomeLayoutViewModel(Translator translator) : ViewModelBase
{
    private HomeLayout _layout = HomeLayout.Default;
    private bool _loading;

    public ObservableCollection<HomeSectionRow> Rows { get; } = [];

    public void Load()
    {
        _layout = HomeLayoutPreferences.Load();
        Refresh();
    }

    [RelayCommand]
    private void MoveUp(HomeSectionRow row) => Move(row, -1);

    [RelayCommand]
    private void MoveDown(HomeSectionRow row) => Move(row, 1);

    [RelayCommand]
    private void Reset()
    {
        _layout = HomeLayout.Default;
        HomeLayoutPreferences.Save(_layout);
        Refresh();
    }

    private void Move(HomeSectionRow row, int delta)
    {
        _layout.Move(row.Section, delta);
        HomeLayoutPreferences.Save(_layout);
        Refresh();
    }

    private void OnRowChanged(HomeSectionRow row)
    {
        if (_loading)
        {
            return;
        }

        _layout.SetVisible(row.Section, row.IsVisible);
        HomeLayoutPreferences.Save(_layout);
    }

    private void Refresh()
    {
        _loading = true;
        Rows.Clear();
        var sections = _layout.Sections;
        for (var i = 0; i < sections.Count; i++)
        {
            Rows.Add(new HomeSectionRow(sections[i].Section, translator[$"HomeSection_{sections[i].Section}"], sections[i].IsVisible, OnRowChanged)
            {
                CanMoveUp = i > 0,
                CanMoveDown = i < sections.Count - 1,
            });
        }

        _loading = false;
    }
}