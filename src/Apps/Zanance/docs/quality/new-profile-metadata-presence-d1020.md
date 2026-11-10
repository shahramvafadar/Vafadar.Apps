# Existing profile metadata cannot become fresh key authority — D-1020

Two actual SqlCipherNewProfile failures are corrected at the metadata-presence
decision, separate from the [D1016 candidate-namespace guard](new-profile-key-namespace-refusal.md).
The earlier guard and successful platform cohorts are not replayed. No financial
model, migration, encryption format, protector, KDF, integrity, pooling, runtime
or GC policy changes.

## Original failures before any real key

File.Exists returned false for a directory at the final generation-journal path.
The method therefore entered new-profile initialization rather than refusing the
existing invalid metadata object. Two new host cases exercise that actual branch:

| Existing final-journal directory | Original fake Prepare calls | Newly written operational metadata | Durability calls |
| --- | ---: | ---: | ---: |
| Fresh explicit initialization, no prior intent |1|2files: intent and restore ancestry|4|
| Retained Original intent and pending ancestry |1|0|0|

The counting fake key preparer intentionally throws at its first call, before any
actual key, schema or financial database can be created. Both baseline assertions
fail with that deliberate InvalidOperationException; selected total2/errors0/
failed2/skipped0/not-run0. The fresh case really writes the two operational files;
this is not a no-write claim. There is no real key replacement or later publication
effect in the baseline, and those original failures remain separate from acceptance.

## Presence predicate and selected host checks

Use File.GetAttributes before decisions for the final journal, unpublished intent
and restore history. Only FileNotFoundException/DirectoryNotFoundException prove
absence. Directories/reparse entries refuse; other unavailable metadata propagates.
An existing ordinary file continues through the original content/identity readers;
this predicate is not format or cryptographic authentication.

The two new failing-directory cases now refuse with IOException before metadata
creation, synchronization or key preparation. Two additional intent/history
directory controls retain their refusal, and one genuinely absent final path with
retained Original intent still reaches the intentionally stopping fake key once.
All five preserve complete captured metadata bytes/directory names, the empty
invalid directory and unpublished endpoint. There is no key read or schema build.
Three directly affected older fresh-profile refusal cases also pass. These are
5new+3affected selected tests on the final strict host build, not eight new tests
or a full suite. The actual host runtime is.NET10.0.12; it is not relabelled14.

## Original Android method and compiled source

Fresh API36/x64/4096, UID10216/PID4905/.NET10.0.14runs the same five named cases once
through the compiled original Data method and real operational metadata journals.
Four invalid-directory cases refuse before fake prepare/read or synchronization;
the absent control reaches only its fake boundary once. All metadata bytes,
directory names, target emptiness and unpublished endpoint remain exact. Each
individual fixture retires after its terminal result. One consumed native request
is used, with no real Keystore, financial database/schema, account/plan/entry/ledger,
authentication, camera or prior accepted workflow.

Strict Windows, complete offline shipping and the private native build pass without
warnings/errors. Native package is12583331bytes, SHA256
`554fb38b82395a83777ae72e4820e61fca00d783581fb972ee84bcb68aca55c0`,
74AOT/85ELF/329static16KiBLOAD/offline/nondebuggable/v2/v3/ZIPCRC/alignment pass.
Shipping is99614623bytes, SHA256
`6e8f5b61b8c43f7599d7cf4a389f84f922f343b4250198d93876a9341fd71f16`,
384AOT/408ELF/1608staticLOAD/offline/v2/v3/ZIPCRC/alignment pass. Both original Mono
slices remain exact. Actual private and shipping compiler documents bind corrected
SqlCipherNewProfile source SHA256
`C4A705441C37A74FED9439FE4C970E51484019E4C09CCB67B636F0E0CAADF83F`.
This is finite source binding, not whole-PE, all path/permission types, ARM64,
minimum-API or hardware16KiB acceptance of this later correction.

## Exact retirement and continuing delivery

Only the fresh package is removed. Original captured OS values, locale/key presence,
boot, geometry and reverse mapping remain exact without settings writes. One owned
console stop leaves all four original host handles signalled and the same
ADB5038/PID65060creation identity alive, without reset/start/reconnect/pair. Official
retirement removes24AVDfiles/4139726117logical bytes. Retained5606/Q02 and terminal
fault classification are not touched.

Five standard own-output Cleans retire5583registered files/1061475771logical bytes;
2861registered files, six userhome files7732bytes and25host-tmp files3941947bytes
remain. Logged targets stay inside current own output roots. Used signed/source/
compiler/original-failure/result evidence remains; no supplemental/frozen cleanup,
alternate path or unrelated successful test is repeated.

Shipping is prepared, not installed on Main here. Main20875/ac38a0ea is only the
dated D1017observation, not fresh liveness or acceptance of this later change.
Wider key lifecycle/recovery/private-source/security, original native-fault remedy,
whole performance/accessibility/platform/provider/Store/release requirements and
canonical parent States remain open. The complete delivery goal stays active.

The one D1018response reports [CI](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/42814487388)
completed/cancelled and [CodeQL](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/42814487397)
completed/success. Its observation is later than the requested15–18minute window;
no timely claim, second GET, job-log read, rerun or cancellation cause is inferred.

Later [D1021 ARM64 qualification and Main adoption](metadata-presence-arm64-main-d1021.md)
complete five new physical conditions and one reused-package no-reset upgrade.
The prepared/not-installed statement above remains the original D1020 cohort.

Later [D1022 minimum Android and ps16k qualification](metadata-presence-minimum-and-ps16k-d1022.md)
adds only five new metadata-presence conditions per platform using the original package.
