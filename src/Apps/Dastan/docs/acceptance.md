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
| M0-03 | The focused graph and the list show the same allowed relationships; keyboard selection and expansion on a phone work | 3, 5 | **Partly** | 2026-11-16: the connections page and the family page draw their graph as SVG on the server and list the same relations below it; for every entity at the last stage, closed and fully opened, and for every family member, in both languages, drawing and list hold exactly the relations of the projection (`ConnectionsPageTests.The_drawing_and_the_list_show_the_same_relations_for_every_entity`, `FamilyAndStoryPageTests.The_family_drawing_and_its_list_…`, `ConnectionsLayoutTests`, `FamilyTreeTests`). In a browser at 360, 412 and 1280 px, English and Persian: no box clips its text, no boxes overlap, the controls that open a row are 44 px, no horizontal scrolling. Open: selection and expansion with the keyboard were not tried, a phone was not used. Browser test, 2026-11-16, Chromium 1.64.1 in the container: with the keyboard alone at a width of 360 px the skip link is the first stop, a control of the drawing has a visible focus and 44 px, Enter opens a row and closes it again, drawing and list stay equal (`JourneyTests.With_the_keyboard_alone_…`). Still open: a phone – a narrow window is not one |
| M0-04 | Story and participant lanes agree on the narrative order and on what an association means | 3, 5 | **Implemented and checked** | 2026-11-16: the story page lists the steps of the projection in its order and shows the lanes as a table of names by steps; steps, rows and filled cells equal the steps, lanes and associations of the projection at the stages tried, and Rostam is "only mentioned" in step 3 in the list and in his lane (`FamilyAndStoryPageTests.The_steps_and_the_lanes_are_those_of_the_projection_in_its_order`, `…Somebody_who_is_only_mentioned_…`; model part see below). In a browser at 360 px, English and Persian: the table scrolls inside its frame, the page does not.. Browser test, 2026-11-16, Chromium 1.64.1 in the container: at 360 px in Persian the eight steps and the eight columns of the lanes are there, the table scrolls inside its frame by the keyboard, the page does not (`JourneyTests.On_a_phone_the_lanes_…`) |
| M0-05 | At reading level 2 a real bundled image and video open with labels, captions and links to their subjects | 5 | **Implemented and checked** | 2026-11-16: after moving to the third stage the gallery lists the diagram and the video; the page of each shows it with its kind, the time it was made, its maker, the rights and links to the four entities and three relations it shows; the files are delivered byte for byte, the video in ranges (`GalleryAndMediaTests`). In a browser at 360 px, English and Persian: the image is shown, the video plays (position advances, 1280 px wide source) and its English captions are shown.. Browser test, 2026-11-16, Chromium 1.64.1 in the container: the gallery is empty, the visitor moves to the third stage with the control, the image arrives, the video plays (position advances, 1280 px, 20 s, captions showing), a subject leads on and Back returns (`JourneyTests.After_moving_to_the_third_stage_…`) |
| M0-06 | Level 0 and lowering the boundary remove later details from current, back and deep-linked views | 3, 5 | **Implemented and checked** | 2026-11-16, model part: no answer of any view discloses a later stage, for the stages 0 to 4; a hidden record answers like an unknown one (`dotnet test --solution Vafadar.Dastan.slnf`, see below). The pages are phase 5 2026-11-16, media: before the third stage the galleries are empty and say nothing, the page of an item is not found and none of its files is delivered; after moving back they are gone again for that visitor (`GalleryAndMediaTests.After_moving_back_…`, `…Before_the_media_are_reached_…`). Browser test, 2026-11-16, Chromium 1.64.1 in the container: after lowering the reading position, going back through the history of the browser shows "Not here" for the page of the image and of Sohrab, and the file answers 404 (`JourneyTests.After_moving_back_in_the_story_…`) |
| M0-07 | The evidence shown supports the selected statement and closes back to where it was opened | 3, 5 | **Implemented and checked** | 2026-11-16: every statement a page makes – an entity, a relation, a step, a part in a step, the description of an image or video – leads to a page that restates it and lists its sources; for every visible statement at the stages 0, 2 and 4 the sources are exactly those of the projection; the page returns to where it was opened, with what was open there, and follows no way back that is not a page of the collection; a source of another site is opened only from a page that says what leaving means (`EvidenceAndSearchTests`; model part see below). In a browser at 360 px, English and Persian: no overflow, links 44 px high.. Browser test, 2026-11-16, Chromium 1.64.1 in the container: from the drawing with a row open to the sources of a relation, to the page before a source, back to the evidence and back to the drawing with the row still open (`JourneyTests.The_sources_of_a_relation_…`) |
| M0-08 | Time examples round-trip with uncertainty, duration, unit, reference, era, calendar and frame preserved | 2 | **Implemented and checked** | 2026-11-16, .NET SDK 10.0.404, Windows 11 x64: `dotnet test --project test/Libraries/Vafadar.Temporal.Tests -p:ContinuousIntegrationBuild=true` – 165 of 165 passed, no warnings. See below |
| M0-09 | Independent visitors keep independent language and reading state | 4 | **Implemented and checked** | 2026-11-16: two visitors with their own cookies, Persian at stage 3 with the dark theme and English at the opening, ask at the same time in 25 rounds; neither gets the other's language, page or theme (`SiteTests.Two_visitors_keep_their_own_…`, see below). Browser test, 2026-11-16, Chromium 1.64.1 in the container: two windows with their own cookies, Persian at the third stage on a dark device and English at the opening on a light one, at the same time (`JourneyTests.Two_visitors_…`) |
| M0-10 | The core routes work at 360 px, 412 px and a wide window; keyboard and focus, light and dark, right to left | 4, 6 | **Implemented and checked** | 2026-11-16, the three pages of phase 4 in a browser: no horizontal overflow at 360 and 412 px, a centred column at 1280 px, light and dark, Persian right to left, stand-alone links and controls at least 44 px high. Keyboard order, focus and the later routes are phase 5 and 6. Browser test, 2026-11-16, Chromium 1.64.1 in the container: twelve views in English and Persian at 360, 412 and 1280 px – no sideways scrolling, nothing of the content outside the window, no box of a drawing clipped or overlapping, the Persian font loaded (`JourneyTests.Every_view_fits_…`, 72 cases). Accessibility, 2026-11-16: thirteen views in English and Persian, light and dark, break no rule of WCAG 2.1 A and AA and no best practice that axe-core 4.14 can decide; two findings were corrected first (a role on the form of the appearance, two regions of the same name on the story page) (`InstalledAppTests.No_view_breaks_a_rule_…`, 52 cases); the keyboard reaches the skip link first and the controls of the drawing with a visible focus (`JourneyTests`). Not done: a phone, a screen reader |
| M0-11 | Manifest, scope, start from a deep link and the behaviour on connection loss and update work | 6 | **Partly** | 2026-11-16, Chromium 1.64.1 in the container: the manifest is read without an error – name, start and scope "/", standalone, icons of 192 and 512 px and a maskable one; a deep link opens its page; without a connection a page asked for is the page for "no connection" and with the connection back the same address is the page again; nothing of a collection is kept ([ADR 0007](adr/0007-installable-app.md), `InstallableAppTests`, `InstalledAppTests`).; a newer worker takes over a page that stays open and removes what the old one kept (`WorkerUpdateTests`). **Not observed: an installation** – Chromium names no obstacle except that a window of the tests is a private one, and no other browser was driven |
| M0-12 | Only the demonstration projection is served; fixtures and the editorial input are not web assets | 3, 6 | **Implemented and checked** | 2026-11-16, model part: the public audience gets no projection of the sample; items that are not eligible are absent; reviewer documents are not in the model (`dotnet test --solution Vafadar.Dastan.slnf`, see below). The pages are phase 5. Serving is phase 4 to 6 2026-11-16, files: one route delivers files and asks the projection; the sample, a file by its name and a path that climbs out of the folder are not found; the public site delivers no file ([ADR 0006](adr/0006-media-delivery.md), `GalleryAndMediaTests.At_the_end_of_the_story_nothing_but_the_named_files_…`, `PublicSiteTests`) 2026-11-16: the demonstration asks with every answer not to be indexed, the public site does not; the service worker keeps nothing of a collection (`InstallableAppTests`) |

