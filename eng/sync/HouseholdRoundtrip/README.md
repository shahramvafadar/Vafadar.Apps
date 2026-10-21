# Protected household HTTP process qualification

This independent Windows executable uses the original protected client factory, device-bound DPAPI generation
keys, reviewed source-built SQLCipher provider, original financial store/capture/review/attachment/apply code,
actual pinned HTTPS service and an explicitly isolated PostgreSQL database. It never opens the main app or owner
data. Its only native fixture root is under `artifacts/household-network/`; symbolic/reparse redirection is refused.
Its only accepted database is `zanance_identity_household_native_d294` on localhost. The fixture owns its synthetic
identity, enrollment outbox and wrapped sessions; no password is persisted or printed. No readable financial
HTTP route is used. Service/runtime resources are separate from other projects.

The initial `prepare` phase requires a fresh owned root and the isolated database. It migrates only that schema,
grants its existing runtime role the required ordinary DML privileges, starts the actual service, provisions an
original protected client, stores its independent root/device keys before reservation, journals its epoch and
literal membership request before publication and freezes the original financial capture/ciphertext before
upload. The fixture has oneEUR account, one1234-minor-unit expense and a2,244,275-byte synthetic attachment.

Run the following modes in order over the **same** root and database, preserving original files, sessions,
service certificate/port, key handles and failed evidence. Do not rerun completed setup phases or reset fixtures.

| Mode | Actual boundary / required result |
| --- | --- |
| `prepare` | PostgreSQL commits the real ciphertext publication; the process exits23 before returning its HTTP completion. |
| `resume` | A distinct process reopens the same original keys/cutoff/ciphertext and recovers the exact committed attempt. It stores the first actual ciphertext feed page, then exits24 during the next download. |
| `finish` | Another process proves its first feed request starts after the retained authenticated address prefix, downloads missing pieces, verifies the complete graph/attachment and invokes the original financial apply. Exit0; one committed server round, exact attachment hash and expense, no retained acknowledged queue and zero readable financial requests. |
| `cold` | Reopens the same completed protected graph and performs an empty local no-op, with no extra server publication. Exit0. |
| `race` | After preparing original amount1500, an independent actually authorized operation publishes1300 at the competing cursor. The coordinator durably tombstones only the stale unpublished attempt, preserves the same logical cutoff, exposes the original explicit Amount conflict and applies the user's remote choice without another payment/publication. A late old open is refused410. Exit0. |
| `race-cold` | Reopens the resolved1300 amount and empty no-op without another server publication. Exit0. |
| `local-apply-cut` | Original production capture/HTTP/inbox/apply components commit amount1400, then the process exits25 before separate household ciphertext-journal retirement. This mode explicitly orchestrates the exact original final transaction boundary. |
| `local-apply-resume` | The production coordinator retires only that actually applied cutoff before creating any new capture; the same1400 amount remains and no fourth financial commit is invented. Exit0. |
| `abandon-cut` | Original amount1500/ciphertext is retained; the actual server commits its exact abandonment tombstone, then process exit26 occurs before local acknowledgement. |
| `abandon-resume` | The actual own-attempt observation recovers abandonment, preserves the original logical operation/cutoff, generates only the now-authorized new attempt and publishes it once. Exit0; four total committed financial rounds across the full sequence. |

```powershell
dotnet build eng/sync/HouseholdRoundtrip -c Release -p:ContinuousIntegrationBuild=true
# Supply only this fixture's local connection through ZANANCE_HOUSEHOLD_TEST_DB; do not print credentials.
dotnet eng/sync/HouseholdRoundtrip/bin/Release/net10.0/win-x64/HouseholdRoundtrip.dll artifacts/household-network/<fresh-id> prepare
# Continue the exact retained root with the next mode after its deliberate exit.
```

The completed actual sequence is retained at `artifacts/household-network/d294-persisted`, with content-free
checkpoints and `household-native-network-*.log` evidence in the outer artifact directory. Exit23/24/25/26 are
intentional actual process-loss boundaries, not failed tests or power-loss proof. The race uses another approved
operation of the same actual device key; it does not claim a new physical device or second household admission.
Earlier managed service scenarios separately cover actual distinct member/device consent, roles and transfer.

