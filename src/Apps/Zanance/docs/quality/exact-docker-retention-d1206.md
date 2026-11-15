# Exact Docker recovery dependencies and bounded retirement (D1206)

## Scope and new evidence

The owner requests exact review of the four earlier image candidates and
unattached Zanance volumes, preserving required data/evidence and other projects.
[D1205](docker-storage-ownership-review-d1205.md) was an inventory, not proof that
these objects were disposable. The owner-linked other-project cleanup is read
for context; current D free space is independently observed above 49 GiB. No action
is sent to that chat and no Loyan/Darwin/NAS resource is modified.

All 11 owned complete snapshot manifests are read inside auto-removed, no-network,
read-only helpers using the existing immutable bootstrap image. Metadata is
bounded to 64 KiB, requires Schema 1/quiesced/source ownership and four literal
SHA256 image roles. Only snapshot/source-project/image dependencies and metadata
hashes leave the helper; no archive, database, private key or credential is read
or exported. The 16 distinct recovery image dependencies include **all four**
earlier candidates:

- `zanance/service:billing-local-d295`
- `zanance-service:d294-household-final`
- `zanance-service:d294-household-privacy-2`
- `zanance-service:d294-household-privacy`

These images are kept. Zero current containers is not evidence of redundancy;
removing them would discard available historical recovery dependencies. No
missing historical image is pulled/rebuilt and no restore/workload is replayed.

## Complete volume disposition

Every one of the 71 Zanance volume names receives an explicit disposition:
8 are attached to the retained original services, 11 are complete recovery snapshots,
35 are unattached original stopped snapshot-source volumes,8belong to the unfinished
tunnel/origin project, and 9 contain standalone qualification/source state whose
independent terminal-evidence retirement has not been proved. Content is not
opened merely to force a deletion decision. Original stopped-source ownership
and latest private-state guards in [the operations guide](../../../../../eng/hosting/README.md)
remain mandatory; retaining an archive alone does not waive them.

The tunnel database candidate reports 0B but fails a literal empty-directory test.
Only entry types are then inspected: exactly two directories, no regular files,
links, sockets or other objects. Immediately before retirement the complete
entry-type walk is checked again with successful find status/no stderr, the
ownership labels match, container references are absent twice, and no retained
snapshot uses that source project. Exactly this volume is removed without force:

`zanance-selfhost-tunnel-d313_database-data`

It contained only empty directory structure, so recovered file-data bytes are 0.
The initial nonempty result is retained rather than relabelled empty. The other
70 Zanance volumes remain. Their current lack of a consumer does not prove the
original identity/data/recovery material can be discarded.

## Preservation and scope limits

All 21 original container IDs/names/images/states remain exact, all 111 original image
IDs/tags remain, and every other original volume remains. An additional non-Zanance
image appears between inventories; the initial whole-image-equality assertion
is explicitly refused, that external addition is recorded without actor
attribution, and no deletion/pull is issued by this task. Temporary metadata
helpers are removed automatically. No global prune, service restart, fixture
preparation, key/financial/authentication operation or phone/Main input occurs.
The previous filesystem-policy refusals are not retried or worked around.

Actual metadata, the single removal receipt, complete volume dispositions and
preservation checks remain under `artifacts/d1206-exact-docker-retirement`.
This retires one proved directory-only resource, not all historical databases
or the whole project. No significant free-space saving or recovery acceptance
is claimed. The broad delivery goal remains paused; the separately requested
host diagnosis continues without replaying an app test or consumed classifier.
