# Docker storage ownership review (D1205)

## Actual scope and capacity

The owner asks whether containers, images and other Docker objects were reviewed.
The preceding file cleanup was a finite build-output review, not a whole-machine
storage audit. This review inventories all objects visible through the local
`desktop-linux` engine: 21 containers (12 running), 111 image rows, 163 volumes,
34 networks and 292 build-cache records. No object is created, stopped, restarted,
removed or pruned. The broad delivery goal remains paused.

Engine-reported totals are 63.89 GB images (42.4 GB reclaimable), 1.323 MB container
writable layers (1.094 MB reclaimable), 6.67 GB volumes (6.04 GB reclaimable), and
57.13 GB build cache (0B reclaimable). All 292 cache records are marked shared with
images and not in use. Cache size is not an additional guaranteed physical-space
saving: shared content cannot be summed or treated as independently removable.
An image or volume being unreferenced does not establish that its data is waste.

Docker's configured WSL directory is
`E:\_Containers\DockerDesktop\DockerDesktopWSL`; the data VHDX is
`E:\_Containers\DockerDesktop\DockerDesktopWSL\disk\docker_data.vhdx`,
with 205881606144 logical bytes at observation. This is file length, not allocated
NTFS bytes or filesystem used-space acceptance. Docker reclamation here is not
D-volume reclamation. D has 2249707520 free bytes at the audit observation, still
below the 3 GiB host-diagnosis entry guard. No disk move, compaction, WSL shutdown,
global configuration, daemon restart or unrelated project action is performed.

## Ownership and retained requirements

Of 21 containers, 5 belong to Zanance, 14 to Loyan and 2 to Darwin. Zanance's current
retained original set is API/proxy/database running and migrate/provision exited.
All five remain intact. [D1073](retained-runtime15-adoption-d1073.md) explicitly
retains the runtime14 rollback image and complete snapshot after runtime15 API
adoption; old age and lack of an attached container do not revoke that purpose.
[D1072](service-runtime15-fresh-qualification-d1072.md) also retains source-built
runtime15 service/bootstrap images for later servicing. None is cleanup waste.

Twelve Zanance image tags are found: 5 container-referenced images, the unreferenced
runtime14 rollback, 2 prepared source images, and 4 earlier unused development images.
The latter are candidates for a separate exact-reference/retention review:

- `zanance-service:d294-household-privacy`
- `zanance-service:d294-household-privacy-2`
- `zanance-service:d294-household-final`
- `zanance/service:billing-local-d295`

Their reported unique sizes sum to about 60.17 MB, not four times their nominal
356-358MB totals. They are not deleted or declared proven redundant merely from
container-reference counts. No current image, preserved producer/toolchain,
historical recovery input or another project's release artifact is discarded.

There are 71 Zanance-named volumes, about 1.146 GB reported in total; 63 have no container
reference. Names and original records show separate identities, databases,
qualification inputs and recovery snapshots. Their contents/credentials are not
opened. Retirement requires source/retention evidence, not a blanket volume prune.
Eighty-six volumes across the engine are anonymously named; missing project
ownership prevents treating those as Zanance-owned disposable data.

Of 292 cache descriptions, only 1 explicitly mentions Zanance, 98 mention Loyan and
193 have no explicit project name. The explicit Zanance record is 328 MB and shared.
Description text alone does not prove exclusive ownership of parent layers.
No global builder/image/volume/system prune or cache-mount removal is issued.

## Evidence and boundary

Allowlisted container state/image/project/service/mount metadata, image usage,
volume names/reference counts, network names, cache sizes/share flags and the
snapshot time remain under `artifacts/d1205-docker-storage-review`. No container
environment values, secret files, private logs, database rows or volume contents
are output. A builder listing includes the NAS builder's status, but NAS images,
containers, volumes and filesystem contents are not inventoried or mutated.
The local default and desktop builder entries do not establish separate stores.

This completes the local Docker metadata inventory, not the entire machine's
storage audit or a data-retirement proof for every unreferenced object. No space
is claimed freed. The earlier refused file targets remain frozen, their manual
lists remain separate, and phone/Main/AVD/accepted financial operations are
untouched. The host's original exception remains unobserved pending sufficient
D space; its no-dump termination classification is unchanged.
