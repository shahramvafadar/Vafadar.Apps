namespace Vafadar.Zanance.App.Presentation;

/// <summary>An editor that knows whether leaving would lose its explicit, unsaved input.</summary>
internal interface IUnsavedChanges
{
    /// <summary>Gets a value indicating whether input changed since the last loaded or successfully saved baseline.</summary>
    bool IsDirty { get; }
}
