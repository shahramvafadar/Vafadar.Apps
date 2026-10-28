# Covered startup guidance for the original space refusal (D-606)

## Defect and correction

D-605 preserved real independent startup failures in encrypted staging and ordinary
shell free-space observations consistent with the original staging bound. It did not
prove an exclusive cause for each prior caught error. The original startup surface
treated all preparation failures alike and offered key-code/backup recovery even for
the actual known insufficient-space refusal. A recovery command captured before the
UI changed could still invoke those callbacks.

The original SQLite staging guard now throws a fixed-message IOException subtype,
`InsufficientDatabaseStorageException`. Its predicate remains exactly
`available < checked((long)maximumBytes * 2 + 16 * 1024 * 1024)`. No required bytes,
parser, key preparation, source/header checks, staging ownership or financial behavior
changes. Only that actual type receives specific safe-storage guidance. Identical
generic IOException text remains generic; arbitrary messages are never classified.

The original startup VM keeps all financial content covered, hides key/backup recovery
for the known space refusal and rejects already retained recovery commands. Its
existing explicit Retry resets the reason and invokes original preparation once.
Successful Retry clears the failure; a new generic failure restores the original
recovery choices. A space refusal during explicit recovery also stays covered and
blocks further recovery until Retry. There is no authentication fallback, automatic
retry, space-threshold relaxation, deletion of user data or new account/profile.

The English, Persian, German, Spanish, French and Italian notice asks the user to
free device storage and retry while financial data remains hidden. The original
scrolling startup page and retry command remain. The unchanged-source distinction
is meaningful: this handles the known preflight guard, not arbitrary disk-full or
I/O errors elsewhere in the storage stack.

## Focused managed evidence

Three genuine new regression failures are retained before the behavior change:
one actual SQLite staging case receives generic IOException, and two original-VM
cases still offer/invoke recovery. The new exception type existed only as baseline
scaffolding; the original guard and VM behavior were unchanged during reproduction.

After correction, seven distinct new cases pass: one real in-memory SQLite portable
image reaches the unchanged guard one byte below its original bound with no candidate
or key-port effects and source bytes unchanged; six original-VM cases cover recovery
visibility, retained commands, identical untyped error text, successful Retry, a new
generic reason with actual property notifications and a space refusal from recovery.
Four affected existing recovery/exclusion cases pass. Direct runtime14 DLL result
XML proves exact counts7+4 with zero failed/skipped/errors; no full unrelated suite,
previous successful financial writes, reminder queue, tap or timed-delivery cohort is
replayed. Fake recovery ports do not qualify actual device authentication.

## Native layout and package adoption

The fresh `pro.vafadar.zanance.startupstoragereview` package uses the exact original
startup XAML/VM, translations, styles and fonts. Its separate Application deliberately
does not construct financial App/database/account/reminder services. The fault port
throws the typed refusal; this is synthetic UI input, not actual device disk exhaustion,
key recovery or authentication. It qualifies these controls rather than the complete
production application host, startup lifecycle or wide-window readable-column policy.

Eighteen distinct native cases pass on API36/x64/4096 at actual OS font_scale2.0:
English/Persian/German, light/dark and widths360/412/1024. Complete actual TextView line
ends, no ellipsis/cropping, complete translated XML text, viewport bounds, absent recovery
captions and at least44dp Retry targets are checked. The maximum notice is8 lines;
retry heights are53px or65px at density1. These18 layout observations are not new unit
tests. No screenshot protection is removed and no TalkBack cohort or financial Save runs.

The three width groups use PIDs3789/4174/4443 with the same signed APK. Each language/theme
is visited once per width. The first process remains previous/adj200 after Home/Settings,
so bounded kill preconditions correctly refuse; no force-stop follows. Normal root Back
closes the completed synthetic window, actual cached=true/adj900 is then proved and only
that fresh process is killed before the new width group. This is harness continuation,
not protected-storage cold recovery, natural low-memory or uninterrupted process survival.

Windows strict build passes with zero warnings/errors. Complete Offline shipping APK
`E:\Zanance.ReviewBuilds\D606-shipping-package\pro.vafadar.zanance-Signed.apk` has SHA256
`1339ab5bcc1bdae052b6ed871dea82f7655e827dd79ae54766f448529f2c1870`;
384 own AOT libraries/408ELF, CRC, static16KiB segments, no INTERNET/debuggable flag
and actual v2/v3 signature checks pass. The independent used APK has SHA256
`d9d084bdb4a3eefa19ac994b7d382384fe6fbb1154805c913e27c6dd80a8b5bd`,
192 own AOT/204ELF with the same static/signature/offline guards. Static16KiB is not
hardware runtime qualification. The initial independent dispatcher compile error is
corrected before installation; the first unused package predates the final native
layout checks and is not claimed as the used witness. Validation/helper import failures
are preserved; their failed entry points did not execute device operations.

One complete signed no-reset Samsung upgrade succeeds from D-605/17668/1f9816c4 to
D-606/20683/1339ab5b. The first10-node loading observation remains unaccepted; only
the same20683 is read again after loading. All141 original public Home attributes and
Home979 then match exactly. UID/CEDE/permission flags/caller/five settings/boot/display
are unchanged. No Main database/key is read and no financial operation/authentication
is replayed. This is adoption/public-state evidence, not full private-data equality.

## Restoration and cleanup

Only the fresh synthetic package is uninstalled including its data; the actual original
emulated Main/caller APKs, UID/CEDE/flags, boot, display, five settings and shell match
their before snapshot. Main is not opened or upgraded on that emulator. Actual font
and size overrides return exactly to their initial values. Twenty-one newly owned
remote XMLs are hash/size matched, removed and their exact paths checked absent:
217171logical bytes. Local source evidence and used signed artifacts remain.

Four standard Clean operations remove3068 registered files,1011599316logical bytes
from only the new d606-data-tests/app-tests/windows/native artifact scopes. Every
registered path is confined before cleaning and confirmed absent afterward. No old
rejected cleanup scope, protected archive or used signed package is touched. Shared
shipping build intermediates remain for ongoing approved delivery; these logical counts
are not a host free-space or all-disk cleanup claim. The owned AVD receives one stop;
its first one-second serial check is still pending. A later read-only continuation
confirms both its serial and original host absent, original configuration unchanged
and both existing ADB servers retained. The stop is never repeated.

Evidence pointer: `%TEMP%\Zanance-D606-current.txt` retains managed XML, source-bound
package checks, finite native cases, before/after public ownership and cleanup receipts.

## Remaining boundaries

This change does not qualify arbitrary full-disk recovery, whole database/key equality,
general security, OEM behavior, punctual delivery, ANR/crash, speed, accessibility,
iOS, providers, Store or publication. TalkBack remains last and Persian speech parked.
The overall delivery goal remains active.
