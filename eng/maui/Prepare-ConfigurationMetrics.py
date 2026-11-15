"""Build the pinned Android Core source modification in repository-owned state only."""
from __future__ import annotations

import argparse
import hashlib
import json
import os
from pathlib import Path, PurePosixPath
import platform
import shutil
import stat
import struct
import subprocess
import sys
import tarfile
import urllib.request
import xml.etree.ElementTree as ET
import zipfile


REPO = Path(__file__).resolve().parents[2]
ENG = Path(__file__).resolve().parent
PROFILE_PATH = ENG / "configuration-metrics.inputs.json"
PROFILE = json.loads(PROFILE_PATH.read_text(encoding="utf-8-sig"))


def require(value, message):
    """Refuse unknown inputs instead of repairing or replacing an existing candidate."""
    if not value:
        raise RuntimeError(message)


def digest(path):
    """Hash without loading a large public source/tool archive into memory."""
    result = hashlib.sha256()
    with Path(path).open("rb") as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b""):
            result.update(block)
    return result.hexdigest()


def write_json(path, value):
    """Write a complete task-owned receipt; never emit credential/environment values."""
    path.parent.mkdir(parents=True, exist_ok=True)
    temporary = path.with_suffix(path.suffix + ".writing")
    with temporary.open("x", encoding="utf-8") as stream:
        json.dump(value, stream, indent=2)
        stream.write("\n")
    temporary.replace(path)


def bounded_target(root, relative):
    """Accept archive-relative regular paths only, including on Windows."""
    name = PurePosixPath(relative)
    require(not name.is_absolute() and ".." not in name.parts and ":" not in relative,
            "Archive path escaped its owned root.")
    result = (root / Path(*name.parts)).resolve()
    require(result.is_relative_to(root.resolve()), "Archive target escaped its root.")
    return result


def native_path(path):
    """Use checked extended-length paths for archive files without a machine-policy change."""
    text = str(path.resolve())
    return "\\\\?\\" + text if os.name == "nt" else text


def download(root, key, spec, seeds):
    """Use a verified supplied public archive or perform one anonymous pinned download."""
    if key in seeds:
        path = Path(seeds[key]).resolve()
        require(path.is_relative_to(REPO / "artifacts"), "Input seeds must be project-owned artifacts.")
        require(path.is_file() and digest(path) == spec["sha256"], "Supplied input digest refused: " + key)
        return path
    target = root / "inputs" / (key + (".tar.gz" if spec["url"].endswith(".tar.gz") else ".zip"))
    target.parent.mkdir(parents=True, exist_ok=True)
    if target.exists():
        require(digest(target) == spec["sha256"], "Existing input digest refused: " + key)
        return target
    partial = target.with_suffix(target.suffix + ".partial")
    require(not partial.exists(), "Prior partial download remains; preserve it: " + key)
    # Do not reuse ambient proxy authentication or credential providers for these public URLs.
    opener = urllib.request.build_opener(urllib.request.ProxyHandler({}))
    with opener.open(spec["url"], timeout=120) as response, partial.open("xb") as stream:
        shutil.copyfileobj(response, stream, 1024 * 1024)
    require(digest(partial) == spec["sha256"], "Downloaded input digest refused: " + key)
    partial.replace(target)
    return target


def unpack_zip(archive, root, prefix=""):
    """Extract checked files and retain executable bits required by Linux SDK tools."""
    require(not root.exists(), "Existing extraction requires its completed receipt.")
    root.mkdir(parents=True)
    with zipfile.ZipFile(archive) as entries:
        require(entries.testzip() is None, "Pinned ZIP CRC refused.")
        for entry in entries.infolist():
            if entry.is_dir() or not entry.filename.startswith(prefix):
                continue
            relative = entry.filename[len(prefix):]
            target = bounded_target(root, relative)
            mode = entry.external_attr >> 16
            require(not stat.S_ISLNK(mode), "Unexpected SDK ZIP link; no unreviewed extraction.")
            os.makedirs(native_path(target.parent), exist_ok=True)
            with entries.open(entry) as source, open(native_path(target), "xb") as output:
                shutil.copyfileobj(source, output, 1024 * 1024)
            if os.name != "nt" and mode & 0o111:
                target.chmod(mode & 0o777)


