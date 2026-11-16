# Dastan – acceptance of M0

The twelve conditions come from [document 20](product/20-implementation-plan.md), section 5. This page is their
current status. A condition changes its status only with evidence: the command or the browser check, the date and
what was observed.

Status values: **Not started**, **Partly** (the named part is implemented and checked, the rest is open),
**Implemented and checked**, **Implemented, not verified**, **Blocked** (with the cause).

| Id | Observable condition | Phase | Status | Evidence |
|---|---|---|---|---|
| M0-01 | A fresh checkout starts the app locally from documented commands with the validated sample | 4, 7 | **Implemented and checked** | 2026-11-16, .NET SDK 10.0.404, Windows 11 x64: a fresh copy of `main` (`e0c989ae`) from GitHub in an empty folder – `dotnet restore`, `dotnet build -p:ContinuousIntegrationBuild=true` without a warning, `dotnet test` (839 passed, the 134 browser tests skipped without their container), `dotnet run --project src/Apps/Dastan/Vafadar.Dastan.Web`; the site answered on `http://localhost:5210`: the start page with the story, one step at the opening, two items in the gallery at the third stage, the video only then, the sample not as a file, Persian right to left. On this machine only; another machine was not available |
| M0-02 | Discover → collection → entity → labelled relation → another entity → back works | 5 | **Implemented and checked** | 2026-11-16, Chromium in the Playwright container: from the start page to the story, to Rostam, along "Father of" to Zal, back to Rostam and back to the story, by clicks and the Back of the browser (`JourneyTests.From_the_start_to_an_entity_…`) |
| M0-03 | The focused graph and the list show the same allowed relationships; keyboard selection and expansion on a phone work | 3, 5 | **Partly** | 2026-11-16: the connections page and the family page draw their graph as SVG on the server and list the same relations below it; for every entity at the last stage, closed and fully opened, and for every family member, in both languages, drawing and list hold exactly the relations of the projection (`ConnectionsPageTests.The_drawing_and_the_list_show_the_same_relations_for_every_entity`, `FamilyAndStoryPageTests.The_family_drawing_and_its_list_…`, `ConnectionsLayoutTests`, `FamilyLayoutTests`). In a browser at 360, 412 and 1280 px, English and Persian: no box clips its text, no boxes overlap, the controls that open a row are 44 px, no horizontal scrolling. Open: selection and expansion with the keyboard were not tried, a phone was not used. Browser test, 2026-11-16, Chromium 1.64.1 in the container: with the keyboard alone at a width of 360 px the skip link is the first stop, a control of the drawing has a visible focus and 44 px, Enter opens a row and closes it again, drawing and list stay equal (`JourneyTests.With_the_keyboard_alone_…`). Still open: a phone – a narrow window is not one |
| M0-04 | Story and participant lanes agree on the narrative order and on what an association means | 3, 5 | **Implemented and checked** | 2026-11-16: the story page lists the steps of the projection in its order and shows the lanes as a table of names by steps; steps, rows and filled cells equal the steps, lanes and associations of the projection at the stages tried, and Rostam is "only mentioned" in step 3 in the list and in his lane (`FamilyAndStoryPageTests.The_steps_and_the_lanes_are_those_of_the_projection_in_its_order`, `…Somebody_who_is_only_mentioned_…`; model part see below). In a browser at 360 px, English and Persian: the table scrolls inside its frame, the page does not.. Browser test, 2026-11-16, Chromium 1.64.1 in the container: at 360 px in Persian the eight steps and the eight columns of the lanes are there, the table scrolls inside its frame by the keyboard, the page does not (`JourneyTests.On_a_phone_the_lanes_…`) |
| M0-05 | At reading level 2 a real bundled image and video open with labels, captions and links to their subjects | 5 | **Implemented and checked** | 2026-11-16: after moving to the third stage the gallery lists the diagram and the video; the page of each shows it with its kind, the time it was made, its maker, the rights and links to the four entities and three relations it shows; the files are delivered byte for byte, the video in ranges (`GalleryAndMediaTests`). In a browser at 360 px, English and Persian: the image is shown, the video plays (position advances, 1280 px wide source) and its English captions are shown.. Browser test, 2026-11-16, Chromium 1.64.1 in the container: the gallery is empty, the visitor moves to the third stage with the control, the image arrives, the video plays (position advances, 1280 px, 20 s, captions showing), a subject leads on and Back returns (`JourneyTests.After_moving_to_the_third_stage_…`) |
| M0-06 | Level 0 and lowering the boundary remove later details from current, back and deep-linked views | 3, 5 | **Implemented and checked** | 2026-11-16, model part: no answer of any view discloses a later stage, for the stages 0 to 4; a hidden record answers like an unknown one (`dotnet test --solution Vafadar.Dastan.slnf`, see below). The pages are phase 5 2026-11-16, media: before the third stage the galleries are empty and say nothing, the page of an item is not found and none of its files is delivered; after moving back they are gone again for that visitor (`GalleryAndMediaTests.After_moving_back_…`, `…Before_the_media_are_reached_…`). Browser test, 2026-11-16, Chromium 1.64.1 in the container: after lowering the reading position, going back through the history of the browser shows "Not here" for the page of the image and of Sohrab, and the file answers 404 (`JourneyTests.After_moving_back_in_the_story_…`) |
| M0-07 | The evidence shown supports the selected statement and closes back to where it was opened | 3, 5 | **Implemented and checked** | 2026-11-16: every statement a page makes – an entity, a relation, a step, a part in a step, the description of an image or video – leads to a page that restates it and lists its sources; for every visible statement at the stages 0, 2 and 4 the sources are exactly those of the projection; the page returns to where it was opened, with what was open there, and follows no way back that is not a page of the collection; a source of another site is opened only from a page that says what leaving means (`EvidenceAndSearchTests`; model part see below). In a browser at 360 px, English and Persian: no overflow, links 44 px high.. Browser test, 2026-11-16, Chromium 1.64.1 in the container: from the drawing with a row open to the sources of a relation, to the page before a source, back to the evidence and back to the drawing with the row still open (`JourneyTests.The_sources_of_a_relation_…`) |
| M0-08 | Time examples round-trip with uncertainty, duration, unit, reference, era, calendar and frame preserved | 2 | **Implemented and checked** | 2026-11-16, .NET SDK 10.0.404, Windows 11 x64: `dotnet test --project test/Libraries/Vafadar.Temporal.Tests -p:ContinuousIntegrationBuild=true` – 165 of 165 passed, no warnings. See below |
| M0-09 | Independent visitors keep independent language and reading state | 4 | **Implemented and checked** | 2026-11-16: two visitors with their own cookies, Persian at stage 3 with the dark theme and English at the opening, ask at the same time in 25 rounds; neither gets the other's language, page or theme (`SiteTests.Two_visitors_keep_their_own_…`, see below). Browser test, 2026-11-16, Chromium 1.64.1 in the container: two windows with their own cookies, Persian at the third stage on a dark device and English at the opening on a light one, at the same time (`JourneyTests.Two_visitors_…`) |
| M0-10 | The core routes work at 360 px, 412 px and a wide window; keyboard and focus, light and dark, right to left | 4, 6 | **Implemented and checked** | 2026-11-16, the three pages of phase 4 in a browser: no horizontal overflow at 360 and 412 px, a centred column at 1280 px, light and dark, Persian right to left, stand-alone links and controls at least 44 px high. Keyboard order, focus and the later routes are phase 5 and 6. Browser test, 2026-11-16, Chromium 1.64.1 in the container: twelve views in English and Persian at 360, 412 and 1280 px – no sideways scrolling, nothing of the content outside the window, no box of a drawing clipped or overlapping, the Persian font loaded (`JourneyTests.Every_view_fits_…`, 72 cases). Accessibility, 2026-11-16: thirteen views in English and Persian, light and dark, break no rule of WCAG 2.1 A and AA and no best practice that axe-core 4.14 can decide; two findings were corrected first (a role on the form of the appearance, two regions of the same name on the story page) (`InstalledAppTests.No_view_breaks_a_rule_…`, 52 cases); the keyboard reaches the skip link first and the controls of the drawing with a visible focus (`JourneyTests`). Not done: a phone, a screen reader |
| M0-11 | Manifest, scope, start from a deep link and the behaviour on connection loss and update work | 6 | **Partly** | 2026-11-16, Chromium 1.64.1 in the container: the manifest is read without an error – name, start and scope "/", standalone, icons of 192 and 512 px and a maskable one; a deep link opens its page; without a connection a page asked for is the page for "no connection" and with the connection back the same address is the page again; nothing of a collection is kept ([ADR 0007](adr/0007-installable-app.md), `InstallableAppTests`, `InstalledAppTests`). **Not observed: an installation** – Chromium names no obstacle except that a window of the tests is a private one, and no other browser was driven. The replacement of a worker by a newer one was not exercised |
| M0-12 | Only the demonstration projection is served; fixtures and the editorial input are not web assets | 3, 6 | **Implemented and checked** | 2026-11-16, model part: the public audience gets no projection of the sample; items that are not eligible are absent; reviewer documents are not in the model (`dotnet test --solution Vafadar.Dastan.slnf`, see below). The pages are phase 5. Serving is phase 4 to 6 2026-11-16, files: one route delivers files and asks the projection; the sample, a file by its name and a path that climbs out of the folder are not found; the public site delivers no file ([ADR 0006](adr/0006-media-delivery.md), `GalleryAndMediaTests.At_the_end_of_the_story_nothing_but_the_named_files_…`, `PublicSiteTests`) 2026-11-16: the demonstration asks with every answer not to be indexed, the public site does not; the service worker keeps nothing of a collection (`InstallableAppTests`) |