## What was not checked

State on 2026-11-16, for all conditions together:

* **No phone.** Narrow windows of 360 and 412 px were used, in a desktop Chromium. That is why M0-03 stays "Partly".
* **No installation.** Chromium reads the manifest and names no obstacle but its private window; nobody installed
  the app, in any browser. That is why M0-11 stays "Partly".
* **No screen reader and no visitor.** The accessibility check is the part a tool can decide.
* **Three engines, no Safari.** At M0 the browser tests ran in Chromium only. Since step 9.2 they run in Firefox and WebKit as well – see "Three browser engines" below. Safari itself, on a Mac or a phone, was not used.
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

## After M0 – the comparative timeline (phase 8)

Not one of the twelve conditions of M0. The owner's requirement D-039 and [ADR 0008](adr/0008-charts.md) say what
the chart has to do; this is what was observed. 2026-11-16, .NET SDK 10.0.404, Windows 11 x64, Chromium 1.64.1 in
the Playwright container; `dotnet test --solution Vafadar.Dastan.slnf` with `DASTAN_BROWSER` set: 1230 passed, none
failed, none skipped (state after the stage and the family tree, D-040 and step 8.5). After the story as a chart
(step 9.1): 1008 passed without a browser and 281 in the browser, in two runs – see the second addendum of ADR 0008.
After the connections as a chart (step 9.3): 1012 without a browser and 308 in the browser (Chromium), in two runs.

