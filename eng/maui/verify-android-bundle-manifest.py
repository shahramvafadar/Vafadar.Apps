"""Verify upload identity from the bundletool-decoded base manifest."""

import sys
from xml.etree import ElementTree

ANDROID = "{http://schemas.android.com/apk/res/android}"


def verify_manifest(manifest, package, display_version, build_number):
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
    print("Verified bundle package, version and release application flags.")


if __name__ == "__main__":
    verify_manifest(ElementTree.parse(sys.stdin.buffer).getroot(), *sys.argv[1:])