def unpack_java(archive, root):
    """Extract the pinned JDK; Linux legal-notice links must remain inside the JDK root."""
    if archive.name.endswith(".zip"):
        unpack_zip(archive, root)
        return
    require(not root.exists(), "Existing JDK extraction requires its completed receipt.")
    root.mkdir(parents=True)
    links = []
    with tarfile.open(archive, "r:gz") as entries:
        for entry in entries:
            target = bounded_target(root, entry.name)
            if entry.isdir():
                target.mkdir(parents=True, exist_ok=True)
            elif entry.isfile():
                target.parent.mkdir(parents=True, exist_ok=True)
                with entries.extractfile(entry) as source, target.open("xb") as output:
                    shutil.copyfileobj(source, output, 1024 * 1024)
                target.chmod(entry.mode & 0o777)
            elif entry.issym() or entry.islnk():
                links.append((entry, target))
            else:
                raise RuntimeError("Unexpected JDK archive entry kind.")
    for entry, target in links:
        linked = ((target.parent / entry.linkname) if entry.issym() else (root / entry.linkname)).resolve()
        require(linked.is_relative_to(root.resolve()) and linked.exists(), "JDK link target refused.")
        target.parent.mkdir(parents=True, exist_ok=True)
        if entry.issym():
            target.symlink_to(entry.linkname)
        else:
            os.link(linked, target)


def restore_linux_sdk_executables(sdk, receipt):
    """Restore workload installer execute permissions omitted from the pinned Linux NuGet archive."""
    if not sys.platform.startswith("linux"):
        return
    host = (sdk / "tools/Linux").resolve()
    require(host.is_relative_to(REPO / "artifacts") and host.is_dir(), "Owned Linux SDK host-tools root required.")
    if receipt.exists():
        previous = json.loads(receipt.read_text())
        require(all((host / item["Path"]).is_file() and
                    digest(host / item["Path"]) == item["SHA256"] and
                    (host / item["Path"]).stat().st_mode & 0o111 == 0o111
                    for item in previous["Executables"]), "Installed Linux SDK tool bytes or modes changed.")
        return
    installed = []
    for path in sorted(host.rglob("*")):
        require(not path.is_symlink(), "Unreviewed SDK tool link refused.")
        if not path.is_file():
            continue
        require(path.resolve().is_relative_to(host), "SDK tool escaped its owned root.")
        with path.open("rb") as stream:
            header = stream.read(64)
            executable = header.startswith(b"#!")
            if header.startswith(b"\x7fELF"):
                require(len(header) == 64 and header[4:6] == b"\x02\x01", "Unexpected Linux host ELF encoding.")
                kind, machine = struct.unpack_from("<HH", header, 16)
                require(machine == 62, "Unexpected Linux host-tool architecture.")
                executable = kind == 2
                if kind == 3:
                    offset = struct.unpack_from("<Q", header, 32)[0]
                    size, count = struct.unpack_from("<HH", header, 54)
                    require(size >= 56 and count <= 256 and offset + size * count <= path.stat().st_size,
                            "Linux SDK program-header bounds refused.")
                    for index in range(count):
                        stream.seek(offset + index * size)
                        if struct.unpack("<I", stream.read(4))[0] == 3:  # PT_INTERP identifies executable PIEs.
                            executable = True
                            break
        if executable:
            before = path.stat().st_mode & 0o777
            expected = digest(path)
            path.chmod(before | 0o111)
            require(digest(path) == expected, "SDK tool bytes changed while installing execute mode.")
            installed.append({"Path": path.relative_to(host).as_posix(), "SHA256": expected,
                              "BeforeMode": oct(before), "InstalledMode": oct(before | 0o111)})
    require(any(item["Path"] == "aapt2" for item in installed), "Pinned Linux AAPT2 executable was not installed.")
    write_json(receipt, {"Executables": installed, "NoToolBytesChanged": True,
                         "NoSharedLibrariesMarkedExecutable": True})


