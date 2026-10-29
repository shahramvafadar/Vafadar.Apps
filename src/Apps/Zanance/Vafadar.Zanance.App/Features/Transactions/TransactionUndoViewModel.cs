using CommunityToolkit.Mvvm.Input;
using Vafadar.Zanance.App.Interaction;
using Vafadar.Zanance.App.Presentation;

namespace Vafadar.Zanance.App.Features.Transactions;

/// <summary>The actual transaction Undo command, with explicit native feedback and list refresh ports.</summary>
public sealed partial class TransactionUndoViewModel(UndoService undo, IAppInteraction interaction, Func<Task> refresh,
    Func<bool>? isCurrent = null, Vafadar.Localization.Translator? translator = null)
{
    private bool _executing;
    /// <summary>Gets whether the original Undo still owns its accepted refresh and feedback lifetime.</summary>
    public bool IsExecuting => _executing;

    [RelayCommand]
    private async Task UndoDeleteAsync()
    {
        // D-128: include feedback/refresh in the command lifetime; a dialog must not admit a second Undo.
        if (_executing || isCurrent?.Invoke() == false) return;
        _executing = true;
        try
        {
            await undo.UndoAsync(isCurrent);
            if (isCurrent?.Invoke() != false) await refresh();
        }
        catch (OperationCanceledException) when (isCurrent?.Invoke() == false) { }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            if (undo.LastAccepted && translator is not null)
                await interaction.AlertAsync(translator["Error_Title"], translator["Occurrence_SavedRefreshFailed"], translator["Common_Ok"]);
            else await interaction.ShowFailureAsync(ex);
        }
        finally
        {
            _executing = false;
        }
    }
}
