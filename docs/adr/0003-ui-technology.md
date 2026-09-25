# 0003. .NET MAUI XAML + MVVM by default; Blazor Hybrid when a web version is planned

- Status: Accepted
- Date: 2026-09-25

## Context

Most apps are mobile apps (Android first, then iOS; Windows is useful for personal use). A few ideas will also need
a web version. .NET MAUI offers two UI models: native XAML, or Blazor Hybrid (Razor components in a web view) which
can share UI code with a Blazor web app. A Syncfusion license covers both MAUI and Blazor controls.

## Decision

* **Default: .NET MAUI with XAML** and MVVM (CommunityToolkit.Mvvm), compiled bindings and XAML source generation,
  Syncfusion MAUI controls. Best native look, performance and platform integration.
* **When an app is planned for web and mobile from the start**, consider **MAUI Blazor Hybrid + Blazor Web App**
  sharing a Razor class library, with Syncfusion Blazor controls. This is decided per app, in that app's docs.
* Business logic always lives in `Vafadar.<App>.Core`, so a web front end can be added later even to a XAML app.

## Consequences

* Two UI stacks may exist in the repository; shared infrastructure is split accordingly (`Vafadar.Maui` for XAML,
  a future `Vafadar.Web` for Blazor), while localization, data and backup are UI-independent.
* XAML apps need a separate web UI if a web version is added later; the domain and use cases are reused.