| What | Status | Evidence |
|---|---|---|
| Several subjects stand side by side over one axis of years, with single events marked | **Implemented and checked** | The comparison "Iran, China and Rome side by side": three lanes, 18 periods and 6 events from 600 BCE to 700 CE; periods of one lane that overlap get rows of their own; every event has a callout with its year and a line to its lane (`TimelineLayoutTests`, `ComparisonTests`, `ComparisonPageTests`) |
| Across the page and down it | **Implemented and checked** | Both drawings come from one layout. Without a choice the screen decides (down below 40 rem), a choice of the visitor holds on every screen (`ChartTests.On_a_wide_screen_…`, `…On_a_phone_…`, `…A_choice_of_the_visitor_…`) |
| Easy to scroll | **Implemented and checked** | The chart scrolls inside its stage in both directions, by touch, wheel and keyboard; across the page the names of the lanes stay at the left, down the page at the top; the page itself does not scroll sideways at 360, 390, 412 and 1280 px (`ChartTests`, `FamilyTreeBrowserTests`) |
| Easy to open further | **Implemented and checked** | Seven scales; the time back to the comparison's own stretch and out to everything it holds; and the steps at the four sides (next row) – all by links, each state an address that can be sent and that Back undoes (`ComparisonPageTests`, `ChartTests.Opening_further_…`, `…Moving_to_an_earlier_time_…`) |
| Larger from each of its four sides (D-040) | **Implemented and checked** | Each side of the stage has a "−" and a "+" of at least 44 px. Timeline: earlier and later years at the two sides the time runs between, the events where their callouts stand, the lanes where the last lane ends. Family tree: ancestors, descendants, and the relatives beside the line at both other sides. A step that cannot be taken is no link and says so; on a Persian page the sides are the same sides of the screen (`ComparisonPageTests.The_time_is_opened_and_closed_…`, `…The_single_events_…`, `…The_lanes_…`, `FamilyAndStoryPageTests.Generations_are_opened_and_closed_…`, `ChartTests.Each_of_the_four_sides_…`, `…A_plus_at_a_side_…`, `FamilyTreeBrowserTests.A_plus_above_the_tree_…`) |
| The whole width and height of the page (D-040) | **Implemented and checked** | The stage of a chart reaches the edges of the page – all of the window on a phone – and is as high as the window; what is read keeps the width of a column of text (`ChartTests.The_stage_takes_the_width_of_the_page_…`, `FamilyTreeBrowserTests.The_tree_has_the_page_…`) |
| A family as a tree (step 8.5) | **Implemented and checked** | Ancestors, descendants and relatives beside the line are opened a step at a time, or the whole family is shown; generations run down the page or across it; partners stand beside each other, a child below the middle of its parents, and every family has a line of its own; never more than the reading position allows (`FamilyTreeTests`, `FamilyAndStoryPageTests`, `FamilyTreeBrowserTests`). Seen in a browser with a family of sixteen in a copy of the sample outside the repository; the sample itself has five |
| Time as it is stated | **Implemented and checked** | No year zero on the axis; "about" and "between two years" are drawn as such and worded in the table; what cannot be placed is named and not drawn (`YearAxisTests`, `ComparisonTests`, `ComparisonPageTests.The_table_states_a_time_…`, `…A_bar_carries_what_it_has_room_for_…`) |
| English and Persian, light and dark | **Implemented and checked** | Persian texts and digits, the Persian font, the time still from left to right; no name and no callout cut and nothing overlapping at the three widths in both languages; no finding of axe-core 4.14 in both appearances (`ChartTests.A_chart_fits_the_window_…`, 30 cases; `…A_chart_breaks_no_rule_…`, 16 cases) |
| The story as a chart (step 9.1) | **Implemented and checked** | On the page of the story the names stand in rows and the steps of the telling in columns, in a stage with its four sides: the first and the last step shown, the names of the steps, the rows. Marks, steps and names are those of the projection at every reading stage tried; never a step beyond the reading position, whatever the address asks for; on a Persian page the chart begins at the right (`StoryChartTests`, `StoryChartPageTests`, `StoryChartBrowserTests`) |
| The connections of a name as a chart (step 9.3) | **Implemented and checked** | The name stands in the middle of a stage: what leads to it above, what it leads to below, what is the same from both sides beside it. A step at a side opens the nearest boxes there and closes the farthest; a single box opens and closes as before. Every relation of the view once, the list below says the same; never a name beyond the reading position, whatever the address asks for; on a Persian page the chart begins at the right (`ConnectionsLayoutTests`, `ConnectionsPageTests`, `ConnectionsChartBrowserTests`). Not solved: with much opened above the name, the name itself is reached by scrolling the stage |
| The timeline of a collection (step 9.4) | **Implemented and checked** | What a collection states about its entities with a date, and when its images and videos were made, as the chart and the table of a comparison at `/<collection>/timeline`. A statement of a later reading stage is on no page before it, whatever the address asks for, and is not part of the entity a visitor gets; the story itself is not on the chart (`DatedStatementTests`, `DatedStatementReaderTests`, `TimelinePageTests`; the page is one of the views of the browser tests). The collection of the repository has no dated statements yet; the tests use their own |
| The same facts without the drawing | **Implemented and checked** | A table per lane with every period and event, its time in full, the state of its review and its sources; a bar and a callout lead to their rows (`ComparisonPageTests.Every_period_and_event_…`, `ChartTests.A_bar_and_a_callout_…`) |
| Only what is released is shown | **Implemented and checked** | The first comparison is released to the local demonstration only: the public site has no link to it and answers its address as not found (`PublicComparisonTests`) |
| The content of the first comparison is right | **Not verified** | Its periods and years are the commonly given ones, written down without sources; every statement is "proposed" and the page says so. Checking them against sources is editorial work that was not done |

