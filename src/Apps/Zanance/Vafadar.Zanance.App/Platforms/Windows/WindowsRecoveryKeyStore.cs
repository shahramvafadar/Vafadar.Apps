using Vafadar.Data;

namespace Vafadar.Zanance.App.Security;

/// <summary>Captures explicitly enrolled recovery material while preserving CurrentUser DPAPI as normal device authority.</summary>
internal sealed class WindowsRecoveryKeyStore(WindowsGenerationKeyStore native)
    : RecoveryCapturingGenerationKeyStore(native, native);
