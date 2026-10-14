using Vafadar.Data;

namespace Vafadar.Zanance.App.Security;

/// <summary>Supplies Android native directory/file authority to the shared covered installation protocol.</summary>
internal sealed class AndroidProtectedInstallation(IServiceProvider services) : ProtectedInstallation(services,
    static (roots, validate) => new AndroidProfileRetirementFiles(roots, validate),
    static root => new AndroidGenerationFileDurability(root));
