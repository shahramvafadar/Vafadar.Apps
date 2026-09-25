# 0007. Runtime-switchable localization; calendar independent of language

- Status: Accepted
- Date: 2026-09-25

## Context

Apps must be multilingual from the start (English, Persian, German), including right-to-left layout for Persian.
Persian users may prefer the Persian calendar, but some prefer Gregorian dates; German or English users may want
the Persian calendar too.

## Decision

* Standard **`.resx`** resources per project (English neutral, `*.fa.resx`, `*.de.resx`), keys `Area_Name`.
* A **`Translator`** singleton exposes strings through an indexer and raises a change notification when the
  language changes; XAML uses `{v:Translate Key}`. The language switches at runtime without restarting.
* **`LocalizationService`** persists the choice, applies process cultures and flow direction.
* The **calendar is a separate setting** (default follows the language); all dates are stored Gregorian and
  formatted through `IDateFormatter`.
* A test verifies that every translation has exactly the keys and placeholders of its neutral file.

## Consequences

* Adding a string requires editing all language files (enforced by the test).
* No dependency on third-party localization libraries; `.resx` is supported by Visual Studio's editor and
  translation tools.
* Web apps can reuse the same resources and services with `IStringLocalizer` adapters later.
