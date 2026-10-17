"""Validate the reviewed storage-crypto input before passing individual options to Configure."""

import argparse
import hashlib
import json
from pathlib import Path


def read_profile(path: Path) -> tuple[list[str], str]:
    """Return only reviewed feature removals; reject changes to required storage/security facilities."""
    raw = path.read_bytes()
    profile = json.loads(raw.decode("utf-8-sig"))
    if not isinstance(profile, dict):
        raise ValueError("Storage crypto profile must be an object")
    allowed = {
        "no-dsa", "no-dh", "no-cms", "no-cmp", "no-ocsp", "no-ts", "no-quic",
        "no-deprecated", "no-legacy", "no-engine", "no-dso", "no-ec", "no-ecx",
        "no-tls", "no-dtls", "no-sock", "no-dgram", "no-http", "no-winstore", "no-ui-console",
        "no-ml-dsa", "no-ml-kem", "no-slh-dsa", "no-sm2", "no-sm3", "no-sm4",
        "no-aria", "no-bf", "no-blake2", "no-camellia", "no-cast", "no-chacha", "no-cmac",
        "no-des", "no-idea", "no-md4", "no-mdc2", "no-ocb", "no-poly1305", "no-rc2", "no-rc4",
        "no-rc5", "no-rmd160", "no-scrypt", "no-seed", "no-siphash", "no-siv", "no-srp",
        "no-whirlpool", "no-argon2",
    }
    required = {"AES-256-CBC", "SHA1", "SHA256", "SHA512", "HMAC", "PBKDF2", "RAND/DRBG"}
    options = profile.get("opensslConfigureOptions")
    primitives = profile.get("requiredPrimitives")
    if (profile.get("schemaVersion") != 1 or profile.get("sourceForkOrWarningSuppression") is not False
            or not isinstance(options, list) or not options
            or any(not isinstance(option, str) or option not in allowed for option in options)
            or len(set(options)) != len(options)
            or not isinstance(primitives, list) or len(primitives) != len(required)
            or any(not isinstance(item, str) for item in primitives) or set(primitives) != required):
        raise ValueError("Storage crypto profile changes an unreviewed capability or has invalid provenance")
    return options, hashlib.sha256(raw).hexdigest()


def main() -> None:
    """Emit one argument per line only after complete validation; shell callers must check the exit status."""
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("profile", type=Path)
    parser.add_argument("--proof", required=True, type=Path)
    args = parser.parse_args()
    options, digest = read_profile(args.profile)
    with args.proof.open("x", encoding="utf-8") as output:
        json.dump({"schemaVersion": 1, "profileSha256": digest, "options": options,
                   "sourceForkOrWarningSuppression": False, "perDiagnosticSafetyAccepted": False}, output, indent=2)
    print("\n".join(options))


if __name__ == "__main__":
    main()
