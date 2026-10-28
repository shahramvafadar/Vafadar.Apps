using CommunityToolkit.Mvvm.ComponentModel;
using Vafadar.Zanance.App.Presentation;

namespace Vafadar.Zanance.App.Features.Accounts;

/// <summary>Original native query contract and semantic balance colors, separate from original account-detail policy.</summary>
public sealed partial class AccountDetailViewModel : IQueryAttributable
{
    /// <summary>Gets the original negative-balance color independently of the original recorded amount.</summary>
    [ObservableProperty] public partial Color? BalanceColor { get; set; }

    partial void ApplyBalanceColor(bool negative) => BalanceColor = negative ? EntryPresenter.DangerColor : Palette.AmountText;
}
