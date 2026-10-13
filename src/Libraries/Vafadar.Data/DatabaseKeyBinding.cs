using System.Text;

namespace Vafadar.Data;

/// <summary>Non-secret identity authenticated with a platform-protected profile data key.</summary>
public sealed record DatabaseKeyBinding
{
    /// <summary>Captures one profile, installation and key generation; empty identities are never accepted.</summary>
    public DatabaseKeyBinding(Guid profileId, Guid installationId, Guid generationId)
    {
        if (profileId == Guid.Empty || installationId == Guid.Empty || generationId == Guid.Empty)
            throw new ArgumentException("A data key requires complete binding identities.");
        ProfileId = profileId;
        InstallationId = installationId;
        GenerationId = generationId;
    }

    /// <summary>The captured profile identity, never its label or filename.</summary>
    public Guid ProfileId { get; }
    /// <summary>The local installation identity, never a portable preference.</summary>
    public Guid InstallationId { get; }
    /// <summary>The exact key generation, independent from the mutable database contents.</summary>
    public Guid GenerationId { get; }

    /// <summary>Gets canonical, versioned platform entropy/AAD; these identifiers are not secrets or a key derivation algorithm.</summary>
    public byte[] GetAuthenticatedContext() => Encoding.UTF8.GetBytes(
        $"Vafadar.DatabaseKey.v1|{ProfileId:N}|{InstallationId:N}|{GenerationId:N}");
}
