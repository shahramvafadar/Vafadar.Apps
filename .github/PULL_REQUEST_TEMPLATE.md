## Summary

<!-- What does this change and why? Link related issues (e.g. "Closes #12"). -->

## Affected apps / libraries

- [ ] Finance
- [ ] Shared libraries (`src/Libraries`)
- [ ] Build / CI / docs only

## Checklist

- [ ] Builds without warnings (`dotnet build Vafadar.Apps.slnx`)
- [ ] Tests pass (`dotnet test --solution Vafadar.Apps.slnx`) and new behavior is covered by tests
- [ ] New UI strings exist in **all** languages (`*.resx`, `*.fa.resx`, `*.de.resx`) and work right-to-left
- [ ] Database schema changes come with an EF Core migration
- [ ] No secrets, keys, personal data or signing material committed
- [ ] Docs updated (README, `docs/`, app `CHANGELOG.md`) where relevant
- [ ] Privacy matrix updated if the change collects, stores or sends new data (`docs/privacy/privacy-matrix.md`)