This does not complete main app registration/UI, optional household recovery, selected-account/private-counterpart
projection, household cost storage, local viewer enforcement, large-profile/history bounds, retained deletion,
Android/iOS, physical/hardware acceptance or publication. It is acceptance of the changed original protected
coordinator path inside the still-unfinished whole household batch.

## Packed framing continuation

The retained fixture subsequently exercised these additional explicit modes, without replacing any original
key, financial file, service certificate, failed result or committed operation:

* `bulk`: adds12,000 original account rows, a639,999-character path expectation that was initially incorrect
  (the actual valid path is679,999 characters), and300 distinct two-byte attachments. The original financial
  planner/capture prepares345 original ciphertext parts. Initial single-part uploads hit the actual unchanged
 200-request/minute service limit after195 stored parts. This failure is retained, not bypassed by raising quotas.
* `bulk-resume`: observes exact original part index/address/version/digest metadata, skips those verified stored
  parts, sends missing ciphertext in batches of at most8 and reads complete signed manifests in256-part pages.
  The first continuation completed actual finance but the final incorrect fixture path-length assertion failed.
  After correcting only that expectation, the same retained database/keys/operation reopened with12,001 total
  accounts,301 attachments and the valid original large row. No financial fixture was regenerated/reset.
* `stream-refusal`, executed while the retained cursor was5: actual complete manifest crosses at least two HTTP
  pages and matches the original verified author-signed head. Approved-author candidates with a dishonest final
  row count, missing final stream block and oversized row prefix each refuse without changing the verified graph,
  head or either cursor. This is framing refusal acceptance, not an external security audit.
* `wide-row`: updates the original forecast to47,619 valid typed points and the exact scope of12,001 unique
  original accounts. Its complete canonical original row exceeds1MiB and crosses bounded encryption messages.
  Original household capture/staging/apply now preserve it without altering readable personal HTTP limits.
  `wide-row-cold` reopens and compares both original strings, scope, typed points and exact signed replica.
* `small-attachments`: adds600 more distinct two-byte original attachments. Their financial rows and bytes fit
  fewer than16 original ciphertext parts, including bounded small-piece arrays. A byte changed by the actual
  approved signer is refused at complete SHA/graph verification without replacing any prior verified state.
  Actual original application and `small-attachments-cold` then verify every original byte of all600 new files;
  the fixture has901 total attachments and cursor7. It creates no extra payment or readable financial request.
* `row-bound-refusal`: repeats only the affected allocation-bound case after widening the reviewed local
  complete-row bound to16MiB. A larger declared frame is refused before allocation/application, with the complete
  original graph, publication head and cursors unchanged. Earlier successful unrelated refusal cases are skipped.

Evidence is retained in `artifacts/household-native-network-bulk*.log`, `household-native-stream-refusal.log`,
`household-native-wide-row*.log`, `household-native-small-attachments*.log` and
`household-native-row-bound-refusal.log`. Deliberate old setup/mutation modes are one-use; subsequent continuation
must respect the actual retained cursor/state rather than reset or rerun them.

## Historical content and incremental replay continuation

`archive` requests deletion of only the original fictional large attachment from the live graph. Its first
run exposed that this old already-selected native generation had not been migrated by the independent caller;
the queued original deletion and old ciphertext/keys were preserved. Startup now applies the original additive
migrations after the protected factory is actually opened. `archive-resume` continued that same pending deletion,
with no new identity/key/file. After actual publication/application at cursor8, the replica owned only1800 live
attachment bytes. Its historical2,244,275-byte original attachment was read in original2MiB pieces from the
authenticated retained ciphertext, with the original complete hash and no return to resident caching.

The same case changed only its explicitly captured derived source pointer to another event, checked that both
content return and silent reindex repair were refused, then restored exactly that fixture pointer. Original
financial data/cursors stayed unchanged. Original portable memory export then scrubbed all nine household
authority/archive tables and private device frame, while the original protected index/history stayed present.
`archive-cold` reopens the same actual files/keys and repeats only cold original historical-content reading.

