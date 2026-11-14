# Protected context options lifetime - D1178

[D1179](protected-connection-lifetime-authority-d1179.md) separately binds explicit
captured-native access to the disposed context's original gate lease. It does not
repeat these collectibility cases or turn retention evidence into a crash repair.

## Observed defect and corrected ownership

A new source-linked, cold .NET10.0.15/X64 contract created and disposed two
original contexts without opening a connection or creating a database. The first
context and connection remained reachable; the second pair was collectible.
The contract failed explicitly, with zero opens and no password, key or financial
data. A bounded cache inspection then found one actual framework cache key
referencing the first connection. It found no owning household interceptor in
that key. The initial interceptor-root hypothesis was therefore not accepted.

The exact EF10.0.12 public source from VMR
95017c711e6afc1085133d440e42b4bd78155701 shows that the default service-provider
cache retains its first complete options key, including an externally supplied
SQLite connection. Closing a connection does not remove this options reference.
Public CreateFunction wrapper source also explains the original household
callback closure. A runtime callback probe inspected the wrong tuple field;
its zero callback count is preserved as an observer failure, not proof that the
callback graph is absent or an exported complete runtime graph.

A separate no-open counterfactual, with the same original context/callback and
an explicitly shared key-free SQLite service provider, released both first
objects while the provider remained alive. This isolates a real retention
problem; it does not establish a credential disclosure or native crash cause.

[ProtectedSqliteServices](../../../../Libraries/Vafadar.Data/ProtectedSqliteServices.cs)
now supplies the supported internal SQLite service provider from fixed
registrations, lazily and separately for each context type. No connection,
options, context or profile is passed into that shared graph. The original
protected factory and both protected migration/snapshot builders use it.
Existing per-context interceptors, compiled model, captured-source validation,
normal/maintenance ownership, supplied-connection ownership and disposal remain
in place. Models, schema, writers, financial formulas, key derivation, pooling,
runtime and product collection policy are unchanged. This is neither immediate
key zeroization nor a general claim about every disposed Android object.

## Selected host contracts and package proof

Six new permanent contracts passed: four generic protected-factory combinations
of synchronous/asynchronous and normal/maintenance creation, and two original
Zanance contexts with their closed household callback. Eight directly affected
existing factory-return/constructor/owner/publication contracts also passed.
Services and the original publisher stay alive while the disposed objects must
be collectible; no storage is opened and no database or financial row is written.
These are fourteen selected contracts, not a full-suite rerun.

Strict Windows and complete default-offline Android builds passed without
warnings or errors. The complete APK has SHA256
e02b00dc5d8b90246149d66e97c52358405294978c42abee6106ab6567653841,
99,659,679 bytes, 384 AOT modules, 408 ELF modules and 1,608 static 16KiB-aligned
LOAD segments; v2/v3 signatures, ZIP CRC/alignment, offline and nondebuggable
policy passed. Original stock Mono inputs remain unchanged. Nineteen initial
compiler-document bindings and twelve own-AOT witnesses bind the actual used
product/initial observer builds to their source. Later observer versions have
separate compiler/AOT proofs; whole-PE identity is not inferred.

The initial missing flattened native DLL failed before host contexts were
created. Its corrected actual restored native dependency, an inline correction
syntax error, Windows PDB pivot and AOT basename lookup refusals are retained.
They did not require repeating an accepted contract or successful product build.

## Native lineage, including refused observations

All native work used only a fresh API36/X64/4096 owned emulator, boot
65fe9efc-986c-4fd0-b6ca-cedb84a43ec8. The source-linked observer packages have
91 AOT/102 ELF/397 static LOAD segments and the same stock runtime10.0.15;
they are not complete MAUI shipping packages.

| Version/process | Actual result |
| --- | --- |
| V1 /4912 /UID10216 | Generic sync-normal, sync-maintenance and async-normal passed. Async-maintenance then reported InvalidOperationException. Its exact assertion/cause was not captured; the other two conditions were unissued. The three accepted cases were never replayed. |
| V2 /5293, same UID/CE/DE/boot | A new legacy control waited on its caller thread. A 30-second Join did not prove thread termination. The observer had omitted the actual AddZananceData registration before compiled-model access, including the original Issue31751 initialization precondition. This was an admission mismatch; the exact runtime wait cause was not inspected. No case was accepted. |
| V3 /5735, same UID/CE/DE/boot | Actual original registration preceded model access. After verified caller-thread termination, the new legacy direct-options control remained rooted and the three remaining fixed-provider conditions released their context/connection. All four new observations passed with zero storage opens/database files/financial actions. |

The first three successful cases and the remaining three lifecycle conditions
are qualified across this explicit versioned lineage, not all six on a final
binary. The V1 failure remains unexplained in its original observation. V3 uses
an explicit caller-exit lifecycle protocol, not a relaxed weak-reference
assertion, runtime tuning or a performance benchmark. Both necessary ordinary
no-reset SDK upgrades preserved the original UID10216/CE361724/DE66400, public
flags and boot. The source-corrected V3 upgrade cancelled only the unadmitted
read-only V2 process; no separate force-stop, data reset or marker removal was
used. Prior markers and used signed versions are retained.

V3 SHA256 is
cd26c430a0bfb2d70ec2fa312b0369c4462dbf03026d2181c08739093edbe4ce.
It has nine source-document/four own-AOT witnesses. The V2 duplicate-native-item
build refusal was corrected only in private item exclusions. Package-absence
exit-code and API36 appId parsing refusals occurred before their respective
installation/upgrade and were not counted as product failures or successes.

## Complete startup and terminal preservation

The identical complete shipping APK was installed once into the originally
absent canonical package on that same fresh emulator. One ordinary launch,
PID5853/UID10217, reached the original Onb_WelcomeTitle text, Welcome, in 55
owned native nodes. Two read-only captures followed the same process without
relaunch or input. No account/profile form, Save, authentication, camera, import,
export or financial operation was issued. This demonstrates actual first-run
startup with the changed provider; it is not a whole-private-database, UI glyph,
all migration/restore or speed acceptance.

The original five OS values, locale/key absence, boot, geometry and reverse list
were exactly equal before retirement. Both fresh packages were removed and the
two remote XML files were removed only after exact local/remote size and SHA256
matching. Local raw evidence remains. The original owned emulator/QEMU processes
ended and console closed; the original shared ADB process/creation remains.
The official AVD wrapper first stopped before Java because inherited PATH
lacked findstr, despite exit0: this was not deletion acceptance. With the
original scoped Java/system PATH restored, official deletion removed25 files/
4,684,789,307 logical bytes; six userhome files/7,732 bytes remain.

Used signed packages, original failing/counterfactual host binaries, sources,
PDB bindings and own-AOT evidence remain under
artifacts/d1178-context-options-retention. Ten standard Clean invocations
removed16,919 files/2,203,119,442 logical bytes from this step's registered build
outputs; 4,157 files/625,330,975 bytes remain in those roots. No supplementary
deletion or frozen-scope action was attempted. These are logical counts, not
physical disk-reclamation guarantees.

Phone/Main, retained C7/Q02, consumed one-shot diagnostics, accepted financial
actions, rejected performance candidates and frozen scopes were untouched.
The [original native allocation-path finding](original-allocation-collection-path-d1177.md)
still does not identify the faulting header/type, other workers, earlier writer
or repair. This retention fix does not establish that original crash cause,
whole performance, broad security/accessibility, iOS, genuine provider/Store or
release acceptance. The full delivery goal remains active.
