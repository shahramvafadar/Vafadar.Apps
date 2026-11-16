# Dastan – acceptance of M0

The twelve conditions come from [document 20](product/20-implementation-plan.md), section 5. This page is their
current status. A condition changes its status only with evidence: the command or the browser check, the date and
what was observed.

Status values: **Not started**, **Partly** (the named part is implemented and checked, the rest is open),
**Implemented and checked**, **Implemented, not verified**, **Blocked** (with the cause).

| Id | Observable condition | Phase | Status | Evidence |
|---|---|---|---|---|
| M0-01 | A fresh checkout starts the app locally from documented commands with the validated sample | 4, 7 | Not started | – |
| M0-02 | Discover → collection → entity → labelled relation → another entity → back works | 5 | Not started | – |
| M0-03 | The focused graph and the list show the same allowed relationships; keyboard selection and expansion on a phone work | 3, 5 | **Partly** | 2026-11-16, model part: one answer feeds graph and list; equal for every entity at every stage; expansion (`dotnet test --solution Vafadar.Dastan.slnf`, see below). The pages are phase 5 |
| M0-04 | Story and participant lanes agree on the narrative order and on what an association means | 3, 5 | **Partly** | 2026-11-16, model part: story and lanes share their steps; a lane cell is the stated association with its presence (`dotnet test --solution Vafadar.Dastan.slnf`, see below). The pages are phase 5 |
| M0-05 | At reading level 2 a real bundled image and video open with labels, captions and links to their subjects | 5 | Not started | – |
| M0-06 | Level 0 and lowering the boundary remove later details from current, back and deep-linked views | 3, 5 | **Partly** | 2026-11-16, model part: no answer of any view discloses a later stage, for the stages 0 to 4; a hidden record answers like an unknown one (`dotnet test --solution Vafadar.Dastan.slnf`, see below). The pages are phase 5 |
| M0-07 | The evidence shown supports the selected statement and closes back to where it was opened | 3, 5 | **Partly** | 2026-11-16, model part: every visible statement returns its own sources, locator and review state (`dotnet test --solution Vafadar.Dastan.slnf`, see below). The pages are phase 5 |
| M0-08 | Time examples round-trip with uncertainty, duration, unit, reference, era, calendar and frame preserved | 2 | **Implemented and checked** | 2026-11-16, .NET SDK 10.0.404, Windows 11 x64: `dotnet test --project test/Libraries/Vafadar.Temporal.Tests -p:ContinuousIntegrationBuild=true` – 165 of 165 passed, no warnings. See below |
| M0-09 | Independent visitors keep independent language and reading state | 4 | Not started | – |
| M0-10 | The core routes work at 360 px, 412 px and a wide window; keyboard and focus, light and dark, right to left | 4, 6 | Not started | – |
| M0-11 | Manifest, scope, start from a deep link and the behaviour on connection loss and update work | 6 | Not started | – |
| M0-12 | Only the demonstration projection is served; fixtures and the editorial input are not web assets | 3, 6 | **Partly** | 2026-11-16, model part: the public audience gets no projection of the sample; items that are not eligible are absent; reviewer documents are not in the model (`dotnet test --solution Vafadar.Dastan.slnf`, see below). The pages are phase 5. Serving is phase 4 to 6 |

## Evidence

### M0-08 – time examples

The cases document 20 (step A) and document 04 (TIME-017) name, each as tests of `Vafadar.Temporal`
(`test/Libraries/Vafadar.Temporal.Tests`):