`ordered-replay` qualifies the incremental original verifier and the now-streamed original native ciphertext
cursor. Its invalid first message must not cause a second source pull and must dispose the iterator. Missing,
excess and canceled input refuse before publishing any financial state. The first case exposed cancellation
that could arrive while the iterator advanced; checks now also surround advancement and final publication.
The failure and exact retained-fixture continuation are preserved, not hidden by a reset.

`headers-cut` creates a new original attachment and actual server publication9, stores a valid partial cipher
prefix under the new protected lookup columns, then deliberately exits29 at the next actual feed request.
`headers-resume` first independently verifies that same original pending prefix, proves its first HTTP feed
request begins there, completes the original content/hash/application and retires the exact acknowledged cutoff.
The native fixture ends at cursor9. Completed earlier mutation modes must not be rerun at that later cursor.

Evidence: `household-native-archive*.log`, `household-native-ordered-replay*.log`,
`household-native-headers-cut.log`, `household-native-headers-resume.log`. Resident byte counts cover only owned
attachment arrays, not total process memory. This does not qualify full-profile native/mobile memory, outbound
ciphertext streaming, historical-row/opaque-index compaction, Android/iOS, power loss or final main app adoption.

## Later bounded outbound continuation

`outbox-upgrade-cut` retained old-format exact original operation/cutoff/signature/ciphertext, then exited31 before
upload. `outbox-upgrade-resume` upgraded that same protected file, matched its complete literal-byte digest and
published/applied once at cursor10. No signing key, operation or nonce was regenerated for the retained attempt.

`stream-seal-cut` froze an original change and4,227,077-byte attachment, observed eight actual encrypted part
inserts in the open native transaction and exited32. Cold resume first exposed SQLITE_READONLY_ROLLBACK8/776;
the original file/journal/key and failed evidence were preserved. Completed initial-candidate verification now
permits existing-only native rollback under its drained owner, then sets query_only. It never manually deletes
sidecars or makes unselected candidates/independent rollback snapshots writable.

`stream-seal-resume` continued that same hot journal, verified no half-sealed attempt/parts remained, refused a
late complete-source SHA mismatch and cancellation after real cipher writes with complete rollback, then sealed
the unchanged original cutoff and exited33 before financial HTTP. `stream-seal-publish` compared its original
signature/cipher digest, refused a deliberately changed stored body before upload, restored only that captured
synthetic JSON cell and used bounded original native pages. Actual original application preserved1700 minor units
and the full attachment hash at cursor11. `no-op-race` retained a local zero-part decision while a real concurrent
1800-unit publication advanced the head, replanned only that never-uploaded empty decision at the original cutoff
and applied at cursor12 without an extra financial publication. No readable financial endpoint was used.

Evidence: `household-native-outbox-upgrade-*.log`, `household-native-stream-seal-cut.log`,
`household-native-stream-seal-resume*.log`, `household-native-stream-seal-publish.log`,
`household-native-no-op-race.log`. The retained fixture is now at cursor12; do not rerun earlier mutation phases.
Production keeps at most eight original cipher bodies per upload read and one original2MiB source buffer;
only explicitly bounded negative fixtures collect complete cipher arrays. No production observation hook or
plaintext staging file was introduced. These cases do not qualify restored-generation hot-journal recovery,
full-profile process memory, historical compaction, mobile native behavior, power loss or final app integration.

Accepted native role continuation uses a separate owned root, household-network/d294-roles, without resetting the
large retained stream fixture. `roles` enrolls an independent actual invited viewer, receives original encrypted
finance without server write rounds, checks native store/EF refusals and device-only preferences, then qualifies
owner-approved editor rotation/publication and expiry at a borrowed native Commit with an injected clock.
`roles-resume` is only for its interrupted remaining editor/expiry phase: retain original files/keys/identity and
consume an already committed current role instead of repeating invitations. `roles-revoke` continues that fixture
once with an original pending ciphertext and actual owner revocation; scoped404 must persist local write refusal
without dropping original pending finance/ciphertext. `roles-grace` continues only the remaining owner read-only
host-expiry phase, without repeating successful role cases. These phases mutate only their independent fictional
service room and native files; completed mutation phases must never be rerun. Keep the active batch's failure and
continuation evidence. They do not qualify main household UI/profile, Android/AOT, recovery or whole delivery.