def extract_source(archive, source, receipt):
    """Extract the complete three-project build closure, not unrelated tests/image corpora."""
    if receipt.exists():
        return json.loads(receipt.read_text())
    require(not source.exists(), "Incomplete source extraction remains; do not silently replace it.")
    source.mkdir(parents=True)
    files = {}
    with tarfile.open(archive, "r:gz") as entries:
        for entry in entries:
            parts = PurePosixPath(entry.name).parts
            if len(parts) < 2:
                continue
            relative = "/".join(parts[1:])
            selected = ("/" not in relative or
                        relative.startswith(tuple(PROFILE["source_closure_prefixes"])) or
                        relative.startswith("src/") and relative.count("/") == 1)
            if not selected or entry.isdir():
                continue
            require(entry.isfile(), "Unexpected source-closure link or special file.")
            target = bounded_target(source, relative)
            os.makedirs(native_path(target.parent), exist_ok=True)
            with entries.extractfile(entry) as input_stream, open(native_path(target), "xb") as output:
                shutil.copyfileobj(input_stream, output, 1024 * 1024)
            files[relative] = digest(target)
    require("src/Core/src/Core.csproj" in files and "src/Essentials/src/Essentials.csproj" in files and
            "src/Graphics/src/Graphics/Graphics.csproj" in files, "Source closure misses a real project.")
    write_json(receipt, files)
    return files


def restore_source_executables(archive, source, original, receipt):
    """Preserve executable source scripts from the verified archive, including Gradle's Linux wrapper."""
    if not sys.platform.startswith("linux"):
        return
    if receipt.exists():
        entries = json.loads(receipt.read_text())
    else:
        entries = []
        with tarfile.open(archive, "r:gz") as members:
            for item in members:
                relative = "/".join(PurePosixPath(item.name).parts[1:])
                if item.isfile() and relative in original and item.mode & 0o111:
                    entries.append({"Path": relative, "Mode": item.mode & 0o777,
                                    "SHA256": original[relative]})
        require(any(item["Path"] == "src/Core/AndroidNative/gradlew" for item in entries),
                "Original executable Gradle wrapper metadata missing.")
        write_json(receipt, entries)
    for item in entries:
        target = bounded_target(source, item["Path"])
        require(target.is_file() and digest(target) == item["SHA256"], "Executable source script bytes changed.")
        target.chmod(item["Mode"])
        require(target.stat().st_mode & 0o777 == item["Mode"], "Source executable mode installation failed.")


def isolated_environment(root, dotnet, java, android, workload_root):
    """Do not inherit account tokens, credential plugins or global Java/CLI configuration."""
    allowed = {"SYSTEMROOT", "WINDIR", "COMSPEC", "OS", "NUMBER_OF_PROCESSORS", "PATHEXT",
               "PROCESSOR_ARCHITECTURE", "PROGRAMFILES", "PROGRAMFILES(X86)", "PROGRAMW6432"}
    env = {key: value for key, value in os.environ.items() if key.upper() in allowed}
    directories = [dotnet.parent, java / "bin"]
    for name in ["git", "node", "python", "pwsh"]:
        found = shutil.which(name)
        if found:
            directories.append(Path(found).resolve().parent)
    directories += [Path(os.environ.get("SYSTEMROOT", "/")) / "System32",
                    Path(os.environ.get("SYSTEMROOT", "/"))] if os.name == "nt" else [Path("/usr/local/bin"), Path("/usr/bin"), Path("/bin")]
    for name in ["cli-home", "temp", "profile", "appdata", "localappdata", "packages", "http-cache", "plugins", "java-home", "gradle-home"]:
        (root / name).mkdir(parents=True, exist_ok=True)
    env.update({
        "PATH": os.pathsep.join(dict.fromkeys(str(p) for p in directories)),
        "DOTNET_ROOT": str(dotnet.parent), "DOTNET_ROOT_X64": str(dotnet.parent), "DOTNET_MULTILEVEL_LOOKUP": "0",
        "DOTNET_CLI_HOME": str(root / "cli-home"), "DOTNET_CLI_TELEMETRY_OPTOUT": "1",
        "DOTNET_SKIP_FIRST_TIME_EXPERIENCE": "1", "DOTNET_GENERATE_ASPNET_CERTIFICATE": "false",
        "DOTNET_ADD_GLOBAL_TOOLS_TO_PATH": "0", "DOTNET_CLI_WORKLOAD_UPDATE_NOTIFY_DISABLE": "true", "DOTNET_NOLOGO": "1",
        "DOTNETSDK_WORKLOAD_MANIFEST_ROOTS": str(workload_root / "sdk-manifests"),
        "DOTNETSDK_WORKLOAD_MANIFEST_IGNORE_DEFAULT_ROOTS": "1",
        "DOTNETSDK_WORKLOAD_PACK_ROOTS": str(workload_root) + os.pathsep + str(dotnet.parent),
        "TEMP": str(root / "temp"), "TMP": str(root / "temp"), "HOME": str(root / "profile"),
        "USERPROFILE": str(root / "profile"), "APPDATA": str(root / "appdata"), "LOCALAPPDATA": str(root / "localappdata"),
        "NUGET_PACKAGES": str(root / "packages"), "NUGET_HTTP_CACHE_PATH": str(root / "http-cache"),
        "NUGET_PLUGINS_CACHE_PATH": str(root / "plugins"), "NUGET_CREDENTIALPROVIDER_SESSIONTOKENCACHE_ENABLED": "false",
        "NuGetInteractive": "false", "JAVA_HOME": str(java), "JAVA_TOOL_OPTIONS": "-Duser.home=" + str(root / "java-home"),
        "ANDROID_HOME": str(android), "ANDROID_SDK_ROOT": str(android), "GRADLE_USER_HOME": str(root / "gradle-home"),
        "GRADLE_OPTS": "-Dorg.gradle.daemon=false -Dorg.gradle.workers.max=2 -Dorg.gradle.jvmargs=-Xmx1536m"
    })
    return env


