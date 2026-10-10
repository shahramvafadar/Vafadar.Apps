using CommunityToolkit.Mvvm.Input;
using Vafadar.Zanance.App.Interaction;
using Vafadar.Zanance.App.Presentation;

namespace Vafadar.Zanance.App.Features.Transactions;

/// <summary>The actual transaction Undo command, with explicit native feedback and list refresh ports.</summary>
public sealed partial class TransactionUndoViewModel(UndoService undo, IAppInteraction interaction, Func<Task> refresh)
{
    private bool _executing;

    [RelayCommand]
    private async Task UndoDeleteAsync()
    {
        // D-128: include feedback/refresh in the command lifetime; a dialog must not admit a second Undo.
        if (_executing) return;
        _executing = true;
        try
        {
            await undo.UndoAsync();
            await refresh();
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            await interaction.ShowFailureAsync(ex);
        }
        finally
        {
            _executing = false;
        }
    }
}
