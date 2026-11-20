import com.android.aapt.ConfigurationOuterClass.Configuration;
import com.android.aapt.Resources;
import java.io.IOException;
import java.util.Arrays;
import java.util.HashMap;
import java.util.HashSet;
import java.util.Map;
import java.util.Set;
import java.util.zip.ZipEntry;
import java.util.zip.ZipFile;

/** Verifies the packaged backup exclusions and the two file providers' cache scopes. */
class VerifyAndroidStorageRules {
    private static final String ANDROID = "http://schemas.android.com/apk/res/android";
    private static final String SHARING_RESOURCE = "microsoft_maui_essentials_fileprovider_file_paths";
    private static final Map<String, String> SHARING_PATHS = Map.of(
        "attachments", "attachments/",
        "csv_exports", "csv-export-v1/",
        "accountant_exports", "accountant-export-v1/",
        "report_exports", "report-export/",
        "backup_share", "2203693cc04e0be7f4f024d5f9499e13/");
    private static final Set<String> DOMAINS = Set.of(
        "root", "file", "database", "sharedpref", "external",
        "device_root", "device_file", "device_database", "device_sharedpref");

    public static void main(String[] args) throws IOException {
        if (args.length != 2 && (args.length != 3 || !args[2].equals("--apk-proto"))) {
            throw new IllegalArgumentException("Expected archive path, package and optional --apk-proto.");
        }
        var apk = args.length == 3;
        var prefix = apk ? "" : "base/";
        try (var archive = new ZipFile(args[0])) {
            Resources.ResourceTable table;
            try (var input = archive.getInputStream(requireEntry(archive, prefix + "resources.pb"))) {
                table = Resources.ResourceTable.parseFrom(input);
            }
            var backup = verifyResource(archive, prefix, table, args[1], "backup_rules");
            var extraction = verifyResource(archive, prefix, table, args[1], "data_extraction_rules");
            var camera = verifyResource(archive, prefix, table, args[1], "camera_paths");
            var sharing = verifyResource(archive, prefix, table, args[1], SHARING_RESOURCE);
            if (apk) {
                try (var input = archive.getInputStream(requireEntry(archive, "AndroidManifest.xml"))) {
                    var manifest = Resources.XmlNode.parseFrom(input).getElement();
                    var application = manifest.getChildList().stream().filter(node -> node.hasElement()
                        && node.getElement().getName().equals("application")).map(Resources.XmlNode::getElement).toList();
                    if (application.size() != 1) {
                        throw new IOException("Expected exactly one APK application declaration.");
                    }
                    verifyApkBindings(application.get(0), args[1], backup, extraction, camera, sharing);
                }
            }
        }
        System.out.println("Verified packaged backup exclusions and scoped camera and sharing paths.");
    }

    private static ZipEntry requireEntry(ZipFile archive, String name) throws IOException {
        var entry = archive.getEntry(name);
        if (entry == null || entry.isDirectory()) {
            throw new IOException("Missing Android archive entry: " + name);
        }
        return entry;
    }

    private record BoundResource(Resources.Entry entry, int id) { }

