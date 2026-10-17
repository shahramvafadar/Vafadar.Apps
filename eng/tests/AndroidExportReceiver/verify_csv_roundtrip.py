"""Verify the two original native D-312 ledger exports by stable identity, without application assemblies."""
import argparse
import csv
from decimal import Decimal
import hashlib
import io
import json
from pathlib import Path
import uuid


def require(condition, message):
    """Reject incomplete evidence instead of silently skipping records or duplicate identifiers."""
    if not condition:
        raise ValueError(message)


def read(path):
    """Read the bounded original UTF-8 export and retain every named column."""
    data = path.read_bytes()
    require(len(data) <= 2 * 1024 * 1024 and data.startswith(b"\xef\xbb\xbf"), "Bounded UTF-8 BOM export required")
    reader = csv.DictReader(io.StringIO(data.decode("utf-8-sig"), newline=""))
    rows = list(reader)
    require(reader.fieldnames is not None and len(reader.fieldnames) == 27
            and len(set(reader.fieldnames)) == 27 and reader.fieldnames[-1] == "format_version", "Original format contract")
    require(len(rows) == 5 and len({row["id"] for row in rows}) == 5, "Five unique records required")
    for row in rows:
        uuid.UUID(row["id"])
        require(set(row) == set(reader.fieldnames) and all(value is not None for value in row.values()), "Incomplete row")
        require(row["format_version"] == "2" and row["date"] == "2026-10-17" and row["currency"] == "EUR", "Original fixture scope")
        require(row["account"] == "Zanance_Test_D312_Main", "Original source account")
    return data, {row["id"]: row for row in rows}


def main():
    """Check complete exported values and independently known financial links, allowing tie-order changes."""
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("original", type=Path)
    parser.add_argument("reexport", type=Path)
    args = parser.parse_args()
    original_bytes, original = read(args.original)
    reexport_bytes, reexport = read(args.reexport)
    require(original == reexport, "A stable identifier or exported field changed")
    rows = list(original.values())
    require(sorted((row["kind"], Decimal(row["amount"])) for row in rows) ==
            [("Expense", Decimal("1.00")), ("Expense", Decimal("25.50")), ("Income", Decimal("1000.00")),
             ("Refund", Decimal("5.50")), ("Transfer", Decimal("100.00"))], "Known five financial values")
    transfer = next(row for row in rows if row["kind"] == "Transfer")
    require(transfer["to_account"] == "D312_Fictitious_Cash" and transfer["to_currency"] == "EUR"
            and transfer["to_amount"] == "100.00", "One original transfer with its destination")
    refund = next(row for row in rows if row["kind"] == "Refund")
    food = next(row for row in rows if row["amount"] == "25.50")
    fee = next(row for row in rows if row["amount"] == "1.00")
    require(refund["refund_of"] == food["id"], "Original refund reference")
    require(transfer["group_id"] and transfer["group_id"] == fee["group_id"], "Original transfer and fee group")
    print(json.dumps({"passed": True, "rows": 5, "columnsComparedPerRow": 27,
                      "originalSha256": hashlib.sha256(original_bytes).hexdigest(),
                      "reexportSha256": hashlib.sha256(reexport_bytes).hexdigest()}, indent=2))


if __name__ == "__main__":
    main()
