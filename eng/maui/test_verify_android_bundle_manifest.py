"""Exercise the release bundle permission gate through its command-line entry point."""

import os
from pathlib import Path
import subprocess
import sys
import unittest
from xml.etree import ElementTree


VERIFIER = Path(__file__).with_name("verify-android-bundle-manifest.py")
ANDROID = "{http://schemas.android.com/apk/res/android}"
BASE_MANIFEST = """<manifest xmlns:android="http://schemas.android.com/apk/res/android"
    package="pro.vafadar.zanance" android:versionCode="1" android:versionName="0.1.0">
    <uses-sdk android:minSdkVersion="24" android:targetSdkVersion="36" />
    <uses-permission android:name="android.permission.POST_NOTIFICATIONS" />
    <uses-permission android:name="android.permission.RECEIVE_BOOT_COMPLETED" />
    <application android:allowBackup="false" android:fullBackupContent="@xml/backup_rules"
        android:dataExtractionRules="@xml/data_extraction_rules">
        <provider android:name="pro.vafadar.zanance.CameraFileProvider" android:authorities="pro.vafadar.zanance.camera"
            android:exported="false" android:grantUriPermissions="true">
            <meta-data android:name="android.support.FILE_PROVIDER_PATHS" android:resource="@xml/camera_paths" />
        </provider>
        <provider android:name="microsoft.maui.essentials.fileProvider" android:authorities="pro.vafadar.zanance.fileProvider"
            android:exported="false" android:grantUriPermissions="true">
            <meta-data android:name="android.support.FILE_PROVIDER_PATHS"
                android:resource="@xml/microsoft_maui_essentials_fileprovider_file_paths" />
        </provider>
    </application>
</manifest>"""


class BundlePermissionTests(unittest.TestCase):
    def setUp(self):
        self.manifest = ElementTree.fromstring(BASE_MANIFEST)

    def add_permission(self, name, tag="uses-permission", **limits):
        attributes = {ANDROID + "name": name}
        attributes.update({ANDROID + key: value for key, value in limits.items()})
        ElementTree.SubElement(self.manifest, tag, attributes)

    def verify(self, edition):
        return subprocess.run(
            [sys.executable, str(VERIFIER), "pro.vafadar.zanance", "0.1.0", "1", edition],
            input=ElementTree.tostring(self.manifest, encoding="utf-8"),
            capture_output=True,
            env={**os.environ, "MicrosoftEntraClientId": ""},
            check=False,
        )

    def test_exact_offline_manifest_passes(self):
        result = self.verify("offline")
        self.assertEqual(result.returncode, 0, result.stderr)

    def test_bundle_keeps_exact_minimum_android_version(self):
        for minimum in (None, "23", "26", "Preview"):
            with self.subTest(minimum=minimum):
                self.manifest = ElementTree.fromstring(BASE_MANIFEST)
                sdk = self.manifest.find("uses-sdk")
                if minimum is None:
                    del sdk.attrib[ANDROID + "minSdkVersion"]
                else:
                    sdk.set(ANDROID + "minSdkVersion", minimum)
                result = self.verify("offline")
                self.assertEqual(result.returncode, 1, result.stderr)
                self.assertIn(b"Bundle must declare Android minimum API 24", result.stderr)

    def test_bundle_rejects_sdk_ceiling(self):
        self.manifest.find("uses-sdk").set(ANDROID + "maxSdkVersion", "36")
        result = self.verify("offline")
        self.assertEqual(result.returncode, 1, result.stderr)
        self.assertIn(b"Bundle must not declare android:maxSdkVersion", result.stderr)

    def test_offline_rejects_packaged_microsoft_sign_in(self):
        applications = self.manifest.find("application")
        for name in ("crc648316b0a9aa8cfd61.BrowserTabActivity",
                     "microsoft.identity.client.AuthenticationActivity"):
            with self.subTest(name=name):
                activity = ElementTree.SubElement(applications, "activity", {ANDROID + "name": name})
                result = self.verify("offline")
                applications.remove(activity)
                self.assertEqual(result.returncode, 1, result.stderr)
                self.assertIn(b"Offline bundle registers a Microsoft sign-in activity", result.stderr)

    def test_offline_rejects_packaged_google_sign_in(self):
        application = self.manifest.find("application")
        for tag, name in (
            ("activity", "com.google.android.gms.auth.api.signin.internal.SignInHubActivity"),
            ("service", "com.google.android.gms.auth.api.signin.RevocationBoundService"),
        ):
            with self.subTest(name=name):
                component = ElementTree.SubElement(application, tag, {ANDROID + "name": name})
                result = self.verify("offline")
                application.remove(component)
                self.assertEqual(result.returncode, 1, result.stderr)
                self.assertIn(b"Offline bundle registers a Google sign-in component", result.stderr)

    def test_cloud_keeps_library_sign_in_components(self):
        self.add_permission("android.permission.INTERNET")
        application = self.manifest.find("application")
        for tag, name in (
            ("activity", "microsoft.identity.client.AuthenticationActivity"),
            ("activity", "com.google.android.gms.auth.api.signin.internal.SignInHubActivity"),
            ("service", "com.google.android.gms.auth.api.signin.RevocationBoundService"),
        ):
            ElementTree.SubElement(application, tag, {ANDROID + "name": name})
        result = self.verify("cloud-backup")
        self.assertEqual(result.returncode, 0, result.stderr)

    def test_provider_binding_rejects_public_sharing_provider(self):
        provider = self.manifest.findall("application/provider")[1]
        provider.set(ANDROID + "exported", "true")
        result = self.verify("offline")
        self.assertEqual(result.returncode, 1, result.stderr)
        self.assertIn(b"fileProvider must be non-exported", result.stderr)

    def test_provider_binding_rejects_camera_using_sharing_paths(self):
        metadata = self.manifest.find("application/provider/meta-data")
        metadata.set(ANDROID + "resource", "@xml/microsoft_maui_essentials_fileprovider_file_paths")
        result = self.verify("offline")
        self.assertEqual(result.returncode, 1, result.stderr)
        self.assertIn(b"camera must bind only @xml/camera_paths", result.stderr)

    def test_sdk_qualified_unexpected_permission_fails(self):
        self.add_permission("android.permission.READ_CONTACTS", tag="uses-permission-sdk-23")
        result = self.verify("offline")
        self.assertEqual(result.returncode, 1)
        self.assertIn(b"Unexpected bundle permissions: android.permission.READ_CONTACTS", result.stderr)

    def test_cloud_requires_unrestricted_internet(self):
        internet = "android.permission.INTERNET"
        for tag, limits in (("uses-permission-sdk-23", {}), ("uses-permission", {"maxSdkVersion": "35"})):
            with self.subTest(tag=tag, limits=limits):
                self.manifest = ElementTree.fromstring(BASE_MANIFEST)
                self.add_permission(internet, tag, **limits)
                result = self.verify("cloud-backup")
                self.assertEqual(result.returncode, 1)
                self.assertIn(b"Missing unrestricted bundle permissions: android.permission.INTERNET", result.stderr)
        self.manifest = ElementTree.fromstring(BASE_MANIFEST)
        self.add_permission(internet)
        result = self.verify("cloud-backup")
        self.assertEqual(result.returncode, 0, result.stderr)

    def test_missing_notification_permission_fails(self):
        self.manifest.remove(self.manifest.find("uses-permission"))
        result = self.verify("offline")
        self.assertEqual(result.returncode, 1)
        self.assertIn(b"Missing unrestricted bundle permissions: android.permission.POST_NOTIFICATIONS", result.stderr)


if __name__ == "__main__":
    unittest.main()
