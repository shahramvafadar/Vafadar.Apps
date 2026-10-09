namespace Vafadar.Zanance.App.Features.Settings;

/// <summary>The exact unsaved estimate input and the currency in which the user entered it.</summary>
/// <param name="Text">Raw editable text, including incomplete or currently invalid input.</param>
/// <param name="PeriodIndex">The selected existing estimate period.</param>
/// <param name="CurrencyCode">The ISO currency attached to this input.</param>
internal sealed record EstimateInput(string Text, int PeriodIndex, string CurrencyCode);

/// <summary>Retains unsaved input while refreshing persisted preferences on the same profile's open form.</summary>
internal sealed class EstimateDraftState
{
    private string? _profileId;
    private Guid _settingsId;
    private EstimateInput? _published;

    /// <summary>Refreshes clean inputs, retaining exact dirty inputs only in the same profile/settings context.</summary>
    internal EstimateInput Publish(string profileId, Guid settingsId, EstimateInput incoming, EstimateInput current)
    {
        // D-84: returning to Settings reloads device/account availability, but must not overwrite an unsaved estimate.
        // Include profile identity: restored databases can share the same settings-row identifier.
        var retain = _profileId == profileId && _settingsId == settingsId && _published is not null && current != _published;
        _profileId = profileId;
        _settingsId = settingsId;
        _published = incoming;
        return retain ? current : incoming;
    }

    /// <summary>Accepts only successfully saved input; edits typed while Save awaited remain dirty.</summary>
    internal void AcceptSave(string profileId, Guid settingsId, EstimateInput submitted)
    {
        // A save from a form retired during a profile/restore change cannot replace the new form's baseline.
        if (_profileId == profileId && _settingsId == settingsId) { _published = submitted; }
    }
}
