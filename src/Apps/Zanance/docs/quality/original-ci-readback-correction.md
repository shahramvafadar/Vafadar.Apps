# Original CI readback correction (D-656)

## Scope and current state

This batch investigates the actual library/test job of run `41003762915`, job
`123893875039`, rather than rerunning its successful Android, Windows or unrelated
jobs. On the current source at `8f8b2d6fa51439c3b9703566d2c020882c77225d`, a focused
selection reproduced its 26 failures in 52 original cases. The original failures
and the subsequent observer/fixture failures are retained separately.

Qualification is in progress. A passing focused test or build is not a claim that
the new head has passed CI, that physical adoption is complete, or that all release
requirements have been accepted.

## Original committed import publication

An explicit import retry could acquire current writer rights while its earlier
preview workspace still held retired rights. The original transaction committed,
but the subsequent batch-list read tried to use those earlier rights and reported
a false follow-through failure. The correction carries an opaque read-only receipt
from the actual original writer. It is prepared before commit, activated only after
that commit, and published before fallible `Changed` notification.

Readback requires the original file/settings identity, the actual writer's captured
rights, the original native-generation validation and the committed batch in the
original journal. It cannot recapture unrelated current rights or activate after a
rollback. The original financial writer, all 32 fields, conflict policy, schema,
encryption and journal transaction remain unchanged. A typed interpretation change
also uses the existing interpretation-changed feedback instead of generic failure.

Six focused real-SQLite cases cover committed readback with/without notification
failure, changed rights, retired native ownership, foreign workspace and actual
transaction rollback. All six passed.

## Original account recovery

A changed complete source now retires the stale account writer using a typed
exception. The existing explicit availability-review action then requests a full
source read instead of replacing only rights on the old source. The reader preserves
raw input and retains its checks for changed stored account, default and currency
constraints. It does not rebase a draft over a changed account/default.

Two new negative cases prove that changed account/default facts produce the original
covered failure while preserving the raw draft and complete stored source. The
initial negative fixture used an invalid monetary input and therefore did not reach
the writer; that failed precondition is retained. Only after correcting that input
were both intended boundaries reached and passed.

After a known atomic account/default commit, native return also revalidates the
original file/access. Departure remains quiet; a foreign file cannot borrow the
accepted result to navigate. The known receipt cannot become another Save.

## Corrected historical assertions

The affected tests now reflect the existing atomic account/default writer, opaque
original account-editor route origin, quiet retired callbacks and inline covered
read errors. Complete-source, zero-ledger, no-duplicate-write and original ownership
assertions remain. Preview mutation tests hold actual current validation boundaries;
they do not wait for a removed fourth validation context.

The corrected original selection passed across the recorded focused continuations:
47 cases in the first corrected run and the final 12-case account/plan method after
the account recovery change. Six plan cases overlap those runs and are not additional
distinct coverage. One existing detached-source guard was included alongside the
52 original cases. The eight new receipt/account negative cases are separate.

## Original native qualification

A fresh API 36/x64/4096-byte emulator used the independent complete signed package
`pro.vafadar.zanance.cireadbackreview`, UID 10216, PID 4118, boot
`26addd19-0e02-466d-afbc-e5f0eb6e0063`. Its initial startup remained covered; the same
live process reached onboarding without another launch. The original reminder
explanation was dismissed through its existing Not now action before the baseline.

One zero-opening fictitious funding account was prepared. The original Accounts/Add
route opened the actual account editor. A distinct background metadata account
changed its source; one native Save was refused, raw name and `0.00` remained, and
the explicit original availability action performed the complete read. Save became
available only after that read; the complete source remained equal and ledger empty.
The unaccepted draft was cancelled/discarded, without saving it.

The original Import & export page then chose one distinct fictitious CSV through a
bounded private picker port. Its original preview held inactive access; a separate
explicit private context transition supplied the actual original writer's Free
personal-profile rights. One native Import produced one expense of 1234 minor EUR
and one original import journal batch. The actual batch list became one, no false
follow-through error appeared, and the preview could not be submitted again. Of the
20 complete typed source collections, only Entries and ImportLinks changed. The
original full note, second line, tags, amount and title remained exact. This is a
bounded picker/rights port qualification, not real picker, identity or Store acceptance.

The native package hash is
`345448589451c858533cc042fb775d87e23dc46e68f7ceb28d7816f4650bb519`;
192 AOT assemblies, 204 ELF files and 804 static aligned LOAD segments passed with
offline/non-debuggable manifest, ZIP/CRC and v2/v3 signatures. A private observer's
missing packet member failed compilation before installation and was corrected;
no production source change or replay was required for that error.

The original Home and all five settings, size/density, locale and boot were verified
before retiring only this fresh fixture. Its UID and 29 registered hash-matched XML
files (1,169,313 bytes) were removed, the emulator port closed, and the official AVD
deletion removed 25 files/5,132,727,243 logical bytes. Logical byte counts do not
measure newly freed host disk space. No phone or retained emulator was commanded.

## Final package, cleanup and remaining qualification

The strict Windows, independent native and final shipping builds passed with zero warnings/errors.
The first shipping output failed the offline manifest guard before installation;
its build invocation omitted the original explicit blank cloud-client properties.
The failure and digest are retained. The corrected complete shipping output uses those
original explicit offline properties and has SHA-256
`0e3e684a098f4a542576bfaae6fce7e523439d4daae02c5d3c89c580bcea5540`:
98,918,303 bytes, 384 AOT assemblies, 408 ELF files and 1608 static aligned LOAD
segments; offline/non-debuggable manifest, ZIP/CRC and v2/v3 signatures passed.
No successful installation or accepted financial operation was replayed. Physical
adoption and pushed-head CI are separate; CI is checked once after the push.

Four standard Clean commands completed. The registered inventory for the first
three accounts for 2846 deleted files/854,972,624 logical bytes; that byte count does
not include the fourth shipping Clean or represent host free space. Supplementary
cleanup of 11 exact owned targets was rejected before process creation with
`blocked by policy`: 3715 files/947,761,224 logical bytes remain. No retry, subset,
alternate method, move, escalation or reuse of that scope is permitted. Earlier
refused cleanup scopes remain untouched. Used signed packages and local evidence
are retained.
No physical-device command, D-607 source observation, deadline change or previously
accepted financial operation is required or repeated by this batch. General backup
discovery and other sensitive-page lifetime work remain separate backlog items.
