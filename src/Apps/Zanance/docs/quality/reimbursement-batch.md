# D-351 indexed open reimbursements

## Defect and maintained contract

OpenReimbursements previously called the single-entry calculation for every eligible expense.
With n ledger rows and k positive reimbursable expenses it scanned the history k times, then sorted
by date. Home, Reimbursements, net worth and the reimbursement KPI share this Core authority.
The reference/tenfold Home fixtures do not contain reimbursable amounts; their earlier passes
cannot qualify this defect. No retained fixture is changed or reseeded for this delivery.

Collect eligible original expense objects once, index their identities, then aggregate only linked
Refund rows in original source order. Preserve checked intermediate long additions; unrelated/orphan,
non-expense and nonpositive-request groups must not introduce new overflow failures. Duplicate expense
identities still receive their own requested amount and retain their original objects. Clamp remaining
amounts exactly as the unchanged single-entry API does; retain stable oldest-first ordering. History,
titles, tags, currency association and source metadata remain complete. Complexity becomes
O(n + k log k), with O(k) indexing, instead of O(n*k + k log k). No cached financial substitute,
schema/compiled model, provider, key/KDF or runtime policy change is introduced.

## Focused regression evidence

The original production implementation fails exactly the two new deterministic enumeration-bound
cases: 8,320 row visits for 64 expenses/128 rows and 2,099,200 for 1,024 expenses/2,048 rows.
Thirteen additional cases already pass on that original implementation. They compare with the
unchanged public single-entry API, verify exact original objects/stable equal-date order and full
serialized input preservation, ignore unrelated overflow groups, retain relevant positive/negative
and intermediate overflow refusal, and cover duplicate expense identities. No clock threshold is
used as a unit assertion.

After indexing, all 39 selected reimbursement/report/KPI cases pass (15 new; zero failed/skipped),
under a strict warning-free Core test build. This is a focused run, not a new full-suite count.
The accidental invalid wildcard filter invocation ran no tests and is not counted as verification.
Windows strict application build passes with zero warnings/errors in 26.24 seconds.

## Qualification boundaries

The independent Release/AOT/trimmed x64 Android caller compares the actual unchanged single-entry
API with the batch for 64/1,024/8,192 expenses and checks ordered tuples and complete serialized
inputs. Its new fictitious page fixture exercises the actual original ReimbursementsViewModel and
ReimbursementsPage with a partial refund, native title/amount publication, actual Activity focus
and complete before/after stored account/entry/settings equality. It never activates financial UI
commands or touches the main application's database. The independent presentation SQLite fixture
is deliberately not an encryption qualification; no crypto tests or migrations are replayed as
acceptance of this arithmetic change.

This delivery does not change the reimbursement page layout, virtualize its large list or establish
whole-Home latency, a two-second target, cold/low-memory/ANR, phone, TalkBack, iOS or release acceptance.
Earlier runtime failures remain open and are not attributed to reimbursement arithmetic.

## Actual Android result

Comparison9c7c8a3764634f50ac3827273622ff66 on independent run
9f3a2b0a142a4db0b8ea9bc8e2675c24, PID22994 completes without restart. The first immediate
PID lookup is empty; later authoritative state identifies the same launched process, not a failed
launch. Startup-wait16:02:38.894 becomes startup-ready16:03:39.539. Preserve that observed startup
cost; this delivery does not fix it. Bounded report-observation expiry does not trigger another run.

| Reimbursable expenses / ledger rows | Original single-entry composition ms | Indexed batch ms |
| --- | --- | --- |
| 64 /128 | 150.2774 | 116.1355 |
| 1,024 /2,048 | 38.0960 | 0.4363 |
| 8,192 /16,384 | 2356.9057 | 3.0322 |

These are one cold-sensitive descriptive observation per size, not repeated statistical timing or
whole-Home qualification. All three preserve the exact ordered original tuples and complete serialized
inputs. The original native reimbursement page publishes the partial refund correctly and complete
stored accounts/entries/settings remain identical. Actual Activity focus guards pass before the native
acceptance; no UI financial command or system setting is changed. The large native list remains
unqualified; its existing nonvirtualized layout is not changed by this Core fix.

## Complete main candidate and restoration

Both required builds complete without warnings/errors: Windows strict26.24s and complete offline
Release Android through Build-AndroidApk.ps1. The signed main APK is
artifacts/android-reimbursement-d351/pro.vafadar.zanance-Signed.apk,94,801,653bytes,
SHA2560f7ad8f5fdd770e85e52eb8cb39b09ffedb308733c8d72077cbb409d5e1fc32c.
Signature verification passes with existing development certificateSHA256
92cf83dfc05c274ad7fc99b820f9f1b272ff46eb5fb32b3039267074ed36167b.
Packagepro.vafadar.zanance retains min24/target36, ARM64/x64 and384 AOT images, without INTERNET
permission or debuggable flag. This is a development-signed installable candidate, not Store signing.

After its completed receipt, stop only the independent review package. Upgrade Main with install-r,
without reset/clear/uninstall. MainPID23366 then presents the original English Home/My account and
Recorded balance0.00EUR, Expense/Income/Transfer/Receipt actions and original navigation in its actual
focused native window. Accessibility services are observed absent before the ordinary tree capture;
no settings are changed. The owned remote XML is removed only after its SHA256 matches the saved
capture. No financial Save is performed. Required test-output cleanup completes without errors/warnings.
No previously successful unrelated financial, crypto, OCR, font-scale or reader suite is repeated.
