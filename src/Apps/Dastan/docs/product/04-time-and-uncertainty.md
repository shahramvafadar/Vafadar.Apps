# 04 — Time, Order, and Uncertainty

**Version:** 0.6  
**Date:** 2026-11-15  
**Status:** Conceptual rules for subsequent design and validation  
**Scope:** Personal, historical, natural, narrative, and media time; no storage format or library selection

## TIME-001 — Accepted scope and proposal boundaries

**Accepted requirement:** The product must support everyday dates, BCE dates, ten to twenty thousand years ago, and fifty to sixty million years ago. Events, starts and ends, approximate dates, uncertain ranges, and narratives such as the Shahnameh are all in scope. Multiple visual views and comparisons must preserve these meanings.

**Accepted audience correction:** The first experience serves curious everyday users. It should explain time clearly through useful charts, short labels, and progressively available detail. Researchers may inspect the same information more deeply. A complicated scientific form is not the default experience.

**Proposal:** Represent time with explicit meaning, precision, framework, reference, and source. Rules below are proposals unless an accepted requirement or an external convention is identified. Supporting the meanings from the beginning does not require releasing every advanced editing or analysis tool immediately.

## TIME-002 — Dimensions of a temporal expression

A temporal expression identifies what it describes, which framework applies, what is known, and what supports it.

| Dimension | Proposed examples |
|---|---|
| Nature | Occurrence, period, duration, order, or temporal relationship |
| Framework | Calendar time, geological age, time within a narrative |
| Precision | Day, month, year, century, thousand years, or million years |
| Uncertainty | Specified, approximate, disputed, or several possibilities |
| Reference | Calendar era, reference year, start of a reign, or another event |
| Provenance | Source, estimation method, version, and contributor |

Precision and certainty are independent. A year may be well supported while its month and day are unknown. A fully specified day can still be disputed. Distinguish the time an assertion concerns from when the assertion was entered, reviewed, or published.

## TIME-003 — An uncertain occurrence, a real period, and uncertain boundaries

These meanings must remain distinct from entry through presentation. All examples in this document are illustrative rather than historical assertions.

| Case | Example | Permitted interpretation |
|---|---|---|
| Occurrence with uncertain timing | “It happened sometime between twenty and ten thousand years ago.” | The occurrence falls somewhere in the range; a ten-thousand-year duration has not been asserted |
| Actual period | “It continued from twenty to ten thousand years ago.” | Existence or activity continued across an interval |
| Period with uncertain boundaries | “It began between twenty and eighteen thousand years ago and ended between twelve and ten thousand years ago.” | A real period has uncertain start and end boundaries |
| Duration without a position | “It lasted about thirty years.” | Duration is known or estimated; its calendar position is not yet known |

An uncertainty band must not look like a confirmed activity bar. A guaranteed middle segment is valid only under stated continuity assumptions and compatible bounds. A midpoint may be used as a clearly identified layout aid if needed; it must not become the stored or displayed factual date. An event is not necessarily physically instantaneous merely because its occurrence is represented at a coarse time granularity. In the review fixtures, positioned activity is named `period`; `duration` is reserved for an elapsed quantity that need not locate that activity on a calendar.

## TIME-004 — Partial precision, approximation, and alternatives

“The year 1980” does not mean January 1. “Around 1980” must not silently become 1975–1985 without a supported rule or source. A computational range derived from year-only precision is different from an uncertainty range explicitly asserted by a source. Results and explanations must preserve that distinction.

Alternative possibilities remain separate: “1980 or 1990” is neither 1985 nor a continuous period from 1980 to 1990. Do not invent confidence percentages. Source-supported statistical probabilities may be retained with their method and meaning; they are not editorial popularity scores.

