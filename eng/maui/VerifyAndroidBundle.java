import java.io.File;
import java.io.IOException;
import java.security.KeyStore;
import java.security.cert.Certificate;
import java.util.Arrays;
import java.util.HashSet;
import java.util.Locale;
import java.util.jar.JarFile;

/** Verifies that the release bundle has one complete signature from the configured upload certificate. */
final class VerifyAndroidBundle {
    /** Reads the upload certificate, then verifies all payload entries before the artifact can be published. */
    public static void main(String[] args) throws Exception {
        if (args.length != 2) {
            throw new IllegalArgumentException("Bundle and upload keystore paths are required.");
        }

        var password = System.getenv("ANDROID_KEYSTORE_PASSWORD").toCharArray();
        Certificate uploadCertificate;
        try {
            var keystore = KeyStore.getInstance(new File(args[1]), password);
            uploadCertificate = keystore.getCertificate(System.getenv("ANDROID_KEY_ALIAS"));
            if (uploadCertificate == null) {
                throw new IOException("Upload certificate is missing from the configured keystore.");
            }
        } finally {
            Arrays.fill(password, '\0');
        }

        try (var bundle = new JarFile(new File(args[0]), true)) {
            var names = new HashSet<String>();
            var entries = bundle.entries();
            var buffer = new byte[8192];
            while (entries.hasMoreElements()) {
                var entry = entries.nextElement();
                if (!names.add(entry.getName())) {
                    throw new IOException("Bundle contains duplicate entries.");
                }
                if (entry.isDirectory() || isSigningMetadata(entry.getName())) {
                    continue;
                }
                // JarFile authenticates an entry and exposes its signers only after its bytes are read.
                try (var content = bundle.getInputStream(entry)) {
                    while (content.read(buffer) != -1) { }
                }
                var signers = entry.getCodeSigners();
                if (signers == null || signers.length != 1 ||
                    !uploadCertificate.equals(signers[0].getSignerCertPath().getCertificates().get(0))) {
                    throw new IOException("Bundle has an unsigned entry or an unexpected upload signer.");
                }
            }
            if (!names.contains("base/manifest/AndroidManifest.xml")) {
                throw new IOException("Bundle is missing the base Android manifest.");
            }
        }
        System.out.println("Verified one bundle against the configured upload certificate.");
    }

    private static boolean isSigningMetadata(String path) {
        var name = path.toUpperCase(Locale.ROOT);
        if (!name.startsWith("META-INF/")) {
            return false;
        }
        var file = name.substring("META-INF/".length());
        return !file.contains("/") && (file.equals("MANIFEST.MF") || file.endsWith(".SF") ||
            file.endsWith(".RSA") || file.endsWith(".DSA") || file.endsWith(".EC") || file.startsWith("SIG-"));
    }
}
