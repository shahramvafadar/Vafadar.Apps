using Vafadar.Data;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Startup;

/// <summary>Resolves the current native session after a covered installation recovery selection.</summary>
public interface IProtectedProfileSessionSource
{
    /// <summary>The sole session owning the currently prepared protected installation.</summary>
    SqlCipherProfileSession<ZananceDbContext> Session { get; }
}
