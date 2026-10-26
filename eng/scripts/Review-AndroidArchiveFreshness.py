"""D-569: review incremental Android archive retention without installing an app."""

import argparse
import hashlib
import json
from pathlib import Path
import subprocess
import zipfile
import xml.etree.ElementTree as ET


def run(command, directory, log):
    """Keep complete bounded-task diagnostics in the owned evidence directory."""
    completed = subprocess.run(command, cwd=directory, capture_output=True, text=True)
    log.write_text(completed.stdout + completed.stderr, encoding="utf-8")
    if completed.returncode:
        raise RuntimeError(f"Command failed with exit {completed.returncode}; see {log}")


def review_path_guards(repository, work):
    """Exercise deletion authority with owned sentinels and refused parent/sibling paths."""
    cases = [
        ("owned", "net10.0-android", "Exe", "apk", "intermediate/owned.apk", "intermediate/", True, True),
        ("outside", "net10.0-android", "Exe", "apk", "outside.apk", "intermediate/", False, False),
        ("sibling-prefix", "net10.0-android", "Exe", "apk", "intermediate-other/sibling.apk", "intermediate/", False, False),
        ("parent-traversal", "net10.0-android", "Exe", "apk", "intermediate/../parent.apk", "intermediate/", False, False),
        ("non-apk", "net10.0-android", "Exe", "apk", "intermediate/data.db", "intermediate/", False, False),
        ("empty-root", "net10.0-android", "Exe", "apk", "intermediate/empty.apk", "", False, False),
        ("aab", "net10.0-android", "Exe", "aab", "intermediate/bundle.apk", "intermediate/", True, False),
        ("library", "net10.0-android", "Library", "apk", "intermediate/library.apk", "intermediate/", True, False),
        ("desktop", "net10.0", "Exe", "apk", "intermediate/desktop.apk", "intermediate/", True, False),
    ]
    results = []
    for name, framework, kind, package, archive, root, success, delete in cases:
        sentinel = work / archive
        sentinel.parent.mkdir(parents=True, exist_ok=True)
        sentinel.write_bytes(b"owned-path-guard-sentinel")
        project = ET.Element("Project", DefaultTargets="_BuildApkEmbed")
        properties = ET.SubElement(project, "PropertyGroup")
        for key, value in {"TargetFramework": framework, "OutputType": kind, "AndroidPackageFormat": package,
                           "IntermediateOutputPath": root, "ApkFileIntermediate": archive,
                           "AndroidApplication": "true" if kind == "Exe" else "false"}.items():
            ET.SubElement(properties, key).text = value
        ET.SubElement(project, "Import", Project=str(repository / "eng/AndroidArchiveFreshness.targets"))
        ET.SubElement(project, "Target", Name="_BuildApkEmbed")
        source = work / f"{name}.proj"
        ET.ElementTree(project).write(source, encoding="utf-8", xml_declaration=True)
        completed = subprocess.run(["dotnet", "msbuild", str(source), "-nologo", "-v:minimal"],
                                   cwd=work, capture_output=True, text=True)
        (work / f"{name}.log").write_text(completed.stdout + completed.stderr, encoding="utf-8")
        passed = (completed.returncode == 0) == success and (not sentinel.exists()) == delete
        results.append({"Case": name, "Passed": passed, "ExitCode": completed.returncode,
                        "Deleted": not sentinel.exists()})
    (work / "path-guard-results.json").write_text(json.dumps(results, indent=2), encoding="utf-8")
    assert all(item["Passed"] for item in results), "Archive deletion authority regression."
    print(json.dumps({"PathGuardsPassed": len(results), "Result": str(work / "path-guard-results.json")}))


def main():
    """Reproduce the installed task defect and verify retained and removed inputs."""
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--work-directory", required=True, type=Path)
    parser.add_argument("--guards-only", action="store_true")
    parser.add_argument("--android-tasks-assembly", type=Path)
    parser.add_argument("--java-sdk-directory", type=Path)
    args = parser.parse_args()
    repository = Path(__file__).resolve().parents[2]
    fixture = repository / "eng/tests/AndroidArchiveFreshness"
    work = args.work_directory.resolve()
    # A new dedicated directory prevents overwriting either an earlier receipt or an unrelated file.
    work.mkdir(parents=True, exist_ok=False)
    if args.guards_only:
        review_path_guards(repository, work)
        return
    if args.android_tasks_assembly is None or args.java_sdk_directory is None:
        parser.error("The task assembly and Java SDK are required for archive regression.")
    (work / "intermediate").mkdir()
    classes = work / "classes"
    classes.mkdir()
    run([str(args.java_sdk_directory / "bin/javac.exe"), "--release", "8", "-d", str(classes),
         str(fixture / "ArchiveService.java"), str(fixture / "ArchiveProvider.java")],
        work, work / "javac.log")
    service = "META-INF/services/pro.vafadar.archivefixture.ArchiveService"
    resource = "archive-fixture/version.txt"
    removed = "archive-fixture/obsolete.txt"
    first = {service: b"pro.vafadar.archivefixture.ArchiveProvider\n",
             resource: b"original\n", removed: b"remove-this-entry\n"}
    revised = {service: b"# revised descriptor\npro.vafadar.archivefixture.ArchiveProvider\n",
               resource: b"revised\n"}

    def write_jar(entries):
        with zipfile.ZipFile(work / "fixture.jar", "w", zipfile.ZIP_DEFLATED) as archive:
            for compiled in sorted(classes.rglob("*.class")):
                archive.write(compiled, compiled.relative_to(classes).as_posix())
            for name, value in entries.items():
                archive.writestr(name, value)

    write_jar(first)
    with zipfile.ZipFile(work / "resources.apk", "w") as archive:
        archive.writestr("AndroidManifest.xml", b"packaging-fixture-not-installable")
    command = ["dotnet", "msbuild", str(fixture / "ArchiveRegression.proj"), "-nologo", "-v:minimal",
               f"-p:ReviewWorkDirectory={work}", f"-p:ReviewRepositoryRoot={repository}",
               f"-p:ReviewAndroidTasksAssembly={args.android_tasks_assembly.resolve()}"]
    receipts = []

    def publish(label, guarded, expected, absent=()):
        run(command + [f"-p:ReviewArchiveFreshness={str(guarded).lower()}"], work, work / f"{label}.log")
        apk = work / "intermediate/archive.apk"
        with zipfile.ZipFile(apk) as archive:
            actual = {name: archive.read(name) if name in archive.namelist() else None
                      for name in set(first) | set(revised)}
        matches = all(actual[name] == value for name, value in expected.items())
        matches = matches and all(actual[name] is None for name in absent)
        receipt = {"Case": label, "Guarded": guarded, "Matches": matches,
                   "EntriesPresent": {name: value is not None for name, value in actual.items()},
                   "ArchiveSha256": hashlib.sha256(apk.read_bytes()).hexdigest()}
        receipts.append(receipt)
        (work / "results.json").write_text(json.dumps(receipts, indent=2), encoding="utf-8")
        return matches

    assert publish("original-first", False, first), "Initial Java resources were not packaged."
    defect_reproduced = not publish("original-second", False, first)
    assert publish("guarded-first", True, first), "Guarded archive lost original resources."
    assert publish("guarded-second", True, first), "Consecutive guarded archive lost resources."
    write_jar(revised)
    assert publish("guarded-changed-and-removed", True, revised, [removed]), "Stale JAR resources survived."
    print(json.dumps({"InstalledTaskDefectReproduced": defect_reproduced,
                      "GuardedCasesPassed": 3, "Result": str(work / "results.json")}))


if __name__ == "__main__":
    main()
