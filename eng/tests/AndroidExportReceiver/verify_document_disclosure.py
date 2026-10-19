"""Verify original opted-in and redacted accountant packets from the isolated D-380 phone profile."""
import argparse
import csv
from decimal import Decimal
import hashlib
import io
import json
from pathlib import Path
import uuid
import zipfile

LIMIT = 2 * 1024 * 1024
CORE = {"accounts.csv", "categories.csv", "transactions.csv", "accountant-review.csv",
        "recorded-totals.csv", "README.txt", "manifest.json"}
PDF_HASH = "6f6dcffa10e283897f71d7892733bb2632986810346153aa78f728b2d86a6919"


def require(condition, message):
    """Reject an unexpected packet even when the interpreter disables assertions."""
    if not condition:
        raise ValueError(message)


def table(content, name):
    """Read original UTF-8 CSV rows without reproducing application projections."""
    return list(csv.DictReader(io.StringIO(content[name].decode("utf-8-sig"), newline="")))


def read_packet(path, disclosed, original_pdf):
    """Check the exact known fixture envelope, manifest digests and original stored document bytes."""
    data = path.read_bytes()
    require(len(data) <= LIMIT, "Compressed packet limit")
    with zipfile.ZipFile(io.BytesIO(data)) as archive:
        names = archive.namelist()
        require(len(names) == len(set(names)), "Unique members")
        require(all(item.file_size <= LIMIT for item in archive.infolist())
                and sum(item.file_size for item in archive.infolist()) <= LIMIT, "Expanded packet limit")
        document_names = [name for name in names if name.startswith("documents/")]
        require(len(document_names) == int(disclosed), "Explicit document disclosure")
        if disclosed:
            require(document_names[0] == "documents/" + uuid.UUID(Path(document_names[0]).stem).hex + ".pdf",
                    "Generated document path")
        expected = CORE | ({"documents.csv", *document_names} if disclosed else set())
        require(set(names) == expected, "Exact expected envelope; no unrelated files")
        content = {name: archive.read(name) for name in names}
    manifest = json.loads(content["manifest.json"])
    require(manifest["format"] == "zanance-accountant-data/1" and manifest["country"] == "DE"
            and manifest["calendarYear"] == 2026, "Original export scope")
    require(manifest["ledgerRows"] == 1 and manifest["storedDocumentCopies"] == int(disclosed)
            and manifest["includesNotesAndPayees"] is disclosed
            and manifest["includesStoredDocumentCopies"] is disclosed, "Reviewed disclosure flags and counts")
    require(set(manifest["sha256"]) == expected - {"manifest.json"}, "Complete manifest digest coverage")
    for name, digest in manifest["sha256"].items():
        require(hashlib.sha256(content[name]).hexdigest() == digest, "Member digest: " + name)
    rows = table(content, "transactions.csv")
    require(len(rows) == 1, "One original recorded expense")
    row = rows[0]
    uuid.UUID(row["id"])
    require((row["recorded_date"], row["kind"], row["amount_minor"], row["currency"], row["amount_decimal"])
            == ("2026-10-17", "Expense", "2450", "EUR", "24.50"), "Reviewed original source date, kind and amount")
    require(Decimal(row["amount_decimal"]) * 100 == int(row["amount_minor"]), "Invariant amount representation")
    require(row["payee"] == ("Fictitious_D380_payee_only" if disclosed else "")
            and row["note"] == ("Fictitious_D380_note_only" if disclosed else "")
            and row["reimbursed_by"] == "", "Only explicit payee/note disclosure")
    require(not row["to_account_id"] and not row["to_amount_minor"], "No invented transfer")
    accounts = table(content, "accounts.csv")
    require(len(accounts) == 1 and accounts[0]["id"] == row["account_id"]
            and accounts[0]["name"] == "My account" and accounts[0]["currency"] == "EUR"
            and accounts[0]["explicitly_selected"] == "true", "Only the selected fictitious account")
    require(manifest["selectedAccountIds"] == [row["account_id"]], "Exact selected account identity")
    require(not any("opening" in key.lower() for key in accounts[0]), "Opening balance excluded")
    review = table(content, "accountant-review.csv")
    require(len(review) == 1 and review[0]["entry_id"] == row["id"], "Original review identity")
    require(all(review[0][key] == "" for key in ("tax_category_to_complete", "business_share_to_confirm",
                "vat_treatment_to_confirm", "accountant_comment")), "No invented tax decisions")
    totals = table(content, "recorded-totals.csv")
    require(len(totals) == 1 and totals[0]["currency_and_recorded_kind"] == "EUR/Expense"
            and totals[0]["row_count"] == "1" and totals[0]["source_amount_minor_sum"] == "2450",
            "One recorded source amount; no taxable-profit assumption")
    if disclosed:
        require(content[document_names[0]] == original_pdf, "Byte-exact original stored PDF copy")
        documents = table(content, "documents.csv")
        require(len(documents) == 1, "One document index row")
        document = documents[0]
        require(document["entry_id"] == row["id"]
                and uuid.UUID(document["attachment_id"]).hex == Path(document_names[0]).stem
                and document["archive_path"] == document_names[0], "Original attachment-to-entry relationship")
        require(document["stored_file_name"] == "zanance-d380.pdf"
                and document["stored_media_type"] == "application/pdf"
                and document["bytes"] == "13881" and document["sha256"] == PDF_HASH
                and document["status"] == "stored_copy_not_verified_original", "Stored-copy identity and disclaimer")
    for value in content.values():
        require(all(name not in value for name in (b"Zanance_Test_D312_Main", b"D316_Fictitious_Work",
                    b"D319_Fictitious_Loan")), "No other fictitious profile identity")
    return content, row, manifest, hashlib.sha256(data).hexdigest()


def main():
    """Compare two original phone packets, including redaction without changing the saved transaction."""
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("disclosed", type=Path)
    parser.add_argument("redacted", type=Path)
    parser.add_argument("original_pdf", type=Path)
    args = parser.parse_args()
    original = args.original_pdf.read_bytes()
    require(len(original) == 13881 and hashlib.sha256(original).hexdigest() == PDF_HASH, "Exact reviewed PDF fixture")
    full, before, first_manifest, first_hash = read_packet(args.disclosed, True, original)
    lean, after, last_manifest, last_hash = read_packet(args.redacted, False, original)
    private_fields = {"payee", "note", "reimbursed_by"}
    require({key: value for key, value in before.items() if key not in private_fields}
            == {key: value for key, value in after.items() if key not in private_fields},
            "Every other original transaction field remains identical")
    for name in ("accounts.csv", "categories.csv", "accountant-review.csv", "recorded-totals.csv"):
        require(full[name] == lean[name], "Original non-disclosure content unchanged: " + name)
    print(json.dumps({"passed": True, "originalRows": 1, "storedDocumentCopies": 1,
                      "byteExactPdfSha256": PDF_HASH, "disclosedSha256": first_hash,
                      "redactedSha256": last_hash, "sameOriginalFinancialFields": True,
                      "explicitRedactionVerified": True, "otherProfilesExcluded": True}, indent=2))


if __name__ == "__main__":
    main()