`workspace` uses another fresh independent root and the actual registered HouseholdWorkspace. The local service
commits genesis but withholds its first response; `workspace-resume` continues the same protected profile, root,
device, setup operation and exact proposal. It then exercises original app private invitation, recipient claim,
owner-only pending proof, explicit fingerprint approval, viewer receive and approved editor publication. Completed
named phases are retained in checkpoints; do not reset data/keys or re-enroll a completed case. The original failed
empty-local-history bootstrap is preserved in the first log and fixed by zero-version-only verified history handling.

`workspace-foreground` continues only the remaining original app foreground-dispatch case on the same data. It
compiles actual non-UI app sources, retains native access/configuration/publication ports, and requires a real
protected edit/HTTPS/PostgreSQL application with no readable personal transport. A completed marker/account refuses
rerun. `workspace-switch` checks a setup retired during real identity HTTP using two original keyed factories. An
interrupted failed assertion may reopen its same original target files/keys; a completed checkpoint refuses repeat.
These cases do not establish rendered main UI, Android/AOT, final recovery/cost/history or whole-batch acceptance.

`workspace-rebind` qualifies the app workspace's original device-possession/session composition using the already
approved existing native key/session, with unchanged root/device fingerprints, membership and financial horizon.
It is not a new interactive-login, rendered main UI or new-device approval test. The original service's separate
new-login/private-key tests remain different evidence. Use retained logs; do not rerun a completed phase needlessly.

Current full financial qualification uses a fresh independently owned `costs` root and the explicit household v2 contract; it never resets or relabels historical21-table fictional rooms. `costs-resume` retains original files/keys and completed named phases. The actual two-client cost/share/refund/repayment path must use zero readable personal requests and exact original native writers. `costs-app` runs original app editor source on those same protected financial facts with explicit native-effect ports, refuses a completed Save rerun and checks culture changes, currency clearing, partial-sum refusal, one confirmed publication and draft retirement. `costs-cold` only reopens the same actual native cost/key/role/repayment facts; it is not another financial publication. These cases are distinct from real rendered main controls, Android/AOT, recovery/history and final whole-household acceptance.

`recovery-enroll-cut` continues that same full-contract fixture only: an original password-encrypted fictitious kit
is exported, its actual upload commits before deliberate exit31/lost response, and `recovery-enroll` finishes the
same exact retained private preparation and ciphertext. `owner-recovery-cut` first publishes the existing unsent
original app edit once, then uses only the exported kit and actual fictitious identity session on a separate empty
protected client; its new root/device commit precedes exit32/lost response. `owner-recovery` resumes those original
request bytes and receives the complete existing finance. The old device must be refused. `recovery-cold` reopens
the same recovered device/keys without importing the kit again. `recovery-append` checks the ordinary role change
while a kit is enabled and the unchanged recipient's complete recovered-root verification, with no private-root
delegation to that viewer. `recovery-revoke` retires the kit by replacing both owner key roles, preserving financial
cursor/history and refusing old-kit discovery. Every completed marker refuses mutation reruns. Keep keys/files,
retained first failures and current service/schema; never reset a successful case to make a later phase pass.

`costs-conflict` is a separate new continuation on `d294-cost-v2`, using the actual `recovered-owner` and existing
recipient rather than the retired original owner's session. Its phase journal retains the exact cost and original
note through two-client edits, encrypted conflict staging, original common application typed review/explicit
remote choice, native application and ordinary encrypted note restoration. Phase6 refuses replay. It checks all
three review languages and zero readable personal HTTP/cash entries, but not native rendered conflict controls,
inline-file choice or mobile acceptance. The recipient is now an approved editor for this new scenario.

The first dispatch encountered401 on the obsolete original session; a direct recovered-owner dispatch then found
its session absent. Generated fixture passwords were never retained. Only the two exact cost-v2 example.invalid
identities can use the caller's real local recover/protected-outbox/complete/sign-in renewal; it does not print or
persist passwords/tokens or fabricate session grants. Same approved-device possession rebinds the fresh session
before continuing the same protected files/keys. Retain those failures; never reset financial state or recreate
devices merely to replace a missing session. Service/client source builds compile the actual application review
sources, not duplicated review/financial algorithms.

