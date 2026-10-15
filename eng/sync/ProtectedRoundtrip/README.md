# Protected personal-sync HTTP qualification

Independent Windows console exercising the original source-built SQLCipher provider, protected
profile bootstrap/current generation, DPAPI key files, original stores/coordinator/transport and
the actual ASP.NET Core/PostgreSQL service. Production never references this executable.

Requires the existing explicitly owned fictional personal-sync PostgreSQL fixture and service
root, supplied through `ZANANCE_SYNC_TEST_DB` and `ZANANCE_SYNC_TEST_ROOT`. The service marker is
checked; no database is recreated, schema reset, provider downloaded or unrelated resource used.
The service uses its existing private data-protection keys and pinned local certificate. Only a
new generated `.invalid` account is confirmed internally; no real mail or owner account is used.

```powershell
dotnet build eng/sync/ProtectedRoundtrip/ProtectedRoundtrip.csproj -c Release -p:ContinuousIntegrationBuild=true
dotnet eng/sync/ProtectedRoundtrip/bin/Release/net10.0/win-x64/ProtectedRoundtrip.dll D:/_Projects/Vafadar.Apps/artifacts/protected-sync/<fresh-owned-run>
# After first-process-complete, use a distinct OS process and exactly the same files and keys:
dotnet eng/sync/ProtectedRoundtrip/bin/Release/net10.0/win-x64/ProtectedRoundtrip.dll D:/_Projects/Vafadar.Apps/artifacts/protected-sync/<same-owned-run> resume
```

The first process migrates two separate fictional empty databases through the original protected
bootstrap, checks non-plaintext active headers and removal of their initial plaintext originals,
and signs in through actual pinned HTTPS. The generated password exists only in process memory;
opaque sessions are DPAPI-wrapped locally for the explicit second-process continuation. It checks:

- Lost commit acknowledgement after the original server transaction, then the original round retry.
- A three-MiB opaque attachment, refusal of download part one after part zero, no partially applied
  ledger rows, retained part zero and retry of only missing content, with complete byte comparison.
- Competing amounts remaining pending until an explicit original conflict choice, without duplication.
- Original attachment/entry deletions propagating as tombstones.
- A second OS process reopening the same protected generations and wrapped sessions against the
  same service port/certificate, retaining the account/deleted state and performing actual requests.

The opaque bytes carry a fictional PDF filename only to exercise original attachment transport;
they are not a PDF-rendering fixture. No UI, real account, commercial feature or hosted service is
activated. DPAPI wrapping and completed file flushes are not abrupt-power-loss qualification.
Windows native HTTP evidence does not cover Android/iOS HTTP, phone hardware, remote retention,
all domain validation, the complete sync batch or release acceptance.

Failures print only a checkpoint and exception type. Preserve failed roots, keys and logs; an
observation timeout is not authority to restart/reseed. `resume` is the documented cold-read stage
after a successful first process, not a blanket replay of a failed initial scenario.

## Recorded run

`artifacts/protected-sync/run-a` completed both distinct processes. Strict compilation in
`artifacts/sync-protected-http-build-final.log` has zero warnings/errors after removing an unused
qualification-only scalar helper and documenting the internal surface; successful scenarios were
not repeated for those changes. Initial compiler-only
namespace/platform-contract failures are retained separately; no scenario ran before their fixes.
`artifacts/sync-protected-http-first.log` records four original protected HTTP checkpoints and
`first-process-complete`; `artifacts/sync-protected-http-cold.log` records the same-generation/session
reopen and `cold-process-complete`. This adds independent native qualification, not a new full-suite
test count. Original fixtures and negative evidence remain intact.

## Independent Android counterpart mode

`<fresh-owned-32-hex-run> android-host` starts one bounded original service host and an original
protected Windows client, publishes a fictional expense/three-MiB opaque attachment and writes
only public launch configuration to `host-ready.json`. It waits for explicit observation files
`advance-native-upload` and `advance-native-delete`, whose exact content must equal the public run
identity. Those files advance only the fictional scenario; no special finance/authentication
route is added to the service. Observe the existing live handle; never start another host after
an observation timeout.

The independent `ReviewNetwork=true` Android package derives synthetic credentials from that
public fixture identity solely for its generated `.invalid` account. They are not owner secrets,
production authentication policy or an extra remembered user password. Actual session tokens use
the original platform SecureStorage adapter; original service registration, confirmation, sign-in,
authorization and sync endpoints remain authoritative. No credential is passed through adb or
printed. Only public port/certificate/content hashes enter launch arguments.

After the first native terminal success, stop only that independent process, then write the
upload observation file. The host receives its actual amount edit and publishes a competing
amount against the native retained unsent edit. After `host-conflict-ready`, launch the same native
fixture with `network_resume=true`. It must reuse its protected generation and SecureStorage
session, explicitly resolve the actual money conflict and publish original deletions. Only after
observing native cold success write the deletion observation file. The original Windows client
then verifies those tombstones and the host exits. Failed/interrupted originals remain intact;
these instructions do not authorize reseeding or skipping a failed native stage.

### Recorded Android roundtrip

Run `094bb950f44a4024945b21afc395b010` completed against the original retained service fixture.
`sync-native-network-host-build.log` and final `sync-native-network-first-run-build.log` are strict,
zero-warning/error builds. The independent signed Release/AOT/trimmed Android package was verified
before installation, including its actual permanent package and INTERNET permission. The final APK
is retained under `artifacts/android-sync-network/094bb950f44a4024945b21afc395b010/first-run-reception/`,
SHA-256 `1da663d7b2db73e9210173515fa607606a4f75d29e4c69d83ad1354d1e50e9d6`.

Early evidence remains: the first built package omitted INTERNET because this independent caller
still unconditionally included the offline removal overlay; manifest inspection caught this before
launch. The caller now selects that overlay only outside network mode, matching production's
already-correct edition condition. The first native run then stopped in its ownership check;
Android's `/data/user/0` and `/data/data` aliases were compared inconsistently by the caller. Both
paths now canonicalize, and the retained report confirms PathAliasNormalized=true. Neither issue
changed production financial code or authorized a replacement fixture.

The next run reached the original protocol and retained a refused Settings-only initial proposal.
The caller had omitted the original onboarding receiveFirstRun flag. Its explicit continuation
requires an empty financial destination and exactly that uncommitted Settings review; the original
coordinator abandons only that refused round, retains the same installation/keys/session and the
original store connects in receive-only first-run mode. No pending financial work is discarded or
conflict amount guessed. `sync-native-network-first-run-result.json` confirms the exact Settings
review and continuation, successful native receive, complete attachment bytes, publication of an
original amount edit and a later unsent edit. The original UI already passes this first-run flag;
this was qualification-caller alignment, not a claimed product fix.

After terminal first-stage success, only the independent process was stopped. The Windows client
received the native edit and published a competing amount. A distinct native OS process reused the
same protected generation and original platform SecureStorage session, without sign-in, retained
its unsent amount until explicit original review, selected the exact remote counterpart and then
published original attachment/entry deletions. `sync-native-network-cold-result.json` confirms all
those cold-stage checks. `sync-native-network-host.log` ends with the Windows client receiving
those tombstones and `native-counterpart-complete`. No observation timeout caused a restart;
failed evidence/files/keys remain. The package process and exact labeled PostgreSQL fixture were
stopped after completion. No page-only reviews or successful Windows-only scenarios were repeated.
