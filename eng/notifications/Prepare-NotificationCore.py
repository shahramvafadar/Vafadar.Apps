"""Prepare pinned official notification sources with one reviewed Android replacement."""

from pathlib import Path
import hashlib
import json
import urllib.request
import zipfile


def digest(data):
    """Hash exact source bytes; never accept a partially prepared or changed input."""
    return hashlib.sha256(data).hexdigest()


def prepare(here=None):
    """Populate only this repository's immutable source cache, retaining failed inputs."""
    here = Path(here).resolve() if here is not None else Path(__file__).resolve().parent
    repo = here.parent.parent
    manifest = json.loads((here / "notification-core.inputs.json").read_text())
    assert manifest["SchemaVersion"] == 1
    assert digest((here / "LICENSE.upstream").read_bytes()) == manifest["LicenseSHA256"]
    root = repo / "artifacts" / "notification-core"
    root.mkdir(parents=True, exist_ok=True)
    archive = root / "upstream.zip"
    if not archive.exists():
        # This pinned official source is already MIT-licensed; no binary substitute or credentials.
        with urllib.request.urlopen(manifest["SourceURL"], timeout=60) as response:
            data = response.read(16 * 1024 * 1024 + 1)
        assert len(data) <= 16 * 1024 * 1024 and digest(data) == manifest["ArchiveSHA256"]
        with archive.open("xb") as stream:
            stream.write(data)
    assert digest(archive.read_bytes()) == manifest["ArchiveSHA256"]
    replacement = (here / "AndroidUtils.cs").read_bytes()
    assert digest(replacement) == manifest["ReplacementSHA256"]
    source = root / "source"
    source.mkdir(exist_ok=True)
    with zipfile.ZipFile(archive) as bundle:
        for name, expected in manifest["Files"].items():
            relative = Path(name)
            assert not relative.is_absolute() and ".." not in relative.parts
            data = bundle.read(manifest["ArchivePrefix"] + name)
            assert digest(data) == expected
            if name == manifest["ReplacementFile"]:
                data = replacement
            target = source / relative
            target.parent.mkdir(parents=True, exist_ok=True)
            if target.exists():
                assert target.read_bytes() == data, "Preserve changed source-cache evidence."
            else:
                with target.open("xb") as stream:
                    stream.write(data)
    actual = {p.relative_to(source).as_posix() for p in source.rglob("*.cs")}
    assert actual == set(manifest["Files"]), "Refuse unexpected cached compilation inputs."
    print("Pinned notification Core sources and the single Android replacement verified.")


if __name__ == "__main__":
    prepare()
