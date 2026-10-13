namespace Vafadar.Core.Settings;

/// <summary>Confirms persistence of preceding settings writes under exclusive host ownership.</summary>
public interface ISettingsPersistence
{
    /// <summary>Writes pending settings to persistent storage or throws; never substitutes memory readback.</summary>
    Task PersistAsync(CancellationToken cancellationToken = default);
}
