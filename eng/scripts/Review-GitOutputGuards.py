"""Exercise the actual output/source hook in a new owned repository outside the checkout."""

from __future__ import annotations

import argparse
import json
import os
from pathlib import Path
import subprocess


def review(work: Path, shell: Path) -> dict:
    """Keep generated-output allowance separate from staged-output and hidden-source refusal."""
    repository = Path(__file__).resolve().parents[2]
    hook = repository / "eng/git-hooks/pre-commit"
    work = work.resolve()
    shell = shell.resolve()
    if work.exists() or work.is_relative_to(repository) or not work.parent.is_dir():
        raise ValueError("A new owned directory outside the checkout is required.")
    if not hook.is_file() or not shell.is_file():
        raise ValueError("The original hook and an existing Git shell are required.")
    work.mkdir()
    environment = os.environ.copy()
    for name in ("GIT_DIR", "GIT_WORK_TREE", "GIT_INDEX_FILE", "GIT_COMMON_DIR"):
        environment.pop(name, None)
    for role in ("AUTHOR", "COMMITTER"):
        environment[f"GIT_{role}_NAME"] = "Shahram Vafadar"
        environment[f"GIT_{role}_EMAIL"] = "shahramvafadar@gmail.com"

    def git(*arguments: str) -> None:
        result = subprocess.run(["git", *arguments], cwd=work, env=environment,
                                capture_output=True, text=True, encoding="utf-8", errors="replace")
        if result.returncode:
            raise RuntimeError("Disposable Git fixture preparation failed.")

    git("init", "-q")
    git("config", "--local", "user.name", "Shahram Vafadar")
    git("config", "--local", "user.email", "shahramvafadar@gmail.com")
    (work / ".gitignore").write_text("**/artifacts/\nhidden.cs\n", encoding="utf-8")
    sources = work / "src/Fixture"
    sources.mkdir(parents=True)
    (sources / "kept.cs").write_text("// Fictitious owned hook fixture.\n", encoding="utf-8")
    output = sources / "artifacts/old-output.g.cs"
    output.parent.mkdir()
    output.write_text("// Fictitious generated output, never product source.\n", encoding="utf-8")
    git("add", "--", ".gitignore", "src/Fixture/kept.cs")
    cases = []

    def check(name: str, expected: int, message: str | None = None) -> None:
        result = subprocess.run([str(shell), str(hook)], cwd=work, env=environment,
                                capture_output=True, text=True, encoding="utf-8", errors="replace")
        log = result.stdout + result.stderr
        (work / f"{name}.log").write_text(log, encoding="utf-8")
        passed = result.returncode == expected and (message is None or message in log)
        cases.append({"Name": name, "ExitCode": result.returncode, "Passed": passed})
        if not passed:
            raise RuntimeError(f"Original hook boundary failed: {name}")

    check("ignored-generated-output-is-not-missing-source", 0)
    (sources / "hidden.cs").write_text("// Fictitious omitted real source.\n", encoding="utf-8")
    check("ignored-real-source-still-refuses", 1, "source files are ignored")
    # Force-add belongs only to this negative disposable fixture. The real commit still
    # uses the unchanged identity/secret/attribution checks and never stages build output.
    git("add", "-f", "--", "src/Fixture/artifacts/old-output.g.cs")
    check("staged-generated-output-still-refuses", 1, "these files must not be committed")
    report = {"Cases": cases, "ActualOriginalHook": True, "MainIndexChanged": False,
              "NoCommitPushGlobalConfigurationOrDeletion": True}
    (work / "results.json").write_text(json.dumps(report, indent=2), encoding="utf-8")
    return report


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--work-directory", type=Path, required=True)
    parser.add_argument("--shell", type=Path, required=True)
    options = parser.parse_args()
    print(json.dumps(review(options.work_directory, options.shell)))
