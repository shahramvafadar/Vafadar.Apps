using CommunityToolkit.Mvvm.Input;
using Vafadar.Zanance.Core.Ledger;

namespace Vafadar.Zanance.App.Features.Entries;

public sealed partial class EntryEditorViewModel
{
    private Dictionary<string, IAsyncRelayCommand>? _formCommands;
    private IAsyncRelayCommand FormCommand(string name, Func<object?, Task> action, bool requiresReady = true)
    {
        _formCommands ??= [];
        if (_formCommands.TryGetValue(name, out var existing)) return existing;
        var epoch = _formEpoch; var owner = _loadSession; var source = owner?.Review;
        var command = new AsyncRelayCommand<object?>(value => epoch == _formEpoch && !_formDeparted && IsNotBusy
            && (!requiresReady || IsFormEditable && ReferenceEquals(owner, _loadSession)
                && source is not null && ReferenceEquals(source, owner?.Review)) ? action(value) : Task.CompletedTask);
        _formCommands.Add(name, command); return command;
    }
    private void ResetFormCommands()
    {
        _formCommands = null;
        foreach (var name in new[] { nameof(AddTagCommand), nameof(UndoAccountChangeCommand), nameof(UseReceiptAmountCommand),
            nameof(ApplyTemplateCommand), nameof(SelectCategoryCommand), nameof(UndoSuggestionCommand), nameof(ToggleDetailsCommand),
            nameof(RepeatCommand), nameof(SaveCommand), nameof(CancelCommand), nameof(AddAccountCommand), nameof(ReloadFormCommand) })
            OnPropertyChanged(name);
    }
    private static Task Synchronously(Action action) { action(); return Task.CompletedTask; }
    /// <summary>Gets tag adoption bound to the original form and its current suggestions.</summary>
    public IAsyncRelayCommand AddTagCommand => FormCommand(nameof(AddTagCommand), value => value is string tag && TagSuggestions.Any(t => t.Value == tag)
        ? Synchronously(() => AddTag(tag)) : Task.CompletedTask);
    /// <summary>Gets account-change Undo bound to the original raw draft.</summary>
    public IAsyncRelayCommand UndoAccountChangeCommand => FormCommand(nameof(UndoAccountChangeCommand), _ => Synchronously(UndoAccountChange));
    /// <summary>Gets receipt amount selection bound to its original current candidate instance.</summary>
    public IAsyncRelayCommand UseReceiptAmountCommand => FormCommand(nameof(UseReceiptAmountCommand), value => value is ReceiptChoice choice
        && ReceiptChoices.Any(c => ReferenceEquals(c, choice)) ? Synchronously(() => UseReceiptAmount(choice)) : Task.CompletedTask);
    /// <summary>Gets explicit template adoption bound to the current original reviewed definition.</summary>
    public IAsyncRelayCommand ApplyTemplateCommand => FormCommand(nameof(ApplyTemplateCommand), value => value is EntryTemplate template
        && Templates.Any(t => ReferenceEquals(t, template)) ? ApplyTemplateAsync(template) : Task.CompletedTask);
    /// <summary>Gets category selection bound to its current original tile instance.</summary>
    public IAsyncRelayCommand SelectCategoryCommand => FormCommand(nameof(SelectCategoryCommand), value => value is CategoryChoice choice
        && Categories.Any(c => ReferenceEquals(c, choice)) ? Synchronously(() => SelectCategory(choice)) : Task.CompletedTask);
    /// <summary>Gets categorization suggestion Undo for this original form.</summary>
    public IAsyncRelayCommand UndoSuggestionCommand => FormCommand(nameof(UndoSuggestionCommand), _ => Synchronously(UndoSuggestion));
    /// <summary>Gets details expansion for this original form.</summary>
    public IAsyncRelayCommand ToggleDetailsCommand => FormCommand(nameof(ToggleDetailsCommand), _ => Synchronously(ToggleDetails));
    /// <summary>Gets the unsaved repeating-plan handoff for this original form.</summary>
    public IAsyncRelayCommand RepeatCommand => FormCommand(nameof(RepeatCommand), _ => RepeatAsync());
    /// <summary>Gets Save owned by the original complete source, never a later routed form.</summary>
    public IAsyncRelayCommand SaveCommand => FormCommand(nameof(SaveCommand), _ => SaveAsync());
    /// <summary>Gets Cancel even during a failed initial read, without adopting another routed owner.</summary>
    public IAsyncRelayCommand CancelCommand => FormCommand(nameof(CancelCommand), _ => CancelAsync(), false);
    /// <summary>Gets account creation navigation only from this original ready form.</summary>
    public IAsyncRelayCommand AddAccountCommand => FormCommand(nameof(AddAccountCommand), _ => AddAccountAsync());
    /// <summary>Gets explicit original read Retry, without replacing a published raw draft.</summary>
    public IAsyncRelayCommand ReloadFormCommand => FormCommand(nameof(ReloadFormCommand), _ => ReloadFormAsync(), false);
}
