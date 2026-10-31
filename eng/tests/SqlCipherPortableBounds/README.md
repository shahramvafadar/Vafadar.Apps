# Native SQLCipher portable-image budget checks

Windows-only independent checks call the original shared exporter and memory-image
reader against the actual source-built SQLCipher package. No exporter algorithm,
SQLite scalar function, profile, financial action or native error is simulated.

Run after preparing the reviewed Windows native package:

```powershell
dotnet run --project eng/tests/SqlCipherPortableBounds -c Release -p:ContinuousIntegrationBuild=true --artifacts-path <new-owned-build-directory> -- <new-owned-results-directory>
```

The new result directory must not exist. Seven finite cases cover missing/empty
omission policies, fitting and oversized ordinary packets, an oversized omitted
private journal, the existing nonempty-policy refusal and original cancellation.
Over-budget export must fail before portable preparation/output. Fitting output
uses the original reader and preserves signed values, Unicode, payload digest and
header identifiers, omitting only explicitly selected private rows. Source equality
and attachment release are checked after every outcome. Case JSON is preserved
before failure; no secret or original user data is consumed.

These are actual native boundary checks, not new xUnit cases, app UI acceptance,
peak-process-memory measurements or evidence of a general crash fix. The library
does not reference this host. Android calls the same check source from its separate
owned native fixture; platform results must remain separately labelled.

Use fresh build output for later local work: D701's original local bin/obj cleanup
was refused, so those exact retained output scopes must not be reused or retried.
The committed source and hosted clean-checkout CI are outside those local scopes.
