namespace Vafadar.Zanance.App.Tests;

/// <summary>Isolates the four existing German contracts affected by the native cancellation caption correction.</summary>
public sealed class AiGermanNativeCancellationCaptionTests
{
    /// <summary>Original Send keeps its complete disclosure and current compact negative action.</summary>
    [Fact] public Task German_send_keeps_complete_disclosure() => new AiNativeConfirmationCaptionTests().Native_action_is_short_localized_and_keeps_complete_disclosure("de", "Senden");
    /// <summary>Original Share keeps its complete disclosure and current compact negative action.</summary>
    [Fact] public Task German_share_keeps_complete_disclosure() => new AiOtherNativeConfirmationCaptionTests().Related_action_is_short_and_retains_complete_literal_disclosure("share", "de", "Teilen");
    /// <summary>Original Continue keeps its complete disclosure and current compact negative action.</summary>
    [Fact] public Task German_continue_keeps_complete_disclosure() => new AiOtherNativeConfirmationCaptionTests().Related_action_is_short_and_retains_complete_literal_disclosure("continue", "de", "Fortsetzen");
    /// <summary>Original Close keeps its complete disclosure and current compact negative action.</summary>
    [Fact] public Task German_close_keeps_complete_disclosure() => new AiOtherNativeConfirmationCaptionTests().Related_action_is_short_and_retains_complete_literal_disclosure("close", "de", "Schließen");
}
