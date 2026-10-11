using Vafadar.Localization;
using Vafadar.Zanance.App.Reminders;

namespace Vafadar.Zanance.App.Features.Goals;

/// <summary>Keeps the goal editor on the same contextual native notification flow as Plans and Settings.</summary>
internal sealed class MauiGoalEditorNotifications(ReminderService reminders, Translator translator) : IGoalEditorNotifications
{
    /// <inheritdoc />
    public bool IsSupported => reminders.Scheduler.IsSupported;

    /// <inheritdoc />
    public Task<bool> EnableAsync() => Presentation.PermissionPrompts.EnableNotificationsAsync(reminders, translator);
}