The Library of Congress EDTF specification describes reduced precision, approximation, uncertainty, intervals, open or unknown endpoints, sets of dates, and extended years. It is a useful compatibility reference; full or partial adoption remains an implementation decision. [Official EDTF specification](https://www.loc.gov/standards/datetime/).

## TIME-005 — Starts and ends without hidden assumptions

Each boundary can independently be specified, approximate, or unknown. At least these states remain distinct:

- The end is known.
- According to the latest recorded information, the period is ongoing.
- The period ended, but its end date is unknown.
- It is unknown whether the period continues or has ended.

An empty end value must not collapse these meanings. An ongoing claim should carry a source and last-check time; absence of new information does not prove continuation. Separate employment, membership, or reign periods remain separate. Impossible combinations of uncertain boundaries should be rejected or reviewed, rather than repaired by silently swapping dates.

Open-ended expressions, unknown endpoints, and current-status claims require distinct explanations even if an eventual serialization offers similar-looking notation. Deleting an endpoint must not automatically mark a period as current.

## TIME-006 — Comparable temporal frameworks

Each line or collection needs an explicit temporal framework. A synchronized axis requires a valid mapping between the selected frameworks.

| Framework | Example | Condition for comparison |
|---|---|---|
| Calendar | A birth date or company founding | Known calendar, era, and precision |
| Age relative to a reference | Years before a specified reference year | Known reference and unit |
| Geological age | Millions of years ago | Known convention and source for the age or period |
| Relative to an event | Ten years after a reign began | Identified reference; its uncertainty propagates |
| Narrative chronology | Year five in a fictional world | The same chronology or an explicit mapping between chronologies |
| Order only | A occurred before B | No measurable temporal distance is implied |

An era label such as CE or BCE is not a calendar. Store and display those concepts separately. An unspecified calendar remains unspecified even when the source supplies a common-era year. The same number does not establish simultaneity. Year five of two reigns or stories must not become one real date without a valid reference. A modern event timeline and an unanchored narrative may be shown side by side, but their layout must not suggest a shared historical clock.

## TIME-007 — Deep time and “years ago”

Tens of millions of years should be entered and displayed naturally, without a fabricated month or day. An everyday label can say “about 55 million years ago,” while optional detail shows the value, unit, reference, precision, and source.

The International Commission on Stratigraphy guide describes `ka`, `Ma`, and `Ga` for thousands, millions, and billions of years. The exact age meaning and reference must follow the source convention. [Official ICS stratigraphic guide](https://stratigraphy.org/guide/defs).

**Proposed rule:** Anchor a relative expression when its reference is known, rather than letting a fixed historical assertion drift by one year whenever the current year changes. If a source's reference is unknown, preserve that uncertainty. Do not add displayed digits that imply unsupported precision. Age values and forward-running calendar coordinates require an explicit direction convention.

## TIME-008 — BP, calibration, and original values

In radiocarbon usage, BP refers to a 1950 reference, and a raw radiocarbon age is not necessarily a calendar age. Oxford ORAU explains that calibration can produce several possible calendar ranges and that `cal BP` expresses calendar years before 1950. [Official Oxford ORAU calibration guidance](https://c14.arch.ox.ac.uk/calibration.html).

Preserve raw value, age type, units, calibration method or source, and the resulting calendar interpretation separately. Do not convert a raw BP number to BCE by simple subtraction. An ordinary statement of “years ago” must not acquire a BP label without evidence. Conversion does not overwrite the original and does not remove uncertainty.

If an imported source contains several calibrated ranges or source-supported probabilities, retain their meaning. A simple public summary may collapse visual complexity, but must not turn those possibilities into a single certain date.

## TIME-009 — BCE and the original calendar

The historical BCE/CE convention has no year zero: 1 BCE directly precedes 1 CE. Astronomical year numbering instead maps year zero to 1 BCE and year minus one to 2 BCE. Negating a BCE number is therefore not the correct astronomical conversion. [NASA year-dating conventions](https://eclipse.gsfc.nasa.gov/SEhelp/dates.html).

**Proposal:** Public displays use clear BCE/CE labels. Any internal computational convention must preserve their meaning. Keep the source's original calendar, including Solar Hijri, lunar calendars, Gregorian, Julian, or another identified calendar. A conversion must not imply more precision than the source supports. If the calendar or conversion method is ambiguous, do not replace the source expression with an unqualified exact date.

**Decided (D-043, confirmed by D-100 on 2026-11-24):** a date is stored in the Gregorian calendar only, so that dates are compared without a conversion; the calendar of a source is not what is stored. Showing a date in another calendar (Solar Hijri, Lunar Hijri) is a matter of display and is the last piece of work of the backlog.

English documentation does not select the user-interface language or date-display locale. Translating a label must not silently change the calendar or its chronological meaning.

## TIME-010 — Narrative time and story order

For material such as the Shahnameh, statements belong to a work, narrative, or edition. A proposed historical date, internal story time, and event order are different information. Connecting a character or period with real history is a sourced claim; it does not turn every version into a certain historical chronology.

The order in which a work presents information may differ from the order of events. Support separate concepts of reading or presentation order and in-world event order. When only precedence is known, visual distance does not represent elapsed time. Ordering can be partial; there is no need to fabricate a numbered position for every event.

A contradiction within one asserted ordering should be reportable. Differences between separate narratives are not automatically data errors. An everyday story thread may select an accessible reading route while retaining its edition and spoiler boundary. That route is a presentation choice, not a replacement for the underlying chronology.

## TIME-011 — Source disagreement and temporal revisions

Each temporal assertion can retain its source and review history. A correction changes or withdraws the previous assertion with a reason. A simple view may show a selected account; disagreement and the reason for selection remain available.

Event time, record creation, source publication, review, and correction are independent. “What was known at that time?” differs from “What happened at that time?” Detailed historical-version querying may be introduced later, but these meanings must not be conflated in the conceptual model.

For media, the time of capture or creation is separate from the time of the depicted subject and from upload, publication, import, or sharing. TIME-016 defines the fuller media contract. Correcting a photo's estimated capture year must not reorder a recent-shares view as if a new share occurred.

## TIME-012 — Overlap and search semantics

“Definite” here means **definite within the recorded bounds, assumptions, and assertions**, not guaranteed historical truth.

| Result | Proposed meaning |
|---|---|
| Definite overlap | Overlap occurs in every admissible interpretation of the recorded bounds and assumptions |
| Possible overlap | Overlap occurs in some admissible interpretations but not others |
| Insufficient information | A required framework, bound, or mapping is unavailable |
| No overlap | No admissible interpretation produces overlap |

An occurrence that could be anywhere from 1998 to 2003 is a possible match for a 2000–2005 search window. If all possible times fall inside the search window, membership in that window is definite; occurrence throughout the entire window is not implied.

Overlapping uncertainty ranges for two occurrences do not prove that the occurrences were simultaneous. For periods, continuity assumptions and endpoint-inclusion rules must be explicit. Queries involving multiple statements must respect any recorded dependencies rather than freely combining incompatible alternatives.

Personal-account and profile discovery uses only data authorized for that purpose. Exploration of documented public roles follows its publication rules. Temporal filtering must not expose private information through results, counts, labels, suggestions, or gallery membership.

## TIME-013 — Visual meaning without false precision

Horizontal, vertical, and other layouts must preserve temporal meaning. Distinguish period bars, uncertainty bands, approximate dates, and order-only sequences. Color alone is insufficient; labels, legends, and accessible descriptions are needed.

The everyday experience should offer several meaningful visual starting points with progressively available explanations. Exact presets are design proposals. A vertical card list need not encode elapsed time in card spacing, but it should not pretend to be a scaled axis. A relative-age comparison must be labeled differently from calendar simultaneity.

Comparable calendar tracks normally share the axis, viewport, and zoom. An explicitly selected independent view must make that difference clear. Compressed or logarithmic deep-time views need clear labels and visible explanations: equal screen distances do not necessarily represent equal durations. Order-only axes must not resemble an exact calendar scale without explanation.

Switching among graph, timeline, gallery, and story views should retain the applicable subject, time selection, framework, narrative edition, filters, and return path. A view without a usable date places the item in an honest unknown-time or relative-order context rather than an arbitrary year.

## TIME-014 — Entry and validation rules

Begin a general entry flow with understandable questions such as “An event or a period?” and “How precisely do you know the time?” Reveal specialist controls when they are useful.

Proposed validation rules:

- Do not complete a partial date with invented months or days.
- Do not normalize an unknown calendar or reference into an unqualified exact value.
- Explain impossible values, conversion errors, and source disagreement differently.
- Make the direction of age-based values explicit; larger ages usually refer further into the past under a common reference.
- An uncertain reference must not produce a more certain result without additional evidence.
- Removing an end date must not imply continuation.
- Validate narrative claims in their declared context.
- Preserve original expressions and supported corrections alongside normalized interpretations.
- Do not let media metadata silently override a sourced or user-corrected contextual date.

## TIME-015 — Acceptance examples and open decisions

Design must explain and support: a birth with year only; an occurrence with two alternative dates; two separate employment periods; a reign with uncertain boundaries; an age between fifty and sixty million years; raw BP alongside calibrated output; the transition from 1 BCE to 1 CE; a narrative without calendar dates; and university discovery with possible temporal overlap.

The additional media scenarios are owned by [document 14](14-reference-scenarios.md), including the new SC-15–SC-19 experience and media group. Their definitions must remain consistent with the requirements register and [document 15](15-media-and-galleries.md).

Open decisions include supported initial calendars, conversion and retention methods, endpoint inclusion, EDTF coverage, disagreement display, uncertain-time search behavior, zoom behavior, and gallery time defaults. Select implementation tools after these contracts are clear. Successfully drawing a chart does not establish correct temporal meaning.

## TIME-016 — Media time and gallery chronology

**Proposed semantic rule supporting the accepted media direction:** Keep media capture or creation, depicted time, upload, publication, import, and sharing distinct. This extends the earlier accepted distinction between memory time and posting time. A media item may have more than one applicable time and uncertainty level.

| Temporal concept | What it describes | It must not automatically become |
|---|---|---|
| Original capture time | When a camera recorded a photo or video, possibly over an interval | The date of an ancient subject represented by an object in the frame |
| Work creation time | When an illustration, painting, animation, or edited work was made | The time of the event it depicts |
| Depicted or discussed time | The event, period, or narrative sequence represented by the content | Proof that the media was created then |
| Digitization or transformation time | A scan, restoration, crop, conversion, or other derivative operation | The original work's creation time |
| Publication time | A specified publication of the work or item, with its source or context | The first-ever publication unless that is supported |
| Upload or import time | When a file or reference entered this system | Capture, event, or publication time |
| Share time | A particular authorized sharing action with a defined audience or destination | A change to the age or provenance of the underlying media |

A photograph captured in 1970 and shared with family in 2026 belongs to the 1970 memory context and the 2026 recent-shares context for different reasons. A video made today about a period in 500 BCE may appear in that period's gallery as a modern depiction or explanation, not as contemporary footage or automatic primary evidence.

A video's playback length is distinct from the duration of the history it covers. Scene or excerpt timecodes, if introduced, identify positions within media and need their own mapping to depicted events. Uncertain capture dates remain uncertain; a valid file timestamp alone does not resolve the depicted event's date.

A gallery must state its time basis: captured during, created during, depicting or about, or shared recently. Dynamic and saved galleries preserve that basis. If a timestamp is missing, the product must not silently substitute upload time; an explicit alternative sort may be offered with a clear label. Each share remains an audience-governed activity, so a recent view cannot become a route to otherwise inaccessible media or private contextual associations.

## TIME-017 — Execution checks for the first slice

The first story implementation must preserve its narrative-order meaning even if calendar and deep-time UI arrive later. The temporal spike must separately demonstrate year-only precision, BCE/CE without year zero, an uncertain occurrence versus a continuous period, independent uncertain start/end bounds, unknown versus ongoing ends, relative-age direction, an explicit age reference, and a narrative with no calendar mapping. These are semantic acceptance cases, not a requirement to ship every editor in the first milestone.

No current sample proves arbitrary calendar conversion, geological arithmetic, radiocarbon calibration or statistical overlap. The package validator checks the supplied values and declared conventions; implementing tests must exercise the application algorithms when those are written. Do not claim EDTF compliance merely because a few example strings parse, and do not use a date constructor's default month, day or range as a factual value.
