"""Compare only the known fictitious original portable exports across a real app reinstall."""

import hashlib
import io
import json
from pathlib import Path
import re
import sqlite3
import sys
import zipfile

LIMIT = 8 * 1024 * 1024


def read_packet(path):
    """Validate original manifest checksums and open only its exported SQLite image in memory."""
    path = Path(path)
    assert path.stat().st_size <= LIMIT, "Fixture packet exceeds its bound"
    with zipfile.ZipFile(io.BytesIO(path.read_bytes())) as archive:
        names = archive.namelist()
        assert len(names) == len(set(names)), "Duplicate packet member"
        assert all(name == "manifest.json" or re.fullmatch(r"data/[A-Za-z0-9_.-]+", name) for name in names)
        assert sum(entry.file_size for entry in archive.infolist()) <= LIMIT
        manifest = json.loads(archive.read("manifest.json"))
        assert manifest["appId"] == "pro.vafadar.zanance" and manifest["formatVersion"] == 1
        assert set(names) == {"manifest.json", *("data/" + entry["name"] for entry in manifest["entries"])}
        for entry in manifest["entries"]:
            data = archive.read("data/" + entry["name"])
            assert len(data) == entry["length"] and hashlib.sha256(data).hexdigest() == entry["sha256"]
        preferences = json.loads(archive.read("data/display-settings.json"))
        assert preferences["localization.firstDayOfWeek"] == "Monday"
        db = sqlite3.connect(":memory:")
        db.deserialize(archive.read("data/database.sqlite"))
    assert db.execute("PRAGMA integrity_check").fetchall() == [("ok",)]
    assert db.execute("SELECT Name, Type, CurrencyCode, OpeningBalance, OpeningDate FROM Accounts").fetchall() == [
        ("D403_Cash", 0, "EUR", 3700, "2026-10-20")
    ]
    assert db.execute("SELECT Title, Kind, Amount, Date, Note FROM Entries").fetchall() == [
        ("D403_Expense2", 1, 200, "2026-10-20", "D403_portable_note")
    ]
    assert db.execute("SELECT Name, Amount, Start, AutoPost, ReminderEnabled FROM Schedules").fetchall() == [
        ("D403_Once9", 900, "2026-10-20", 0, 0)
    ]
    assert db.execute("SELECT OriginalDate, Status, EntryId, PaidAmount FROM OccurrenceStates").fetchall() == [
        ("2026-10-20", 2, None, 0)
    ]
    assert db.execute("SELECT Mode, OnboardingCompleted FROM Settings").fetchall() == [(1, 1)]
    return db, preferences


def compare(source, restored):
    """Require identical schema and typed rows, apart from the original onboarding settings timestamp."""
    before, before_preferences = read_packet(source)
    after, after_preferences = read_packet(restored)
    try:
        assert before_preferences == after_preferences, "Portable display preferences changed"
        schema = "SELECT type, name, tbl_name, sql FROM sqlite_master ORDER BY type, name"
        assert before.execute(schema).fetchall() == after.execute(schema).fetchall(), "Exported schema changed"
        tables = [row[0] for row in before.execute("SELECT name FROM sqlite_master WHERE type='table' ORDER BY name")]
        counts = {}
        timestamp_changed = False
        for table in tables:
            assert re.fullmatch(r"[A-Za-z_][A-Za-z0-9_]*", table)
            columns = [row[1] for row in before.execute(f'PRAGMA table_info("{table}")')]
            left = before.execute(f'SELECT * FROM "{table}"').fetchall()
            right = after.execute(f'SELECT * FROM "{table}"').fetchall()
            assert len(left) == len(right), "Exported row count changed: " + table
            if table == "Settings":
                # The original CompleteRestoredOnboardingAsync updates the existing settings row.
                assert len(left) == 1
                index = columns.index("UpdatedAt")
                old_stamp, new_stamp = left[0][index], right[0][index]
                # The original SQLite mapping stores timestamps as integer .NET ticks, not ISO strings.
                assert type(old_stamp) is int and type(new_stamp) is int and new_stamp >= old_stamp
                timestamp_changed = new_stamp != old_stamp
                left = [row[:index] + row[index + 1:] for row in left]
                right = [row[:index] + row[index + 1:] for row in right]
            assert sorted(left, key=repr) == sorted(right, key=repr), "Exported typed rows changed: " + table
            counts[table] = len(left)
        return {"IdenticalSchema": True, "TypedRowsPreserved": True, "TableCounts": counts,
                "OnlySettingsOnboardingTimestampAdvanced": timestamp_changed,
                "PortablePreferencesPreserved": True, "SingleOriginalAccount": True}
    finally:
        before.close()
        after.close()


if __name__ == "__main__":
    assert len(sys.argv) == 3, "Pass only the two owned fictitious original .vbak exports"
    print(json.dumps(compare(sys.argv[1], sys.argv[2]), indent=2))
