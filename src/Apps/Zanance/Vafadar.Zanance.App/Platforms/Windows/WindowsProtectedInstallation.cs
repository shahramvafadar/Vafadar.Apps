using Vafadar.Data;

namespace Vafadar.Zanance.App.Security;

/// <summary>Supplies native local Windows handles and directory flushes to the shared installation protocol.</summary>
/// <remarks>Construction remains lazy; covered startup owns every legacy move and authority publication.</remarks>
internal sealed class WindowsProtectedInstallation(IServiceProvider services) : ProtectedInstallation(services,
    static (roots, validate) => new WindowsProfileRetirementFiles(roots, validate),
    static root => new WindowsGenerationFileDurability(root));
