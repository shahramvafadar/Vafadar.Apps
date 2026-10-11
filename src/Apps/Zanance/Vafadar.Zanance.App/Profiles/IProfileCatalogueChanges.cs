namespace Vafadar.Zanance.App.Profiles;

/// <summary>Original local catalogue events without construction-time database or platform access.</summary>
public interface IProfileCatalogueChanges
{
    /// <summary>Raised after an original catalogue mutation or committed profile switch.</summary>
    event EventHandler? Changed;
}
