"""Verify upload identity from the bundletool-decoded base manifest."""

import json
import os
from pathlib import Path
import sys
from xml.etree import ElementTree

ANDROID = "{http://schemas.android.com/apk/res/android}"
# Google Play's mobile submission requirement effective 2026-08-31.
PLAY_MIN_TARGET_SDK = 36
PERMISSION_POLICY = Path(__file__).resolve().parents[1] / "modules" / "zanance-android-permissions.json"


def verify_msal_redirect(application, entra_client_id):
    """Bind the browser callback to the Entra client used to build the bundle."""
    activities = application.findall("activity")
    redirects = [
        activity for activity in activities
        if activity.get(ANDROID + "name", "").endswith(".MsalRedirectActivity")
    ]
    msal_filters = [
        (activity, intent)
        for activity in activities + application.findall("activity-alias")
        for intent in activity.findall("intent-filter")
        if any(data.get(ANDROID + "scheme", "").startswith("msal") for data in intent.findall("data"))
    ]
    if not entra_client_id:
        if redirects or msal_filters:
            raise SystemExit("Bundle exposes an MSAL redirect without an Entra client configured.")
        return

    if len(redirects) != 1 or len(msal_filters) != 1 or msal_filters[0][0] is not redirects[0]:
        raise SystemExit("Expected one MSAL redirect activity and its single browser filter.")
    activity, intent = msal_filters[0]
    if activity.get(ANDROID + "exported") != "true" or activity.findall("intent-filter") != [intent]:
        raise SystemExit("MSAL redirect activity must expose only the expected browser filter.")
    actions = [action.get(ANDROID + "name") for action in intent.findall("action")]
    categories = [category.get(ANDROID + "name", "") for category in intent.findall("category")]
    if actions != ["android.intent.action.VIEW"] or sorted(categories) != [
        "android.intent.category.BROWSABLE", "android.intent.category.DEFAULT"
    ]:
        raise SystemExit("MSAL redirect filter does not match the browser callback action and categories.")
    # Android combines scheme and host declared in separate data elements within a filter.
    data_attributes = [pair for data in intent.findall("data") for pair in data.attrib.items()]
    if len(data_attributes) != 2 or dict(data_attributes) != {
        ANDROID + "scheme": "msal" + entra_client_id, ANDROID + "host": "auth"
    }:
        raise SystemExit("MSAL redirect filter differs from the configured browser callback.")


def verify_manifest(manifest, package, display_version, build_number, edition, entra_client_id):
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
    policy = json.loads(PERMISSION_POLICY.read_text(encoding="utf-8"))
    required = set(policy["required"] + policy["editions"][edition])
    allowed = required | set(policy["optional"])
    permissions = [element for element in manifest if element.tag.startswith("uses-permission")]
    names = {element.get(ANDROID + "name", "") for element in permissions}
    if unexpected := names - allowed:
        raise SystemExit("Unexpected bundle permissions: " + ", ".join(sorted(unexpected)))
    unrestricted = {
        element.get(ANDROID + "name")
        for element in permissions
        if element.tag == "uses-permission" and element.get(ANDROID + "maxSdkVersion") is None
    }
    if missing := required - unrestricted:
        raise SystemExit("Missing unrestricted bundle permissions: " + ", ".join(sorted(missing)))
    verify_msal_redirect(applications[0], entra_client_id)
    print(f"Verified bundle package, version, target API {target_sdk}, release flags, backup policy bindings, "
          "edition permissions and MSAL redirect configuration.")


if __name__ == "__main__":
    verify_manifest(ElementTree.parse(sys.stdin.buffer).getroot(), *sys.argv[1:],
                    entra_client_id=os.environ.get("MicrosoftEntraClientId", ""))
