# Synthetic analysis provider pilot

## Provider observations

Both providers received the same invented November totals in EUR minor units: income 125000, expenses 41000 and refunds 3000. The expected net expense was 38000. Both completed background operations returned that value in the requested JSON schema. The recorded result was retrieved by its original provider ID.

| Provider | Accepted model | Recorded usage | Cost evidence |
|---|---|---|---|
| OpenAI Responses | `gpt-6.2-sol` | 110 input and 52 output tokens | Estimated USD 0.00074 at the [published standard rates](https://developers.openai.com/api/docs/pricing), within the USD 5 test allowance. The estimate is not an invoice. |
| Gemini Interactions | `gemini-4-hydrogen` | 272 input, 77 output and 102 thought tokens | Run on the owner-designated Free Tier project. No invoice verification or paid-tier switch. |

Gemini returned HTTP 400 for the initial request. The request with `thinking_level: low` was accepted and its original handle completed. Both pilots stored their background operation at the vendor for the status lookup. See [OpenAI background mode](https://developers.openai.com/api/docs/guides/background) and [Gemini background execution](https://aistudio.google.com/docs/background-execution).

## Local operator workflow

Use a dedicated private service root and a loopback PostgreSQL instance whose database is named `zanance_ai_provider_pilot`. Supply the dedicated connection through `ConnectionStrings__Service` and the private directory through `Service__Root`. Set `ASPNETCORE_ENVIRONMENT` to `Integration`, set `Analysis__LocalQualification` and `Analysis__ProviderPilot__Enabled` to `true`, and choose a local HTTPS port with `Analysis__ProviderPilot__Port`. The synthetic pilot refuses a self-hosted deployment. Apply service migrations before selecting a provider.

### Management panel

Set `Analysis__ProviderPilot__OperatorUserId` to the immutable user ID of the existing confirmed operator account in the isolated pilot database. The panel is absent when that setting is empty. Use the approved local TLS identity and open `https://localhost:<pilot-port>/admin/analysis` on the service host. Sign in with that account. The local panel accepts the configured account only and refuses accounts with second-factor enrollment, because it does not implement a second-factor prompt. Its 15-minute browser session is separate from app bearer sessions. Changes are attributed to the original user ID in the selection audit.

Under **Simple reports** or **Financial comparisons**, review **Current model**, select an allowlisted model or **Off**, and save that section. Each section changes independently. A stale form for a changed section asks the operator to review the latest selection, while an unchanged section can still be saved. The panel does not inspect customer data, start an analysis, or contact a model provider. Its synthetic acceptance uses fictitious accounts and model configuration in isolated PostgreSQL. Vendor calls, token usage and API cost for this panel test are all zero. The prior observed inference costs remain documented below.

The real HTTPS forms were verified for sign-in, non-operator and anonymous refusal, invalid and stale forms, six audited changes, two independent Off transitions and sign-out. Edge rendered the signed-in panel at 1060 and 375 CSS pixels with the two labelled model controls. Published Standard rates were rechecked on 2026-11-19 and match the per-purpose table below.

Use **Pending requests** to inspect the oldest 50 unresolved pilot requests and see when more are pending. The queue shows the original task and route after a model switch, accepted and last-check times in UTC, and whether the vendor reference is recorded. It exposes no account identity, aggregate, result, digest or vendor operation ID. Loading it does not query a vendor, alter a request or move credit. A missing reference leaves the outcome unknown. Use **Check status** only for an existing reference. The authenticated, antiforgery-protected form reserves one of three durable per-request attempts and waits at least one minute from the last status lookup. The service looks up the exact original provider execution even when new requests have been switched Off. A confirmed result goes through the existing one-time settlement. Missing, unavailable or unverified results remain pending with the original reservation intact.

The isolated HTTPS fixture covered anonymous refusal, the empty queue, oldest-first ordering, overflow, ordinary and completed request exclusion, the original route after Off, unchanged request and credit records, and zero model calls. PostgreSQL refused the first synthetic seed for violating the request bounds; the fixture then used the required reservation and terminal result fields. No prior selection tests were replayed.

A separate HTTPS and PostgreSQL fixture tested the operator action with synthetic vendor responses. Two simultaneous forms reserved one attempt. Checks at 59 seconds and after the third attempt were refused. A missing reference and an unsupported digest did not use the transport. Three unknown responses kept the original request and reservation pending. A separate valid result used the same retained route and settled once. Four audit rows recorded the original operator and times. The transport accepted only GET for the two synthetic references, so the check incurred no vendor usage.

### Operator health, stop and rollback

Use **Analysis models** to confirm the configured model and route availability for each task, and **Pilot operations** to inspect saved state without a provider call. The operations view shows accepted requests, retained ceilings, observed Standard estimates, usage coverage, unresolved dispatches and completion notices grouped by provider. No customer, request or vendor IDs, payloads or output appear there. Unavailable usage stays distinct from a recorded estimate of zero. An invalid budget setting appears explicitly and refuses subsequent admission. Provider connectivity and external billing are outside this view.

1. To stop new work, set both tasks to **Off** and verify the saved selections. Old unaccepted quotes become invalid. Requests already admitted still own their original routing and continue through the existing worker and status checks.
2. To drain accepted work, keep its dedicated project, credentials and webhook settings available. Use **Pilot operations** to distinguish waiting creates, recoverable requests with a vendor reference, unresolved requests without one, and notices that reached their lookup limit. Use **Pending requests** for the bounded original status action. A missing reference is uncertain; it supplies no authority to send again or release a reservation.
3. To return to a previously approved model for new work, select it again in **Analysis models** after verifying availability and budget. This creates a fresh audited selection. Original pending requests retain the project and revision they were admitted with, even when their model is no longer selected.
4. For an old backup, keep the entire pilot disabled until its outbound history has been reconciled. Turning the two model selections Off alone still allows the original queue to drain.

The isolated HTTPS/PostgreSQL test verified anonymous refusal, exact counts from accepted pilot requests, budget and legacy ceiling treatment, the distinction between unobserved and zero usage, no identifiers, and visible invalid configuration. There were no vendor calls, credit movements or data changes. The directly affected budget concurrency case was rerun after sharing the admission query with this view.

### Application switch and Off

The original Analysis commands and private journal were exercised against the pinned HTTPS service and isolated PostgreSQL. The application first quoted Luna, then the service switched the same task to Flash-Lite. Sending the old reviewed quote closed that request as NotAccepted without reserving credit or contacting a vendor. A fresh quote accepted one simulated Flash-Lite create. After the task was switched back to Luna and set Off, Continue read the original Flash-Lite handle once and settled that original result. Off prevented another quote. Re-enabling Luna admitted a newly reviewed request with one simulated Luna create and one settlement. The local financial fingerprint was unchanged.

The test replaced the service's vendor transport with a closed fixture and observed exactly two simulated creates and one original status read. There were no external requests or vendor charges. It verifies the source-linked application commands and original TLS routes on the host. The native package and the separate public-provider and release checks still need their own evidence.

### Android offline and connected editions

On source revision **bb6ebbcf**, the standard packaging script built both Release APK editions without warnings. The first invocation stopped before packaging because Windows CMD ignored the inherited overlong PATH. A shorter process-local path restored normal tool discovery. The signed offline APK (SHA-256 **12dd05a8cfd434fe83e6cb621a79f466a4f087065cbe51948b412eaae8bc40dd**) has no INTERNET or Google Play BILLING permission. The separately built Personal Sync APK (SHA-256 **a73dbc45bbb58dcacdc18237cda0af8741d811f1b3e257d2bdf3a4d4d22d6ece**) has INTERNET and no BILLING permission. Both carried the same engineering signer and accepted the x64 emulator architecture.

On one fresh owned Android 16 x64 emulator with 4096-byte pages, the installed offline edition used the original More entry and Analysis screen. The device reviewed the initial fictional EUR account and displayed the exact local aggregate with zero activity. Local review and manual share were available; Check provider, Quote and Send were disabled. The connected edition installed over the existing package without a data reset and kept the same package UID and boot. Home retained the original EUR account. Analysis showed the provider check and sign-in controls, while Quote and Send remained disabled without admission. The first connected observer scrolled before the list was ready and found no Analysis button; a scroll after confirming the same More page continued the attempt.

The fixed production destination was left in place. The test performed no account sign-in, provider check, model request, sharing, financial transaction or physical-device action. The temporary app and owned emulator were retired after the native checks. This establishes the package and visible Android edition boundaries. The source-linked pinned local test above covers the connected request sequence separately; native authenticated delivery through the genuine configured service remains open.

### Authenticated native Android analysis

A separate signed Release engineering package used the original Android foreground access, encrypted database, aggregate snapshot, credential storage, private analysis journal and connected analysis client. It signed in as a fictional account to the original service over pinned local HTTPS, used an empty EUR report and received a result from each route after a service-side switch. The isolated host confirmed one original reservation and settlement per model, one simulated create per adapter and no external calls. The native client read back the final private record, rechecked its original financial source and verified that no ledger entries were written.

The Android 16 x64 run used 4096-byte pages and fresh package UID 10216. The signed APK SHA-256 was **e34b4ff05d0345d448b8cc5eee7805bf2e5ba7a02a98ba801d70c24dda2763a4**. The first test-host compile lacked a certificate namespace import. The first native builds found a generated Windows file in the engineering source glob and a missing notification-settings method in the review activity. The corrections exclude the app's build artifacts and refuse settings navigation in the review activity. The final strict builds and both native and service terminal checks passed. The temporary package, owned emulator and database were retired.

The synthetic provider replies were generated by an isolated test transport. This run verifies the original source-linked Android client against the pinned local service. Production address selection, real account sign-in and external model delivery still require their own evidence. The separate native consent cohort below uses the actual Android screen and dialog. See the [independent native runner](../../../../../../eng/benchmarks/Zanance.AndroidSyncReview/README.md#authenticated-analysis-network-variant).

### Native Android cancel and approve

The same isolated engineering variant opened the original Analysis page, reviewed one empty fictional EUR period, checked the provider and quoted Luna through pinned local HTTPS. The Android confirmation displayed the exact destination, provider, model and maximum credit reservation. Native Cancel closed the dialog, left Send available and wrote neither a private intent nor a server request or credit event. The same quote then displayed identical terms. One native Send action created one request, one synthetic reservation and one settlement. The original page displayed the retained approved snapshot and mock result. The matching private record was readable and the original source check and zero-entry check passed.

The strict Release APK was signed and verified on Android 16 x64 with 4096-byte pages. Its SHA-256 was **d2a383ebcd9706691c29cb42fed783f5d0bcf4c2db4229215b8aa55ba5636ffa**. The two-model native scenario and the installed editions were not repeated. The fake transport performed no external requests and incurred no vendor cost. The fresh package, emulator and synthetic database were retired. This verifies the original native disclosure and confirmation against the isolated service. The shipping destination and public authentication remain separate.

### Original dispatch recovery

An accepted synthetic request records a queued create in the same transaction as consent, the vendor cost ceiling and the single credit reservation. The original submission path and the background drain race on a conditional PostgreSQL update. Only the winner can send the original create. It records the durable claim and UTC time before touching the transport, and keeps the original route even when that model is subsequently switched Off for new work. A normal service restart can pick up a never-claimed request. A claimed create is never recovered by reissuing it, even if its receipt is lost. The user status path observes the original operation only, and earlier rows without a dispatch stage have no queue authority. Keep the pilot disabled when opening an old backup unless its outbound history has been reconciled independently.

Two isolated store instances sharing PostgreSQL competed for an unclaimed request after reconstruction. The only synthetic POST observed its committed claim before the simulated lost receipt. An already claimed request and a previous-format pending request did not dispatch. A subsequent drain after advancing the clock by 30 days made no extra call or credit movement. The first test seed lacked the original gift grant and the existing wallet invariant refused it; the corrected fixture includes the grant and all three reservations.

The general identity workflow formerly treated opt-in provider tests without their fixture as failures. The fake-provider tests now belong to the isolated analysis workflow and each receives its own fresh fictional database. The live test remains explicitly opt-in. The [repaired identity workflow](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/44634484329) completed successfully, including its general identity job and isolated fake-provider step. Four pre-fix identity jobs reported respectively three, four, five and six opt-in skips counted as failures, and no other failures in those jobs. The [fourth job log](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/44633558080/job/135946227251) includes the cost case. See the [first](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/44631663207/job/135939590610), [second](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/44632280421/job/135941760440) and [third](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/44633009171/job/135944295913) job logs.

### OpenAI lost-receipt recovery

The isolated pilot exposes the HTTPS completion receiver at **/analysis/pilot/openai/completions** only when the existing pilot is enabled and **Analysis__ProviderPilot__openai__WebhookSecret** is supplied privately. The receiver admits **response.completed** and **response.failed**. This code does not create a public endpoint or a vendor subscription. The receiver authenticates the exact body and the signed delivery headers with the Standard Webhooks HMAC format. It bounds the timestamp window to five minutes, the body to 8 KiB and the inbox to 128 notices still eligible for lookup per provider. It records only stable delivery and response IDs and lookup metadata. Duplicate delivery IDs or response IDs acknowledge the original inbox entry without storing another.

The background worker can make up to three authenticated GETs for a receipt with a minimum one-minute gap. It uses the dedicated OpenAI project. To bind the response, the retrieved ID, completed or failed state, opaque operation metadata, frozen project revision and exact model must match an original claimed synthetic request without a saved vendor ID. The original result validator and one-time credit settlement apply. The callback itself never supplies the result or tells the service to repeat a create. Foreign, duplicate and already known notices cannot settle a second request. An unmatched, unavailable or exhausted notice does not release credit.

The recovery query compares the exact dispatch correlation in PostgreSQL and returns only matching eligible rows. It no longer acknowledges a valid notice without lookup merely because more than twenty old claims exist. The expanded transaction fixture seeded one original and twenty-one unrelated unbound claims across owners, above current pilot admission limits. With a non-ASCII owner ID, the authenticated original still matched the same UTF-8 correlation, rolled back atomically on an induced settlement error and settled once after recovery. Unrelated claims stayed pending. The admission ceiling, notice-attempt limits and vendor-call policy remain in effect.

The same transaction fixture now exercises the actual inbox drain with a transient PostgreSQL serialization failure and a simulated HTTP client timeout ahead of another notice. The database failure previously escaped the drain. Both interrupted notices retain the lookup claim and remain eligible only after the original one-minute delay. The unrelated notice finishes in the same drain, and the original settlement commits once after the transient failure clears. No new provider create is sent. Caller cancellation still propagates.

Completed and exhausted receipts still prevent duplicate delivery or response IDs. They do not occupy active inbox capacity. A focused signed HTTPS/PostgreSQL test seeded 128 historical receipts and 127 active notices. The old receiver refused the next valid notice. The corrected receiver admitted it, refused an additional active notice, acknowledged two historical duplicates without adding rows, and admitted a notice in the other provider namespace. The fixture made no outbound request and created no customer credit record.

One synthetic HTTPS/PostgreSQL test refused an altered body and an expired timestamp, coalesced repeated deliveries and responses, and ran concurrent inbox drains. The fake transport performed two authenticated reads: one matching result recovered its original request and settled once, while the other response could not bind. A fresh duplicate notice after the signature window caused no additional read. There were no real vendor calls or changes to a provider project. Public webhook delivery and secret rotation on the provider remain open. See [OpenAI webhooks](https://developers.openai.com/api/docs/guides/webhooks).

### Gemini lost-receipt recovery

To stage recovery for the isolated Gemini project, use a dedicated static webhook secret configured as **Analysis__ProviderPilot__gemini__WebhookSecret**, with the receiver **/analysis/pilot/gemini/completions** and events **interaction.completed** and **interaction.failed**. This entry point remains absent when its setting is unset. It uses the same bounded raw-body HMAC authentication, durable inbox and bounded GETs as the OpenAI receiver. Delivery and interaction identifiers are deduplicated within their provider namespace. The additive migration backfills all existing inbox rows as OpenAI without removing receipts. A separate upgrade probe preserved the original receipt, lookup count and completion marker, and admitted the same delivery and response IDs in the Gemini namespace. The first schema fixture used the default constraint name instead of the original named constraint; correcting the fixture left the shipped migration unchanged.

Only in that configured synthetic mode, the original Gemini create appends an opaque operation reference to its fixed fictional input. No account identity or ledger data is in that reference. A notice triggers authenticated retrieval of the original interaction with its stored input. Exactly one text block in the completed original user-input step must end with the expected operation reference. A model-output echo or the callback body itself supplies no binding authority. The original project revision, model, synthetic digest and existing result validator still apply. No dynamic webhook or public project changes were made.

The isolated HTTPS and PostgreSQL test used the original create path and a fake transport. It observed one create with the unique input reference and a simulated lost acknowledgement. The two authenticated reads qualified one original result and refused a foreign reference. Invalid, stale and replayed notifications did not create another lookup or settlement. The directly affected OpenAI case was checked separately on the shared inbox. No real vendor calls were made. Live static webhook delivery, exact provider response conformance, signing-secret rotation and release acceptance remain open. See [Gemini static webhook signatures](https://aistudio.google.com/docs/webhooks#verify-static-signatures), [stored interaction steps](https://aistudio.google.com/docs/interactions-overview#how-it-works) and [Google SDK input retrieval](https://github.com/googleapis/python-genai/blob/main/google/genai/_gaos/interactions.py).

### Signed failed-operation recovery

A failed notice uses the same bounded inbox and authenticated retrieval as a completion. Only a retrieved original ID with terminal failed or completed status and its original route, model and operation binding can restore a missing handle. A failed original without usage records the vendor failure and leaves the customer reservation open. Webhook error text does not enter storage or supply authority to release credit or dispatch another request.

Two local HTTPS/PostgreSQL cases used signed fictional failure notices and fake provider responses. Each recovered exactly one already claimed operation and refused the foreign correlation. Stale, altered, duplicate and later duplicate notices did not repeat provider operations. Both preserved the original pending state, zero settlements/refunds and unknown usage. The Gemini fixture made only its original synthetic create before the simulated lost acknowledgement. There were zero external provider calls. A Gemini failure without the original stored user-input step remains unrecoverable through this mechanism. Live webhook delivery still needs the real configured environment. See [OpenAI webhook events](https://developers.openai.com/api/reference/typescript/resources/webhooks) and [Gemini webhook events](https://aistudio.google.com/docs/webhooks).

### Recovery transaction after a service interruption

Saving a recovered handle and applying its authenticated terminal observation now share the original owner lock and one PostgreSQL transaction. A settlement-write interruption rolls back the handle, usage, status and credit movement together. A later inbox attempt can retrieve the same original result. If an earlier service version already bound a pending handle without its terminal status, the inbox checks that preserved handle on its original route. Requests with an existing terminal observation or settlement need no duplicate vendor lookup.

A new isolated test interrupted the settlement insert for an authenticated result, verified that no new binding or credit was committed, and reconstructed the store. The original lookup then settled once. A separate seeded partial binding obtained its failed status without movement or synthesized usage. All calls were fake authenticated reads; there was no creation or external charge. The regression now belongs to the existing isolated analysis CI lane.

### Signing-key rotation

The current signing secret remains the primary key for each provider. To admit its previous key during an authorized rotation, configure **PreviousWebhookSecret**, **PreviousWebhookSecretFromUtc** and **PreviousWebhookSecretUntilUtc** in the same private provider settings as **WebhookSecret**. Specify both times in UTC using **yyyy-MM-ddTHH:mm:ssZ**. Use the provider's actual overlap and keep it at most 24 hours. A missing, invalid or out-of-range window gives the old secret no authority; it cannot substitute for an invalid current key. At the exact deadline, old signatures are refused. Removing the previous secret revokes it immediately on configuration reload or process start. Use immediate removal if revoking a compromised key. Keep provider secrets and windows separate and omit the old values after cutover.

The isolated HTTPS/PostgreSQL test used a controllable clock, separate fictional keys for both providers and the actual signed receiver. It verified pre-window refusal, old and current signatures during overlap, a dual-signature duplicate, cross-provider refusal, an overlong and missing deadline, an invalid current setting, immediate revocation, and exact expiry. Only five unique inbox receipts were stored; there were no vendor calls or settlements. Provider-side rotation and public delivery remain unverified. See [Gemini signing-secret rotation](https://aistudio.google.com/docs/webhooks#rotate-signing-secret) and [OpenAI webhooks](https://developers.openai.com/api/docs/guides/webhooks).

### Synthetic vendor cost ceiling

Before a new synthetic request is accepted, the same PostgreSQL lock used for route selection and pilot admission checks the monetary ceiling. The accepted row stores the uncached Standard ceiling in integer millionths of a US dollar, rounded up from 2048 input tokens and the output limit on the original model. The customer credit price is unchanged. Requests refused at the cost limit neither reserve customer credits nor call a vendor.

| Model | Standard USD per million input / billable output | Output token limit | Maximum reserved USD |
|---|---:|---:|---:|
| GPT-6 Luna | 0.10 / 0.50 | 512 | 0.000461 |
| GPT-6.2 Sol | 2.00 / 10.00 | 1024 | 0.014336 |
| Gemini 3.5 Flash-Lite | 0.30 / 2.50 | 512 | 0.001895 |
| Gemini 4 Hydrogen | 1.50 / 7.50 | 4096 | 0.033792 |

The configured limits may be lowered with integer values for **Analysis__ProviderPilot__Budget__RequestMicros**, **Analysis__ProviderPilot__Budget__UserMicros**, and **Analysis__ProviderPilot__Budget__ProjectMicros**. Their defaults and upper bounds are 33792, 135168 and 675840 respectively. The user limit counts all routes for that owner. The project limit conservatively pools accepted requests on the same provider within the pilot store, including older project revisions. A legacy request without a stored ceiling is counted at 33792, and completion does not release a commitment. This admission estimate assumes the existing fixed synthetic prompt fits the input allowance. External charges and invoice reconciliation are separate. The existing limit of twenty attempts per provider still applies.

The isolated PostgreSQL test used the actual request store and a synthetic transport. A request at 461 microdollars was refused against a 460 limit, a second request for the same owner could not bypass its 461 total, and two owners competing for the last 461 admitted exactly one create. The two accepted requests and credit reservations totaled 922 microdollars of planned vendor exposure. No real vendor requests were made. Standard text tariffs were checked on 2026-11-19. [OpenAI pricing](https://developers.openai.com/api/docs/pricing) and [Gemini pricing](https://aistudio.google.com/docs/pricing).

### Vendor usage on unpublishable results

A terminal response retrieved from the original authenticated provider route can report usage even if the existing exact-result validator refuses publication. The service retains input, billable output, available cache and thinking counts, and a rounded-up uncached Standard estimate in millionths of a US dollar under the reference tariff **standard-uncached-text-2026-11-19**. OpenAI reasoning is already part of output, while Gemini output and separate thinking tokens are added. The pending operator view shows this estimate separately from customer units. Repeated observations do not accumulate usage. Conflicting evidence cannot overwrite the first recorded terminal counts or settle the request.

Unknown usage does not become zero, and the estimate neither reduces the original cost ceiling nor asserts an invoice amount. The pending operator view also shows the authenticated original terminal vendor outcome separately from the reservation. The first matching terminal status is retained even when usage is absent. Foreign references and contradictory statuses cannot publish a result, overwrite that status or release credit. A missing terminal status or usage is shown as unavailable. Existing requests have no backfill from unauthenticated evidence. A suppressed result stays pending with its customer reservation. Customer refund rules for unusable output remain a separate decision before real billing.

An isolated PostgreSQL test used two fake providers. OpenAI returned an incorrect amount with 100 input, 50 billable output, 25 cached input and 20 reasoning tokens. The estimator stored USD 0.000035. Gemini returned 48 input, 500 output, 20 additional thinking and 8 cached input, exceeding the output limit. It stored USD 0.001315 without settling customer credit. A later read with conflicting counts left the first record unchanged. The matching valid OpenAI result settled once. Real vendor usage and test charges were zero. See [OpenAI response usage](https://developers.openai.com/api/reference/resources/responses/methods/retrieve) and [Gemini usage fields](https://googleapis.github.io/js-genai/release_docs/interfaces/gaos_resources.Interactions.Usage-1.html).

### Original create refusal evidence

The pending operator view shows a content-free HTTP classification for the original synthetic create when it receives 400, 401, 403, 404 or 429. It separates that response from a stored vendor handle, terminal model outcome and metered usage. A 404 can refer to the endpoint or model; a 429 can reflect a rate or quota limit. The response body, any model output and raw provider diagnostic text do not enter this observation. Unclassified replies leave the field empty. None of these categories proves that a reservation can be released or another create is safe. See [OpenAI error codes](https://developers.openai.com/api/docs/guides/error-codes) and [Gemini troubleshooting](https://aistudio.google.com/docs/troubleshooting).

The isolated PostgreSQL test refused an unknown configured model before any vendor call, then exercised six distinct creates with the genuine service store and synthetic HTTP replies. Each create was sent once despite repeat submission and a later dispatch drain. The five classified replies and one unclassified 503 retained their original reserves, without a handle, usage, customer settlement or refund. The directly affected private HTTPS panel test verified the displayed 401 classification and zero provider traffic. Both tests used the additive migration and regenerated compiled model. There were no real vendor calls or costs.

### Create acknowledgement correlation

The original OpenAI create path now checks its own operation metadata when the acknowledgement echoes the correlation field. Two fictional responses previously attached the returned handle despite a different operation or an explicit null value. The corrected path leaves both outcomes unresolved without attaching a handle. Matching metadata and acknowledgements that omit metadata still bind the returned handle once. Four focused cases used a closed transport and contacted no provider. See the [Responses API reference](https://developers.openai.com/api/reference/resources/responses/methods/create).

Subsequent status reads also compare the echoed OpenAI operation against the original request, including user continuation, operator lookup and signed notice processing for a known handle. Two fictional terminal replies with the expected handle but foreign or null correlation previously yielded a publishable result. They now supply neither a result nor terminal status or metering. The matching-correlation control still publishes through the adapter. The three focused status checks used a closed transport and did not perform a live lookup.

### Explicit refusal in structured output

A separate output check exercised the original OpenAI status adapter with a fictional response containing both valid structured text and a refusal. The text was previously accepted when the refusal was in the same message or in a second message. Both cases now suppress the result and preserve the provider completion status and reported token usage. Existing reservation reconciliation remains in effect. The two focused checks used a closed transport and no live calls. See [structured output refusals](https://developers.openai.com/api/docs/guides/structured-outputs#refusals-with-structured-outputs).

### Invalid Gemini thinking usage

A separate simulated Gemini status check found that a quoted, fractional or out-of-range thinking-token count was interpreted as absent. All three responses had valid business fields and previously completed as if the reported thinking usage were zero. Invalid supplied counts now suppress publication and leave metering unavailable while retaining the original terminal vendor status. The three focused checks did not call a vendor or change customer accounting. See [Gemini pricing](https://aistudio.google.com/docs/pricing#gemini-4-hydrogen) for the inclusion of thinking in billable output.

### Explicit retention deletion for completed pilot results

The isolated operator CLI accepts `--delete-analysis-pilot-result <original request UUID> --confirm-vendor-deletion`. Use it only for one explicitly approved completed synthetic request after retaining the needed evaluation evidence. The original recorded route revision, existing project, approved digest and stored handle must all match before the operation is reserved. A later model switch or Off does not change that destination. There is no customer-facing route or automatic deletion job.

The separate PostgreSQL receipt records the operator and authorization time before transport. The command sends at most one DELETE. One authenticated read must first prove the completed resource ID, frozen model and original operation reference. OpenAI checks response metadata. Gemini retrieves the stored user input and checks its opaque operation suffix. A Gemini fixture created without signed completion recovery configured has no such suffix and cannot be deleted through this command. Missing or conflicting evidence leaves either attempt unconfirmed without a DELETE. OpenAI confirmation then requires the original response ID and deleted receipt. Gemini confirmation requires the addressed API's successful deletion status. An ambiguous response, a mismatched receipt or 404 leaves the original attempt unconfirmed and does not permit a second DELETE. The local result, metering and credit journal remain immutable. The receipt concerns the addressed result resource; vendor-wide retention and legal erasure need separate evidence. See [OpenAI response deletion](https://developers.openai.com/api/reference/resources/responses/methods/delete) and [Gemini background interaction deletion](https://aistudio.google.com/docs/background-execution).

One isolated PostgreSQL test used closed transports for both providers. A changed project and a pending request refused before dispatch. Concurrent calls used one DELETE for the same request. Both matching acknowledgements were recorded; a different OpenAI receipt ID and Gemini 404 remained unconfirmed. The test used the service migration and compiled model. The first seed was refused by the pending-row constraint. The next run encountered the terminal-result immutability guard, which remains in place; the final design stores receipts in a separate table. No live vendor request or cost occurred. The expanded test reproduced a confirmed deletion from a contradictory OpenAI operation before the fix. The corrected path made one read and no deletion or second attempt for that result. The Gemini regression additionally reproduced deletion for a foreign stored operation before the fix. The corrected test rejects both foreign and absent Gemini references after one read each and preserves the matching-receipt case. The test is included in the isolated analysis workflow. Provider-side deletion on an approved disposable result remains open.

### Server command alternative

Supply each dedicated provider project reference with `Analysis__ProviderPilot__openai__Project` or `Analysis__ProviderPilot__gemini__Project`. Inject the corresponding `Analysis__ProviderPilot__openai__ApiKey` or `Analysis__ProviderPilot__gemini__ApiKey` through the private process configuration. The OpenAI profile sends the configured project header by default. Set `Analysis__ProviderPilot__openai__SendProjectHeader=false` only for a key already scoped to the dedicated pilot project, and keep its separate project reference configured. Keep credentials outside Git, command arguments and logs. Keep the original project profiles available for pending requests.

Run the following commands against the same isolated service configuration. Each successful command records a new selection in PostgreSQL under the local operator identity and exits without starting the listener or contacting a vendor.

```powershell
dotnet Vafadar.Zanance.Api.dll --list-analysis-models
dotnet Vafadar.Zanance.Api.dll --select-analysis-model report gpt-6-luna
dotnet Vafadar.Zanance.Api.dll --select-analysis-model report gemini-3.5-flash-lite
dotnet Vafadar.Zanance.Api.dll --select-analysis-model comparison gpt-6.2-sol
dotnet Vafadar.Zanance.Api.dll --select-analysis-model comparison gemini-4-hydrogen
dotnet Vafadar.Zanance.Api.dll --select-analysis-model report off
dotnet Vafadar.Zanance.Api.dll --select-analysis-model comparison off
```

The latest selection for each purpose governs its new offers. A switch invalidates unaccepted offers from older selections for that purpose. Pending requests retain the original provider, model and project revision. `off` stops new offers for the selected purpose while the original pending requests with known handles can still be checked. A pending request without a recorded handle remains pending for investigation. Selecting a model never resends an existing request or routes it to a different provider. Existing provider-only selections and the legacy `--select-analysis-provider` command continue to address the original report profiles.

The pilot admits only explicitly allowlisted synthetic payloads for each purpose. Reports use low reasoning with a 512-token output limit. Comparisons use medium reasoning, with a 1024-token output limit for Sol and 4096 for Hydrogen. The quota counts both purposes toward 20 admissions per provider per pilot database and uses only gifted test units. Database recreation resets this count. There is no public route for selecting providers.

## Verification boundary

The isolated PostgreSQL exercise covered persistence across store reconstruction, switch in both directions and Off, stale offers, rejection of an altered payload, original-handle status requests, captured usage and an unacknowledged create with no replay. It used a deterministic fake HTTP transport. The administrator command was also exercised against that database with fictitious configuration. Provider observations above came from direct synthetic requests, so live provider transport through the hosted API and mobile workflow remains to be qualified.

Before real-data activation, complete the recovery, privacy, budget and evaluation gates in the [provider plan](27-ai-openai-gemini-provider-plan.md).

## Lower-cost model comparison, 2026-11-19

Each candidate received one identical synthetic request through its provider's original endpoint. The request used foreground mode, low reasoning, a 512-token output limit, structured JSON, and disabled optional response storage. It checked a period total, separate EUR and USD totals excluding transfers and unpaid plans, refusal to combine currencies without a rate, and four cash-flow timeline results. The expected tuple was `(38000, 21500, 7000, false, 8, -3000, 18000, 3000)`. All four responses completed with the requested model ID and matched all eight fields.

| Model | Standard USD per million text tokens, input / output | Observed input / billable-equivalent output tokens | Estimated standard USD per request | Wall time, one sample |
|---|---:|---:|---:|---:|
| `gpt-6-luna` | 0.10 / 0.50 | 354 / 190 | 0.0001304 | 4.54 s |
| `gpt-6.1-terra` | 1.00 / 5.00 | 354 / 116 | 0.0009340 | 4.60 s |
| `gemini-3.1-flash-lite` | 0.25 / 1.50 | 284 / 247 | 0.0004415 | 1.77 s |
| `gemini-3.5-flash-lite` | 0.30 / 2.50 | 284 / 120 | 0.0003852 | 1.40 s |

The two OpenAI calls total an estimated USD 0.0010644. Both Gemini calls used the existing Free Tier project. The Gemini estimates show the paid Standard equivalent, with thinking tokens included. OpenAI output totals already include reasoning tokens. No invoice was checked. At the same published Standard rates the earlier pilot models cost USD 2.00 / 10.00 (`gpt-6.2-sol`) and USD 1.50 / 7.50 (`gemini-4-hydrogen`) per million input / output tokens. Recheck live public tariffs and report both rates and observed usage in future evaluations. See [OpenAI pricing](https://developers.openai.com/api/docs/pricing) and [Gemini pricing](https://aistudio.google.com/docs/pricing).

Luna and Gemini 3.5 Flash-Lite are candidates for explaining totals already calculated by Zanance. Keep higher-capability models as candidates for a separate financial scenario evaluation. This single English sample is too small to qualify financial advice, output quality across languages, sustained latency, or production routing. No product model selection was changed.

## Per-purpose routing verification, 2026-11-19

The isolated service selected both model choices for each purpose and switched them independently. The comparison fixture spans two months and two currencies. A valid answer must report EUR net spending change of −6000 and net cash flow change of +7000, USD net spending change of +5000 and net cash flow change of −3000, and reject a combined total because no exchange rate was supplied. Each accepted response also includes a validated English explanation. This is a retrospective comparison of synthetic figures and does not qualify personalized financial advice.

The PostgreSQL fixture exercised the existing request store with the real network adapters. It verified that switching reports retires the earlier report quote while leaving the earlier comparison quote available, that modified synthetic inputs and models selected for the wrong purpose are refused, and that already admitted work remains pinned after switching or disabling both routes. The operator commands were also exercised on the isolated service with placeholder credentials and no egress. This test used the server request store directly. It did not exercise an Android or Windows client screen.

| Purpose and model | Standard USD per million text tokens, input / output | Observed input / billable output tokens | Estimated standard USD |
|---|---:|---:|---:|
| Report on GPT-6 Luna | 0.10 / 0.50 | 92 / 76 | 0.0000472 |
| Report on Gemini 3.5 Flash-Lite | 0.30 / 2.50 | 62 / 55 | 0.0001561 |
| Comparison on GPT-6.2 Sol | 2.00 / 10.00 | 295 / 182 | 0.0024100 |
| Comparison on Gemini 4 Hydrogen, adjusted limit | 1.50 / 7.50 | 538 / 1090 | 0.0089820 |

The first Hydrogen comparison exhausted the 1024-token limit and ended `incomplete`, with 538 input, 3 visible-output and 1021 thinking tokens. It was never accepted as an answer. Its paid Standard equivalent is USD 0.0084870. A focused synthetic test using the adjusted 4096-token limit completed through the original adapter after disabling its route. The three successful cases were not replayed. The two OpenAI estimates total USD 0.0024572 against the existing USD 5 allowance, and Gemini used the existing Free Tier project. These are usage-based estimates with thinking included, not invoice amounts. See [OpenAI pricing](https://developers.openai.com/api/docs/pricing) and [Gemini pricing](https://aistudio.google.com/docs/pricing).

## Application flow and period shortcuts, 2026-11-19

The original Analysis command methods and connected app client ran against isolated SQLite and PostgreSQL, using the real local TLS listener, certificate pin, maintained sign-in, protected test journal and original live vendor adapters. Both tests used explicit simulated platform confirmation, checked cancellation before creating a request, then completed one distinct request with the selected route. Follow-up could resolve the original request after its route was disabled. The local financial snapshot was unchanged in both cases. This portion exercised the shipping application logic linked into the host test, with a fixture endpoint and simulated platform confirmation.

- Gemini 3.5 Flash-Lite completed the report with 62 input and 30 billable output tokens. Its Standard rates are USD 0.30 / 2.50 per million tokens. The USD 0.0000936 estimate is the paid equivalent. The test used the existing Free Tier project.
- GPT-6.2 Sol completed the comparison with 299 input and 207 billable output tokens. Its Standard rates are USD 2.00 / 10.00 per million tokens. The USD 0.002668 estimate is below the existing USD 5 test allowance.

The app compares the equally long period immediately before the selected dates. For November 1–30 it uses October 2–31. The server pilot accepts that exact synthetic scope alongside the original server-only fixture. The app requests purpose-aware availability while the legacy availability response keeps its original shape. A comparison can obtain its own quote while the report route is off.

Analysis preserves the integrated service destination across language changes when only the
comparison route is available. Quote eligibility uses the purpose in the reviewed payload.
Four synthetic host cases cover comparison only, explanation only, both routes off and
the older availability response without a comparison flag. The latter uses the existing
shared availability behavior. The comparison-only case previously showed an unavailable
service after switching to German. There was no new provider lookup on language change
and no real model request, credit usage or financial posting. A separate Windows app
review observed the same native destination text before and after the language change,
with one metadata load and equal financial source fingerprints. Evidence is retained in
`artifacts/ai-purpose-availability`.

The Home shortcuts and the Reports period overview now open Analysis with the original local file, date range and account selection. Reports carries its report currency/account scope and confirmed-only choice. Home carries the accounts included in its period totals. Its shortcuts remain accessible with the default home layout, which hides the optional period chart. The comparison shortcut additionally selects the existing preceding-period option. Back returns to the source page. Neither shortcut reviews, checks providers or sends data automatically. This first scope explains period aggregates. The detailed Reports actions are described below.

Four focused host cases passed for prefill, local-only review and refusal of a different file or missing account. The independent Windows review used the actual native Invoke pattern for all four Home and Reports actions, verified the selected scope and purpose on the original Analysis screen, and returned with the financial source unchanged and zero provider calls. The initial Windows control was hidden beneath Home's optional chart in the default layout. The verified version moved both actions to a separate visible card. No successful live request was replayed for native verification.

Strict Windows review and Android development builds succeeded with no warnings or errors. The Android build was not installed for this check.


## Spending changes and planned-versus-actual reports, 2026-11-19

The spending-change action starts from the top-level categories in the visible Reports comparison. It uses the report's exact current and preceding financial-calendar periods, including their alignment in a partial current period. Values are net spending after refunds for one selected currency. The plan action starts from the financial-month commitments report. It includes known planned amounts, amounts paid for settled occurrences, settled/open/unknown counts, and a variance only when the settled planned amounts are known. An open or unknown item is never counted as a saving. Rows keep their currencies and kinds, and there is no cross-currency or cross-kind rollup.

Both actions use the existing preview and native confirmation. Analysis keeps the original report scope fixed and revalidates its read lease before preview, manual share and provider submission. Names are visible only in the device's review, indexed by disclosure order. The version 2 contract carries at most twelve anonymous rows. Version 1 period-only payloads keep their existing serialization and digest. The category action uses the manager's Financial comparisons selection and the plan action uses Simple reports.

The isolated application flow completed one real model request per new fixture. It exercised the application draft and private intent journal over pinned local TLS, including a retired-source refusal and cancellation before the approved send. The category fixture checked that a category rose by 10000 minor units and another fell by 16000. The plan fixture checked a settled overrun, a settled underrun, an open item and a settled item with an unknown planned amount. The provider adapter matched the declared structured answers before publishing the accompanying text. Both local financial snapshots remained unchanged.

| Report and model | Standard USD per million text tokens, input / output | Observed input / billable-equivalent output | Estimated charge | Paid Standard equivalent |
|---|---:|---:|---:|---:|
| Spending changes, GPT-6.2 Sol | 2.00 / 10.00 | 148 / 52 | USD 0.000816 | USD 0.000816 |
| Planned versus actual, Gemini 3.5 Flash-Lite | 0.30 / 2.50 | 151 / 117 | USD 0 (Free Tier) | USD 0.0003378 |

The independent Windows native review invoked both new Reports buttons and the original Review action, verified the resulting locked scopes and anonymous packets, and used zero provider calls. It exercised one changed category and one open planned item with an unchanged local financial fingerprint. No prior provider or native case was replayed.

The paid estimate uses reported usage, including reasoning or thinking in output, without invoice verification. The pilot remains restricted to exact fictitious fixtures. See [OpenAI model pricing](https://developers.openai.com/api/docs/models/gpt-6.2-sol) and [Gemini model pricing](https://aistudio.google.com/docs/pricing#gemini-3.5-flash-lite).


The plan comparison contract now checks the settled planned amount implied by the reported variance. It must be between zero and the full known planned amount. Where there is a settlement and every planned amount is known, the variance must be present. Four new synthetic cases reproduced acceptance of an impossible or missing variance and passed after correction. They also checked valid mixed open and settled amounts and an unknown-amount comparison. The original provider payload format is unchanged. No live calls or inference costs occurred.

## Budget and goal funding explanations, 2026-11-19

Budget explains the overspent overall or flexible limit first and, without one, the largest overspent category limit in the visible scope. It retains the original financial period, account filter, review choice, effective limit and signed carry. The comparison shows the original base limit and a local draft amount that would match the already recorded spending with the same carry. It does not forecast affordable spending. The existing editor opens for the exact budget.

The budget editor explains how many complete periods contributed to the historical net spending average, how the local rule rounded the limit, and how the result compares with the saved limit if present. This is a comparison with the saved limit, since prior unsaved suggestions are not historical records. The editable-draft action returns to the same editor with its existing draft and leaves its existing Use suggestion and Save actions explicit.

Monetary goal detail explains the target, the amount covered, the gap and uncovered earmarks, using the original funding priority calculation. If eligible contribution dates remain, the local engine supplies the required amount per date beside the planned contribution. Goals without remaining dates show the outstanding amount without a per-date promise. Quantity goals have different units and stay in their existing local screens. The draft action opens the original goal editor without updating an amount.

The new version 3 payload is locked to the original scope, one currency and a fixed type of funding evidence. Preview, draft navigation, share and submit use the original source validation and the existing explicit confirmation rules. User-defined names stay local. The isolated pilot only admits exact synthetic payloads under the existing Simple reports operator setting. No data or model is sent when opening a draft. Local captions were added in all six application languages. The live model fixtures used English.

| Fixture | Model | Standard USD per million input / output tokens | Observed input / billable output | Estimated charge | Paid equivalent |
|---|---|---:|---:|---:|---:|
| Budget limit, accepted | GPT-6 Luna | 0.10 / 0.50 | 147 / 190 | USD 0.0001097 | USD 0.0001097 |
| Budget suggestion, first response rejected | Gemini 3.5 Flash-Lite | 0.30 / 2.50 | 106 / 122 | USD 0 (Free Tier) | USD 0.0003368 |
| Budget suggestion, new corrected fixture | Gemini 3.5 Flash-Lite | 0.30 / 2.50 | 139 / 52 | USD 0 (Free Tier) | USD 0.0001717 |
| Monetary goal, accepted | Gemini 3.5 Flash-Lite | 0.30 / 2.50 | 153 / 87 | USD 0 (Free Tier) | USD 0.0002634 |

The first suggestion response multiplied the minor-unit amounts, so the service refused publication. Status of the same vendor operation confirmed completion and usage, with no repeat create for that case. The new fixture supplies exact local integers separately from prose. The first goal fixture setup collided with the previous synthetic credit grant before dispatch. Distinct grant identities resolved the setup failure. The accepted live cases exercised the application model, exact disclosure, retired-source and draft-entry checks, pinned local TLS and the private request journal. Their local financial fingerprints did not change. Prices are usage-based Standard estimates with no invoice verification. See [OpenAI](https://developers.openai.com/api/docs/models/gpt-6-luna) and [Gemini](https://aistudio.google.com/docs/pricing#gemini-3.5-flash-lite).


A focused local check covered signed carry, exact draft arithmetic, currency/scope refusal and a goal with no future contribution dates. The independent Windows fixture used original native buttons for the three explanations, Review, and Open editable draft. It observed the budget overrun, the same budget suggestion editor on return, and the original monetary goal editor, with an unchanged local financial fingerprint. The first suggestion navigation refused the all-accounts sentinel. The correction reuses the suggestion calculator's empty-scope meaning. The accepted budget-limit case was not repeated. An isolated WinUI title-bar error was recorded when closing the review window after the three receipts.


### Funding contract consistency, 2026-11-20

The shared validator checks that the effective budget limit equals the original base limit plus signed carry. It also requires the goal's per-date alternative to match the local goal calculator when dates remain and to be absent when no dates remain. In three new synthetic regression cases, the original validator admitted an inconsistent effective limit, a rounded-down contribution, and a missing alternative despite available dates. All three passed after correction. Each case also validated its consistent counterpart. This verification made no provider requests and incurred no inference cost. Existing live pilot results above were retained.

The shared validator also requires a disclosed budget suggestion to match the budget calculator's original rounding for the disclosed currency. Three new synthetic cases exposed acceptance of a high and a low EUR suggestion and a JPY suggestion rounded as if fractional units existed. Each case now rejects the inconsistent amount and accepts its locally calculated counterpart. Strict API and Windows application builds passed. The local Android build stopped before application verification when the notification dependency build could not launch `pwsh` (exit 9009). The payload format and earlier live pilot evidence stay intact. The focused verification made no provider requests and incurred no inference cost.

## Forecast and plan scenario explanations, 2026-11-19

Each eligible Forecast currency card opens the existing Analysis preview with its actual payment-account scope, as-of date, horizon and projected end-of-day path. It shows the first negative day and balance separately from the absolute minimum. Relevant outgoing and unknown events are shown with signed amounts or unknown status, including classifications for overdue, estimated, moved, changed-amount and excluded items. The selected what-if is compared with a fresh baseline calculated from the same frozen original inputs. Unknown amounts do not change the numeric path and remain visible even if that path has no negative day. The bounded version 4 input has no prior ledger period and includes up to twelve anonymous events per path. Original event names stay in the local review. Back returns to the existing Forecast with its original scenario and row actions.

The exact original file, finance source, advanced-forecast right, selected date and calendar are rechecked before preview or transmission. Analysis does not change a plan, create a payment or save a forecast. The synthetic pilot uses the existing Financial comparisons selection. It allows the exact reference and comparison fixtures only. Local captions are available in all six app languages; the live fixtures used English.

The focused calculator/protocol check distinguishes a first negative balance of −5000 from the later minimum of −11000. Moving the earlier expense behind planned income and excluding another brings the modeled closing balance from 18000 to 24000, and removes the negative path while one amount remains unknown. The native Windows fixture verified the original Forecast button and local preview in both states with a frozen currency and no changes to the financial fingerprint. The live app-model checks passed via the pinned isolated service with one baseline request and one comparison per provider. Previously accepted report and funding flows were not replayed.

| Fixture | Model | Standard USD per million input / output | Observed input / billable output tokens | Estimated charge | Paid equivalent |
|---|---|---:|---:|---:|---:|
| Baseline | GPT-6.2 Sol | 2.00 / 10.00 | 161 / 90 | USD 0.001222 | USD 0.001222 |
| Selected scenario | GPT-6.2 Sol | 2.00 / 10.00 | 201 / 97 | USD 0.001372 | USD 0.001372 |
| Selected scenario | Gemini 4 Hydrogen | 1.50 / 7.50 | 387 / 465 | USD 0 (Free Tier) | USD 0.004068 |

The usage estimates include reasoning or thinking in output. No invoice was checked. See [OpenAI](https://developers.openai.com/api/docs/models/gpt-6.2-sol) and [Gemini](https://aistudio.google.com/docs/pricing#gemini-4-hydrogen).


### Forecast contract consistency, 2026-11-20

For complete event lists, the shared validator checks the closing balance, dated minimum, first negative day and balance, and count of unknown active effects against the supplied path events. It groups all effects for the same day before testing the balance. Excluded events do not affect balances or the unknown count. Bounded excerpts still receive the original structural validation because omitted events can affect the path. Five new synthetic cases admitted contradictory summaries before the change and refused them after it. The focused local pilot contract also checks the original synthetic fixture and refusal of an otherwise valid payload outside the exact pilot fixtures. The local checks used no vendor requests and generated no inference charges.

## Questions about selected data, 2026-11-19

The Analysis page interprets two bounded questions locally in the six app languages: what drove a change in spending, and which plans contribute to a projected shortfall. An unsupported question, multiple currencies or mismatched account scope requires clarification before a preview. A recognized question shows its source and date/account scope for review. The original words, account names and contributing category or plan labels stay local.

Spending uses the same net expenses and refunds as Reports and compares two equally long periods. The forecast uses the existing daily calculation and projects the original plans and recorded entries for one payment currency. It separates unknown amounts in the interval from unknown amounts before its opening estimate. A next-month question uses the file's financial month and current calendar. The app rechecks the exact source and scope before sharing or submitting, and makes no ledger or plan changes.

Eight focused synthetic cases passed, covering all six languages, mixed scope, source retirement and a custom financial month. The isolated WinUI page used the original native entry, Interpret and Review for both intents. A forecast observation initially missed the preview. Waiting for the native text binding and operation completion resolved the observation without replaying the accepted spending case. Both native receipts report an unchanged financial fingerprint and zero provider calls.

The isolated connected fixture sent one new spending request to OpenAI and one new planning request to Gemini through the existing Financial comparisons route. OpenAI completed in the app flow. The Gemini operation was still in progress when the initial app check ended. A later read of the same operation observed completion and the original authenticated, pinned status endpoint published that result under the same request and unchanged vendor handle. There was no second submission. Neither provider received any real data.

| Synthetic fixture | Model | Standard USD per million input / output | Observed input / billable output | Estimated charge | Paid equivalent |
|---|---|---:|---:|---:|---:|
| Spending change | GPT-6.2 Sol | 2.00 / 10.00 | 141 / 49 | USD 0.000772 | USD 0.000772 |
| Next-month shortfall | Gemini 4 Hydrogen | 1.50 / 7.50 | 304 / 636 | USD 0 (Free Tier) | USD 0.005226 |

Output includes thinking tokens. This cost estimate uses measured usage and published text prices without an invoice comparison. [OpenAI pricing](https://developers.openai.com/api/docs/models/gpt-6.2-sol) and [Gemini pricing](https://aistudio.google.com/docs/pricing#gemini-4-hydrogen). The pilot matches the two exact synthetic payloads. General vendor activation and real-data acceptance remain separate.

The bounded question parser asks for clarification when the text contains a number. Written dates and relative day counts used to fall through to the selected period. Four new synthetic cases used an ISO date range, Persian digits, a relative day count and full-width digits. The original parser recognized an intent for all four while ignoring the numeric qualifier. The corrected app keeps the selected dates, shows the existing clarification and creates no disclosure. The financial snapshot matched before and after each case. No provider was contacted.

Currency recognition normalizes distinct ISO codes such as `usd` and `eUr`.
Codes that are also ordinary words (GEL, MAD, RUB, TRY) retain the uppercase rule.
Two different codes require clarification even when their case differs. A parsed
currency still has to match the selected account before the app creates a preview.
The original application test accepted an EUR preview with a lowercase USD question.
The corrected case refused it and preserved the synthetic financial snapshot.
The focused question suite passed 21 cases with no provider calls. An isolated Windows
review displayed the scope warning on the original Analysis page for the same mismatch.
The first native observer set the platform text directly without proving the managed
binding. The corrected observer used the original bound entry and the same synthetic
account. The financial fingerprint stayed equal and no disclosure was produced.
Evidence is under `artifacts/ai-question-currency-scope`.

The signed offline Release APK for revision `3705dd77` passed a separate Android 16
(API 36 x86-64, 4096-byte pages) review. The installed APK matched SHA-256
`ae6a1bc50397b9c7a62c265128bd0f1ca2f4301336b34417d144623d5484fa5b`.
One fresh zero-opening account in EUR was selected. The native entry accepted a
lowercase `usd` question, Interpret displayed the requested code USD, and Review
displayed the account-scope warning. The upper and lower Analysis viewports showed
no outgoing preview. The native provider check stayed disabled and Transactions
remained empty. The first entry observer expected just the typed value, while
Android exposed the caption and value together. The observer continued with the same
input and did not repeat setup or text entry. The disposable package and emulator
were retired. Native captures and the receipt are under `artifacts/ai-question-android36`.

Interpret now owns the question text captured at entry. In a focused synthetic application test, the first scope notification replaced a spending question with a next-month forecast question. The original command previously applied the replacement and changed the period. It now leaves the replacement unprocessed until a separate Interpret action. That action applied the next-month scope without creating a disclosure, contacting a provider, or changing the financial source.

## Six-language empty and comparison corpus

The synthetic adapter admits twelve additional exact payloads covering the six app languages. Each language has an empty reporting period and an equal-length comparison with both EUR and USD, refunds and income reversals. The earlier English fixtures keep their original scope. Only exact approved digests select the new prompts. The controlled prompt sets the requested prose language and forbids digits in prose, recommendations and unsupported currency conversion. The local totals supply the exact structured fields. For the comparison, the expected spending changes are 0 EUR and 2000 USD in minor units, cash-flow changes are -12000 EUR and 5000 USD, and a combined amount is unavailable.

The local PostgreSQL integration test used the real request store, operator routes and response validator with both synthetic transports. All 24 model/fixture pairs preserved the provided localized text and exact structured fields. Changing the source refused eligibility, and a response claiming that currencies can be combined remained pending without publication. This validates the app and adapter contract with fake provider responses. It does not qualify provider-generated prose, latency, refusal rates or production results. No live calls or charges occurred.

A full live run would use twelve new requests per provider. Under the existing conservative uncached per-request bounds, the maximum total estimates are USD 0.088782 for OpenAI and USD 0.214122 at Gemini paid reference rates. Actual consumption and Free Tier treatment need separate evidence from the dedicated pilot projects. Rates rechecked 2026-11-20. See [OpenAI pricing](https://developers.openai.com/api/docs/pricing) and [Gemini pricing](https://aistudio.google.com/docs/pricing).


### Live six-language results, 2026-11-20

The opt-in `AiPilotLiveLocaleTests` ran the source-linked request store against isolated PostgreSQL and both dedicated provider projects. It sent each of the 24 model/locale pairs once, exclusively using the exact fictional corpus. Each request had its own original vendor reference. The same reference was used for follow-up. There were 24 local synthetic holds and 22 settlements. No prior successful case was repeated.

| Model | Accepted / sent | Metered input / billable output tokens | Observed p50 / p95, seconds | Metered amount at Standard rates, USD |
|---|---:|---:|---:|---:|
| GPT-6 Luna | 6 / 6 | 660 / 360 | 35.25 / 38.35 | 0.000247 |
| GPT-6.2 Sol | 6 / 6 | 1872 / 620 | 28.57 / 48.80 | 0.009944 |
| Gemini 3.5 Flash-Lite | 6 / 6 | 426 / 183 | 38.28 / 39.30 | 0.000588 |
| Gemini 4 Hydrogen | 4 / 6 | 2292 / 2806 | 43.18 / 55.02 | 0.024484 |

Timings cover admission through the first locally observed accepted result, including the polling delay. The sample uses the nearest-rank p95, which is the maximum accepted time in each cohort of four or six. The two failed cases are excluded from those latency estimates. The Standard estimates are rounded up to whole microdollars per request. Metered calls total USD 0.010191 for OpenAI and USD 0.025072 at equivalent paid Gemini rates. The isolated Gemini project used Free Tier. Neither invoice nor usage for the two failures was established. See [OpenAI Luna](https://developers.openai.com/api/docs/models/gpt-6-luna), [OpenAI Sol](https://developers.openai.com/api/docs/models/gpt-6.2-sol), [Gemini Flash-Lite](https://aistudio.google.com/docs/pricing#gemini-3.5-flash-lite) and [Gemini Hydrogen](https://aistudio.google.com/docs/pricing#gemini-4-hydrogen).

Direct read-only diagnostics after the observer ended confirmed HTTP 200, matching original references and model, and `failed` without usage for both Persian and Italian Hydrogen comparisons. No recognized error category explained the cause. Those two cases remained locally pending with the original holds and no output. The observer's one passing test records evidence collection. The model-level acceptance was 22 of 24. The isolated fixture was retained for reconciliation without new creation. The later isolated PostgreSQL regression replayed the failure shape with a fake transport, rejected a foreign reference and a conflicting completion, and preserved the hold. A separate authenticated panel test showed Failed with no vendor estimate and no provider traffic.

### Restored multilingual pilot archive

The retained PostgreSQL archive from the original live synthetic corpus was restored once into a separate loopback database and migrated to the current service schema. The test used a scoped copy of the original fictional Data Protection key ring. All 24 original requests, frozen models, bound vendor references and protected aggregates matched the retained manifest. All 22 protected results and their metering matched the terminal receipts. The Persian and Italian Hydrogen holds remained pending with no result or inferred usage. There were 24 original reservations, 22 settlements and zero refunds.

The original HTTPS readiness endpoint returned 200 after migration. With local analysis and the provider pilot disabled, both Analysis availability and the management panel returned 404. A refusing outbound client factory observed zero calls. The rehearsal does not authorize enabling a restored pilot or substituting its held state for a vendor invoice. The isolated database and copied key file were retired, and the original archive checksum and live fixture were preserved.

All accepted replies passed the exact structured checks. Manual review found the requested languages and no unsupported claims or cross-currency additions in those texts. The French Flash-Lite reply omitted French accent marks. This corpus covers the simple empty and financial period-comparison contracts. It does not qualify the full set of feature-specific tasks, sustained latency, customer use or real-data activation.
