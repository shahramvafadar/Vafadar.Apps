"""Verify upload identity from the bundletool-decoded base manifest."""

import sys
from xml.etree import ElementTree

ANDROID = "{http://schemas.android.com/apk/res/android}"


def verify_manifest(manifest, package, display_version, build_number, edition):
    """Require the requested release identity and a non-debuggable application."""
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

    applications = manifest.findall("application")
    if len(applications) != 1:
        raise SystemExit("Expected exactly one application in the base manifest.")
    for attribute in ("debuggable", "testOnly"):
        if applications[0].get(ANDROID + attribute, "false") != "false":
            raise SystemExit(f"Bundle enables or does not resolve android:{attribute}.")
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
    print("Verified bundle package, version, release flags and edition network permissions.")


if __name__ == "__main__":
    verify_manifest(ElementTree.parse(sys.stdin.buffer).getroot(), *sys.argv[1:])