`app-source-server` creates one fresh independent `d294-app-source` local fixture for the main app identity
`pro.vafadar.zanance.householdsourceflow`; `app-source-server-resume` serves only that original root/port/schema.
The original app Debug route `household-source-flow` receives that exact owned root through
`VAFADAR_HOUSEHOLD_SOURCE_FIXTURE`. Only a generated example.invalid identity password is briefly current-user
DPAPI-protected for the separate process; it is not printed or entered for the owner. It expires for sign-in and
was deleted after verified completion. Existing actual native sessions can still use the public peer metadata.
The original app progressed through native selection/review/consent/flyout/prompt, actual durable profile creation/
drained switching, explicit destination copy and real encrypted publication. It required unchanged complete source
finance and same-key target reopen. The service observes only its exact public completion marker and refuses a
completed rerun. Its six-minute deadline ending without completion is terminal host evidence; resume only the same
root when another original app continuation is needed, never recreate its identity or financial state.

The two incomplete native observer attempts stopped before target creation: Windows action-sheet items were
mistaken for ContentDialog, then a managed TextBox Value provider was unavailable. Native flyout Invoke and
acceptance of the original explicitly pre-filled profile label completed the flow without desktop input/focus.
This is functional main Windows adoption, not native text-entry pattern, Android/AOT/iOS/phone or whole-household
acceptance. The original18-case source-control layout checks were not repeated. Native TextBox peer implementation
details are documented by the platform at
https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.automation.peers.textboxautomationpeer .


`device-admin-cut`, `device-admin-resume`, `device-admin-cold` continue only the original `d294-cost-v2`
recovered owner and an independently generated current-user-protected second device key. Pending preservation,
wrong-fingerprint refusal, actual delegated private-root opening, root/epoch/recovery-archive retirement, lost
membership acknowledgement at exit35 and same-key cold denial are distinct checkpoints. Phase5 refuses replay.
`pending-member-preservation` has its own original phase journal and fictional recipient: unaccepted claim
preservation, exact approval, read-only reception, old-invitation finish/digest refusal, and explicit removal.
Phase6 refuses replay. Do not run the old original owner's session, recreate keys, reset either financial fixture
or repeat a completed phase. Generated fixture identity credentials never appear in output or retained passwords.

`app-management-server` serves only the completed original source root/port/schema for the new main native
approved/pending-device page review. It neither recreates nor reads the deleted generated identity password.
`app-management-root-server` serves the subsequent distinct root-display/stale-invitation native action review.
Each waits only for its own public completion marker; prior source and successful18-case layout runs are not replayed.
Stop a terminal waiting host before rebuilding its loaded Windows binaries.


`device-access-cut`, `device-access-resume`, `device-access-cold` use one new `self-enrolled-owner` protected client
of the actual retained cost owner. Exit36 follows real enrollment commit before local remote-id binding. Resume uses
one actual fresh fictional identity session and the original private key/setup operation; login/pending metadata grant
no keys. Exact owner approval/typed anchored history/native finance/private-root adoption and separate cold reopening
complete phase5, which refuses replay. Do not rerun completed cost/device/root recovery scenarios.

`first-device-receive-cut` creates a distinct `first-receive-owner`, approves its actual retained key, verifies denied
financial writes before/after the committed actual receiving connection, then exits37 before first apply. Only
`first-device-receive-resume` continues those same files/keys/connection. The initial new-guard sealing refusal is
retained; its zero-proposal original receiving cutoff resumes through the restricted local receipt, not a write grant.
The complete original source financial-column digest (including Settings) must match after first apply. Phase3 refuses
replay. No pre-existing financial files, keys or progress markers are replaced to make this pass.

`app-device-access-server` serves only the retained original main source root/port/schema and waits for its distinct
main cold completion marker, with a bounded12-minute host lifetime. The original app's `household-device-access` route
uses original native export/request/Cancel/admission actions. Owner workspace approval remains an independently checked
fictional fixture step; native owner text entry is explicitly not accepted. A separate process with
`VAFADAR_HOUSEHOLD_DEVICE_COLD=1` reopens the same original target and verifies the actual native write boundaries in
fully rolled-back transactions. The new18-case device-access control matrix has its own progress and is never replayed
during a cold-only continuation. Window input experiments were removed after the hidden window exposed only its chrome;
no desktop-root fallback or OS/script policy change is allowed. Platform references:
https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.automation.peers.textboxautomationpeer
and https://learn.microsoft.com/dotnet/api/system.windows.automation.automationelement.fromhandle .


