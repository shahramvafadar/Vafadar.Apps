namespace Vafadar.Zanance.App.Features.Goals;

/// <summary>Contextual native notification choice used only after an explicit goal-editor toggle.</summary>
public interface IGoalEditorNotifications
{
    /// <summary>Gets whether this platform can deliver notifications, without prompting or changing permission.</summary>
    bool IsSupported { get; }

    /// <summary>Offers the established explanation and native permission dialog for the explicit choice.</summary>
    Task<bool> EnableAsync();
}
