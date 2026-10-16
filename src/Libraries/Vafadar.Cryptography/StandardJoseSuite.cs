using Jose;

namespace Vafadar.Cryptography;

/// <summary>Maintained standard algorithms with explicit strict verification and no reflection-based payload serialization.</summary>
public static class StandardJoseSuite
{
    /// <summary>Creates caller-owned settings with exactly ES256, direct/RSA-OAEP-256 and A256GCM; no global mutation.</summary>
    public static JwtSettings CreateSettings()
    {
        var settings = new JwtSettings().RegisterMapper(new StandardJoseHeaderMapper());
        foreach (var algorithm in Enum.GetValues<JwsAlgorithm>()) if (algorithm != JwsAlgorithm.ES256) settings.DeregisterJws(algorithm);
        foreach (var algorithm in Enum.GetValues<JweAlgorithm>()) if (algorithm is not (JweAlgorithm.DIR or JweAlgorithm.RSA_OAEP_256)) settings.DeregisterJwa(algorithm);
        foreach (var encryption in Enum.GetValues<JweEncryption>()) if (encryption != JweEncryption.A256GCM) settings.DeregisterJwe(encryption);
        foreach (var compression in Enum.GetValues<JweCompression>()) settings.DeregisterCompression(compression);
        return settings;
    }
}
