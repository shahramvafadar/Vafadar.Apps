# Original UI on an isolated experimental CoreCLR package (D-576)

## Scope and runtime boundary

On 2026-10-27, a new Samsung-only package, `pro.vafadar.zanance.coreclrruntimereview`,
UID10399, exercised the original application through its ordinary launcher and original
forms. This is a finite feasibility experiment, not production adoption, a diagnosis of
the previous Mono/SGen failure, or general stability/performance acceptance.

The official .NET10 MAUI documentation labels Android CoreCLR experimental:
[MAUI10 runtime documentation](https://learn.microsoft.com/en-us/dotnet/maui/whats-new/dotnet-10?view=net-maui-10.0#experimental-coreclr).
The previously reviewed public runtime report
[123537](https://github.com/dotnet/runtime/issues/123537) does not establish that this
application has the same cause, or that an installed servicing release fixes its crash.
No old rejected bridge, nursery, serial-GC or allocation candidate was replayed.

The independent project's explicit `ReviewCoreClrRuntime=true` flavor selects its own
ARM64 identity, CoreCLR and the SDK's research-only `EnablePreviewFeatures` opt-in.
The original XA1040 refusal is retained; no `NoWarn` or warning-gate bypass was used.
An initially mismatched Crossgen2 package was rejected before installation. Both the
actual runtime and Crossgen2 download identities were then pinned to10.0.14, with official
NuGet signatures and MIT metadata checked. SDK10.0.401/Android36.1.69 remained installed;
no global workload, environment, clock, permission or security policy was changed.

The final independent APK has SHA256
`c16e088d27c6c8f07fecea3a064caa1d41be804d50b54f10b5ee41734569cedb`,
86,791,735 bytes,13 ELF files and39 statically16KiB-aligned load segments, valid ZIP/v2/v3,
and no INTERNET permission or debug/profile flag. It contains actual CoreCLR/JIT and ReadyToRun
composition, not Mono AOT libraries. ReadyToRun is not counted as192/384 Mono AOT outputs.
It ran on API36/ARM64/4096-byte pages; this is not physical16KiB acceptance.

## Source-only observation

The additional provider exists only in that independent compile flavor. The shipping
manifest contains neither this provider nor its authority. Read/write admission requires
the original DUMP permission; the implementation additionally binds the actual new
package/UID, shell UID2000, Samsung model/API/runtime, a32-hex run and fixed checkpoint names.
It admits no arbitrary query, file, SQL or financial operation. `OnCreate` performs no
storage work. Original covered startup, an owned focused activity, unlocked access and
the sole fresh Main profile are required before source services are resolved.

An empty-account/empty-ledger seal is written once before the first financial command.
Subsequent observations are bounded to two fictitious EUR accounts and four titled
entries. The original protected factory and publication validation are used. The original
complete fingerprint includes schema SQL, SQLite headers and every row/column, explicit
storage classes, raw text/blob bytes and exact real bits. It projects away no revision,
cache, migration or financial bookkeeping. Keys, raw private Main storage, heap, registers,
exception messages and trace files are not exported.

Six new AT-576 policy cases pass in one focused .NET10.0.14 execution, including four
distinct readiness refusals whose source callbacks remain uncalled. No old successful
managed suite was rerun. These six cases are separate from the native observations below.

## Original accepted operations, once each

The ordinary original onboarding created `D576_CoreCLR_Source` with100EUR opening balance.
The original account form later created `D576_CoreCLR_Dest` with zero opening balance.
Neither opening created a ledger entry. Each subsequent command has a durable before
marker and a source/result receipt; a missing observation never caused a Save replay.

| Original command | Amount | Native total afterward | Ledger rows |
| --- | ---: | ---: | ---: |
| Expense, Food, `D576_CoreCLR_Expense10` |10EUR |90EUR |1 |
| Income, Salary, `D576_CoreCLR_Income7` |7EUR |97EUR |2 |
| Internal transfer, `D576_CoreCLR_Transfer20` |20EUR |97EUR |3 |
| Refund linked to the original expense, `D576_CoreCLR_Refund4` |4EUR |101EUR |4 |

Every date is the actual2026-10-27. Previously observed account and entry identity,
amount, date, account/category and link fields remained exact after the next command.
This is not an unrecorded per-old-row32-column comparison across those financial writes;
the complete final four-row source is the baseline preserved through later reads. The original expense, refund link/category/account and one-row transfer
were checked separately. The original same-currency transfer deliberately stores null
`ToAmount`; the original effect uses `ToAmount ?? Amount`. A mistaken observer expectation
of a redundant2000 value was retained and corrected without changing that stored null.
The native account balances are81/20EUR; the original transaction day net is+1EUR, with
the transfer excluded and refund reducing spending. No old source, seed, key, import,
Undo, accepted financial command or successful native preparation was replayed.

## Refusals and the actual product defect

The first source observer allowed a managed readiness exception to cross its JNI query
boundary, ending process27961 with Android's APP CRASH(EXCEPTION). Content-free type/method
evidence identifies the observer's own readiness refusal, not a native SGen crash.
The observer now returns a bounded refusal instead; a modal/unfocused request performed
no source read and left process30348 alive. An edit modal remains ineligible for source
queries. Saves use the last verified original root-page checkpoint with no intervening
financial command, then reconcile the actual accepted result after returning to that root.
The already accepted opening was recovered through normal launch, never created twice.

Other retained refusals include the wrong pre-install AppContext switch name, a Windows
APT2264 intermediate-path limit resolved only by a shorter owned output root, an automatic
keyboard covering the intended input, and an unsaved refund text10.004 rejected before
Save and replaced through actual native Select All. No cause was assigned to the runtime
from that input failure. Initial covered Home loading was retained and observed to finish
in the same new process, without restart or rearming.

The retained Accounts page actually displayed97/0/97 after transfer/refund while the
original ledger and Home held81/20/101. That original application defect is corrected in
[D-577](retained-account-overview-navigation.md), using these already accepted rows.
Its initial failure remains evidence. Category chips also exposed only their names,
without their selected state; that bounded accessibility issue remains separate and open.

## Six retained contexts and real process-death recovery

After the Accounts correction, one original retained Accounts instance in process6333
passed English/Persian/German and Light/Dark, using real application settings and actual
native tab callbacks. All six native amount/caption observations and complete protected
source fingerprints matched. These are six native contexts, not new unit tests or full
spoken/scaled accessibility acceptance. Language and theme returned to English/Light.

The process initially had `cached=false`; no kill was issued and the predicate was not
weakened. Independent ready Mono product delivery continued. Only after actual
`cached=true` and all raw/current OOM adjustments900 was `am kill` issued once for this
new package. PID6333 absence was proved by exit1 with empty stdout/stderr. Ordinary launch
created12938. Its initially covered loading frame was not mistaken for readiness; the same
process then displayed101EUR and read the exact complete source fingerprint, two accounts
and four original entries. No selector, run, prepare, seed, financial replay or force-stop
was involved. This is controlled cached-process recovery, not natural low-memory,
punctuality, reboot, a cold benchmark or a general ANR/crash fix.

## Retirement and remaining gates

Only the newly installed UID10399 package was uninstalled, without KEEP_DATA, after its
source acceptance. Its package/live PID, active AlarmManager and posted notification
records were absent. Original Main and the retained b1d5 caller remained separate.
The complete Mono product was independently adopted in D-577; CoreCLR was not shipped.
The original timeout300000, font1.0, stay-on15 and absent/inactive reader settings were
restored and verified. The timed D567 emulator, ADB server, other projects and shared
caches were untouched.

104 newly owned hash-matched remote XML files/4,971,530 logical bytes were removed while
all local evidence was retained. Twelve standard cleans removed13,059 registered files/
4,023,652,380 logical bytes. Validated finished output and unused-package roots then
removed13,054 remaining files/5,283,122,241 logical bytes. These are logical file counts/
bytes, not a host free-space measurement. All five used APKs and the proofs remain.
Previously refused cleanup scopes were not retried.

General Mono/SGen/ANR/crash, memory/latency, the two-second target, complete accessibility,
other Android platforms, iOS, provider/account, Store and publication acceptance remain open.