Not checked for the charts: a phone, a screen reader, Safari itself, and how a drawing behaves
with hundreds of periods or of people. Timelines of a collection's own records (step 8.6) are not built.

## Three browser engines (step 9.2)

2026-11-16, the Playwright container 1.64.1 of the Compose project `test`: Chromium 156, Firefox 157, WebKit 27.2.
The same 308 browser tests in each engine, each in a run of its own.

| Engine | Passed | Not asked | Why |
|---|---|---|---|
| Chromium | 308 | 0 | |
| WebKit | 307 | 1 | Whether the app can be installed is a question only Chromium answers |
| Firefox | 305 | 3 | The same; and the two tests of the page for "no connection": what Playwright turns off in Firefox is Firefox's own "work offline", which answers from the cache of the browser and does not stop what a service worker asks for |

What the other engines showed, and what was done:

* **Nothing in the site had to be changed.** Every chart, every page at 360, 412 and 1280 px, both languages and
  both appearances pass the same checks in the three engines, the automatic accessibility check included.
* **The tests were wrong in four places**, not the site: they expected Chromium's 40 px for a press of an arrow
  key in a frame; they followed a link while its page was still loading, which WebKit does not always take; they
  expected captions marked as the default to be shown unasked, which is a setting of the visitor in WebKit; and
  Firefox, driven by the tests, forgot the dark appearance of the device on the first page of a window – so the
  dark checks would have run on light pages there. All four are in `BrowserSite` and the tests now.
