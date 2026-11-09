# D964 CI failures before complete job admission - D-965 diagnosis

## Evidence and scope

The consumed one-shot head-SHA workflow-list response for0647b277 at15m19.75after
recorded push reports both CI/CodeQL completed/failure. No second head-status GET
is issued. The owner's separate failed-CI diagnosis authority permits reading
job/check metadata and failure annotations; these are not status polling, reruns
or successful-job log downloads.

| Failed run | Actual admission/error evidence |
|---|---|
| [CI42674239590](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/42674239590) | Internal server error; correlation852fae48-e3b0-430a-8e11-2e3758808c46. Only Android build129351706479 is present and completed/success. |
| [CodeQL42674239629](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/42674239629) | Internal server error; correlationce3ec449-c4e1-4538-96f0-dc4b0159f587. No started job/check run is returned. |

The connected first-page job wrapper exposes one successful Android job and an
empty CodeQL list, without a total count. Separate bounded check-suite check-run
responses independently show total_count1and0. The public run annotations then
provide the exact server-error messages. No failed job/log is available in those
returned records, so no test stack trace or code-failure cause is fabricated.
Sign-in is not attempted, and no successful Android job log is read. Its returned
step metadata shows the default and selected billing-sandbox builds succeeded;
this does not establish tests, Windows or CodeQL completion for the failed run.

The Android Node20 cache-action deprecation/forcedNode24 warning and pending
ubuntu-latest image-change notice are separate annotations. They are not a
demonstrated cause of these server failures. Do not infer billing/quota, SDK,
application code or a workflow repair from the absence of jobs. The deeper
GitHub server cause is unknown; no runtime, dependency or workflow changes are
made to rescue these runs, and no rerun/status poll is requested.

The exact original consumed response, connected metadata, full bounded check-run
responses and extracted failure/correlation fields are retained in
`artifacts/d965-analysis-language-publication` and the original D964root. Both
[local language/invalidation correction](analysis-language-publication.md) and its actual local/native qualification are independent of these CI admission
errors. A legitimate subsequent changed commit gets its own once-only check;
that is not a rerun of a successful job. Full CI/security/release acceptance stays
unproved, and the complete delivery goal remains active.
