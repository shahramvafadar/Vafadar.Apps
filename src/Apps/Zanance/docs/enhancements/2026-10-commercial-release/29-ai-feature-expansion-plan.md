# Zanance AI feature expansion proposal

Reviewed on 2026-11-19 against the product specification and the main-branch implementation. This is planning only. The browser operator panel and vendor routing are isolated synthetic pilot features. The existing Analysis screen already previews bounded personal period aggregates, supports manual sharing, and implements offer, consent, submission and follow-up. Completing that screen's synthetic journey through the current hosted adapters is the next step. See the [product specification](../../spec/Zanance-Product-Specification.md), [local analysis implementation](15-ai-local-delivery.md), [provider plan](27-ai-openai-gemini-provider-plan.md) and [pilot evidence](28-ai-synthetic-provider-pilot.md).

See the [application-flow and period-shortcut evidence](28-ai-synthetic-provider-pilot.md#application-flow-and-period-shortcuts-2026-11-19) for progress against priorities 0 and 1. Priority 2 is documented under [budget and goal funding explanations](28-ai-synthetic-provider-pilot.md#budget-and-goal-funding-explanations-2026-11-19). Priority 3 is documented under [forecast and plan scenario explanations](28-ai-synthetic-provider-pilot.md#forecast-and-plan-scenario-explanations-2026-11-19). See [local category review](30-ai-local-category-review.md) for priority 4. Priority 5 is documented under [questions about selected data](28-ai-synthetic-provider-pilot.md#questions-about-selected-data-2026-11-19).

## Recommended sequence

| Order | Area | Proposed user experience | Existing implementation to reuse | Estimate |
|---|---|---|---|---:|
| 0 | Shared foundation | Finish the signed app-to-server synthetic journey for both task routes. Review the selected scope, provider and quote before sending. Retain the original provider/model and request through interruption. | Analysis consent and private journal, durable requests, local administrator panel and two vendor adapters. | 18–28 h |
| 1 | Reports and Home | Explain the current period or compare it with an explicitly selected previous period. Explain which categories or planned-versus-posted differences drove a change. Offer the same monthly summary entry point from Home on demand. Link claims to the original report. | Report calculator, existing period review, commitment variance and Analysis screen. Detect candidate changes locally from reviewed records and comparable periods. | 10–16 h |
| 2 | Budgets and goals | Explain why a limit or goal is short of funding, or why a budget suggestion changed. Present one or two alternatives alongside the locally calculated values and assumptions. Open an editable draft for any action. | Budget suggestions, rollover and envelope rules, goal coverage and priority calculations. | 12–20 h |
| 3 | Forecast and plans | Explain the first projected shortfall, the affected currency and the events that contribute to it. Compare a baseline with a user-selected scenario, such as moving or excluding a payment. Explain unknown amounts and timeline assumptions. | Forecast and saved snapshot comparison, open plan occurrences and existing scenario overrides. The local engine recomputes every alternative. | 16–26 h |
| 4 | Transactions, categories, receipts and CSV review | Suggest a category only when existing rules do not give a confident answer. Explain ambiguity with one short reason and offer an explicit no-match result. Keep suggestions and proposed rules editable before applying. Use local OCR and CSV parsing as the initial source. | Existing on-device OCR and receipt review, categorization rules, CSV review, bulk editing and Undo. Start with local signals and predefined taxonomy; evaluate any optional remote fallback separately. | 14–24 h |
| 5 | Questions about selected data | Support questions such as “why did my EUR spending rise?” or “which recorded plans contribute to next month's shortfall?” Map the question to a finite set of read-only intents and ask for scope when ambiguous. Show the resulting filter and source before sending an aggregate. | Existing reports, forecast and transaction filters. Allowlisted local query parameters and official calculation methods supply the facts. | 18–30 h |

Total estimate is **88–144 engineering hours** for the described isolated implementation and focused synthetic acceptance. It excludes provider contracts, production privacy/legal review, deployment and customer pricing. Order 1 is the first product slice after the shared foundation. The document, image and free-text cloud paths are separate decisions and are not assumed in that estimate.

## Scope decisions for the remaining areas

- Keep transaction amounts, transfers, accounting, rates, settlement, automatic posting, reminders, reconciliation and backup/restore deterministic. The model can explain a published aggregate without calculating the source of truth or dispatching a command. Keep authentication, security settings, exports, sync conflicts and data deletion outside the model workflow.
- Keep statements, receipts, full notes, full CSVs and account or merchant names off the vendor path by default. The first categorization experiment should compare local methods. Any future cloud receipt fallback would require a separate per-document preview, data-minimization assessment and consent.
- Defer shared household analysis until authority, participant disclosure and redaction can be qualified separately. For holdings and loans, consider a later explanation of recorded changes only. Investment recommendations, loan eligibility, personalised legal or tax advice and live market claims are outside the baseline.

## Shared contracts and acceptance

The device-wide AI switch in Settings → Privacy and security applies across local profiles and
is retained when the app restarts. Turning it off hides Analysis and its shortcuts, stops new
requests and manual AI handoffs. On-device receipt OCR and deterministic category matching remain
available alongside manual entries, attachments, CSV category selection and explicit category rules.
The choice does not erase existing records or recall work already accepted by a service.

The initial 2026-11-20 Android review used one new API 36 emulator and one zero-opening synthetic account.
The native switch hid Analysis in More and receipt scanning on Home while manual expense and transfer
actions stayed available. The same settings choice and hidden actions survived a new application
process. Re-enabling restored both shortcuts. Two focused host tests covered saved choice and
retirement of an existing review and its provider and sharing actions. Strict Windows and Android
builds passed. No provider call or financial transaction was made. The owner's subsequent clarification
supersedes the initial receipt visibility behavior: the original receipt commands stay available with AI off.

Three focused synthetic regressions verified the current fallback with AI off. The original local
reader handled a newly selected receipt and a stored attachment and presented the parsed amount
for review. The generic CSV preview kept deterministic category suggestions editable. All three
checks preserved the input financial data and made no provider requests.

The Reports overview's explain and compare commands retain the AI session from the original
tap across report-source validation. Turning AI off retires the pending command, even when
the user turns it back on before validation finishes. A focused host test holds the real
report source validation, retires the original session and verifies that a new session can
validate the same report. This test covers the report command lease and uses no provider.

The signed offline Release APK for revision `384658d0` (SHA-256 `9803b374c8bdebf43f8339e2ddcb1690795f9977b04a85cc0ebad124f80769a5`)
passed the APK handoff checks and ran on an owned Android 16 x86-64 emulator. One synthetic
EUR 4.56 expense made the original Reports overview available. With AI off, both AI actions
disappeared and the period, account scope, and EUR 4.56 spending stayed the same. The original
local Surplus explanation opened and showed negative EUR 4.56. After re-enabling AI, one
fresh comparison opened Analysis with the report account selected and the preceding
interval switch on. No provider or share command was invoked. The same app process and
boot were retained through the checks. The disposable installation and emulator were
retired. Local evidence is under `artifacts/android-release-acceptance-20261120`.
The native check covers the switch and a fresh comparison, while the in-flight command
case remains covered by the focused host test above.

Signed Offline Release APK `e7cf917b` also verified the switch across two local profiles
on an owned Android 16 x86-64 emulator. AI was turned off once in Main. The second profile
showed the switch off and omitted Analysis in More. Returning to Main in the same process
and boot preserved both observations. The profile observer initially expected the display
name alone and continued from the existing Profiles screen using its accessible action
label. Only synthetic zero-opening accounts were set up. No ledger, provider or share
command was used. The disposable emulator was retired. Evidence is under
`artifacts/ai-opt-out-profile-scope`.

- Define a small versioned input and output contract for each approved task. Keep calculations, exact dates, sign and currency semantics in Core. Map model-generated facts to permitted source references so the app can open the original local view. Render output as text. Validate its schema, references, totals and bounds, including zero or insufficient data.
- Use the current manager selection and Off control for simple and reasoning tasks. Extend the administrator's per-task selections as new distinct tasks are qualified. Treat content such as a note or OCR line as data. A task receives only the selected period, filtered scope, permitted fields and exact destination disclosed to the user. Avoid silent provider fallback. Wait for separate privacy, retention, recovery, budget and commercial gates before sending genuine data.
- Qualify on a fixed set of synthetic cases for each task. Include refunds, transfers, incomplete or stale inputs, currency separation, date boundaries and the six supported languages. Confirm visible source links, offline continuity, per-request bounds, denial and failure without ledger changes, exact continuation after switching provider and stale suggestion refusal. Use a deterministic baseline before adding an AI dependency, and retain a separate measured quality and cost report for each task.

## Cost assumptions

Standard text rates were checked on 2026-11-19. Illustrative inference-only budgets use 500 input and 250 billable output tokens for a simple report, and 1500 input and 750 billable output tokens for a reasoning task. Output allowances include thinking tokens. These are usage assumptions, not measured quotes, customer prices or invoice amounts.

| Route | Model | USD per million input / output tokens | USD per 1000 successful calls at the assumed usage |
|---|---|---:|---:|
| Simple | GPT-6 Luna | 0.10 / 0.50 | 0.175 |
| Simple | Gemini 3.5 Flash-Lite | 0.30 / 2.50 | 0.775 |
| Reasoning | GPT-6.2 Sol | 2.00 / 10.00 | 10.500 |
| Reasoning | Gemini 4 Hydrogen | 1.50 / 7.50 | 7.875 |

The reasoning estimate is scenario-based. The earlier Hydrogen comparison actually recorded 1090 billable output tokens. Measure successful and incomplete attempts, and set bounded budgets before activation. No vendor call was made to produce this plan. See [OpenAI pricing](https://developers.openai.com/api/docs/pricing) and [Gemini pricing](https://aistudio.google.com/docs/pricing).