## What was not checked

State on 2026-11-16, for all conditions together:

* **No phone.** Narrow windows of 360 and 412 px were used, in a desktop Chromium. That is why M0-03 stays "Partly".
* **No installation.** Chromium reads the manifest and names no obstacle but its private window; nobody installed
  the app, in any browser. That, and the replacement of a service worker by a newer one, is why M0-11 stays "Partly".
* **No screen reader and no visitor.** The accessibility check is the part a tool can decide.
* **One browser.** The browser tests run in Chromium 1.64.1 only; Firefox and Safari were not used.
* **One machine.** Everything ran on the development machine; the browser tests are skipped in CI, which has no
  container for them.

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

### M0-09, and the website part of M0-06 and M0-12 (phase 4)

Checked on 2026-11-16 with `dotnet test --solution Vafadar.Dastan.slnf`. `Vafadar.Dastan.Web.Tests` starts the
site in memory with the Development settings and asks it over HTTP. Each visitor of a test keeps its own cookies
and sends a cookie only to the paths it was set for, as a browser does.

| Condition | What is checked | Tests |
|---|---|---|
| M0-09 | Two visitors, different language, reading position and theme, 25 rounds of requests at the same time; the start address of each goes to their own language | `SiteTests.Two_visitors_keep_their_own_language_reading_position_and_appearance`; for the culture alone `Vafadar.Web.Tests…Requests_in_different_languages_at_the_same_time_…` |
| M0-06 | From the start page every link is followed, for the stages 0 to 4 in both languages; no page contains a text that belongs only to hidden records. An address cannot raise the reading position; moving back removes the later part; a hidden entity and an unknown one are the same page | `SiteTests.No_page_a_visitor_can_reach_…`, `…An_address_cannot_move_a_visitor_forward`, `…Moving_back_…`, `…Something_not_read_yet_and_something_that_does_not_exist_…` |
| M0-12 | The sample, its translation, its media and the fixtures cannot be downloaded under any address tried; started for the public the site shows nothing of the sample and no reading position changes that | `SiteTests.The_collection_its_media_and_the_fixtures_cannot_be_downloaded`, `PublicSiteTests` |