Ownership continuation uses the retained original d294-cost-v2 recovered-owner and recipient, never its revoked old
client. ownership-resume retains one exact operation and exits38/39/40 before prepare/authorize/accept HTTP, then41
after final membership commit. Continue only that original incomplete mode; completed operation refuses replay.
ownership-cold is a distinct one-time cold committed-root/epoch/writer check after explicit handoff Finish. The
original recipient is now the actual owner; the previous recovered owner has left and cannot use financial endpoints.
Original cost/financial rows and key files remain retained. app-ownership-server serves only the original separate
main-app no-pending ownership status review and stops at its exact native completion marker; it does not manufacture
a transfer or repeat completed source/device acceptance. Follow11-household-delivery.md for current fixture authority.


The subsequent one-time ownership-catchup case deliberately exposed a downloaded-versus-applied financial gap,
retained the same Prepared operation/candidate, completed bounded incoming adoption and Viewer disposition, then
restored its own fictitious cost title through actual Save/sync. Its phase8 is complete. ownership-catchup-auto then
qualified a fresh Offered-stage automatic receive, wrong-operation and ordinary-rotation refusals, clean-horizon retry,
no reusable writer scope and Editor disposition; phase5 is complete. Both actual complete financial digests match
the original baseline. Never replay either completed mode. The current owner is recipient; returning-owner is the
approved Editor for the original owner identity. recovered-owner remains a revoked device and supplies only its
actual current fictional identity session. Do not reuse old role/key assumptions from earlier completed cases.

The new one-time app-lifecycle-server mode resumes only the original independent d294-app-source host for new main lifecycle/audit status layouts, applies the actual additive service schema and stops at original-app-lifecycle-status.complete. Do not replay original source/device/ownership layouts. Separate original protected process/deletion/snapshot/audit evidence and commands are in ../HouseholdLifecycle/README.md. Whole D-294 remains unfinished.

## Bounded financial history only

history-prepare creates a separate independently owned current-contract fixture with12000 accounts, a large row
and8 synthetic signed publications. It is native storage qualification without hosted/API acceptance.
history-prepare-resume is only for the retained initial failed connection preparation; it demands cursor0 and
reuses all original keys/data. Do not use it after completed preparation. history and history-cold read the exact
retained fixture once per process, with no HTTP, and check complete financial/history digests, bounded pages and
original row framing. history tests old-baseline conflict and corrupt/missing derived positions; history-cold does
not replay those successful cases. history-export separately checks the new private-index portable boundary.
Completed result files refuse replay. Retain negative evidence and keys. Current root:
artifacts/household-network/d294-current-history; old d294-persisted remains preserved after contract refusal.

history-compact-export separately qualifies the retained fixture through the production pre-allocation private-row omission. history-native-bound checks only the small native page-budget/detach/financial-byte mechanism. Each has its own completion guard; never replay accepted history, source, crypto or UI stages. The Android ReviewHouseholdSnapshot caller source-links only that native mechanism and uses an independent Release AOT/trimmed package.


## Original monetary ownership continuation (D-436)

`costs-money` uses only the retained d294-cost-v2 returning-owner protected client and its actual
current Editor permission, without HTTP, new service identities or replacement keys. It applies the
original current additive local metadata migration before reading. The first older-schema read
failure is retained. It checks consent edit-and-return refusal and original writer ownership, then
accepts exactly one cost-title metadata edit and deliberately fails the applied observer. Its durable
start/accepted/completion receipts refuse uncertain or accepted financial replay. Explicit readback
recovers the original application form; amounts and cash movements are not reposted.

`costs-money-rollback` is a separate one-time continuation of that completed source. It submits a
changed cost title and a changed repayment note and retires each original owner after SaveChanges,
before commit. Complete original cost/share/repayment/link facts must remain identical. It never
replays the previously accepted metadata edit. The initial no-change rollback checks are not counted
as additional changed-row evidence. Preserve both completion receipts and all original keys/files.
These are original protected source/runtime checks, not rendered native UI or whole-device acceptance.
