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

The pilot admits one fixed synthetic payload per purpose. Reports use low reasoning with a 512-token output limit. Comparisons use medium reasoning, with a 1024-token output limit for Sol and 4096 for Hydrogen. The quota counts both purposes toward 20 admissions per provider per pilot database and uses only gifted test units. Database recreation resets this count. There is no public route for selecting providers.

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