* **The windows of earlier tests are closed.** Left open they took more than 6 GB in WebKit and stopped Docker on
  the development machine, with every other container on it. The container has a limit of memory now.

After the timeline of a collection (step 9.4), 2026-11-17, 318 browser tests: 318 passed in Chromium; 315 in
Firefox, 3 not asked; in WebKit 316 passed, 1 not asked, and 1 did not get its page within the 30 seconds a test
waits – in a run in which the development machine had no free memory and every engine took twice its time. The
same happened once more when that group of tests was run again; it was not run a third time under a free machine,
so WebKit is **not confirmed clean** for this state. Two tests that failed now and then in WebKit before were
corrected: the font of the Persian text is asked for with the page (a link chosen while the text still moved
was missed), and a test of the story chart asked for more room than a phone of 390 px has.

Run again on 2026-11-17 with free memory, after the rule of the published revision (ADR 0009): 318 passed in
Chromium, 317 in WebKit (1 not asked), 315 in Firefox (3 not asked) – no test failed in any engine. WebKit is
confirmed clean for this state.

Not checked: Safari on a Mac or a phone, Firefox on Android, and the page for "no connection" in Firefox.

## What everybody may see (ADR 0009)

2026-11-17. Not a condition of M0; the first gate of section 8 of the delivery plan.

| What | State | Evidence |
|---|---|---|
| An editorial sample reaches nobody outside a demonstration | **Implemented and checked** | `PublishedRevisionTests`, `PublishedSiteTests.An_editorial_sample_is_not_shown_to_the_public_at_all` |
| Only a numbered revision with nothing but reviewed statements can be approved | **Implemented and checked** | `PublishedRevisionTests`, `PublishedRevisionReaderTests`: an approved collection without a revision, a revision with a statement that is not reviewed, an approved editorial sample are refused, each statement named |
| The public gets no draft text and no media that are not allowed for the public | **Implemented and checked** | `PublishedSiteTests`: a site in the Production settings with revision 3 made from a copy of the collection |
| The reading position holds for the public | **Implemented and checked** | `PublishedSiteTests.The_reading_position_holds_for_the_public_as_for_a_demonstration` |
| A published revision says who reviewed each statement and when | **Implemented and checked** | `RevisionHistoryTests`, `ContentRevisionTests`: a revision without the record, with a review dated after it, a record on a statement that is not reviewed are refused |
| A revision is made from the working file by a command that refuses what is not ready | **Implemented and checked** | `ContentRevisionTests`: revision 1 and 2 of a collection made from the template; wrong number, missing or foreign revision before, a revision as input; never over a file |
| A public page says what each revision changed, without giving a later part away | **Implemented and checked** | `RevisionHistoryTests.A_visitor_gets_the_revisions_newest_first_…`, `RevisionsPageTests` |
| A disputed statement is said to be disputed wherever it is shown, with what is disputed on the page of its sources | **Implemented and checked** | `DisputedStatementTests`: entity, connections, family, story, timeline, evidence; a page without one has no mark. Measured in a browser on a published build in both languages at phone width (marks inside their boxes, no overflow); no picture was taken |
| A withdrawn statement is shown nowhere and is told by the history with its reason, by reading stage | **Implemented and checked** | `DisputedAndWithdrawnTests`, `ContentRevisionTests`, `DisputedStatementTests.A_withdrawal_is_told_by_the_history_…` |
| A work that may not be shown stands in the galleries as a reference: its catalogue record and the way to its holder, nothing of the image | **Implemented and checked** | `MediaReferenceTests`, `GalleryAndMediaTests` (works held elsewhere), `PublishedSiteTests`; `Disclosure` keeps what a reference leaves out a secret |
| A chart drawn around a name shows the name however much is opened; on a phone the chart has the whole width of its stage | **Implemented and checked** | `StageBrowserTests`, in Chromium, Firefox and WebKit |
| The site runs as a container without content, says that it is alive and passes the checks of a host from outside its process | **Implemented and checked** on this machine | `eng/Check-Site.ps1` against the container of `compose.site.yaml` with a revision of the tests; `SiteHealthTests`; [ADR 0010](adr/0010-running-the-site.md) |
| The site is hosted | **Not done** | No host, no address, no certificate |
| A collection is published | **Not done** | The collection of the repository is an editorial sample; nothing of it is reviewed |
| A public site was looked at in a browser | **Not done** | The tests ask the site in memory; no browser and no host were used |

