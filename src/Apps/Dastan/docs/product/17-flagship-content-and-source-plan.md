# 17 — Flagship Content and Source Plan

**Product:** Dastan  
**Version:** 0.6  
**Date:** 2026-11-15  
**Status:** Source-backed editorial sample and proposed content plan  
**Selection status:** Rostam and Sohrab is a provisional flagship, not a confirmed launch commitment  
**Companion data:** [shahnameh-pilot.json](../../content/samples/shahnameh-pilot.json)

## 1. Purpose and authority

**Accepted:** The user selected the product name **Dastan** and authorized the next stage: a bounded release PRD, representative content, and main journey design. The intended audience is the curious general public. Useful visual exploration, concise introductions, connected subjects, and contextual media remain central. Documentation is English; this does not decide the interface language.

**Proposed:** Use a selected Rostam and Sohrab arc from the Shahnameh to exercise the common model. It offers people, an animal character, a meaningful object, places, family connections, actions, narrative order, and later artistic depictions within one manageable collection. This selection does not commit Dastan to becoming only a Shahnameh service. Company, team, historical, and optional personal uses continue to share the wider concepts.

The [release PRD](16-discovery-pilot-prd.md) owns release scope. The [discovery brief](18-discovery-flows-and-design-brief.md) owns the main screen and journey contracts. This document owns the actual flagship sample, its evidence, and its editorial limitations. It does not claim a working application, completed specialist review, or a complete edition of the poem.

## 2. The selected name and the classical association

