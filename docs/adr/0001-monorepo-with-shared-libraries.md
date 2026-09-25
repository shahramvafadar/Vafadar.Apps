# 0001. One repository, one solution, shared libraries

- Status: Accepted
- Date: 2026-09-25

## Context

Many small apps will be built over time, mostly independent, sometimes sharing features (backup, localization,
authentication) or even data (web + mobile version of one idea). The alternatives were one repository per app with
shared code published as NuGet packages, or a single repository.

## Decision

* One public GitHub repository ([`shahramvafadar/Vafadar.Apps`](https://github.com/shahramvafadar/Vafadar.Apps)) with one solution, `Vafadar.Apps.slnx`.
* Shared code lives in `src/Libraries` and is referenced as **project references**, not packages.
* Each app lives in `src/Apps/<App>/` and never references another app.
* Solution filters (`Vafadar.<Product>.slnf`) open only what is needed for one product.
* Tests mirror the source tree under `test/`.

## Consequences

* A change in a shared library is immediately used – and tested – by all apps; there is no package publishing.
* Breaking changes in a library must be fixed in all apps in the same change (CI builds everything).
* CI time grows with the number of apps; solution filters and per-platform jobs keep it manageable.
* If an app ever needs a separate repository (e.g. a different owner), its folder can be extracted with history and
  the libraries it uses can be published as packages at that point.