def run(command, cwd, env, log, witnesses=()):
    """Keep native exit status and the complete failed attempt; never auto-retry it."""
    command_key = hashlib.sha256(json.dumps([str(x) for x in command]).encode()).hexdigest()
    state = log.with_suffix(".terminal.json")
    if state.exists():
        previous = json.loads(state.read_text())
        if previous["ExitCode"] == 0:
            require(previous["CommandSHA256"] == command_key and all(
                Path(p).is_file() and digest(p) == value for p, value in previous["Witnesses"].items()),
                "A successful preparation phase changed; do not silently repeat it.")
            return
        attempt = 2
        while log.with_name(log.stem + ".attempt-" + str(attempt) + log.suffix).exists():
            attempt += 1
        log = log.with_name(log.stem + ".attempt-" + str(attempt) + log.suffix)
    with log.open("x", encoding="utf-8") as output:
        result = subprocess.run([str(x) for x in command], cwd=cwd, env=env, stdout=output, stderr=subprocess.STDOUT)
    write_json(state, {"ExitCode": result.returncode, "CommandSHA256": command_key,
                       "Witnesses": {str(p): digest(p) for p in witnesses if Path(p).is_file()}})
    require(result.returncode == 0, "Source preparation command refused; inspect " + str(log.relative_to(REPO)))


def make_nuget_config(path):
    """Clear inherited sources/mapping and declare anonymous official feeds only."""
    config = ET.Element("configuration")
    sources = ET.SubElement(config, "packageSources")
    ET.SubElement(sources, "clear")
    mapping = ET.SubElement(config, "packageSourceMapping")
    ET.SubElement(mapping, "clear")
    for index, url in enumerate(PROFILE["anonymous_feeds"]):
        name = "maui-public-" + str(index)
        ET.SubElement(sources, "add", {"key": name, "value": url})
        item = ET.SubElement(mapping, "packageSource", {"key": name})
        ET.SubElement(item, "package", {"pattern": "*"})
    ET.ElementTree(config).write(path, encoding="utf-8", xml_declaration=True)


def package_file(path, entries):
    """Do not let current timestamps make otherwise identical package entries differ."""
    require(not path.exists(), "Private package version already exists without a matching receipt.")
    with zipfile.ZipFile(path, "x", zipfile.ZIP_DEFLATED, compresslevel=9) as package:
        for name, data in sorted(entries.items()):
            info = zipfile.ZipInfo(name, (2026, 11, 15, 0, 0, 0))
            info.compress_type = zipfile.ZIP_DEFLATED
            info.external_attr = 0o100644 << 16
            package.writestr(info, data)


