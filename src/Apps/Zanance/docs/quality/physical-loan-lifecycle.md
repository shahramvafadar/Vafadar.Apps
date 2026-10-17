# Physical debt and lent-money lifecycle (D-319)

## Complete original-form batch

Use the owner-approved Samsung SM-A176B, API36/ARM64/4096-byte pages, only after the other project
acknowledges the exclusive input turn. Initial app bytes are the already qualified D-318 signed APK;
no unchanged build, unit suite or encryption probe is replayed for this financial scenario.
Create D319_Fictitious_Loan through ordinary Profiles/setup, English/light/Simple. Its original checking
account D319_Fictitious_Cash opens at 2,000 EUR on October 17, 2026. All names/data are fictitious.
Keep Main and D316_Fictitious_Work separate; do not reset an installation or use direct database writes.

1. Ordinary Add debt saves D319_Fictitious_Debt with positive input 1,000 EUR, annual rate 12 and
   installment 100 EUR. Actual details correctly show -1,000 EUR and its fictitious lender.
   Default outside-cash exclusion remains explicit; do not mistake liquid Home balance for net worth.
2. Original schedule shows 11 estimated installments, November 2026 through September 2027, total
   interest 58.98 EUR. The first payment splits 90 principal/10 interest, leaving 910; the last clamps
   to 58.98 (58.40 principal/0.58 interest), leaving zero. Captured native packets contain every row.
3. Native confirmation explicitly names principal as transfer and interest as expense from the checking
   account. Cancel leaves original Transactions empty. Opening the estimate/confirmation is not a payment.
4. A new explicit confirmation Save runs once. Actual ledger contains exactly principal transfer 90 and
   Fees expense 10; checking is 1,900 and debt is -910. Its next estimate is 90.90/9.10, not another posting.
5. Original Record another amount opens a real transfer with the correct checking/debt endpoints.
   Explicit 910 principal, title D319_Principal_Final, no fee, saves once. Debt becomes zero, checking
   becomes 990, ledger has three rows, and the day income/spending result stays -10. No estimated
   remaining future interest is automatically recorded by this explicit principal-only transfer.
6. Ordinary Add debt with Someone owes me saves D319_Fictitious_Receivable, positive 500 EUR, rate 12,
   installment 100 and its fictitious borrower. It remains outside liquid totals by the original default.
7. Its real estimate has six rows ending April 2027, total interest 15.55, with first principal 95/interest 5
   and final 15.40/0.15 clamped payment 15.55. No due date/reminder or financial posting is created by viewing it.
8. Its native confirmation names principal as transfer and interest as income into the checking account.
   One explicit Save adds only transfer 95 and income 5: receivable 405, checking 1,090, five original rows.
9. Original Record another amount has receivable/checking endpoints. One explicit 405 principal transfer,
   D319_Receivable_Final, no fee, leaves both debt/receivable at zero and checking 1,495. Four transfers
   total 1,500 principal, actual expense is 10 and income 5; the day result is -5. The six row identities
   by their full displayed title/account direction/amount are distinct in this controlled scenario.

Independent integer arithmetic over all 11 + 6 published native schedule rows checks payment equals
principal plus interest, consecutive remaining principal, configured-payment bounds, complete principal
repayment and summed interest. This verifies the displayed samples, not a real lender's contract.
The estimator's future dates are separate from explicitly recording today's actual payment.

## Observed defect and final correction

Both fully repaid account pages incorrectly asked to add interest/installment under Edit although the
stored terms existed. HasLoanTerms combined configuration with positive outstanding balance; its
inverse was used as missing-data evidence. Introduce MissingLoanTerms from actual debt/installment
configuration and use it only for that existing six-language hint. Preserve the original outstanding,
manual action, entitlement, schedule, writer and financial calculations. No new strings, schema,
migration, model, encryption, dependency, permission or commercial activation change is involved.

An independent hidden Windows application pro.vafadar.zanance.loanterms qualifies original
AccountDetailViewModel/Page against retained owned SQLite data. There are 36 zero-loan/zero-lent
contexts (en/fa/de, light/dark,360/412/1280, process-local200% font stress), plus active configured and
actually missing-installment cases. All 38 pass actual native visibility; the genuine hint retains
complete scaled glyphs and native scrolling. Complete account/settings/ledger packets are unchanged.
Own-window captures only; no desktop input/focus, OS scaling or screen-reader acceptance is implied.

Two initial diagnostic viewport failures are retained (the old root-only scroll finder returned null;
then unfiltered descendants also included horizontal amount viewports). Select the actual vertical
viewport and continue only 37 incomplete cases on the same database; the original successful case is
retained. The initial failed runner also reported a closing-window native exception; final successful
qualification has no error file. No production payment was repeated because of those observer failures.
One incomplete ledger viewport was continued to its actual last row; a host JSON encoding failure
preserves its empty output separately and is corrected without any device/data write.

## Complete signed upgrade and retained financial evidence

Final standard Windows build and canonical complete offline Android Release build have zero warnings/errors.
The native qualifiers are the focused regression for this presentation change; unchanged unit counts
and successful unrelated crypto/import/backend/device scenarios are not rerun or increased.

Package artifacts/android-loan-lifecycle-d319-qualified/pro.vafadar.zanance-Signed.apk is 95,854,495 bytes,
SHA256 66b5ba24983073e1b3ac2ba6c48ce852913866adfdd9c1c68c94b3b15059c80c.
Permanent package pro.vafadar.zanance; minimum API 24 / target 36; ARM64/x64; 384 embedded AOT assemblies;
no Internet/debuggable flag. Signature v2/v3 verifies the same local test certificate
92cf83dfc05c274ad7fc99b820f9f1b272ff46eb5fb32b3039267074ed36167b.
ARM64 SQLCipher bytes remain 48d06bc639afdd119833d998da14976c64fc4fb3a9cc43dac682879cea72475c.

Actual upgrade without reset restarts the original app from PID 11414 to 17966. The same six full native
financial row packets compare equal, checking remains 1,495 and both counterpart balances stay zero.
Both original zero-balance detail pages now omit the false missing-terms instruction between the
visible estimate title and following action. Read-only lent Edit still shows 12% / 100.00 and is cancelled
without Save. No original financial operation is replayed. Main profile returns to Home at 979 EUR;
the new fictitious profile and its records remain retained. Exclusive device input is then released.

Local derived artifacts are under artifacts/phone-loan-work-d319 and artifacts/loan-terms-d319/layout;
keep them untracked. This is bounded native displayed-state/cold-upgrade qualification, not complete raw
main-database equality, bank-contract accuracy, all currencies/calendars/debt histories, notifications,
real authentication, hardware-key/16KiB/API24 matrix, screen readers, iOS, OAuth, store signing or release.
The canonical QA backlog and full development goal remain open; hosted CI is a separate result.
