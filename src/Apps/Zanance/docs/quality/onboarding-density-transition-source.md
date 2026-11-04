# Saved onboarding density-transition source correlation (D-819)

4 November2026. Continue the new D818 restoration-width finding by reading its saved
packets and public dependency source. No Activity, provider, installed package, AVD,
OS setting, crash record, authentication or financial operation is invoked again.
The retired D818 callers remain retired and D809 userdata/system record stay preserved.

## Saved evidence and original app policy

The four saved restoration packets belong to UID10222/PID9258 and72 completed contexts.
Three contain no attached caption yet; the fourth contains the settled native caption.
The first local audit incorrectly assumes one packet and refuses before producing a
result. Preserve it, account for all four, and correlate only the existing terminal
caption. No new device capture or favorable-frame selection occurs.

The caption is fully visible at[180,101,900,143], width720px; body/footer span[260,820],
width560px. Original ReadableWidth.Max is720 units; original onboarding caps its body
and footer at560 units. Both observed widths match those limits at scale1. The source
Secondary style has font size13 and the same packet reports34.125px, a ratio2.625.
The restored recorded density420dpi implies420/160=2.625 and physical1080px would be
411.428571dp at that scale. The native packet records no actual Page.Width, current
Context.DisplayMetrics or cached conversion density. These calculations establish
consistency, not exclusive cause or a read of those missing runtime values.

Original MainActivity declares ConfigChanges.Density and handles configuration changes
through the platform base, localization, widget refresh and system-bar icons. Do not
infer that its source alone updates every framework metric cache. The original sizing
rules and activity source remain unchanged; source hashes and the saved packet digest
are retained with the arithmetic correlation.

## Versioned public-source premise

The configured Controls package is10.0.120. Resolve its official tag to immutable
commit`6aca5366139ed0e1cd05cca3e32b6a887d5a8dd0` and retain the original response/source.
In [that ContextExtensions source](https://github.com/dotnet/maui/blob/6aca5366139ed0e1cd05cca3e32b6a887d5a8dd0/src/Core/src/Platform/Android/ContextExtensions.cs),
EnsureMetrics populates one static display-density field on first use, then returns
before consulting a later Context. FromPixels divides by that cache; ToPixels multiplies
by it. The file contains its initializer and one population assignment, without another
reset there. This is a concrete public-source premise compatible with the scale1 widths
after changing from160dpi to420dpi; the process's actual field value is not observed.

The retained source is14590bytes, SHA256
`f6f40cd55e7ad8908fae52c48f0a9695693f95dc7adb2e35c802853e0ef75e08`.
Matching the declared package version is not installed-binary/source identity or proof
of the precise original execution path. No dependency upgrade, reflection/private-field
reset, runtime patch, forced process restart, new display change or production workaround
is attempted from this premise. A distinct future transition check must capture current
native metrics and public conversion values together, with a new fictitious caller and
its own before/after restoration; it must not relaunch the retired caller to rescue D818.

The108 named D818 OS200% caption contexts retain their exact within-cohort width proof.
The extra normal-font restoration frame still does not qualify complete live-window
restoration. Original native fault/R13 unknown, whole performance/security/accessibility/
platform/provider/release gates remain open. No product test/build/financial success,
cleanup or frozen action repeats. This is source admission and saved-evidence arithmetic,
not a new native test, defect repair or overall acceptance. The overall goal stays active.

Related: [D818 finite contexts and restoration limits](onboarding-visible-step-counter.md),
[remaining frontier](remaining-delivery-evidence.md).

[D820 independent native transition](public-density-conversion-transition.md) subsequently
observes stale public conversion on a new inverse transition, with exact restoration.
It does not reread or upgrade the missing runtime proof from the retired D818 caller.
