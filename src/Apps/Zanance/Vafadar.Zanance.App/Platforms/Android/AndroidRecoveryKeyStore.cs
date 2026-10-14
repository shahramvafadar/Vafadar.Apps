using Vafadar.Data;

namespace Vafadar.Zanance.App.Security;

/// <summary>Supplies the qualified Android native key store to the shared recovery capture policy.</summary>
/// <remarks>Ordinary reads retain native device authority; only explicit enrollment attaches public recovery.</remarks>
internal sealed class AndroidRecoveryKeyStore(AndroidGenerationKeyStore native)
    : RecoveryCapturingGenerationKeyStore(native, native);
