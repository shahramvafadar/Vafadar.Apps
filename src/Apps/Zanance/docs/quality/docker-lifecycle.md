# Local Docker lifecycle consolidation (D-502)

## Scope and owner authority

On 2026-10-23 the owner requested removal of unnecessary resources, ongoing cleanup and one
Docker Desktop group for this project. Only Zanance resources were changed. Public hosting,
credentials, machine settings, mobile qualification and other projects were outside this operation.

## Actual cleanup

The initial engine inventory contained 89 containers, including 63 named Zanance containers.
Completed qualification callers and superseded deployment/restore containers were retired after
checking ownership, mounted-volume identities and the retained snapshot manifests.

| Resource | Removed | Retained boundary |
| --- | ---: | --- |
| Zanance containers | 58 | Five original containers in one Compose group |
| Image IDs | 9 | Active dependencies and all locally available snapshot-required images |
| Empty owned networks | 16 | Three original networks of the retained stack |
| Volumes | 0 | All 192 engine volumes, including nine private snapshots |

Nine snapshot manifests identified 16 distinct helper/runtime image dependencies. Nine are locally
available and retained; seven historical dependencies were already absent. The removed image-ID
set does not intersect these 16 dependencies. A retained archive is therefore not proof that every
historical snapshot is immediately restorable; exact missing images require separate reconstruction
and qualification before any such restore.
Private snapshot archives, database contents, key material and container environments were not
printed or exported. No forced image deletion, global prune, volume removal or image pull occurred.
The obsolete native-host image tag was removed; its current reviewed snapshot replacement remains.
Bounded metadata/deletion receipts are private local operator evidence, not uploaded artifacts.

After cleanup, engine-reported image size changed from 31.82GB to 26.61GB. This is logical Docker
image accounting, not a claim that the Windows virtual disk shrank by the same amount. The shared
323.4GB build cache was not safely attributable exclusively to Zanance and was left untouched.
The IDs and running/stopped states of all 26 unrelated containers were compared unchanged;
the complete engine volume-name inventory was also unchanged.

## One retained group and actual runtime verification

`zanance-selfhost-privacy-restore-d294` is the only remaining Zanance Compose group. Its historical
name preserves original identity, volume ownership and recovery metadata. It contains:

- `database`, `api`, `proxy`: original long-running service containers;
- `provision`, `migrate`: original successful one-shots, remaining exited with code zero.

`python eng/hosting/desktop.py stop` drained API/proxy and stopped their database. The corresponding
`start` resumed the same container IDs and waited for database/API health before dependent startup.
Both health checks returned healthy; proxy returned running. The original loopback endpoint
`https://localhost:14645/health/ready` returned HTTP200 with its existing public root certificate,
normal certificate validation and the documented offline-CRL handling. No insecure TLS flag was used.
The base Compose configuration also parsed successfully; that static check is separate from the
actual retained configuration and runtime verification.

No container recreation, migration/provision replay, financial command, application build or test
suite rerun was needed. App/APK and unit-test counts remain unchanged. This does not establish
public reachability, real mail/provider acceptance, another restore or product release readiness.

## Continued lifecycle policy

Use the group controls in Docker Desktop or the ownership-checked `desktop.py status/start/stop`
commands. Do not accidentally create a fresh default stack with `compose up`. Finish and remove
temporary callers instead of accumulating completed containers; prefer `--rm` where continuation
is not needed. Preserve failed/unfinished fixture state until its remaining work has a terminal
result, then retain necessary bounded evidence and retire its runtime resources.

Recovery volumes and their image dependencies are deliberately retained. Old stopped source
containers were retired, so historical restoration still requires reconstructing their exact
source ownership before the existing privacy-source guard will permit it. No guard was weakened
and old snapshot restoration was not rerun. Never remove a private volume, snapshot or its image
dependency as a routine retry, and never prune unrelated/shared engine resources globally.
