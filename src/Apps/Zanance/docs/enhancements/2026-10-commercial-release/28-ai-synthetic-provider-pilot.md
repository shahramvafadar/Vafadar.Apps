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

### Original dispatch recovery

An accepted synthetic request records a queued create in the same transaction as consent, the vendor cost ceiling and the single credit reservation. The original submission path and the background drain race on a conditional PostgreSQL update. Only the winner can send the original create. It records the durable claim and UTC time before touching the transport, and keeps the original route even when that model is subsequently switched Off for new work. A normal service restart can pick up a never-claimed request. A claimed create is never recovered by reissuing it, even if its receipt is lost. The user status path observes the original operation only, and earlier rows without a dispatch stage have no queue authority. Keep the pilot disabled when opening an old backup unless its outbound history has been reconciled independently.

Two isolated store instances sharing PostgreSQL competed for an unclaimed request after reconstruction. The only synthetic POST observed its committed claim before the simulated lost receipt. An already claimed request and a previous-format pending request did not dispatch. A subsequent drain after advancing the clock by 30 days made no extra call or credit movement. The first test seed lacked the original gift grant and the existing wallet invariant refused it; the corrected fixture includes the grant and all three reservations.

The general identity workflow formerly treated opt-in provider tests without their fixture as failures. The fake-provider tests now belong to the isolated analysis workflow and each receives its own fresh fictional database. The live test remains explicitly opt-in. The [repaired identity workflow](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/44634484329) completed successfully, including its general identity job and isolated fake-provider step. Four pre-fix identity jobs reported respectively three, four, five and six opt-in skips counted as failures, and no other failures in those jobs. The [fourth job log](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/44633558080/job/135946227251) includes the cost case. See the [first](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/44631663207/job/135939590610), [second](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/44632280421/job/135941760440) and [third](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/44633009171/job/135944295913) job logs.

### OpenAI lost-receipt recovery

The isolated pilot exposes the HTTPS completion receiver at **/analysis/pilot/openai/completions** only when the existing pilot is enabled and **Analysis__ProviderPilot__openai__WebhookSecret** is supplied privately. The approved dashboard event is **response.completed**. This code does not create a public endpoint or a vendor subscription. The receiver authenticates the exact body and the signed delivery headers with the Standard Webhooks HMAC format. It bounds the timestamp window to five minutes, the body to 8 KiB and the inbox to 128 notices. It records only stable delivery and response IDs and lookup metadata. Duplicate delivery IDs or response IDs acknowledge the original inbox entry without storing another.

The background worker can make up to three authenticated GETs for a receipt with a minimum one-minute gap. It uses the dedicated OpenAI project. To bind the response, the retrieved ID, completion state, opaque operation metadata, frozen project revision and exact model must match an original claimed synthetic request without a saved vendor ID. The original result validator and one-time credit settlement apply. The callback itself never supplies the result or tells the service to repeat a create. Foreign, duplicate and already known notices cannot settle a second request. An unmatched, unavailable or exhausted notice does not release credit.

One synthetic HTTPS/PostgreSQL test refused an altered body and an expired timestamp, coalesced repeated deliveries and responses, and ran concurrent inbox drains. The fake transport performed two authenticated reads: one matching result recovered its original request and settled once, while the other response could not bind. A fresh duplicate notice after the signature window caused no additional read. There were no real vendor calls or changes to a provider project. Public webhook delivery and secret rotation on the provider remain open. See [OpenAI webhooks](https://developers.openai.com/api/docs/guides/webhooks).

### Gemini lost-receipt recovery

To stage recovery for the isolated Gemini project, use a dedicated static webhook secret configured as **Analysis__ProviderPilot__gemini__WebhookSecret**, with the receiver **/analysis/pilot/gemini/completions** and event **interaction.completed**. This entry point remains absent when its setting is unset. It uses the same bounded raw-body HMAC authentication, durable inbox and bounded GETs as the OpenAI receiver. Delivery and interaction identifiers are deduplicated within their provider namespace. The additive migration backfills all existing inbox rows as OpenAI without removing receipts. A separate upgrade probe preserved the original receipt, lookup count and completion marker, and admitted the same delivery and response IDs in the Gemini namespace. The first schema fixture used the default constraint name instead of the original named constraint; correcting the fixture left the shipped migration unchanged.

Only in that configured synthetic mode, the original Gemini create appends an opaque operation reference to its fixed fictional input. No account identity or ledger data is in that reference. A notice triggers authenticated retrieval of the original interaction with its stored input. Exactly one text block in the completed original user-input step must end with the expected operation reference. A model-output echo or the callback body itself supplies no binding authority. The original project revision, model, synthetic digest and existing result validator still apply. No dynamic webhook or public project changes were made.

The isolated HTTPS and PostgreSQL test used the original create path and a fake transport. It observed one create with the unique input reference and a simulated lost acknowledgement. The two authenticated reads qualified one original result and refused a foreign reference. Invalid, stale and replayed notifications did not create another lookup or settlement. The directly affected OpenAI case was checked separately on the shared inbox. No real vendor calls were made. Live static webhook delivery, exact provider response conformance, signing-secret rotation and release acceptance remain open. See [Gemini static webhook signatures](https://aistudio.google.com/docs/webhooks#verify-static-signatures), [stored interaction steps](https://aistudio.google.com/docs/interactions-overview#how-it-works) and [Google SDK input retrieval](https://github.com/googleapis/python-genai/blob/main/google/genai/_gaos/interactions.py).

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