def main():
    """Prepare one immutable package, with public-source/API and selected-asset witnesses."""
    parser = argparse.ArgumentParser()
    parser.add_argument("--work-root", default="artifacts/maui-core")
    parser.add_argument("--dotnet", required=True)
    parser.add_argument("--android-sdk", required=True)
    parser.add_argument("--input-seeds")
    parser.add_argument("--nuget-fallback", action="append", default=[])
    args = parser.parse_args()
    system = "windows" if sys.platform == "win32" else "linux" if sys.platform.startswith("linux") else "unsupported"
    require(system != "unsupported" and platform.machine().lower() in ["amd64", "x86_64"], "Only reviewed Windows/Linux x64 producers are supported.")
    root = (REPO / args.work_root).resolve()
    require(root.is_relative_to(REPO / "artifacts"), "Preparation root must remain under repository artifacts.")
    root.mkdir(parents=True, exist_ok=True)
    inputs = [PROFILE_PATH, Path(__file__), ENG / "PrivateCoreVersion.targets", ENG / "CoreBuildProof/Program.cs",
              ENG / "CoreBuildProof/CoreBuildProof.csproj", REPO / PROFILE["patch"]]
    fingerprint = hashlib.sha256("\n".join(digest(p) for p in inputs).encode()).hexdigest()
    receipt = root / ("package-proof." + PROFILE["private_version"] + ".json")
    output = root / "feed" / (PROFILE["private_package"] + "." + PROFILE["private_version"] + ".nupkg")
    if receipt.exists():
        prior = json.loads(receipt.read_text())
        require(prior["RecipeSHA256"] == fingerprint and output.is_file() and digest(output) == prior["PackageSHA256"],
                "Prepared package/recipe changed; no silent rebuild of an immutable version.")
        print("Verified existing private Core package; no successful build/API check repeated.")
        return
    require(shutil.disk_usage(root).free > 8_000_000_000, "Insufficient space for the isolated producer.")
    seeds = json.loads(Path(args.input_seeds).read_text()) if args.input_seeds else {}
    specs = {name: PROFILE[name] for name in ["source", "official_core", "android_manifest", "android_reference", "api_tool"]}
    specs["android_sdk"] = PROFILE["android_sdk"][system]
    specs["java"] = PROFILE["java"][system]
    archives = {name: download(root, name, spec, seeds) for name, spec in specs.items()}
    source = root / "source"
    original = extract_source(archives["source"], source, root / "source-archive-files.json")
    restore_source_executables(archives["source"], source, original, root / "source-executable-modes.json")
    require(digest(REPO / PROFILE["patch"]) == PROFILE["patch_sha256"], "Reviewed source patch changed.")
    patched = root / "source-patched.json"
    if not patched.exists():
        for item in PROFILE["patch_files"]:
            require(digest(source / item["path"]) == item["before"], "Patch precondition bytes changed.")
        require(shutil.which("git") is not None, "Git is required to apply the reviewed byte-preserving patch.")
        command = ["git", "-c", "core.autocrlf=false", "-c", "core.eol=lf", "apply",
                   "--directory=" + source.relative_to(REPO).as_posix(), str(REPO / PROFILE["patch"])]
        # No environment mutation or source download occurs in this checked Git invocation.
        with (root / "patch.log").open("x") as log:
            result = subprocess.run(command, cwd=REPO, stdout=log, stderr=subprocess.STDOUT)
        require(result.returncode == 0, "Reviewed patch application failed.")
        write_json(patched, PROFILE["patch_files"])
    for item in PROFILE["patch_files"]:
        require(digest(source / item["path"]) == item["after"], "Patched source bytes changed.")
    workload = root / "android-toolchain"
    for name, package in [("android_sdk", "Microsoft.Android.Sdk.Windows" if system == "windows" else "Microsoft.Android.Sdk.Linux"),
                          ("android_reference", "Microsoft.Android.Ref.36")]:
        target = workload / "packs" / package / "36.1.115"
        done = root / (name + "-extracted.json")
        if not done.exists():
            with zipfile.ZipFile(archives[name]) as archive:
                require(b"The MIT License" in archive.read("LICENSE.TXT"), "SDK license differs from the reviewed MIT notice.")
            unpack_zip(archives[name], target)
            write_json(done, {"ArchiveSHA256": specs[name]["sha256"]})
    restore_linux_sdk_executables(workload / "packs/Microsoft.Android.Sdk.Linux/36.1.115",
                                 root / "linux-sdk-executable-modes.json")
    for band in ["10.0.100", "10.0.400"]:
        target = workload / "sdk-manifests" / band / "microsoft.net.sdk.android" / "36.1.115"
        if not target.exists():
            unpack_zip(archives["android_manifest"], target, "data/")
    java_root = root / "jdk"
    if not (root / "java-extracted.json").exists():
        unpack_java(archives["java"], java_root)
        write_json(root / "java-extracted.json", {"ArchiveSHA256": specs["java"]["sha256"]})
    java = next(p for p in java_root.iterdir() if (p / "bin" / ("java.exe" if system == "windows" else "java")).is_file())
    dotnet = Path(args.dotnet).resolve()
    android = Path(args.android_sdk).resolve()
    require((android / "platforms/android-36/android.jar").is_file() and (android / "build-tools/36.0.0").is_dir(),
            "Existing Android36/build-tools36.0.0 required; no implicit SDK installation/license acceptance.")
    env = isolated_environment(root, dotnet, java, android, workload)
    version = subprocess.run([str(dotnet), "--version"], cwd=REPO, env=env, capture_output=True, text=True)
    require(version.returncode == 0 and version.stdout.strip() == PROFILE["host_sdk"], "Repository source-build SDK version changed.")
    make_nuget_config(source / "NuGet.config")
    (source / "Directory.Packages.props").write_text('<Project><PropertyGroup><ManagePackageVersionsCentrally>false</ManagePackageVersionsCentrally></PropertyGroup></Project>\n')
    build = source / "artifacts-private"
    properties = ["-p:TargetFrameworks=net10.0-android36.0", "-p:TargetFramework=net10.0-android36.0",
                  "-p:IncludeAndroidTargetFrameworks=true", "-p:ContinuousIntegrationBuild=true", "-p:CI=false",
                  "-p:AndroidRestoreOnBuild=false", "-p:AcceptAndroidSDKLicenses=false", "-p:NuGetInteractive=false",
                  "-p:EnableSourceControlManagerQueries=false", "-p:EnableSourceLink=false",
                  "-p:SourceRevisionId=" + PROFILE["upstream_commit"], "-p:ArtifactsDir=" + str(build) + "/",
                  "-p:AndroidSdkDirectory=" + str(android), "-p:JavaSdkDirectory=" + str(java),
                  "-p:RestoreConfigFile=" + str(source / "NuGet.config"), "-p:ManagePackageVersionsCentrally=false",
                  "-p:TreatWarningsAsErrors=true", "-p:UseSharedCompilation=false", "-p:LangVersion=14.0",
                  "-p:CustomAfterMicrosoftCommonTargets=" + str(ENG / "PrivateCoreVersion.targets"),
                  "-p:InformationalVersion=" + PROFILE["informational_version"]]
    properties += ["-p:Include" + name + "TargetFrameworks=false" for name in ["Windows", "Ios", "MacCatalyst", "MacOS", "Tizen"]]
    if args.nuget_fallback:
        properties.append("-p:RestoreFallbackFolders=" + "%3B".join(str(Path(p).resolve()) for p in args.nuget_fallback))
    project = source / "src/Core/src/Core.csproj"
    out = build / "bin/Core/Release/net10.0-android36.0"
    run([dotnet, "restore", project, *properties, "--verbosity", "minimal"], source, env, root / "restore.log",
        [build / "obj/Core/project.assets.json"])
    # Keep the actual portable PDB for source binding. Android packages do not publish converted Windows symbol-store PDBs.
    run([dotnet, "build", project, "-c", "Release", "-f", "net10.0-android36.0", "--no-restore", *properties,
         "-p:PublishWindowsPdb=false",
         "--verbosity", "minimal"], source, env, root / "build.log",
        [out / name for name in ["Microsoft.Maui.dll", "Microsoft.Maui.pdb", "Microsoft.Maui.aar", "maui.aar"]])
    proof_flags = ["-p:ContinuousIntegrationBuild=true", "-p:UseSharedCompilation=false", "-p:ArtifactsPath=" + str(root / "proof-build"),
                   "-p:RestoreConfigFile=" + str(source / "NuGet.config")]
    proof_project = ENG / "CoreBuildProof/CoreBuildProof.csproj"
    run([dotnet, "restore", proof_project, *proof_flags, "--verbosity", "minimal"], REPO, env, root / "proof-restore.log")
    run([dotnet, "build", proof_project, "-c", "Release", "--no-restore", *proof_flags, "--verbosity", "minimal"], REPO, env, root / "proof-build.log")
    run([dotnet, root / "proof-build/bin/CoreBuildProof/release/CoreBuildProof.dll", PROFILE_PATH,
         out / "Microsoft.Maui.dll", out / "Microsoft.Maui.pdb", source, root / "source-archive-files.json", root / "core-proof.json"],
        REPO, env, root / "source-proof.log")
    api_root = root / "api-tool"
    if not api_root.exists():
        unpack_zip(archives["api_tool"], api_root)
    official = root / "official-core"
    if not official.exists():
        unpack_zip(archives["official_core"], official)
    # Linux generates LF for these two text entries; all packaged entries still use exact official bytes.
    # Refuse any other difference, including XML whitespace, ordering, values or additional resources.
    aar_proof = []
    for name in ["Microsoft.Maui.aar", "maui.aar"]:
        with zipfile.ZipFile(out / name) as left, zipfile.ZipFile(official / "lib/net10.0-android36.0" / name) as right:
            lh = {n: hashlib.sha256(left.read(n)).hexdigest() for n in left.namelist() if not n.endswith("/")}
            rh = {n: hashlib.sha256(right.read(n)).hexdigest() for n in right.namelist() if not n.endswith("/")}
            require(lh.keys() == rh.keys(), "Private AAR entry inventory differs from the reviewed official resources.")
            text_entry = ".net/__res_name_case_map.txt" if name == "Microsoft.Maui.aar" else "AndroidManifest.xml"
            differences = [n for n in lh if lh[n] != rh[n]]
            for entry in differences:
                require(system == "linux" and entry == text_entry and
                        left.read(entry).replace(b"\r\n", b"\n") == right.read(entry).replace(b"\r\n", b"\n"),
                        "Private AAR contents differ beyond the two reviewed Linux text line endings.")
            aar_proof.append({"Archive": name, "BuiltSHA256": digest(out / name),
                              "PackagedOfficialSHA256": digest(official / "lib/net10.0-android36.0" / name),
                              "LineEndingOnlyEntries": differences, "AllOtherEntryBytesExact": True})
    write_json(root / "aar-proof.json", aar_proof)
    references = root / "api-references"
    references.mkdir(exist_ok=True)
    assets = json.loads((build / "obj/Core/project.assets.json").read_text())
    folders = [Path(p) for p in assets["packageFolders"]]
    for package, entry in assets["targets"]["net10.0-android36.0"].items():
        library = assets["libraries"].get(package)
        if not library or library["type"] != "package":
            continue
        for name in entry.get("compile", {}):
            if name.endswith(".dll"):
                file = next(p / library["path"] / name for p in folders if (p / library["path"] / name).is_file())
                destination = references / file.name
                if destination.exists():
                    require(digest(destination) == digest(file), "Public API reference name collision.")
                else:
                    shutil.copy2(file, destination)
    for name, folder in [("Graphics", "Graphics"), ("Essentials", "Essentials")]:
        shutil.copy2(build / ("bin/" + folder + "/Release/net10.0-android36.0/Microsoft.Maui." + name + ".dll"), references)
    designer = list((build / "obj/Core").rglob("_Microsoft.Android.Resource.Designer.dll"))
    require(len(designer) == 1, "Actual source-built resource designer missing or ambiguous.")
    shutil.copy2(designer[0], references)
    netrefs = dotnet.parent / "packs/Microsoft.NETCore.App.Ref" / PROFILE["host_runtime"] / "ref/net10.0"
    androidrefs = workload / "packs/Microsoft.Android.Ref.36/36.1.115/ref/net10.0"
    require(netrefs.is_dir(), "Pinned managed targeting pack missing.")
    search = ",".join(str(p) for p in [references, netrefs, androidrefs])
    api_env = dict(env, DOTNET_ROLL_FORWARD="Major")
    run([dotnet, api_root / "tools/net8.0/any/Microsoft.DotNet.ApiCompat.Tool.dll", "--left-assembly",
         official / "lib/net10.0-android36.0/Microsoft.Maui.dll", "--right-assembly", out / "Microsoft.Maui.dll",
         "--strict-mode", "--enable-rule-cannot-change-parameter-name", "--left-assembly-references", search,
         "--right-assembly-references", search, "--verbosity", "High"], REPO, api_env, root / "api-compat.log")
    require(not (root / "api-compat.log").read_text().strip(), "Public API comparison emitted unresolved references or diagnostics.")
    proof = json.loads((root / "core-proof.json").read_text())
    ns = "http://schemas.microsoft.com/packaging/2013/05/nuspec.xsd"
    ET.register_namespace("", ns)
    tag = lambda name: "{" + ns + "}" + name
    package = ET.Element(tag("package"))
    metadata = ET.SubElement(package, tag("metadata"))
    for name, value in [("id", PROFILE["private_package"]), ("version", PROFILE["private_version"]),
                        ("authors", "Shahram Vafadar"), ("description", "Reviewed Android configuration-metrics source modification of MAUI Core.")]:
        ET.SubElement(metadata, tag(name)).text = value
    ET.SubElement(metadata, tag("license"), {"type": "expression"}).text = "MIT"
    group = ET.SubElement(ET.SubElement(metadata, tag("dependencies")), tag("group"), {"targetFramework": "net10.0-android36.0"})
    ET.SubElement(group, tag("dependency"), {"id": "Microsoft.Maui.Core", "version": "[" + PROFILE["upstream_version"] + "]"})
    original_nuspec = ET.parse(official / "microsoft.maui.core.nuspec")
    original_group = next(x for x in original_nuspec.getroot().iter() if x.tag.endswith("group") and x.get("targetFramework") == "net10.0-android36.0")
    for item in original_group:
        ET.SubElement(group, tag("dependency"), dict(item.attrib))
    entries = {PROFILE["private_package"] + ".nuspec": ET.tostring(package, encoding="utf-8", xml_declaration=True),
               "LICENSE.upstream.txt": (official / "LICENSE.TXT").read_bytes(),
               "THIRD-PARTY-NOTICES.upstream.txt": (official / "THIRD-PARTY-NOTICES.TXT").read_bytes(),
               "NOTICE.txt": ("Private source modification; not an official upstream binary.\nSource " + PROFILE["upstream_commit"] +
                              "\nPatch SHA256 " + PROFILE["patch_sha256"] + "\nPrivate informational version " + PROFILE["informational_version"] + "\n").encode(),
               "source-build-profile.json": json.dumps({"RecipeSHA256": fingerprint, "Platform": system,
                   "HostSDK": PROFILE["host_sdk"], "CoreSHA256": proof["CoreSHA256"],
                   "SourceCommit": PROFILE["upstream_commit"], "PatchSHA256": PROFILE["patch_sha256"]}, indent=2).encode()}
    for name in ["Microsoft.Maui.dll", "Microsoft.Maui.pdb", "Microsoft.Maui.aar", "maui.aar"]:
        file = official / "lib/net10.0-android36.0" / name if name.endswith(".aar") else out / name
        entries["lib/net10.0-android36.0/" + name] = file.read_bytes()
    # No activation is hidden here: consuming projects must explicitly exclude original Core assets.
    output.parent.mkdir(parents=True, exist_ok=True)
    package_file(output, entries)
    write_json(receipt, {"RecipeSHA256": fingerprint, "PackageSHA256": digest(output), "CoreSHA256": proof["CoreSHA256"],
                        "Platform": system, "HostSDK": PROFILE["host_sdk"], "HostRuntime": PROFILE["host_runtime"],
                        "SourceProof": proof, "StrictApiCompatibility": True, "PackagedAarsOfficialBytesExact": True,
                        "BuiltAarEntryAdmission": aar_proof,
                        "NoProductRuntimeAcceptanceClaim": True})
    print("Pinned private Android Core package prepared; product activation/runtime qualification remains separate.")


if __name__ == "__main__":
    main()
