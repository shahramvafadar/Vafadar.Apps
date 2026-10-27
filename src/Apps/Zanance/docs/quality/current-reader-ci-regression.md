# Current reader candidate regression repair (D-588)

Status: exact hosted cause reproduced/corrected locally and fresh exact-head CI/CodeQL successful.

## Actual hosted evidence

The one consumed check for final D585 headf559571d and the one for D586 headef9d03bc
both report CI failure and CodeQL success. Delete both one-shot schedules after
their only requests. Connected job metadata shows Windows and Android Release
builds successful in both runs, with only Libraries and tests failing. Inspect
only those failed job logs; do not rerun successful native build jobs.

- [D585 failed library/test job](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/40523365153/job/122256831379)
- [D586 failed library/test job](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/40528774108/job/122275490552)

Both logs identify exactly `PhysicalReaderCategoryCaptionTests.Current_installed_category_candidate_is_required`
at line56, Assert.True with actual false. The observed overall summary is5576 total,
one failure and25 skipped. The current production admission policy intentionally
requires signed f748. The older D579 regression still expects510a to be admitted,
so it contradicts that correct policy. The new D585 candidate test did not replace
or update this older independent assertion. This is a stale expectation, not an
observed application, financial, runtime or native-build defect.

## Narrow correction and proof

On latest ef9d03bc, compile the actual test project strictly and execute only the
identified method: one total/one failure reproduces the hosted cause. Change its
independent expected-current literal to the actually qualified f748 digest. Reject
510a explicitly and retain rejection of the earlier4a6c candidate. Do not substitute
the policy constant for the expected literal, remove the failing case, weaken
admission or change any shipping/helper behavior.

Rebuild only the affected test project and run only that same method: one total/
one passed/zero failed. No new test case is added. Previously successful resource,
financial and native tests, full application builds and accepted device actions
are not replayed. Standard Clean removes393 registered test-output files/
164750580 preflight logical bytes after ownership checks; no old refused deletion
scope is revisited. These counts are not physical host free-space evidence.

The old hosted failures remain immutable. A fresh pushed commit still requires
its own single delayed CI result; local success does not establish hosted success.
The independent temporary speech-engine trial and all broader accessibility,
stability/performance, iOS/provider/Store/public release gates remain separate.
The overall goal stays active until its actual scope is achieved.


## Fresh hosted outcome

The only GET for a8c5db09d05bdfa342fb368ea44a6f9d17521512 reports both workflows
completed/success: [CI40538715325](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/40538715325)
and [CodeQL40538715374](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/40538715374).
Preserve the atomic consumption marker and full response; delete only this schedule.
The observation occurs21.20 minutes after the commit, later than the desired15–18
minutes. Initial PowerShell DateTime-kind subtraction incorrectly reports negative
elapsed minutes; preserve it and record the timezone-aware correction separately.
Do not repeat the request. This closes the witnessed stale-test failure, without
reopening successful native checks or accepting broader product/release gates.
