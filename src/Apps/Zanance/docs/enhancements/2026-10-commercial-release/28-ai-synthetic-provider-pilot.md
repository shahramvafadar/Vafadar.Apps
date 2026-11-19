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

Supply each dedicated provider project reference with `Analysis__ProviderPilot__openai__Project` or `Analysis__ProviderPilot__gemini__Project`. Inject the corresponding `Analysis__ProviderPilot__openai__ApiKey` or `Analysis__ProviderPilot__gemini__ApiKey` through the private process configuration. Keep credentials outside Git, command arguments and logs. Keep the original project profiles available for pending requests.

Run the following commands against the same isolated service configuration. Each successful command records a new selection in PostgreSQL under the local operator identity and exits without starting the listener or contacting a vendor.

```powershell
dotnet Vafadar.Zanance.Api.dll --select-analysis-provider openai
dotnet Vafadar.Zanance.Api.dll --select-analysis-provider gemini
dotnet Vafadar.Zanance.Api.dll --select-analysis-provider off
```

The latest selection governs new offers. A switch invalidates unaccepted offers from older selections, including earlier selections of the same provider. Pending requests retain the original provider, model and project revision. `off` stops new offers while the original pending requests with known handles can still be checked. A pending request without a recorded handle remains pending for investigation. Selecting a provider never resends an existing request or routes it to a different provider.

The pilot admits one fixed synthetic payload, bounds output to 512 tokens, caps admissions at 20 per provider per pilot database and uses only gifted test units. Database recreation resets this count. There is no public route for selecting providers.

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
