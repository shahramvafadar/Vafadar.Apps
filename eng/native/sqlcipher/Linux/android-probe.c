/* Independent native Android evidence: fictitious files only, never application storage or keys. */
#define SQLITE_HAS_CODEC 1
#include "sqlite3.h"
#include <stdint.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>

/* Fail without printing keys or interpreting an unrelated I/O failure as cipher acceptance. */
static void require(int condition, const char *message) {
    if (!condition) { fprintf(stderr, "Native check failed: %s\n", message); exit(1); }
}
static void execute(sqlite3 *db, const char *sql) {
    require(sqlite3_exec(db, sql, NULL, NULL, NULL) == SQLITE_OK, "original SQL operation");
}
static int read_result(const char *file, const unsigned char *key) {
    sqlite3 *db = NULL; sqlite3_stmt *stmt = NULL;
    int rc = sqlite3_open_v2(file, &db, SQLITE_OPEN_READONLY, NULL);
    require(rc == SQLITE_OK, "owned native file opened");
    if (key) require(sqlite3_key(db, key, 32) == SQLITE_OK, "original key input accepted");
    rc = sqlite3_prepare_v2(db, "SELECT balance FROM fixture", -1, &stmt, NULL);
    if (rc == SQLITE_OK) {
        rc = sqlite3_step(stmt);
        if (rc == SQLITE_ROW) { require(sqlite3_column_int64(stmt, 0) == INT64_MIN, "signed minor units preserved"); rc = SQLITE_OK; }
    }
    sqlite3_finalize(stmt); sqlite3_close(db); return rc;
}
int main(void) {
    unsigned char key[32], wrong[32]; FILE *random = fopen("/dev/urandom", "rb");
    require(random != NULL && fread(key, 1, sizeof key, random) == sizeof key, "fictitious random key generated");
    fclose(random); memcpy(wrong, key, sizeof key); wrong[0] ^= 1;
    sqlite3 *db = NULL; sqlite3_stmt *stmt = NULL;
    require(sqlite3_open("fictitious.db", &db) == SQLITE_OK, "fictitious database opened");
    require(sqlite3_key(db, key, sizeof key) == SQLITE_OK, "original codec key");
    require(sqlite3_prepare_v2(db, "PRAGMA cipher_version", -1, &stmt, NULL) == SQLITE_OK && sqlite3_step(stmt) == SQLITE_ROW, "cipher version available");
    require(strncmp((const char *)sqlite3_column_text(stmt, 0), "4.19.0", 6) == 0, "official cipher version"); sqlite3_finalize(stmt);
    require(sqlite3_prepare_v2(db, "PRAGMA cipher_provider_version", -1, &stmt, NULL) == SQLITE_OK && sqlite3_step(stmt) == SQLITE_ROW, "provider version available");
    require(strstr((const char *)sqlite3_column_text(stmt, 0), "OpenSSL 3.5.9") != NULL, "official provider version"); sqlite3_finalize(stmt);
    require(sqlite3_prepare_v2(db, "PRAGMA foreign_keys", -1, &stmt, NULL) == SQLITE_OK && sqlite3_step(stmt) == SQLITE_ROW && sqlite3_column_int(stmt, 0) == 1, "foreign keys enabled"); sqlite3_finalize(stmt);
    execute(db, "PRAGMA journal_mode=WAL; CREATE TABLE fixture(balance INTEGER, currency TEXT, date TEXT, label TEXT); INSERT INTO fixture VALUES(-9223372036854775808,'EUR','2026-10-12','Fictitious native marker');");
    require(sqlite3_prepare_v2(db, "SELECT currency,date,label FROM fixture", -1, &stmt, NULL) == SQLITE_OK && sqlite3_step(stmt) == SQLITE_ROW, "original packet read");
    require(strcmp((const char *)sqlite3_column_text(stmt,0), "EUR") == 0 && strcmp((const char *)sqlite3_column_text(stmt,1), "2026-10-12") == 0, "currency/date preserved"); sqlite3_finalize(stmt); sqlite3_close(db);
    require(read_result("fictitious.db", key) == SQLITE_OK, "correct-key reopen");
    int rc = read_result("fictitious.db", wrong); require(rc == SQLITE_NOTADB || rc == SQLITE_CORRUPT, "wrong key refused");
    rc = read_result("fictitious.db", NULL); require(rc == SQLITE_NOTADB || rc == SQLITE_CORRUPT, "missing key refused");
    FILE *input = fopen("fictitious.db", "rb"), *output = fopen("tampered.db", "wb"); require(input && output, "owned integrity fixture opened");
    unsigned char buffer[4096]; size_t n, total = 0;
    while ((n = fread(buffer, 1, sizeof buffer, input)) != 0) {
        if (total == 0) { require(n > 200, "complete first-page integrity fixture"); require(memcmp(buffer, "SQLite format 3", 15) != 0, "plaintext header absent"); buffer[200] ^= 1; }
        require(fwrite(buffer, 1, n, output) == n, "owned integrity fixture copied"); total += n;
    }
    fclose(input); fclose(output); require(total >= 4096, "complete cipher page");
    rc = read_result("tampered.db", key); require(rc == SQLITE_NOTADB || rc == SQLITE_CORRUPT, "modified page refused");
    puts("{\"nativeCipherAndProviderMatched\":true,\"correctKeyReopenMinorUnitsCurrencyDate\":true,\"foreignKeysEnabled\":true,\"wrongMissingKeyTamperRefused\":true,\"plaintextHeaderAbsent\":true,\"fictitiousOnly\":true}");
    return 0;
}
