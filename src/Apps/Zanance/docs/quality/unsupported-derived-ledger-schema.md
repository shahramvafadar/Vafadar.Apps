# Unsupported optional ledger columns retain the original reader and writer (D-612)

## Actual defect and constrained fix

The optional state reader checked only that three table names existed before querying
columns. A renamed state column throws SQLite error1 before fallback. A renamed block
column can do the same after the schema cookie is current. Unsupported dirty columns
could still be admitted by a getter, while subsequent original account saves attempted
optional rebuilding and failed. Original financial authority must remain independent
of an unsupported optional transport contract.

Replace the one existence query with one metadata-only statement. Require the exact
three ordinary tables and eleven ordered known columns, declared affinities, nullability,
primary keys, absence of defaults and absence of hidden/generated/extra columns through
pragma_table_xinfo. Decline unsupported metadata before querying or maintaining those
rows. Preserve the actual schema-cookie check independently. No broad exception catch:
SQLite IO, locks, source access failures and cancellation still propagate in the original
transaction. No schema/model migration, original Entry trigger, writer conflict policy,
financial field, codec, identity or comparison normalization changes.

## Focused evidence and retained observation failures

Fourteen distinct new cases cover seven renamed-column faults through the actual SQLite
reader and original account-metadata writer. Every corrected case preserves all original
entry fields and every captured optional table column/value; getters neither repair nor
publish a partial ledger, and accepted account names persist. Getter fault preparation
explicitly sets the optional schema-cookie value to the actual cookie to isolate column
admission; that is the declared fault input, never a comparison-normalization allowance.
Writer cases leave that cookie untouched and require no cache mutation.

The first harness build refuses interpolated SQL; change to a closed literal identifier
switch. Six initial reader cases fail before reaching production because the connection
is closed; retain them as harness failures, not product regressions. The original seven
writer cases and the final original seven reader cases fail. One intermediate all-green
reader result is inadmissible as an original baseline: Copy-Item retained an old timestamp
and incremental compilation reused corrected code. Touch the exact restored source,
compile the original and preserve the genuine seven reader failures before restoring
and compiling the fix. Corrected unique coverage is14 new plus3 existing affected cases:
normal complete read, original changed-value commit and schema-cookie rebuild. Do not
repeat the successful writer/valid-path cases merely to consolidate result files.
No full-suite total or native observation is counted as a new managed test.

## Native scope and actual package

One fresh API36/x64/4096 emulator owns a new independent package and one fictitious
two-row source. One original transaction accepts income100EUR/expense10EUR against
zero opening, then only its optional CodecVersion column is renamed. The source is
captured with the existing32MiB-bounded protected memory exporter. Original native
Home publishes90EUR, records zero block queries and one complete scalar query, and
passes the original complete schema/header/typed-table verifier against that snapshot.
No normalization, plaintext file/key export, repair or accepted financial/setup replay
occurs. Host SQLite tests qualify the original account writer; this native assertion
qualifies only the actual getter/publication, not every writer or platform.

Product APK SHA256 `ddc96f3699a946d2f7607c5edd577e461a115b2c36fd81a012f80b391d245d05`, 97066911bytes; own AOT384/ELF408.
Strict Windows and complete signed Offline Android builds pass without warnings/errors.
ZIP CRC, actual ZIP16KiB alignment, ELF ABI/LOAD alignment, original-owner v2/v3 signature
and offline/nondebuggable checks are retained. Static alignment is not hardware16KiB.
Independent managed payloads are not declared byte-identical to product payloads.

Actual native run `6c50d25e3f224700871253c3e5f5535a`, UID10216/PID4788,
package `pro.vafadar.zanance.ledgerschemareview`, APK SHA256
`07c8d749072426f85967b4afd4d61a0ec2e9e7f5f5316dd3fea89d860093dae1`
(59256738bytes,192AOT/204ELF/804LOAD) completes on the first accepted fixture;
104 owned public nodes include the original formatted90EUR. Entry-row digest is
`60a9d0b9caaa9d2815a149ce2e20a87d2ffd7a4af1919f759cb390a6cd36651d`.
Both actual x64 Data images contain the metadata-admission query; their PE bytes
are not equal, so no shipping-equivalent performance measurement is claimed.

Fresh AVD `Zanance_D612_Schema` and its registration are deleted with avdmanager
only after fresh UID/data absence and actual host serial/ports absence:24files,
4578227000 logical bytes. One owned39831byte XML is hash-bound and removed.
Original fresh settings/boot/display/shell and retained AVD configuration match.
Standard Clean removes2805 registered outputs/790937047 logical bytes. Further
four-root deletion is rejected before process creation with `blocked by policy`:
1508 artifact files/248417153 bytes and six isolated user-home files/7732 bytes
remain. No key contents are read/hashed and no subset/alternate deletion is tried.
Those residuals remain explicit; no host-free-space recovery is inferred.

## Retirement and remaining gates

Only the terminal fresh fixture, owned hash-bound XML and fresh AVD are retired;
settings/boot/display remain exact, retained AVD and original ADB servers are preserved.
Cleanup receipts distinguish registered Clean outputs from any refused residual scope.
The phone receives no query, install or input. D-607's live future reminder cohort and
its original accepted source remain untouched; physical adoption of the new product
waits behind that cohort. Do not revive rejected performance candidates or another
unchanged timing matrix. General stability, two-second performance, complete security,
TalkBack/A11Y, iOS, provider/Store and publication gates remain open. Goal stays active.