    private static int verifyResource(
        ZipFile archive, String prefix, Resources.ResourceTable table, String packageName, String ruleName
    ) throws IOException {
        var matches = table.getPackageList().stream().filter(p -> p.getPackageName().equals(packageName))
            .flatMap(p -> p.getTypeList().stream().filter(type -> type.getName().equals("xml"))
                .flatMap(type -> type.getEntryList().stream().filter(entry -> entry.getName().equals(ruleName))
                    .map(entry -> new BoundResource(entry, p.getPackageId().getId() << 24
                        | type.getTypeId().getId() << 16 | entry.getEntryId().getId()))))
            .toList();
        if (matches.size() != 1) {
            throw new IOException("Expected exactly one xml/" + ruleName + " resource.");
        }
        var resource = matches.get(0);
        var configs = resource.entry().getConfigValueList();
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
            try (var input = archive.getInputStream(requireEntry(archive, prefix + path))) {
                var node = Resources.XmlNode.parseFrom(input);
                switch (ruleName) {
                    case "camera_paths" -> verifyCachePaths(node, ruleName, Map.of("camera", "camera/"));
                    case SHARING_RESOURCE -> verifyCachePaths(node, ruleName, SHARING_PATHS);
                    default -> verifyRules(node, ruleName);
                }
            }
        }
        return resource.id();
    }

    private static void verifyApkBindings(Resources.XmlElement application, String packageName,
        int backup, int extraction, int camera, int sharing) throws IOException {
        requireBoolean(application, "allowBackup", false);
        requireReference(application, "fullBackupContent", backup);
        requireReference(application, "dataExtractionRules", extraction);
        verifyApkProvider(application, packageName + ".CameraFileProvider", packageName + ".camera", camera);
        verifyApkProvider(application, "microsoft.maui.essentials.fileProvider", packageName + ".fileProvider", sharing);
    }

    private static void verifyApkProvider(Resources.XmlElement application, String name, String authority, int resource)
        throws IOException {
        var candidates = application.getChildList().stream().map(Resources.XmlNode::getElement)
            .filter(element -> element.getName().equals("provider"))
            .filter(element -> name.equals(literal(element, "name"))
                || Arrays.asList(literal(element, "authorities").split(";")).contains(authority))
            .toList();
        if (candidates.size() != 1 || !name.equals(literal(candidates.get(0), "name"))
            || !authority.equals(literal(candidates.get(0), "authorities"))) {
            throw new IOException("Unexpected APK provider for " + authority + ".");
        }
        var provider = candidates.get(0);
        requireBoolean(provider, "exported", false);
        requireBoolean(provider, "grantUriPermissions", true);
        var metadata = provider.getChildList().stream().map(Resources.XmlNode::getElement)
            .filter(element -> element.getName().equals("meta-data")
                && literal(element, "name").equals("android.support.FILE_PROVIDER_PATHS")).toList();
        if (metadata.size() != 1) {
            throw new IOException("Expected one APK paths resource binding for " + authority + ".");
        }
        requireReference(metadata.get(0), "resource", resource);
    }

    private static Resources.XmlAttribute androidAttribute(Resources.XmlElement element, String name) {
        var matches = element.getAttributeList().stream().filter(attribute ->
            attribute.getNamespaceUri().equals(ANDROID) && attribute.getName().equals(name)).toList();
        return matches.size() == 1 ? matches.get(0) : Resources.XmlAttribute.getDefaultInstance();
    }

    private static String literal(Resources.XmlElement element, String name) {
        var attribute = androidAttribute(element, name);
        return attribute.hasCompiledItem() ? "" : attribute.getValue();
    }

    private static void requireBoolean(Resources.XmlElement element, String name, boolean expected) throws IOException {
        var value = androidAttribute(element, name).getCompiledItem().getPrim();
        if (value.getOneofValueCase() != Resources.Primitive.OneofValueCase.BOOLEAN_VALUE
            || value.getBooleanValue() != expected) {
            throw new IOException("Unexpected Android " + name + " in APK " + element.getName() + ".");
        }
    }

    private static void requireReference(Resources.XmlElement element, String name, int expected) throws IOException {
        var value = androidAttribute(element, name).getCompiledItem();
        if (!value.hasRef() || expected == 0 || value.getRef().getId() != expected) {
            throw new IOException("Unexpected APK resource binding for android:" + name + " in " + element.getName() + ".");
        }
    }

    private static void verifyCachePaths(Resources.XmlNode node, String resource, Map<String, String> expected)
        throws IOException {
        var remaining = new HashMap<>(expected);
        for (var child : requireElement(node, "paths").getChildList()) {
            var path = child.getElement();
            if (!child.hasElement() || !path.getName().equals("cache-path") || !path.getNamespaceUri().isEmpty()
                || path.getChildCount() != 0 || path.getAttributeCount() != 2) {
                throw new IOException("Unexpected file access scope in xml/" + resource + ".");
            }
            var attributes = new HashMap<String, String>();
            for (var attribute : path.getAttributeList()) {
                if (!attribute.getNamespaceUri().isEmpty() || attribute.hasCompiledItem()) {
                    throw new IOException("Expected literal cache path attributes in xml/" + resource + ".");
                }
                attributes.put(attribute.getName(), attribute.getValue());
            }
            var name = attributes.get("name");
            var value = attributes.get("path");
            if (name == null || value == null || !value.equals(remaining.remove(name))) {
                throw new IOException("Unexpected or duplicate cache path in xml/" + resource + ".");
            }
        }
        if (!remaining.isEmpty()) {
            throw new IOException("Missing approved cache paths in xml/" + resource + ".");
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
