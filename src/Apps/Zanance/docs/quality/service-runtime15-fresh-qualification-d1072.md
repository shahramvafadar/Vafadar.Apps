# Service runtime15 fresh qualification - D-1072

## Delivered source changes

Five existing service/runtime/qualification Dockerfiles now pin the official
SDK10.0.404 and ASP.NET10.0.15 noble images by their immutable registry index
identities. Anonymous official-registry index/child/config responses are retained;
raw SHA256 equals each Docker-Content-Digest and referenced child/config digest.
The selected child is Linux/amd64. No floating latest tag or installed workload
is substituted. Original source authority, package pins, application algorithms,
non-root workers and service protection boundaries are unchanged.

| Official source | Index SHA256 | Linux/amd64 child SHA256 |
| --- | --- | --- |
| [SDK10.0.404-noble](https://mcr.microsoft.com/v2/dotnet/sdk/manifests/10.0.404-noble) | fe0fd4e93bd2fbb47c883b7a6369e5c8d3db8bf01cdc5981f6ee35fbdf3ff4fb | ddbaedfc8b774389d91a511af08b77296f2c7a8d15d86f6214d2a87166933625 |
| [ASP.NET10.0.15-noble](https://mcr.microsoft.com/v2/dotnet/aspnet/manifests/10.0.15-noble) | bfd912c83295ce66d4b706ebb8541b9f14259b0e76fbb88b2751f58ce2399d2e | 05acd0ca475e2130832bab441d28a7dd31041129bad28eac7928151fd73cd2c5 |

The [official SDK Dockerfile](https://github.com/dotnet/dotnet-docker/blob/main/src/sdk/10.0/noble/amd64/Dockerfile)
also identifies404 over15. Its bundled software remains inside Docker; no global
SDK/runtime/PowerShell installation is performed.

Current Directory.Build.targets unconditionally imports AndroidArchiveFreshness
and NotificationCore.Android targets. The three source-build context allowlists
previously omitted both files. Each now admits only those exact targets plus the
notification directory traversal required to reach the one file. Android-specific
execution conditions remain original. No Android dependency graph or protected
Brotli source is built or admitted. Existing deny-all Dockerfile-specific rules,
secret/key/database/bin/obj exclusions remain; a root .dockerignore is unnecessary
for these builds because their Dockerfile-specific allowlists are present.

Service, bootstrap, Qualification, HouseholdQualification, TunnelQualification and
runtime-only rebase image commands complete successfully under their strict
ContinuousIntegrationBuild inputs, without compiler warning/error diagnostics.
The service/bootstrap publication is reused through the successful cached layer
rather than republished just for another receipt. No Windows/Android product build
or old successful managed/financial suite is rerun for container-only changes.
The source-built qualification images are compilation evidence; their old identity,
household and tunnel protocol stages are not replayed or newly claimed here.

## One fresh original deployment

The exact new project zanance-runtime15-d1072 uses only fresh classified fictitious
volumes, existing reviewed immutable database/proxy software images and new
service/bootstrap images. It has no external volume, Docker socket, owner env file,
public bind, production activation or real credential. The original provisioner
creates software-managed machine identities inside its new volumes; these never
leave Docker. The original migration runs once against only this new empty schema.

Initial Compose creation exits1 because Docker's automatic address pools are fully
subnetted. Authoritative inspection shows zero containers and zero new networks;
five empty project volumes exist. Nothing is inferred to have started. Preserve
that failure and those volumes. A project-only override assigns verified nonoverlapping
172.29.107.0/24 service,172.29.108.0/24 database and172.29.109.0/24 edge networks;
no daemon address-pool setting, other network or global cleanup changes. The same
fresh project/volumes continue. This corrected start exits0: provision/migrate
exit0, API/database become healthy, proxy runs on127.0.0.1:14572.

The actual API container image is
`f3abcd2909826944608ffdf30e76ab7ef7067ace822415232e87464c55382e4d`.
Only NETCore/AspNetCore10.0.15 are listed in that image. The original API starts and
its original trusted TLS database health probe succeeds. The same image separately
runs the hash-bound framework-only probe retained from D1071 on the newly affected
Linux platform, network-none/read-only/user1654/no capabilities. It reports.NET10.0.15,
X64 and loaded CoreLib/ASP.NET assembly paths under/usr/share/dotnet/shared/10.0.15.
This directly observes the separate probe's loaded frameworks; API's own readiness
and sole available framework version are separate evidence, not exported process
memory or a newly instrumented application-version endpoint.

A single host HTTPS request to the original/health/ready validates the new proxy's
certificate using only its copied public CA, returning200. Certificate verification
is not bypassed; the hash-bound public copy is removed afterward. Product identity
count is zero before/after the bounded workload. No registration, confirmation,
login, account/profile preparation, financial request or accepted-operation replay
is made. Fresh schema migration/machine authority are technical setup, not a claim
of zero database writes or genuine provider/financial acceptance.

An actual network-none SDK-image command also reports10.0.404. Bootstrap's original
provisioning image intentionally has no explicit Config.User, so Docker defaults
to root for its existing tightly bounded volume preparation; API/callers remain
1654. This behavior is not changed by servicing.

## Runtime-only path preserves original public software

The updated ServiceRuntime.Dockerfile builds a separate unused-on-retained-service
runtime15 image from original immutable public software image
`ead1ca804ed400a18e22b81416749baeb51cc892abb4f175c8df9d47752c89e7`.
A temporary new source alias is bound to that ID and removed afterward. Two
network-none/read-only/non-root hash processes compare every31/app and/probe file:
all path/digest lines match exactly. New image
`0655647266c72d1b5fd865f70769fe5a918af139adb593e2f157e7d7e176142f`
lists both frameworks15. No private volume is mounted, no original file content or
credential is exported, and no retained container is stopped/recreated/upgraded.

This qualifies public-software preservation and runtime availability in the rebase
artifact. It does not qualify the historical application's full private state,
old-to-new service continuity, backup/restore or deployment. The independent fresh
original-host readiness above uses the separately source-built current service,
not a relabelled original-protocol run or D556 cohort. D556's exact-project caller
and runtime-servicing overlay remain unchanged and are not reused.

## Terminal cleanup and retained limitations

The exact fresh cohort retires5containers,8volumes and3networks once. Its keys,
private machine state and database are not copied out. All temporary runtime/hash/
SDK observation containers are removed after their original terminal exit0. Three
unused new qualification/household/tunnel image tags are removed by exact tag;
source service/bootstrap and runtime-only rebase images remain for subsequent
servicing work. Shared bases/build cache are not globally pruned, and no physical
space savings are inferred from image sizes/shared layers.

After successful cohort cleanup, the public image metadata reader initially
requires Config.User on the bootstrap image and fails because that optional field
is absent. The same failed reader/log is preserved. Only the remaining image
metadata and unused-tag work continues using optional-map access; no cohort,
provisioning, probe, successful cleanup or source build is repeated. All45preexisting
container IDs/names/images/running-or-exited states match their original snapshot.
The initial service transport console was truncated; its complete publication and
terminal export tail is retained, not claimed as a full original download log.
The other image build logs and all terminal receipts are complete.

The [D1070 single CI response](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/43053839608)
is completed/cancelled and
[CodeQL](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/43053839614)
is completed/success. Neither is requeried or rerun. Current-image startup/TLS/
readiness is not the full identity/financial/provider suite, public activation,
retained-data adoption or historical restore acceptance. Global Windows SDK/runtime
servicing remains separate from [project-local SDK preparation](local-serviced-sdk-d1071.md).
No Main/phone/emulator/ADB/authentication/financial/native-fault/runtime-GC action
occurs. Protected Brotli source, retained5606/Q02, all frozen scopes, canonical
parent States and terminal D607/D752/enrollment/financial checkpoints remain intact.
Original fault cause/remedy, whole-performance/security/accessibility, genuine
providers, iOS/Store/release and the full delivery goal remain open.

[Retained runtime15 adoption D1073](retained-runtime15-adoption-d1073.md) follows
the exact original Compose authority and complete drained private snapshot; only
API changes, with original mounts/proxy/other containers preserved.