The intended association with the Persian word for a story is compatible with the selected Latin-script name. The classical association needs a precise correction: **Dastan is a name of Zal, Rostam's father.** It should not be introduced as Rostam's own epithet. [Encyclopaedia Iranica's Zal entry](https://www.iranicaonline.org/articles/zal/) identifies both the alternative name and the parentage. In a [primary passage outside this selected arc](https://ganjoor.net/ferdousi/shahname/kkavoos/sh7), Rostam introduces himself and names Dastan, Sam, and Nariman as his lineage.

Suggested brand wording is: “Dastan evokes stories and a connection to the Shahnameh through Dastan, the name of Zal, father of Rostam.” This is a proposed explanation of the user's chosen name. It is not an etymological claim that the common word and the proper name are identical, or evidence of domain or trademark availability. The [brand brief](11-brand-and-positioning.md) owns those separate naming decisions.

## 3. What the sample contains

The initial companion dataset contains **14 entities, 20 typed relationship claims, 8 selected narrative events, 18 source records, and 5 media records (3 external image references, 1 original local video, and 1 original local image)**. These are actual records prepared for design work. They are not synthetic company or family fixtures, but neither are they assertions that literary characters or events have established historical dates.

| Group | Included records | Boundary |
|---|---|---|
| Work and arc | Shahnameh; Rostam and Sohrab | One selected arc, not the complete work |
| Characters | Rostam, Zal, Tahmineh, Sohrab, the king of Samangan, Afrasiab, Kay Kavus | The unnamed king remains unnamed |
| Other subjects | Rakhsh; Rostam's keepsake token | An animal and an object retain distinct types |
| Places | Samangan, Zabulistan, Turan | Narrative places and regions; no modern coordinates or border equivalence |
| Relationships | Parentage, marriage, riding, visiting, support, opposition, a summons, a gift, mourning, and other scoped actions | Relations retain their meaning and context; opposition is not a permanent personal identity |
| Media | Two Met image references; one UMMA reference awaiting permission verification; one original local explanatory video and one original family diagram | Museum image bytes are not bundled; the original diagram image, video and their companions are bundled |

Intervening episodes are condensed. The sample does not attempt to represent every encounter, commander, battle, verse, or causal explanation. For example, it does not add complete entries for Gordafarid, Hojir, Gudarz, or every messenger merely because they appear in surrounding passages. A later expansion should add them with their own evidence rather than treating this limited cast as complete.

An original approximately 20-second explanatory diagram video is bundled with a poster, English captions and a transcript. Its four scenes introduce a connection, show Zal to Rostam, show Rostam/Tahmineh to Sohrab, then combine the parentage links. It is present-day explanatory content, not a historical recording or implemented product demonstration. The video and its derivatives require spoiler level 2. [Video](../../content/media/dastan-parentage-explainer.mp4) · [Captions](../../content/media/dastan-parentage-en.vtt) · [Viewer transcript](../../content/media/dastan-parentage-transcript-en.txt) · [Reviewer transcript and provenance](../../content/media/dastan-parentage-transcript.md).

## 4. Base text and edition context

The selected base is the actual verse in **Ganjoor's online Shahnameh transcription, Sohrab sections**, observed on 2026-11-15. The pages attribute their initial source to **WikiDorj**. The precise critical print edition behind this transcription has not been independently established. The dataset records that limitation explicitly and does not label it the Moscow edition or another critical edition without evidence.

The edition identifier is `edition-ganjoor-sohrab-observed-2026-11-15`; the shared world identifier is `world-shahnameh`. These identify the chosen context, not all possible Shahnameh traditions. A different edition or adaptation must remain distinguishable. Record genuine conflicts as alternative statements with provenance; do not silently combine them into one supposedly certain version.

The English descriptions are original short paraphrases prepared for this sample. They are not a copied modern translation. Ganjoor's AI-generated summaries and simplified paraphrases are excluded from the evidence base. The site itself warns that those generated sections can be wrong; the selected claims were checked against the verse instead. See, for example, the poem and separate generated material in [section 17](https://ganjoor.net/ferdousi/shahname/sohrab/sh17).

For a publication-ready edition, a specialist should confirm the base edition, the exact passage locators, transliteration choices, and any interpretation that affects the short overview. This is a remaining editorial task, not a reason to hide the useful sample or call its sources unexamined.

## 5. Claim and source inventory

Each relationship and event has source IDs. A source record includes a URL, the observation date, source type, and a descriptive passage locator. These locators identify passages without inventing stable verse numbers for an online transcription that may change.

| Source IDs | Primary passage or record | Use in this sample |
|---|---|---|
| `src-ganjoor-index` | [Sohrab index](https://ganjoor.net/ferdousi/shahname/sohrab) | Work/arc hierarchy and section order |
| `src-ganjoor-s02`, `src-ganjoor-s03` | [Section 2](https://ganjoor.net/ferdousi/shahname/sohrab/sh2), [section 3](https://ganjoor.net/ferdousi/shahname/sohrab/sh3) | Opening journey, horse, hospitality, and setting |
| `src-ganjoor-s04` | [Section 4](https://ganjoor.net/ferdousi/shahname/sohrab/sh4) | Encounter, marriage, and keepsake |
| `src-ganjoor-s05`, `src-ganjoor-s06` | [Section 5](https://ganjoor.net/ferdousi/shahname/sohrab/sh5), [section 6](https://ganjoor.net/ferdousi/shahname/sohrab/sh6) | Parentage, expedition, and competing intentions |
| `src-ganjoor-s09` | [Section 9](https://ganjoor.net/ferdousi/shahname/sohrab/sh9) | Summons and Rostam's stated knowledge |
| `src-ganjoor-s17`, `src-ganjoor-s18` | [Section 17](https://ganjoor.net/ferdousi/shahname/sohrab/sh17), [section 18](https://ganjoor.net/ferdousi/shahname/sohrab/sh18) | Combat and later recognition |
| `src-ganjoor-s19`, `src-ganjoor-s20`, `src-ganjoor-s21` | [Section 19](https://ganjoor.net/ferdousi/shahname/sohrab/sh19), [section 20](https://ganjoor.net/ferdousi/shahname/sohrab/sh20), [section 21](https://ganjoor.net/ferdousi/shahname/sohrab/sh21) | Remedy, death, and aftermath |
| `src-iranica-zal`, `src-ganjoor-lineage` | [Iranica](https://www.iranicaonline.org/articles/zal/), [primary lineage passage](https://ganjoor.net/ferdousi/shahname/kkavoos/sh7) | Zal/Dastan identity and Rostam's parentage |
| `src-met-452419`, `src-met-453265`, `src-met-open-access` | Museum records and policy in section 8 | Artwork metadata and reuse basis |
| `src-umma-1963-1-46` | Museum record in section 8 | A candidate reference with unresolved image-use permission |

An important source-quality distinction emerged. In the selected verse, Rostam explicitly refers to having a son in [section 9](https://ganjoor.net/ferdousi/shahname/sohrab/sh9). The Met's [object 453265 description](https://www.metmuseum.org/art/collection/search/453265) simplifies the story by presenting him as unaware that he has a son. The sample follows the selected poem for narrative claims and uses the museum record for artwork metadata. It therefore describes failure to recognize the opponent, without claiming that Rostam did not know a son existed. This difference is not automatically evidence of a separate textual edition.

## 6. Time, sequence, and the eight selected beats

Every event uses `orderBasis: narrative`. Its `entityAssociations` identify each subject's role and presence meaning; they replace the ambiguous version 0.5 `participantIds` list. Rostam being named as Sohrab's father in `event-03`, for example, is a mention, not an attendance claim. Places, objects, actors and subjects of reports remain distinct. A beat may condense several scenes, so even two acting characters are not automatically co-located or simultaneous. Its ordinal position orders this editorial selection. It is not an elapsed duration, a calendar date, a lifespan, or a claim that characters are continuously present between two positions. Omitted passages remain omitted; equal spacing in a diagram cannot imply equal elapsed time.

| Event | Editorial beat | Minimum spoiler level |
|---|---|---:|
| `event-01` | Missing horse and welcome | 0 |
| `event-02` | Meeting and keepsake | 1 |
| `event-03` | Lineage and intention | 2 |
| `event-04` | Expedition and rival plans | 2 |
| `event-05` | A chance to stop the fighting | 3 |
| `event-06` | Recognition after the fatal encounter | 4 |
| `event-07` | Remedy and death | 4 |
| `event-08` | Mourning and return | 4 |

The JSON contains the fuller short descriptions and their evidence. No BCE, CE, geological, or modern date is assigned to the narrative events. No midpoint or January 1 value fills missing time. Real creation dates appear only on later artworks and the present-day explanatory video and illustration; they describe those media objects, not the literary events depicted. CE is recorded as an era, separately from calendar. The museum records do not specify a calendar convention, so the sample leaves it unspecified; the known 2026 video and illustration creation dates use Gregorian. The general temporal model in [document 04](04-time-and-uncertainty.md) remains responsible for approximate dates, intervals, relative chronology, and deep time in other collections.

## 7. Spoilers and reader knowledge

The following boundary is **proposed**, not a final reader preference policy:

| Level | Reader-facing stage | Intended visibility |
|---:|---|---|
| 0 | Opening and orientation | Work, opening situation, suitable background connections, and a non-revealing entry |
| 1 | Samangan encounter | The meeting, marriage, and token |
| 2 | Lineage and expedition | Sohrab's parentage and the developing plans |
| 3 | Encounters in combat | The selected combat episode before the resolution |
| 4 | Complete selected arc | Recognition, death, aftermath, and revealing media |

The reader learns Sohrab's parentage early in the narrative, in section 5. The characters' later recognition is a different event. Hiding the parentage until the ending would misrepresent the narrative's use of dramatic irony. Conversely, putting the ending in a default relationship label would spoil a visitor who has only opened the introductory view.

Filter before rendering or counting. The boundary applies to summaries, edges, paths, source titles and snippets, thumbnails, captions, recommendations, and restored navigation state. A revealing artwork cannot have a hidden caption but a visible revealing thumbnail. A visitor who lowers the boundary must not recover future information through the Back action.

The complete JSON is an editorial input containing the ending. Its explicit publication state permits local demonstration work and records that public release is not yet approved. Local-demo eligibility does not waive the stated source, rights or reader-boundary rules. It is **not** a safe public response for a spoiler-limited client. The first implementation must derive the permitted subset and recompute dependent references, labels, counts, paths and recommendations before responding. Keep the editorial JSON outside public static directories and service-worker precaches. A deliberately full-story editorial demonstration must be clearly labelled and must not be described as spoiler-filtered. External source pages may contain more than the cited passage; opening them cannot promise the same spoiler boundary.

For an explanatory parentage diagram, `relation-02` uses `src-iranica-zal` and `src-ganjoor-lineage` at level 0. `relation-04` and `relation-05` use `src-ganjoor-s05` at level 2. A diagram combining Zal, Rostam, Tahmineh, and Sohrab therefore requires at least **level 2**, regardless of its modern creation date.

## 8. Media evidence, rights, and availability

| Media record | Catalog evidence | Reuse and delivery status | Context safeguard |
|---|---|---|---|
| `media-met-452419` | [Met 452419](https://www.metmuseum.org/art/collection/search/452419), approximately 1450; catalog does not identify a painter | Object page marks the image Public Domain; eligible under the [Met's CC0 policy](https://www.metmuseum.org/hubs/open-access). External reference only in this package | An earlier Rakhsh-capture episode, outside the bounded arc; do not attach it as an image of the later theft or recovery |
| `media-met-453265` | [Met 453265](https://www.metmuseum.org/art/collection/search/453265), approximately 1610; catalog does not identify a painter | Public Domain marker and the same CC0 policy verified; external reference, with runtime delivery not established | Ending-sensitive depiction, level 4; object creation date is not event time |
| `media-umma-1963-1-46` | [UMMA 1963/1.46](https://umma.umich.edu/objects/tahmina-comes-to-rustam-from-the-shahnama-of-firdausi-1963-1-46/), approximately 1460; Iranian attribution | The page directs publication users to a rights request. No permission was requested or established; `url` remains null and public image rendering is excluded | A meeting depiction at level 1; the catalog is not authority for a modern location match |

Ferdowsi's credit as literary author does not make him the painter of a manuscript created centuries later. Each record distinguishes the underlying work from the unidentified image creator. Digital capture, upload, publication, and share dates remain unknown rather than inheriting the artwork's creation year.

The dataset contains source URLs, rights observations, and credit information. It does not contain museum image bytes, a verified local gallery, completed alt-text review, or a third-party video license. The external museum-image descriptions are explicitly marked as drafts pending visual inspection; the original local diagram has its own project-authored description. Actual delivery, image quality, crops, accessibility descriptions, and runtime availability must be checked before displaying these candidates in a public product. The bundled explanatory diagram video and separate family illustration supply real local media for the first viewer implementation without a network dependency or an archival claim. The new image is available as [PNG](../../content/media/dastan-family-connections.png) and [SVG](../../content/media/dastan-family-connections.svg), with its own `media-dastan-family-illustration` identity, representation label, sources, alt text and level 2 boundary. The video container metadata and representative diagram frames were checked. The new illustration is an exact diagram whose labels and links must match the three cited parentage claims. Product playback, streaming, access and spoiler behavior remain unimplemented.

## 9. Useful first-layer content and connected exploration

Proposed opening copy:

> A missing horse brings Rostam to Samangan. Explore the people, places, and choices around this Shahnameh story through connections, a story sequence, and later illustrations. Choose how far into the story you want to see.

This entry needs no prior name search, research skill, account, or family setup. A visitor can start with Rostam, Rakhsh, or the setting, then follow a labeled connection. More detail and the supporting passage are optional, while the literary context and edition limitation remain visible in concise form.

At level 2, a family view can show Zal to Rostam and Rostam/Tahmineh to Sohrab. The story sequence uses the same entities. The gallery can link a permitted depiction to its subjects and return to the same story position. Selecting an illustration should identify whether it depicts this arc or related background. Switching views preserves focus, world, edition, spoiler boundary, and the useful return position.

These are content-supported design routes, not completed interfaces. They correspond to `R-EXP-01` through `R-EXP-06` and the media rules in [document 15](15-media-and-galleries.md). The sample makes these routes concrete without requiring a complete Shahnameh catalogue.

## 10. Review gates and unresolved choices

The following are proposed content acceptance checks for the next stage:

1. Every displayed narrative claim resolves to an appropriate passage in the selected context; a museum synopsis does not silently replace the base text.
2. All entity, relation, event, source, and media references resolve within the dataset. A relation's type and direction agree with its label and explicit scope; an event association retains its role without inventing attendance or co-location.
3. Narrative axes never display invented calendar dates or equal-duration implications. Art creation dates remain separate from depicted time.
4. Each spoiler level produces a coherent permitted view, including counts, media, evidence labels, and Back behavior. Raw editorial data is not described as filtered output.
5. Images awaiting permission or visual review are handled honestly. A reference link is not counted as a downloaded, accessible, tested media asset.
6. A complete relation diagram can be traced to the parentage claims and carries the correct spoiler threshold, source IDs, and representation label.
7. Short introductions remain useful to a casual visitor; essential uncertainty is not buried in a source panel.

Remaining decisions are the user's confirmation or replacement of the flagship, the selected critical edition for publication, the initial reader boundary and controls, a consistent transliteration scheme, whether and how to add omitted episodes, and which media candidates should enter a public release. Specialist literary review, museum-image visual and alt-text review, and product usability testing remain unperformed. Representative frames of the original explanatory video were inspected; that inspection does not establish product playback, access or spoiler behavior. The source-backed sample and this plan make those reviews concrete; they do not claim those gates have passed.

## 11. Input format and repeatable validation

The sample now uses `schemaVersion: dastan-pilot-0.2`; the cross-domain review fixtures use `formatVersion: 0.2`. Existing record IDs retain identity. The original dataset identifier is also retained even though its legacy spelling contains v0.5; `documentVersion` identifies this revision. Version 0.2 intentionally changes the ambiguous event field to `entityAssociations`, with `{entityId, role, presence}`. Relationship `scope` and `directionality` support correct labels and bounded claims. This input format is not a production database or API commitment.

Run `python tools/validate_samples.py --self-test` from the extracted package directory, or pass the script's absolute path from another directory. Python 3.10 or later and the standard library are sufficient. The tool checks unique IDs, references, narrative associations, source and media boundaries, local file paths, temporal field distinctions and the declared synthetic access examples. Its negative checks mutate copies in memory to confirm rejection of selected invalid inputs; source files are not changed.

A successful run establishes **package input integrity and fixture consistency only**. It does not establish historical truth, literary specialist approval, app authorization, safe endpoints, media playback, device installation or user comprehension. The implementing developer must validate the actual projection and user journeys separately.

For the first local demo, only the two original local media records have `demoRenderEligibility.allowed: true`; both require reader level 2. The three museum candidates remain in editorial input and may be promoted only after the applicable delivery, visual/accessibility and rights checks are recorded. The UMMA image remains uncleared. The sample does not count references or placeholders as functioning gallery assets.

### Reviewer references and safe evidence routes

`reviewerProvenancePath` is a package-only review reference. It must not become a product source link, static download, media action or metadata URL. The full source plan and combined transcript/provenance Markdown contain links to the complete editorial dataset and can reveal later story material. Both original media records therefore leave `sourceUrl` null and supply reviewer provenance separately.

Build visible evidence from the media record's `sourceIds` through the same publication/reader-boundary projection used by other claims. The video's `transcriptUrl` points to a separate [plain-text viewer transcript](../../content/media/dastan-parentage-transcript-en.txt), containing the transcript and descriptive account only; render it as text after the level 2 check. Its captions and poster use that same media boundary. The reviewer Markdown remains in this documentation package for provenance, without being copied into a product transcript route.
