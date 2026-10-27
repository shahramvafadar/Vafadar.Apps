"""Prepare pinned official notification sources with one reviewed Android replacement."""

from pathlib import Path
import hashlib
import json
import urllib.request
import zipfile


def digest(data):
    """Hash exact source bytes; never accept a partially prepared or changed input."""
    return hashlib.sha256(data).hexdigest()


def require(condition, message):
    """D-581: source admission must remain active under -O or PYTHONOPTIMIZE."""
    if not condition:
        raise ValueError(message)


def prepare(here=None):
    """Populate only this repository's immutable source cache, retaining failed inputs."""
    here = Path(here).resolve() if here is not None else Path(__file__).resolve().parent
    repo = here.parent.parent
    manifest = json.loads((here / "notification-core.inputs.json").read_text())
    require(manifest["SchemaVersion"] == 1, "Unsupported notification source manifest.")
    require(digest((here / "LICENSE.upstream").read_bytes()) == manifest["LicenseSHA256"],
            "Preserve the changed upstream license input.")
    root = repo / "artifacts" / "notification-core"
    root.mkdir(parents=True, exist_ok=True)
    archive = root / "upstream.zip"
    if not archive.exists():
        # This pinned official source is already MIT-licensed; no binary substitute or credentials.
        with urllib.request.urlopen(manifest["SourceURL"], timeout=60) as response:
            data = response.read(16 * 1024 * 1024 + 1)
        require(len(data) <= 16 * 1024 * 1024 and digest(data) == manifest["ArchiveSHA256"],
                "Official notification archive size or digest refused.")
        with archive.open("xb") as stream:
            stream.write(data)
    require(digest(archive.read_bytes()) == manifest["ArchiveSHA256"],
            "Preserve the changed notification archive.")
    replacement = (here / "AndroidUtils.cs").read_bytes()
    require(digest(replacement) == manifest["ReplacementSHA256"],
            "Preserve the unreviewed Android replacement.")
    source = root / "source"
    source.mkdir(exist_ok=True)
    with zipfile.ZipFile(archive) as bundle:
        for name, expected in manifest["Files"].items():
            relative = Path(name)
            require(not relative.is_absolute() and ".." not in relative.parts,
                    "Notification source path escapes the owned cache.")
            data = bundle.read(manifest["ArchivePrefix"] + name)
            require(digest(data) == expected, "Original notification source digest refused.")
            if name == manifest["ReplacementFile"]:
                data = replacement
            target = source / relative
            target.parent.mkdir(parents=True, exist_ok=True)
            if target.exists():
                require(target.read_bytes() == data, "Preserve changed source-cache evidence.")
            else:
                with target.open("xb") as stream:
                    stream.write(data)
    actual = {p.relative_to(source).as_posix() for p in source.rglob("*.cs")}
    require(actual == set(manifest["Files"]), "Refuse unexpected cached compilation inputs.")
    print("Pinned notification Core sources and the single Android replacement verified.")


if __name__ == "__main__":
    prepare()
