# Rejected startup preparation candidates (D-177)

The asynchronous preparation/appearance-gating candidates were not delivered. Their source,
resource strings, diagnostics and helper tests were removed after actual Release timing failed
to improve ready Home. Original D-175 startup, lock, templates and display-unit import are retained.
This record prevents a rejected experiment being counted as a delivered feature or scheduled again.

## Evidence

- D-176 process-cold/warm-cache ready Home observations: 18,670.44 / 14,981.63 / 13,107.65 ms.
- Async candidate: 30,400.00 / 26,496.94 / 25,622.67 ms. Observations include native-dump overhead.
- Consecutive same-fixture control: D-175 at 16,320.77 ms versus candidate at 28,819.26 ms.
- A preassigned Shell-content candidate had one bounded miss, then 36,443.51 / 30,860.54 ms.
- Original-template appearance gating reached Home at 27,834.85 ms, without demonstrated improvement.
- Debug-only phase evidence: worker start 123 ms, migration return 3,918, settings return 7,302,
  root construction 7,773, lock return 8,748, root publication 8,776-9,185, foreground scheduling
  9,187-9,207, Home appearing 10,614 and Home publication return 13,971 ms relative to OnStart.

These are not exact rendering measurements, a permanent deadlock, a Release causal profile,
or physical-device acceptance. Temporary source probes and their owned cache file were removed.
The delivered-change candidate instead removes the proven unnecessary detach/reinsert operations
in original Home layout application. QA-06, ANR, the two-second objective and platform/device gates remain open.
