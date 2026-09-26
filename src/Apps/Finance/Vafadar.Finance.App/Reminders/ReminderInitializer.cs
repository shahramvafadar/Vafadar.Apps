namespace Vafadar.Finance.App.Reminders;

/// <summary>
/// Subscribes to snooze actions while the app is built. Android may start the process only to deliver a notification
/// action, without creating the application window, so this cannot wait for <see cref="App"/>.
/// </summary>
internal sealed class ReminderInitializer : IMauiInitializeService
{
    public void Initialize(IServiceProvider services) => services.GetRequiredService<ReminderService>().WatchSnoozes();
}