# Own CSV protected-field budget (D-716)

## Original failure

Seven actual own-export/parse/preview cases fail before the change. A complete65,536-character
note beginning with `=`, whitespace plus `+`, tab, carriage return, line feed, apostrophe or `@`
receives one reversible format-three marker on export. The parser counts that marker as source
text and refuses the resulting65,537-character cell before the original own preview can decode it.
This is a separate input boundary from D-715's accountant packet writer.

## Admission and unchanged budgets

Retain at most one provisional character beyond the field budget, only for an apostrophe-leading
cell. Before publishing the complete row, require all of the following:

- A recognized original entry or holdings header and one unambiguous version column.
- This actual row declares version3, including when the column precedes appended metadata.
- This is an original text column decoded by that own-file reader. Typed and unknown columns have
  no allowance; holdings use only the first named text column, matching their decoder.
- The existing `UnprotectLiteralText` predicate removes exactly that one marker, leaving at most
  the original field budget. An original apostrophe is preserved by its existing doubled encoding.

Generic files, mere `format_version` labels, old/unknown versions, ambiguous headers and oversized
original values remain refused. Headers gain no allowance. All byte/total-character/row/column/cell
budgets remain unchanged and include transport. Only an extended row needs the additional field
validation loop; ordinary rows do not acquire a second complete cell scan. No stripped string is
allocated merely to validate a marker. No output format, financial amount, identity, source policy,
schema, migration, localization or startup work changes.

## Preserved implementation correction and focused results

An early draft checked the unique version label without binding the original format/text columns.
Its initial mapped-file test incorrectly expected a generic preview to remove the marker. The actual
generic importer intentionally keeps human text literal. That failure and source assumption are
preserved; the draft was neither published nor installed. Final admission explicitly matches the
original two readers, and the corrected generic-spoof case now requires refusal.

The initial113-case run has112 successes and that one wrong expectation. After narrowing the header/
column rule, all27 new cases pass. The95 affected existing literal-text, quoting, input-budget and
ordinary CSV cases are then run on final source and pass; their repetition is justified by that
changed header path. Final distinct scope:122 selected cases, not the whole test suite or a new
native financial suite. Host tests use the installed .NET10 host; native runtime proof is separate.

## Actual Android Core execution

One fresh API36/x64/4096 component APK links the actual product Core and Vafadar.Core projects,
with no Data project or SQLite library. One process/run-bound start marker admits only one task.
Actual UID10216/PID4900 completes eight original round-trips: the seven entry-note prefixes plus
one holdings-type note. Actual own exporters, bounded readers and original previews preserve their
identities, dates, amounts and complete note text. Two further inputs are refused: a generic version
label trying to borrow the marker allowance and an oversized original own note.

The task finishes with8round-trips/2refusals and an independently reproduced original-text digest:
`5049babec12495434211ca3936f039403e3ef2bc793815ad275ef426d7ddae86`.
The263ms observation only establishes completion of this bounded task, not Home performance or a
release latency target. Original source calls, dependency absence, actual counters and digest support
the scope; fixed status labels alone are not acceptance. No financial database, fixture ledger Save,
Import command, file picker, native share, authentication or old successful cohort is executed.

## Builds, package identities and retirement

Strict Windows and complete offline signed Android builds have zero errors/warnings. Shipping:
`d2ee4ab5ac57c830bcfff095ec5e39852dd7269aa1a5882f0456af223485c25a`,99,348,383bytes;
384AOT/408ELF/1608static16KiB LOAD checks, CRC/ZIP alignment and v2/v3 signatures pass.
The independent authority is absent from shipping. The used component is
`b977059d0d2a22e93b9fce3f6074056c552487c7079bbec27a0a69cb8d8832c1`,19,238,934bytes;
94AOT/104ELF/406staticLOAD checks pass. Every AOT log entry matches a packaged AOT library;
required actual Core assemblies are present and Data/SQLite are absent. The installed APK digest
is exact and packaged SGen remains the reviewed `edfa02c346f7eafc6cab665b27c667a93ecc84b7f40d042110ca4e343b2de8e4`.
No identity of every linked BCL across different applications is inferred.

Original boot22aa6873-958d-434b-b9ae-137a7343ce79,1080x1920/density420, locale, API/page size and
all five captured settings remain exact. Only the fresh package/UID and two owned hash-matched
XMLs/3,404bytes are removed. Host80080 and console5572 close; official AVD removal retires25files/
5,118,505,979logical bytes. Four standard Cleans pass with3,308 registered deletion lines. Those
counts are not distinct-file or measured host free-space claims.

A single supplementary seven-target removal is rejected before process creation with
`blocked by policy`:485files/157,305,820logical bytes remain frozen in this batch's private source,
focused outputs, residual owned-AVD root and four unused publication files. No retry/subset/alternate/
move/escalation/reuse is allowed. Used signed packages, named source archives and bounded evidence
were retained outside that scope before the attempt. Earlier refused scopes remain untouched.

## CI and remaining gates

D-715's only cached response records [CI in progress](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/41258059748),
[local identity backend success](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/41258059782),
[self-hosted service success](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/41258059749) and
[CodeQL success](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/41258059746).
Its consumed schedule is deleted with no second GET or later-result inference. D-716 receives its
own exact final-head one-shot follow-up after push.

Samsung/D-607's three deadlines, consumed reboot, unread post-reboot source and Main-adoption
condition remain untouched. The new shipping candidate is prepared, not physically adopted. This
is bounded CSV source-contract acceptance, not whole-device/private-DB/key/security/stability/
Home-speed/accessibility/provider/Store/release acceptance. iOS remains after Android completion.
