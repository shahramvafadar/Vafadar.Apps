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
