# 0011. Each app declares the languages of its texts

- Status: Accepted
- Date: 2026-11-15

## Context

Until now one list of languages applied to the whole repository: `AppLanguages.All` in `Vafadar.Localization`,
`SatelliteResourceLanguages` in the root `Directory.Build.props`, and a test that required every resource file under
`src/` to exist in all six languages ([ADR 0007](0007-localization.md)).

The second app, Dastan, is a website with its own texts and starts with English and Persian; German and others
follow later. Its translations are independent of Zanance's, and it does not use the shared texts of
`Vafadar.Localization`. With one repository-wide list, Dastan would have to ship four translations it does not offer,
or the completeness check would have to be switched off for it.

## Decision

* The languages of a project's texts are the MSBuild property **`VafadarLanguages`**, a list of culture names
  separated by semicolons. **English is first**: it stays the neutral language of every resource file.
* The root `Directory.Build.props` holds the default – the six languages of today – and derives
  `SatelliteResourceLanguages` from the property.
* An app with other languages sets the property in a `Directory.Build.props` in its own folder, **before** importing
  the file above it:

  ```xml
  <Project>
    <PropertyGroup>
      <VafadarLanguages>en;fa</VafadarLanguages>
    </PropertyGroup>
    <Import Project="$([MSBuild]::GetPathOfFileAbove('Directory.Build.props', '$(MSBuildThisFileDirectory)../'))" />
  </Project>
  ```

* `ResourceCompletenessTests` reads the declaration that applies to each neutral resource file (the nearest
  `Directory.Build.props` above it that has the property) and requires exactly those translations, with the same keys
  and placeholders. The reader is `ProjectLanguages` in `Vafadar.Testing`.
* The declaration is made in `Directory.Build.props` files only – not in project files and not under a condition –
  so that the build and the test read the same value without evaluating MSBuild in a test.
* The rule that a translation is a real translation, never a copy of the English text, is unchanged.

## Consequences

* Zanance and the shared libraries have no declaration of their own and keep the six languages; their build output
  and their test results are the same as before. A test keeps the default equal to `AppLanguages.All`.
* Adding a language to one app means: add it to that app's `VafadarLanguages` and add the translation files; the
  completeness test then lists what is missing. The other apps are not affected.
* A shared library that carries texts is translated into the default list. An app with fewer languages that used
  such a library would ship more translations than it offers; libraries written for several apps therefore carry no
  texts (`Vafadar.Web`, `Vafadar.Temporal`).
* An app's runtime list of offered languages should come from the same property, so that the build, the test and the
  running app cannot disagree. Dastan does this when its web project is created.
* `eng/scripts/Add-Strings.ps1` still writes the six languages of Zanance; it is a Zanance tool and unchanged.
