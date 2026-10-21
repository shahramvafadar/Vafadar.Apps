using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vafadar.Zanance.App.Presentation;

namespace Vafadar.Zanance.App.Features.Home;

public sealed partial class HomeViewModel
{
    private readonly Transactions.TransactionUndoViewModel _undoAction;
    /// <summary>Gets the shared original offer; the receipt itself retains actual-file and deadline authority.</summary>
    public UndoService Undo { get; }
    /// <summary>Gets whether an original aggregate adjustment can still be undone from its Home return.</summary>
    [ObservableProperty]
    public partial bool ShowUndo { get; private set; }
    /// <summary>Gets the complete meaning of Undo, including retention of the new detail.</summary>
    public string UndoText => _translator["Aggregate_UndoNotice"];
    /// <summary>Gets the existing guarded Undo flow followed by this actual Home's original reader.</summary>
    public IAsyncRelayCommand UndoEntryCommand => _undoAction.UndoDeleteCommand;
    /// <summary>Publishes only the original remaining offer; appearing or refreshing never extends its deadline.</summary>
    public void UpdateUndo()
    {
        OnPropertyChanged(nameof(UndoText));
        ShowUndo = Undo.NoticeKind == UndoNoticeKind.AggregateAdjusted && Undo.CanUndo;
    }
}
