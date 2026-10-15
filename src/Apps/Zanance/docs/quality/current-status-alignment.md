# D-287 current-status alignment

## Concrete problem

Several current summaries still described the pre-integration app: plaintext financial databases,
enabled Android OS backup, an unprotected safety path, missing loan principal/interest estimates,
and no identity or quota implementation. Later D-number records contained the delivered changes,
but those records did not make the contradictory current tables correct. This audit uses main
70190ac8 as the code/evidence baseline; it does not reopen or rerun successful runtime verification.

## Source-to-document reconciliation

| Current fact | Inspected authority | Corrected summaries |
|---|---|---|
| Android/Windows select the official-source cipher and require protected data | MauiProgram provider selection, AddZananceData protectionRequired and native ProtectedStorageRegistration adapters | ADR 0010; Community/Android/device-key/generation/portable/security guides; spec Section 28/31 |
| Shared lifecycle contracts own startup/profile/restore/safety/automatic protection | Startup/ProtectedStorageRegistration and Android/Windows host registration; D-263/D-264 and D-281 evidence | Current storage/validation matrices and dependency inventory |
| Android requests exclusion from OS backup and transfer | AndroidManifest allowBackup=false, backup_rules.xml and data_extraction_rules.xml, including all nine domains | Privacy profile and mirrored starter DF-16/18/20; draft policy; ADR/spec |
| iOS protected runtime and final OS exclusion remain unqualified | Platform-conditional provider/registration and canonical QA-07/SEC platform gates | Every new current summary distinguishes iOS from delivered Android/Windows |
| Loan estimate and principal-transfer/interest-expense posting exist | Core/Accounts/LoanCalculator, completed P2-18 and D-171 evidence | Spec Phase 2A summary removes its incorrect not-built claim, retaining the estimate/contract distinction |
| Local quota boundaries exist with activation off | ZananceDataServiceCollectionExtensions uses InactiveCommercialWriteAccessSource; D-173 finite entry-point inventory | D-61 audit explicitly historical; privacy summary/inventory preserve no commercial activation |
| Identity backend exists locally, not financial sync or hosted production | Zanance.Api Program/AddZananceService and D-272 source/restore evidence; open owner decisions | Spec Phase 2B and current dependency inventory |
| Current tests have bounded evidence, not one full 3,610 run | D-283/285/286 reports and retained focused/native proof | Spec Section 31 summary names 3,610 covered behaviors and the prior hosted 3,591 baseline with 25 explicit skips |

The current-profile local Windows warning inventory still records 704 occurrences. No warning is
silenced, source-forked or blanket accepted by this alignment. The earlier successful hosted Native
cipher candidates run 38707538867 remains dated baseline evidence, not proof of a new current-head
workflow. Open provider, hardware, OEM, low-memory, iOS, security-review and release gates remain open.

The privacy policy remains a draft with its effective date/contact/release gates unchanged. It now
distinguishes always device-encrypted automatic copies from optional-password portable files and
the independent PIN access gate. No policy is published and no account/provider action is taken.

## Verification and limits

Focused source/summary checks confirm both privacy matrices have identical DF-16/17/18/20 rows,
Android's explicit exclusion inputs match those statements, current summaries no longer claim all
main data is plaintext, and spec loan/status/test rows match their named authorities. Historical
inventories and D-number records retain their original statements under explicit historical scope;
they are not retroactively rewritten into new runtime acceptance.

Only documentation changes. No app/library/model/package/workflow/source input, secret, user data,
financial writer, build, test execution, device operation or APK changes in this step. The current
complete signed APK and 3,610 covered-behavior baseline remain D-286. The full development objective
is not complete: the canonical backlog and owner/external decisions still govern remaining work.
