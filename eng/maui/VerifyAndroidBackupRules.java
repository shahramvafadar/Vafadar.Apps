import com.android.aapt.ConfigurationOuterClass.Configuration;
import com.android.aapt.Resources;
import java.io.IOException;
import java.util.HashMap;
import java.util.HashSet;
import java.util.Map;
import java.util.Set;
import java.util.zip.ZipEntry;
import java.util.zip.ZipFile;

/** Verifies the compiled rules referenced by the release manifest's OD-10 backup policy. */
class VerifyAndroidBackupRules {
    private static final Set<String> DOMAINS = Set.of(
        "root", "file", "database", "sharedpref", "external",
        "device_root", "device_file", "device_database", "device_sharedpref");

    public static void main(String[] args) throws IOException {
        try (var archive = new ZipFile(args[0])) {
            Resources.ResourceTable table;
            try (var input = archive.getInputStream(requireEntry(archive, "base/resources.pb"))) {
                table = Resources.ResourceTable.parseFrom(input);
            }
            verifyResource(archive, table, args[1], "backup_rules");
            verifyResource(archive, table, args[1], "data_extraction_rules");
        }
        System.out.println("Verified packaged legacy, cloud-backup and device-transfer exclusions.");
    }

    private static ZipEntry requireEntry(ZipFile archive, String name) throws IOException {
        var entry = archive.getEntry(name);
        if (entry == null || entry.isDirectory()) {
            throw new IOException("Missing bundle entry: " + name);
        }
        return entry;
    }

    private static void verifyResource(
        ZipFile archive, Resources.ResourceTable table, String packageName, String ruleName
    ) throws IOException {
        var matches = table.getPackageList().stream().filter(p -> p.getPackageName().equals(packageName))
            .flatMap(p -> p.getTypeList().stream().filter(type -> type.getName().equals("xml")))
            .flatMap(type -> type.getEntryList().stream().filter(entry -> entry.getName().equals(ruleName)))
            .toList();
        if (matches.size() != 1) {
            throw new IOException("Expected exactly one xml/" + ruleName + " resource.");
        }
        var configs = matches.get(0).getConfigValueList();
        if (configs.stream().noneMatch(config -> config.getConfig().equals(Configuration.getDefaultInstance()))) {
            throw new IOException("Missing default configuration for xml/" + ruleName + ".");
        }
        for (var config : configs) {
            var item = config.getValue().getItem();
            if (!item.hasFile() || item.getFile().getType() != Resources.FileReference.Type.PROTO_XML) {
                throw new IOException("Expected compiled XML for xml/" + ruleName + ".");
            }
            var path = item.getFile().getPath();
            if (!path.startsWith("res/") || path.contains("..")) {
                throw new IOException("Unexpected compiled resource path for xml/" + ruleName + ".");
            }
            try (var input = archive.getInputStream(requireEntry(archive, "base/" + path))) {
                verifyRules(Resources.XmlNode.parseFrom(input), ruleName);
            }
        }
    }

    private static void verifyRules(Resources.XmlNode node, String ruleName) throws IOException {
        if (ruleName.equals("backup_rules")) {
            verifyExclusions(requireElement(node, "full-backup-content"));
            return;
        }
        var root = requireElement(node, "data-extraction-rules");
        var sections = new HashSet<>(Set.of("cloud-backup", "device-transfer"));
        for (var child : root.getChildList()) {
            var name = child.getElement().getName();
            if (!sections.remove(name)) {
                throw new IOException("Unexpected or duplicate backup section.");
            }
            verifyExclusions(requireElement(child, name));
        }
        if (!sections.isEmpty()) {
            throw new IOException("Missing cloud-backup or device-transfer section.");
        }
    }

    private static Resources.XmlElement requireElement(Resources.XmlNode node, String name) throws IOException {
        var element = node.getElement();
        if (!node.hasElement() || !element.getName().equals(name) || !element.getNamespaceUri().isEmpty()
            || element.getAttributeCount() != 0) {
            throw new IOException("Unexpected element or attributes in " + name + ".");
        }
        return element;
    }

    private static void verifyExclusions(Resources.XmlElement section) throws IOException {
        var remaining = new HashSet<>(DOMAINS);
        for (var child : section.getChildList()) {
            var exclude = child.getElement();
            if (!child.hasElement() || !exclude.getName().equals("exclude") || !exclude.getNamespaceUri().isEmpty()
                || exclude.getChildCount() != 0 || exclude.getAttributeCount() != 2) {
                throw new IOException("Unexpected rule in " + section.getName() + ".");
            }
            var attributes = new HashMap<String, String>();
            for (var attribute : exclude.getAttributeList()) {
                if (!attribute.getNamespaceUri().isEmpty() || attribute.hasCompiledItem()) {
                    throw new IOException("Unexpected exclusion attribute in " + section.getName() + ".");
                }
                attributes.put(attribute.getName(), attribute.getValue());
            }
            var domain = attributes.get("domain");
            if (domain == null || !attributes.equals(Map.of("domain", domain, "path", ".")) || !remaining.remove(domain)) {
                throw new IOException("Expected one whole-domain exclusion per domain in " + section.getName() + ".");
            }
        }
        if (!remaining.isEmpty()) {
            throw new IOException("Missing domain exclusions in " + section.getName() + ": " + remaining);
        }
    }
}
