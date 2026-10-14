# D-272 local identity backend delivery

The approved local ASP.NET Core/PostgreSQL/private-storage foundation is implemented independently
of MAUI and financial SQLite. Identity 10.0.13 and Npgsql EF 10.0.3 restore from NuGet; audit reports
no known vulnerable direct/transitive packages from current configured sources. No credentials,
client registration, hosted resources, mail sender or commercial enforcement were introduced.

Four end-to-end acceptance scenarios use the actual HTTP host and PostgreSQL, covering:

- confirmation required, wrong/consumed/expired challenges, password recovery and session revocation;
- concurrent refresh yields exactly one grant; prior-generation access/refresh is refused;
- cross-user object/session denial, exact recovered bytes, tampered ciphertext rejected;
- durable request limits survive a second service host; runtime role cannot create tables;
- durable local outbox lease replay and idempotent protected delivery;
- concurrent final-capacity uploads yield one creation/one rejection; physical deletion resumes;
- cold missing-key startup refuses replacement; original keys restore the original grant;
- real quiesced pg_dump/pg_restore into a distinct DB/root preserves objects and revoked sessions.

Final independent security-patched runtime run: 4 passed, 0 errors/failures/skips/not-run,
6.355 seconds on .NET/ASP.NET 10.0.13. XML: artifacts/zanance-backend-tests-runtime13/results.xml.
The earlier framework-dependent Release run passed four scenarios on the installed shared runtime.
The final published server and tests carry their own 10.0.13 runtime; no global runtime was changed.
Actual Kestrel coreclr module version and runtimeconfig included frameworks were inspected.
Fictional restore measured about 0.7 seconds, committed snapshot loss zero for this quiesced fixture.
This is not a production-size RTO/RPO, daily scheduled backup or physical-host-loss acceptance.

Actual Windows Kestrel readiness returned 200; the hidden worker drained on its owned request marker.
The operator's backup/provision/restore/start flow succeeded in a distinct independently named
PostgreSQL container/volume and private root; recovered Kestrel readiness returned 200. Operator
fixture was empty identity schema, while the HTTP acceptance restore contains real fictional users,
sessions and an encrypted object. Do not merge those verification scopes.

Observed failures retained: changing an administrator-group directory owner required privilege;
the final DACL restricts the current user without owner reassignment. First operator restore failed
on a source runtime-role grant; final restore omits source ACL/ownership and initializes destination
privileges separately. A final test build encountered the still-running owned server DLL lock;
the exact worker was gracefully stopped and affected backend tests/build resumed. No mobile test,
app screen, upstream cipher corpus or APK was replayed.

Main app and complete signed APK remain D-268. The service has no mobile sign-in screen, financial
sync/sharing/billing routes or hosted deployment. External sender, public certificate/key provisioning,
provider/legal approval, hosted operations/availability and real user/device acceptance remain gates.
The full development goal remains open; country/currency/holiday work follows this complete batch.

The final maintenance batches have stable ordering. The operator verifies both binary and private
root for stop ownership and waits for its own PID plus readiness before reporting Start success.
The published server is delivered under artifacts/zanance-identity/runtime/win-x64; all owned
review workers are stopped after acceptance. Hosted Linux certificate/CI results remain unverified.

PowerShell 5.1 parsing passed. Its direct script execution was blocked by the existing system
execution policy; that policy was not changed or bypassed. Actual operator execution used the
available PowerShell 7 host. This is not a claim of PowerShell 5.1 runtime acceptance.