## The store of the editors (phase 11)

| Condition | Status | Evidence |
|---|---|---|
| A collection goes into the store and comes out as it went in, with its translations | **Implemented and checked** | `EditorialStoreTests`, with a copy of the collection of the repository |
| Every change of a record is kept with who, when, why, before and after | **Implemented and checked** | `EditorialStoreTests`: a changed record, a review, a removed record, a changed translation |
| What the site cannot use does not get into the store | **Implemented and checked** | `EditorialStoreTests`: an unusable file and a published revision are refused and change nothing |
| A collection names real persons, organisations, places and regions beside its story and says who wrote, taught, was born or died where, belonged to, founded, lies in – a relation between the wrong kinds of thing is refused | **Implemented and checked** with test data | `RealWorldTypesTests`, `RealWorldPagesTests`. The collection of the repository uses none of it; the pages were not looked at in a browser |
| A collection can name every kind of thing the product documents speak of and say between them what the documents distinguish (DOM-002 to DOM-012); every type has a meaning and a name in both languages | **Implemented and checked** with test data | `WorldVocabularyTests`. No real collection uses the types; no page with them was looked at in a browser |
| A relation of the world says when it held; two tenures are two spans; nothing of the story is dated (DOM-007) | **Implemented and checked** with test data | `RelationTimeTests`, `RealWorldPagesTests`. Not looked at in a browser |
| An entity is found by any of its names and its page names them and its identifiers in other catalogues; a likeness of names merges nothing (DOM-003) | **Implemented and checked** with test data | `EntityNamesTests`, `RealWorldPagesTests`. Not looked at in a browser |
| A collection is about one story, several stories or something that is no story; its file is written in any language (DOM-002) | **Implemented and checked** with test data | `CollectionSubjectTests`, `CollectionSubjectPagesTests`, `ContentToolTests` (template `subject`). Not looked at in a browser; the tool of the editors was not tried with such a collection |
| Editors change the names of the reading stages and the texts of dated statements in every language of a collection, in the tool (step 12.5) | **Implemented and checked** | `EditorialStagesAndDatedTests`, `StudioStagesAndDatedTests`. Not looked at in a browser |
| A quantity with its unit is a statement about an entity, with or without a time, shown on the page of the entity as stated (DOM-005) | **Implemented and checked** with test data | `QuantityTests`, `RealWorldPagesTests`. Not looked at in a browser |
| A real place says where it is and how exactly; a place of a story never does (DOM-011) | **Implemented and checked** with test data | `PlacePositionTests`, `RealWorldPagesTests`. Not looked at in a browser; the `geo:` link was not tried on a device |
| Editors keep comparisons in the store and change them in the tool, with every change recorded (step 12.6) | **Implemented and checked** | `ComparisonStoreTests`, `StudioComparisonsTests`. Not looked at in a browser |
| Editors fill in the time of a statement or of a relation with inputs; what cannot be a time is refused with the form kept (step 13.1) | **Implemented and checked** | `StudioTimeFormTests`. Not looked at in a browser |
| A form opened before somebody else changed what it shows is not kept over that change (step 13.2) | **Implemented and checked** | `StudioStaleFormTests`. Two real browsers were not tried |
| Writing, reviewing and publishing are roles of an account; an editor changes the own password and sees when it was tried (steps 13.3, 13.4; GOV-002) | **Implemented and checked** | `EditorRolesTests`, `StudioRolesTests`. Not looked at in a browser |
| Editors add a record with a form and a language to a collection, without a file (step 13.5) | **Implemented and checked** | `StudioNewRecordTests`. Not looked at in a browser |
| Two entities that are one are merged by an editor with a reason and a trail; a likeness of names merges nothing (DOM-003) | **Implemented and checked** | `EntityMergeTests`, `StudioMergeTests`. Not looked at in a browser |
| A comparison is filled in with forms, lane by lane; a list that has no row yet can be begun (step 13.6b) | **Implemented and checked** | `StudioComparisonFormTests`. Not looked at in a browser |
| The rules of private spaces: acceptance by the other side, both sides' audiences, permission to be found, tags that widen nothing (document 06, step 15.2) | **Implemented as a library and checked** by the ten acceptance examples of the document; **not offered to anybody** – no account, no store, no page | `PrivateSpaceRulesTests` |
| A visitor gets a copy of everything the site keeps about their account as a file, for the password; no secret and nothing of strangers is in it (D-055, step 16.7) | **Implemented and checked** | `OwnDataTests`, `VisitorAccountTests`. Not looked at in a browser |
| A visitor makes an account with a name and a password, signs in and out, sets another password, gets back in with a recovery code, closes the account and opens it again within thirty days; nothing else about the person is kept; a site without a store has no accounts (D-048 to D-050, D-055, step 16.1) | **Implemented and checked**; Google and Microsoft sign-in not built (step 16.8) | `PasswordHashTests`, `VisitorAccountsTests`, `VisitorAccountTests`. Looked at in a browser (en 800 px, fa 375 px, dark). Not tried in a container, with a password manager or a screen reader |
| Signed in, the list and the reading position follow the visitor to every device; a device keeps its own for who is signed out; nothing is taken from a device unasked (B-016, step 16.2) | **Implemented and checked** | `BelongingsAndInvitationsTests`, `AccountFollowsTests`. Looked at in a browser; not tried on two real devices |
| A family tree has an owner who names who may see and who may change it; records of people without an account; every change kept with who made it (D-054, PRIV-002, PRIV-003, step 16.4) | **Implemented and checked** as a list; the drawing of a tree is not built | `FamilyTreesTests`, `FamilyTreePagesTests`. Looked at in a browser |
| An account says that a record of a tree is them; who made the record confirms or refuses, never the claimant; confirming merges nothing and widens no audience; the account takes it back (D-052, PRIV-004, B-017, step 16.4) | **Implemented and checked** | `PrivateSpaceRulesTests`, `IdentityClaimsTests`, `FamilyTreePagesTests`. Not looked at in a browser |
| Two people show a relationship together: one asks, only the other accepts, each side says who else may see it and it is shown to who both allow; either ends it; circles name an audience (B-017, PRIV-005 to PRIV-009, PRIV-012, SC-09, SC-10, step 16.3) | **Implemented and checked** | `PrivateSpaceRulesTests`, `RelationshipsAndCirclesTests`, `PeoplePagesTests`. Looked at in a browser with two accounts. Nobody is told outside the site (no mail service) |
| Somebody with an account invites another by a link they pass on; the invitation makes an account and nothing else, is used once, runs out and can be taken back (R-PER-04, PRIV-006, D-049, step 16.2) | **Implemented and checked** for a link; no mail is sent (no mail service is decided) | `BelongingsAndInvitationsTests`, `AccountFollowsTests`, `PrivateSpaceRulesTests` |
| A visitor keeps pages in a list of their own, without an account; the list never shows more than its pages do (B-016, step 15.1) | **Implemented and checked** | `SavedListTests`. Not looked at in a browser; the list does not follow a visitor to another device |
| A collection leads to the other tellings of its world; the reading position is not carried (R-STORY-02, FLOW-06, step 14.11) | **Implemented**, except a position for each story | `SameWorldTests`, `StageCorrespondenceTests`, `CarriedReadingTests`. A reading position is carried where the editors state the correspondence. A position for each story of one collection is not built. Not looked at in a browser |
| Several collections on one site, each under its own name with its own reading position (step 14.10) | **Implemented and checked** with a second collection of the tests | `SeveralCollectionsTests`, `SiteSearchTests`. Not looked at in a browser |
| Threads the editors lay, each stop with its reason; where to go on from a name; nothing of a later part named or counted (R-EXP-04, R-PUB-06, B-025, step 14.9) | **Implemented and checked** with a thread of the tests | `ThreadsTests`, `EditorialThreadsTests`, `ThreadPagesTests`, `StudioThreadsTests`. The page of a thread was looked at in a browser, the tool was not; the collection of the repository has no thread; no test sends the form of a new thread in the tool |
| A visitor reports an error from a statement; the editors find it in their tool; nothing about the visitor is kept (R-GOV-03, B-015, step 14.8) | **Implemented and checked** | `ReportBoxTests`, `VisitorReportTests`, `StudioReportsTests`. Not looked at in a browser; not tried with the site and the tool on two machines |
| A moment of a video has an address (MED-014, step 14.6); a statement is cited with its revision (R-PUB-07, step 14.7) | **Implemented and checked** | `MomentAndCitationTests`. Not played and not looked at in a browser; an earlier revision cannot be shown |
| Finding by kind, place and year; a gallery by the time of the making and by kind (R-PUB-04, MED-003, step 14.5) | **Implemented and checked** with records of the tests | `FindingTests`. Not looked at in a browser; no gallery of a period or a theme |
| A page names its address in each language and what it is about; a public site lists its pages for engines without anything of a later reading stage; pages print (R-PLAT-04, step 14.4) | **Implemented and checked** | `FindablePagesTests`. Not looked at in a browser, not printed; no picture for the preview of a link |
| The same quantity stated for several years is drawn over time on the page of its entity (step 14.3) | **Implemented and checked** with numbers of the tests | `QuantitySeriesTests`, `SeriesFigureTests`. Looked at in a browser, English and Persian, which found the drawing pressed together and cut – corrected (ADR 0008, eighth addendum) |
| Choosing a year shows what held then and what only may have (R-PUB-03, step 14.2) | **Implemented and checked** with records of the tests | `YearCoverTests`, `MomentPageTests`. Not looked at in a browser; the collection of the repository states no year of the world |
| The real places of a collection are drawn where they lie to each other and listed with what is stated to be there; nothing is loaded from anybody (step 14.1) | **Implemented and checked** with places of the tests | `PlacesViewTests`, `PlacesPageTests`. Looked at in a browser with made-up places (ADR 0008, eighth addendum), which found the labels beside their places – corrected; the collection of the repository names no real place |
| Editors bring an image or a video in through the tool; what is no media file is refused; an image is kept without its metadata (step 13.7) | **Implemented and checked** with files made by the tests | `MediaIntakeTests`, `StudioMediaIntakeTests`. Not looked at in a browser; no real photograph and no large video tried; smaller sizes of an image are not made |
| Editors change a record and record a review in the store without a file in between; what the site could not use is refused and changes nothing | **Implemented and checked** | `EditorialActsTests` |
| Editors publish from the store: the next revision by the rule of ADR 0009, kept as it was published, read by a public site from its content folder | **Implemented and checked** | `EditorialPublishingTests` |
| Editors see in a browser what the store holds: records, what waits, the history of every change, the revisions | **Implemented and checked** | `StudioTests`; measured in a browser at phone width in both languages |
| Editors act in the tool: review, change a record as text, take out, add, publish – done says so, refused says why and changes nothing | **Implemented and checked** | `StudioActsTests`; the forms measured in a browser at phone width |
| Editors change the fields of a record in a form: each field with the input that fits it, choices from the vocabulary of the readers, records to point to, rows to add and take out | **Implemented and checked** | `StudioFormTests`: every record of the sample comes back from its form as it was; measured in a browser at phone width, right to left, and one change sent from the browser |
| Editors change the texts of the other languages of a record in the tool: below the field they translate, kept with the record in one act | **Implemented and checked** | `EditorialTextsTests`, `StudioFormTests`; measured in a browser at phone width, and a row taken out with its Persian role from the browser. Not in the tool: the texts of a statement with a date, of the reading stages, and a new language |
| The store knows who an editor is: an account with a password of which only a derived value is kept; too many wrong passwords close it for a while; another password or a closed account ends every sign-in | **Implemented and checked** | `EditorAccountsTests` |
| With accounts the tool shows nothing to who has not signed in, answers another machine over an encrypted connection only, and keeps every act under the account – whatever name a form gives | **Implemented and checked** | `StudioAccountsTests`; signing in, an act and signing out done in a browser at phone width, right to left. Not checked: a browser signing in through a proxy with a certificate |
| The tool runs as a container for a server: the store and the keys on a volume, alive or not, a copy of the store written while it runs and brought back | **Implemented and checked on this machine** | `StoreCopyTests`, `StudioAccountsTests`; the image built and run, `eng/Check-Studio.ps1` passed (it found a cookie that was not Secure, which was corrected), a store put into the volume, signed in as a proxy would pass it on, a copy written in the container. Not done: a real server, a proxy with a certificate, an account made with a terminal in the container |
