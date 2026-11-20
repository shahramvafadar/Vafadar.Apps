"""Verify upload identity from the bundletool-decoded base manifest."""

import sys
from xml.etree import ElementTree

ANDROID = "{http://schemas.android.com/apk/res/android}"
# Google Play's mobile submission requirement effective 2026-08-31.
PLAY_MIN_TARGET_SDK = 36


def verify_manifest(manifest, package, display_version, build_number, edition):
    """Require the requested release identity, Play target SDK and edition policy."""
    if manifest.tag != "manifest":
        raise SystemExit("Expected a decoded Android manifest.")
    expected = {
        "package": package,
        ANDROID + "versionName": display_version,
        ANDROID + "versionCode": build_number,
    }
    for attribute, value in expected.items():
        if manifest.get(attribute) != value:
            raise SystemExit(f"Bundle {attribute.removeprefix(ANDROID)} differs from the requested release.")

    sdk_elements = manifest.findall("uses-sdk")
    if len(sdk_elements) != 1:
        raise SystemExit("Expected exactly one uses-sdk in the base manifest.")
    target_sdk = sdk_elements[0].get(ANDROID + "targetSdkVersion", "")
    if not target_sdk.isascii() or not target_sdk.isdecimal():
        raise SystemExit("Bundle does not declare a numeric android:targetSdkVersion.")
    if int(target_sdk) < PLAY_MIN_TARGET_SDK:
        raise SystemExit(f"Bundle targets API {target_sdk}; Google Play requires API {PLAY_MIN_TARGET_SDK} or higher.")

    applications = manifest.findall("application")
    if len(applications) != 1:
        raise SystemExit("Expected exactly one application in the base manifest.")
    for attribute in ("debuggable", "testOnly"):
        if applications[0].get(ANDROID + attribute, "false") != "false":
            raise SystemExit(f"Bundle enables or does not resolve android:{attribute}.")

    # OD-10 keeps device-bound storage out of OS backup and device transfer.
    backup_policy = {
        "allowBackup": "false",
        "fullBackupContent": "@xml/backup_rules",
        "dataExtractionRules": "@xml/data_extraction_rules",
    }
    for attribute, value in backup_policy.items():
        if applications[0].get(ANDROID + attribute) != value:
            raise SystemExit(f"Bundle does not preserve the required android:{attribute} backup policy.")

    if edition not in ("offline", "cloud-backup"):
        raise SystemExit("Unknown Android bundle edition.")
    permissions = [element for element in manifest if element.tag.startswith("uses-permission")]
    names = {element.get(ANDROID + "name") for element in permissions}
    if "android.permission.ACCESS_NETWORK_STATE" in names:
        raise SystemExit("Release bundle requests ACCESS_NETWORK_STATE.")
    if edition == "offline" and "android.permission.INTERNET" in names:
        raise SystemExit("Offline release bundle requests INTERNET.")
    if edition == "cloud-backup" and not any(
        element.tag == "uses-permission"
        and element.get(ANDROID + "name") == "android.permission.INTERNET"
        and element.get(ANDROID + "maxSdkVersion") is None
        for element in permissions
    ):
        raise SystemExit("Cloud backup release bundle lacks unrestricted INTERNET.")
    print(f"Verified bundle package, version, target API {target_sdk}, release flags, backup policy bindings "
          "and edition network permissions.")


if __name__ == "__main__":
    verify_manifest(ElementTree.parse(sys.stdin.buffer).getroot(), *sys.argv[1:])
