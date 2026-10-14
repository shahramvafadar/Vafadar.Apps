"""Requalify the reviewed OpenSSL secure-heap list-index narrowing sites, without modifying native inputs."""
import argparse
import hashlib
import json
from pathlib import Path

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument("--root", required=True, type=Path, help="Existing independently owned Windows native build.")
parser.add_argument("--inventory", required=True, type=Path, help="Original unsuppressed compiler occurrence inventory.")
parser.add_argument("--output", required=True, type=Path, help="Fresh owned review evidence file; never replaced.")
args = parser.parse_args()
root = args.root.resolve()
source = root / "openssl/crypto/mem_sec.c"
expected = "2406f86ebc633aa7cdf27ab9617a8a49b6ec238e6d9dd3792b749cf7257e5d5e"
if hashlib.sha256(source.read_bytes()).hexdigest() != expected:
    raise SystemExit("The reviewed official source differs; review its changed range expressions.")
manifest = json.loads((root / "candidate/manifest.json").read_text(encoding="utf-8-sig"))
if manifest["runtimeIdentifier"] != "win-x64" or not any(s["name"] == "openssl" and s["commit"] == "45e844fa2a14ec92d146bd8f5778ac130b6625fb" for s in manifest["sources"]):
    raise SystemExit("The source/ABI does not match the reviewed Windows build.")
library = root / "candidate/runtimes/win-x64/native/sqlcipher.dll"
if hashlib.sha256(library.read_bytes()).hexdigest() != manifest["librarySha256"]:
    raise SystemExit("The reviewed native candidate changed.")
inventory = json.loads(args.inventory.read_text(encoding="utf-8-sig"))
rows = [row for row in inventory if row["Path"].replace("\\", "/").endswith("openssl/crypto/mem_sec.c")]
expected_lines = [654,655,663,664,670,671,675,680,681,704,705,709,710,712,713,715,716,726,727,740]
if sorted(row["Line"] for row in rows) != expected_lines or any(row["Code"] not in ("C4244", "C4267") for row in rows):
    raise SystemExit("Secure-heap warning coverage differs from the original occurrence inventory.")
# x64 size_t admits power-of-two arena sizes only through 2^63. Two 8-byte
# SH_LIST pointers force an effective power-of-two minsize of at least 16.
# Accepted nonzero bittables require minsize <= arena; ratio = 2^(a-m).
combinations = []
for arena_exp in range(4, 64):
    for minimum_exp in range(4, arena_exp + 1):
        k = arena_exp - minimum_exp
        # sh_init rejects a bittable smaller than one complete byte.
        if k < 2:
            continue
        combinations.append({"arenaExponent": arena_exp, "minimumExponent": minimum_exp, "maximumListIndex": k})
maximum = max(case["maximumListIndex"] for case in combinations)
if maximum != 59 or maximum >= (1 << 31):
    raise SystemExit("The derived valid list interval does not fit the Windows int ABI.")
# Init computes freelist_size=k+1; malloc starts at k and checks negative
# list/slist before calling helpers. Getlist starts at k and shifts its
# ratio<=bit<2*ratio until an allocated ancestor, so valid return is 0..k.
# Free coalesces toward list 0; root buddy is bit 0, which is never set
# by valid positive-node setbit calls, preventing a decrement below zero.
report = {
    "schemaVersion": 1,
    "scope": "valid secure-heap list-index conversions only; not whole allocator or arbitrary corrupted-pointer acceptance",
    "sourceSha256": expected,
    "nativeSha256": manifest["librarySha256"],
    "inventorySha256": hashlib.sha256(args.inventory.read_bytes()).hexdigest(),
    "occurrencesReviewed": len(rows),
    "sizeTBits": 64,
    "intBits": 32,
    "effectiveMinimumBytes": 16,
    "maximumListIndex": maximum,
    "validParameterClasses": len(combinations),
    "reviewedSites": rows,
    "otherWarningsRemainUnreviewed": True,
    "noSourcePatchOrSuppressionOrBinaryChange": True,
}
with args.output.open("x", encoding="utf-8") as output:
    json.dump(report, output, indent=2)
    output.write("\n")
print("ReviewedSecureHeapListConversions=20; MaximumValidListIndex=59; NativeBytesUnchanged=true")