| Case | Tests |
|---|---|
| An exact day; a year without month or day | `PositionTests`, `JsonTests.A_year_is_written_without_month_or_day` |
| The step from 1 BCE to 1 CE without a year zero | `PositionTests.The_year_before_year_one_…`, `ComparerTests.Years_before_and_of_the_common_era_…` |
| An uncertain occurrence against a continuous period | `ComparerTests.An_event_somewhere_in_a_range_and_a_period_across_it_answer_differently` |
| A period with independently uncertain ends | `ExpressionTests.The_bounds_of_a_period_are_uncertain_independently`, `ComparerTests` (SC-06 group) |
| A duration without a position | `ExpressionTests.An_elapsed_time_…`, `ComparerTests.A_time_without_a_place_gives_no_answer_instead_of_a_guess` |
| Unknown, ended-unknown and ongoing ends | `ExpressionTests.The_four_open_ends_…`, `ComparerTests.No_open_end_proves_a_later_time` |
| An anchored range of 50 to 60 million years; direction and reference of an age | `ComparerTests` (deep time group), `PositionTests.An_age_keeps_…` |
| A story order without any calendar | `ComparerTests` (story order group), `JsonTests.A_place_in_a_story_is_written_without_any_date` |
| Incompatible comparisons give an explicit result | `ComparerTests.A_place_in_a_story_is_never_a_date`, `…An_age_and_a_date_have_no_common_scale`, `…Years_of_different_eras_…` |
| Round trip without invented precision | `JsonTests.An_expression_comes_back_as_it_went_in` over 23 expressions of every kind |

Limits of this evidence: the tests exercise the library. The sample's own time values (eight story positions, five
media creation dates) are read into these types since phase 3 (`PilotSampleTests`: the events are places in the
telling, a painting of "circa 1450" stays an approximate year without a calendar). Calendars are
not converted ([ADR 0012](../../../../docs/adr/0012-temporal-model.md), point 8).

### M0-03, M0-04, M0-06, M0-07, M0-12 – the model part (phase 3)

Checked on 2026-11-16 with .NET SDK 10.0.404 on Windows 11 x64: `dotnet build Vafadar.Dastan.slnf
-p:ContinuousIntegrationBuild=true` without warnings, `dotnet test --solution Vafadar.Dastan.slnf` – see the delivery plan for the
count. No web host is involved; these tests answer the questions the pages will ask.

| Condition | What is checked | Tests |
|---|---|---|
| M0-03 | Graph and list are one answer; for every visible entity at every stage the edges equal its relations; expansion adds connected relations only | `SampleProjectionTests.The_graph_of_every_entity_and_its_list_…`, `…Expanding_every_node_…`, `ProjectionTests` (connections group) |
| M0-04 | Story and lanes have the same steps; a lane cell is the association of the event; Rostam is mentioned, not present, when Sohrab learns his lineage | `SampleProjectionTests.The_story_and_the_lanes_agree_…`, `…In_the_lanes_Rostam_is_named_and_not_present_…` |
| M0-06 | For the stages 0 to 4 nothing that belongs only to hidden records can be reached from any answer – identifiers, names in English and Persian, summaries, roles, files, addresses, locators; lowering the boundary takes the later stages away | `SampleProjectionTests.No_answer_discloses_…` (5 stages), `…Lowering_the_boundary_…`, `ProjectionTests.A_hidden_record_and_a_record_that_does_not_exist_…` |
| M0-07 | Every visible entity, relation, event, association and media item returns exactly its own sources | `SampleProjectionTests.Every_visible_statement_has_evidence_and_only_its_own` (5 stages) |
| M0-12 | No projection for the public audience; museum items absent at every stage; a reviewer document cannot be reached from the model; the reader refuses input declared public | `SampleProjectionTests.The_sample_is_not_shown_to_the_public`, `…A_museum_painting_is_not_shown_at_any_stage`, `PilotSampleTests.A_document_for_reviewers_is_not_part_of_the_read_model`, `PilotSampleRefusalTests` (publication group) |

Limits: a browser, the Back button, a deep link, the keyboard and a phone are not part of this evidence – they
are checked with the pages in phase 5. The check compares whole texts; it does not judge the wording the editors
chose for a visible text ([ADR 0002](adr/0002-one-projection.md)).

## Deviations from the wording of document 20

| Condition | Document 20 | Here | Decision |
|---|---|---|---|
| M0-10 | English, Persian and German layouts; resource parity for six languages | English and Persian layouts; every text exists in each language the app declares | D-031, D-032 |

No other condition is changed. A condition that cannot be checked on the available machine – for example the
installation on a phone – stays **Not verified** with the exact remaining step; it is never reported as passed.
