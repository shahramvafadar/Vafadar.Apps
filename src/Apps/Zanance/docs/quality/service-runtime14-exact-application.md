# D-556 - Exact-application service runtime14 servicing

## Scope and original authority

D-554 updates packaged app runtimes, but the retained local API still runs .NET/ASP.NET10.0.13.
Pin official MCR ASP.NET10.0.14-noble and SDK10.0.403-noble by immutable manifest digests in the
four existing service/qualification Dockerfiles. Raw registry manifest SHA256 matches the
Docker-Content-Digest and selected Linux/amd64 child. Existing notices/licences still apply.

[Official release](https://github.com/dotnet/core/blob/main/release-notes/10.0/10.0.14/10.0.14.md),
[ASP.NET image](https://github.com/dotnet/dotnet-docker/blob/main/README.aspnet.md),
[SDK image](https://github.com/dotnet/dotnet-docker/blob/main/README.sdk.md).

The retained API image1c0f26086501 was built16October; canonical sources contain later changes.
A fresh source build is compilation evidence, not authority to deploy its features/schema.
ServiceRuntime.Dockerfile copies only original published /app and /probe onto the pinned14 base.
Every software file hash matches exactly; actual dotnet --list-runtimes changes both frameworks
13 to14. No private volume, owner credential or environment enters an image. Keep UID1654 and
original read-only/capability/namespace restrictions. Actual rebase image:
ead1ca804ed400a18e22b81416749baeb51cc892abb4f175c8df9d47752c89e7.

## One fresh original protocol cohort

Use only new project zanance-runtime14-qualification-d556, loopback14556/private172.29.96.0/24.
Original immutable API13/bootstrap/database/proxy images start on fresh project-owned volumes;
no external volume, public bind, Docker socket or owner environment file. The original independent
caller requires this exact project for new runtime stages; older stages retain their allowlist.
Never impersonate the hosted CI runner or run its destructive fresh setup on retained resources.

Runtime-before registers one fictitious .invalid identity through the original production HTTPS
client, leaving confirmation unfinished. Preserve original identity/challenge/encrypted-mail digest.
Recreate only this new API onto14. Runtime-after requires those exact values, unwraps the original
13 outbox with software machine authority inside this exact fixture, confirms once through the
original HTTPS client and verifies persistence. Request/output markers reject duplicate success
and preserve partial requests instead of posting again. No owner credential or financial suite.
These two stages are not new managed cases or zero database writes.

Both actual server versions are observed; both protocol processes exit0 with explicit pass messages.
Retain requests, frozen assertion source, runtime observations, full logs and terminal receipts.
Volume-local assertion JSON is removed with the completed fixture, not exported: do not claim a
separately retained host copy of that report. Retire only this project's containers/networks/volumes
and hash-matched public root copy. Software private keys never leave those volumes.

## Retained local adoption and rollback

Retained project zanance-selfhost-privacy-restore-d294 stays loopback-only14645, household
qualification enabled, private172.29.87.0/24. Read only allowlisted public configuration. Reproduce
its original API Compose hash exactly before mutation; never dump credential environments.
The existing backup operation drains API/proxy and streams a complete private snapshot inside
Docker. Fresh zanance-hosting-snapshot-d556-runtime13-before14 passes every archive integrity check.

Only then recreate API with --no-deps/--no-build onto the exact-app14 image and restart the same
proxy. No migration/provision, newer schema, public route or unrelated project changes. All five
original mount sets remain exact; four other container IDs/images/states remain unchanged. The
new API reports both frameworks14/healthy; original verified local-trust proxy returns HTTPS200.
Hash-check/remove only the public certificate copy. Retain original immutable API, private snapshot,
privacy-deletion authority and live14 image. This is runtime adoption/health evidence, not complete
private database/key equality, historical restore, real mail/provider or public readiness.

## Preserved refusals and cleanup

First rebase completes with InvalidDefaultArgInFrom warning and is unadmitted, never deployed.
A valid fail-closed scratch default fixes that warning; repeat only rebase, preserving successful
source/qualification builds. Byte-edit/newline/source-freeze and post-receipt console-encoding
refusals remain local evidence.

Compose --wait initially exits1 because preparation successfully exits0. Observe the same six
containers: API/database healthy, proxy running, three one-shot jobs exited0. Retain failed wait
and separate readiness proof; never replay startup/registration or reset its volumes.

Remove two unused new source/qualification images. Earlier warning image is already absent;
its refused lookup is retained without another deletion claim. No global prune/cache/free-space
claim. Live14 and original rollback/snapshot remain. Strict source/qualification builds and final
rebase have no warnings/errors; unchanged app builds/financial suites/native cohorts do not repeat.
General security/stability/ANR/crash/performance/spoken-A11Y/iOS/provider/public/Store/release and
D555 phone foreground acceptance remain separate.
