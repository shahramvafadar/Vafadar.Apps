"""Check original physical D-312 accountant packets without production assemblies or database access."""
import argparse
import csv
import hashlib
import io
import json
from decimal import Decimal
from pathlib import Path
import zipfile


def require(condition, message):
    """Fail explicitly even when Python assertions are disabled."""
    if not condition:
        raise ValueError(message)


def read_packet(path):
    """Read bounded known members, check their recorded digests and parse original CSV bytes."""
    data = path.read_bytes()
    require(len(data) <= 2 * 1024 * 1024, "Packet exceeds receiver limit")
    expected = {"accounts.csv", "categories.csv", "transactions.csv", "accountant-review.csv",
                "recorded-totals.csv", "README.txt", "manifest.json"}
    with zipfile.ZipFile(io.BytesIO(data)) as archive:
        require(len(archive.infolist()) == 7 and set(archive.namelist()) == expected, "Unexpected members")
        require(sum(item.file_size for item in archive.infolist()) <= 2 * 1024 * 1024, "Expanded limit")
        content = {name: archive.read(name) for name in expected}
    manifest = json.loads(content["manifest.json"])
    require(manifest["format"] == "zanance-accountant-data/1" and manifest["country"] == "DE"
            and manifest["calendarYear"] == 2026, "Unexpected scope")
    require(manifest["includesNotesAndPayees"] is False
            and manifest["includesStoredDocumentCopies"] is False
            and manifest["storedDocumentCopies"] == 0, "Unexpected disclosure")
    require(set(manifest["sha256"]) == expected - {"manifest.json"}, "Missing digest coverage")
    for name, digest in manifest["sha256"].items():
        require(hashlib.sha256(content[name]).hexdigest() == digest, "Digest mismatch: " + name)
    tables = {name: list(csv.DictReader(io.StringIO(value.decode("utf-8-sig"), newline="")))
              for name, value in content.items() if name.endswith(".csv")}
    rows = tables["transactions.csv"]
    require(len(rows) == manifest["ledgerRows"] and len({row["id"] for row in rows}) == len(rows), "Row identity")
    accounts = {row["id"]: row for row in tables["accounts.csv"]}
    categories = {row["id"] for row in tables["categories.csv"]}
    require(set(manifest["selectedAccountIds"]) == {key for key, row in accounts.items()
            if row["explicitly_selected"] == "true"}, "Selection metadata")
    for row in rows:
        require(row["recorded_date"] == "2026-10-17" and row["currency"] == "EUR", "Fixture date/currency")
        require(Decimal(row["amount_decimal"]) * 100 == int(row["amount_minor"]), "Decimal mismatch")
        require(row["account_id"] in accounts and (not row["to_account_id"] or row["to_account_id"] in accounts), "Account reference")
        require(not row["category_id"] or row["category_id"] in categories, "Category reference")
        require(all(row[key] == "" for key in ("payee", "note", "reimbursed_by")), "Default redaction")
    reviews = tables["accountant-review.csv"]
    require({row["entry_id"] for row in reviews} == {row["id"] for row in rows}
            and len(reviews) == len(rows), "Review identity")
    require(all(row[key] == "" for row in reviews for key in
            ("tax_category_to_complete", "business_share_to_confirm", "vat_treatment_to_confirm", "accountant_comment")), "Invented tax fields")
    require(all(row["tax_classification"] == "" for row in tables["categories.csv"]), "Invented category tax classification")
    require("opening_balance" not in tables["accounts.csv"][0], "Unexpected opening balances")
    return tables, manifest, hashlib.sha256(data).hexdigest()


def main():
    """Compare the two original Samsung packets against the independently observed fictitious ledger."""
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("all_accounts", type=Path)
    parser.add_argument("cash_only", type=Path)
    args = parser.parse_args()
    all_tables, all_manifest, all_hash = read_packet(args.all_accounts)
    cash_tables, cash_manifest, cash_hash = read_packet(args.cash_only)
    rows = all_tables["transactions.csv"]
    require(len(rows) == 5 and len(cash_tables["transactions.csv"]) == 1, "Expected five and one rows")
    require(sorted((row["kind"], int(row["amount_minor"])) for row in rows) ==
            [("Expense", 100), ("Expense", 2550), ("Income", 100000), ("Refund", 550), ("Transfer", 10000)], "Fixture amounts")
    transfer = next(row for row in rows if row["kind"] == "Transfer")
    require(cash_tables["transactions.csv"] == [transfer], "Cash-only must retain the exact single transfer")
    require(transfer["to_amount_minor"] == "10000" and transfer["to_currency"] == "EUR", "Destination money")
    accounts = {row["name"]: row for row in cash_tables["accounts.csv"]}
    require(set(accounts) == {"D312_Fictitious_Cash", "Zanance_Test_D312_Main"}, "Account identities")
    require(accounts["D312_Fictitious_Cash"]["explicitly_selected"] == "true"
            and accounts["Zanance_Test_D312_Main"]["explicitly_selected"] == "false", "Linked identity is not selected")
    require(len(all_manifest["selectedAccountIds"]) == 2 and len(cash_manifest["selectedAccountIds"]) == 1, "Disclosure selection")
    refund = next(row for row in rows if row["kind"] == "Refund")
    food = next(row for row in rows if row["amount_minor"] == "2550")
    fee = next(row for row in rows if row["amount_minor"] == "100")
    require(refund["refund_of"] == food["id"], "Refund relationship")
    require(transfer["group_id"] and transfer["group_id"] == fee["group_id"], "Fee relationship")
    require({row["currency_and_recorded_kind"]: (int(row["row_count"]), int(row["source_amount_minor_sum"]))
            for row in all_tables["recorded-totals.csv"]} ==
            {"EUR/Expense": (2, 2650), "EUR/Income": (1, 100000), "EUR/Refund": (1, 550), "EUR/Transfer": (1, 10000)}, "Separate recorded kinds")
    require(len(cash_tables["recorded-totals.csv"]) == 1
            and cash_tables["recorded-totals.csv"][0]["currency_and_recorded_kind"] == "EUR/Transfer", "No unrelated totals")
    print(json.dumps({"passed": True, "allRows": 5, "cashRows": 1,
                      "allSha256": all_hash, "cashSha256": cash_hash}, indent=2))


if __name__ == "__main__":
    main()