In a browser (the built-in preview, 2026-11-16): the reading form was sent from the Persian page, the page of
Sohrab was "not found" before and readable after. This check found a defect the HTTP tests had missed – the
cookie of the form token was scoped to `/fa` and the form was refused – which is fixed in `Vafadar.Web` and now
covered by a test. Limits: no automated browser test exists yet (phase 5); the Back button and a phone were not
tried; of the screenshots planned only two could be taken, the other layouts were measured in the page.

## Deviations from the wording of document 20

| Condition | Document 20 | Here | Decision |
|---|---|---|---|
| M0-10 | English, Persian and German layouts; resource parity for six languages | English and Persian layouts; every text exists in each language the app declares | D-031, D-032 |

No other condition is changed. A condition that cannot be checked on the available machine – for example the
installation on a phone – stays **Not verified** with the exact remaining step; it is never reported as passed.

### The states of document 18 (section 5)

Checked on 2026-11-16 with `dotnet test --project test/Apps/Dastan/Vafadar.Dastan.Web.Tests`. Pages are written by the
server in one answer (ADR 0003), so there is no state in which a page waits for its content.

| State | What the site does | Tests |
|---|---|---|
| Empty permitted selection | A gallery, a family, the connections, the appearances, the search and the story say that there is nothing to show here and offer the way back or another view; no count and no name of what is hidden | `GalleryAndMediaTests.Before_the_media_are_reached_…`, `…The_gallery_of_an_entity_…`, `StatesTests.A_view_that_has_nothing_to_show_…`, `EvidenceAndSearchTests.A_name_that_is_not_read_yet_…` |
| Unknown or uncertain time | A date is written as the record states it – "about", the era, the calendar it is in; a time the pages cannot word is "not stated"; the time of an image is labelled as the time it was made | `GalleryAndMediaTests.The_page_of_the_diagram_…`, `…A_Persian_page_of_an_item_…` |
| Incompatible view or frame | The story names its frame – the order of telling, not a calendar – instead of offering a calendar; a gallery says that its order says nothing about dates; a family without members points to the connections | `StatesTests.The_story_says_that_it_is_not_a_calendar_…`, `…A_view_that_has_nothing_to_show_…`, `GalleryAndMediaTests.From_the_third_stage_…` |
| Unavailable media or source | A missing file leaves the page of the item with everything else and the sentence that it cannot be shown; no player, no placeholder. A source of another site is announced as one the site has no say over | `MissingFilesTests`, `EvidenceAndSearchTests.The_page_before_a_source_…` |
| Access denied or revoked | Not part of M0 (private spaces). What the public site may not show answers as not found | `PublicSiteTests` |
| Spoiler-limited content | Every view shows what the reading position allows and says in general that the story continues; what is hidden answers exactly like what does not exist – pages, files, evidence, search | `SiteTests` (the crawl for the stages 0 to 4), `StatesTests.What_is_not_read_yet_and_what_does_not_exist_…`, `GalleryAndMediaTests.Before_the_media_are_reached_none_of_their_files_…` |
| Loading or processing | Does not occur: a page arrives complete. Images below the first screen are loaded when they are reached; a video loads only what shows its length | – |
| Recoverable failure | A failure has its own page in the language of the address, with the failed address to try again and the start; it is neither an empty result nor a missing page and shows nothing of the failure. A form that is refused leads to a page that says that nothing was changed. Without a connection the visitor gets a page that says so, with the same address to try again | `FailureTests`, `StatesTests.A_form_that_is_refused_…`, `InstalledAppTests.Without_a_connection_…` |
| Invalid link | One page for an address that leads nowhere, with the start, the story and the reading position | `StatesTests.What_is_not_read_yet_and_what_does_not_exist_…` |
