# Review of the new histories – 2026-11-28

Batch N9 of [the next content plan](../content-plan-next.md): the review of the five collections added in part N
(Egypt, Mesopotamia, Anatolia, Greece, India) and of the three regional comparisons, done as D1–D4 of
[the plan for the three histories](../content-plan-histories.md) were done for Iran, Rome and China. Nothing here is
marked reviewed; every statement stays `source_backed_editorial_review_pending` and every comparison item
`proposed`. The review of an editor is still to come.

## What was checked

| Check | How | Result |
|---|---|---|
| The site can read every file | the content tool's `check` on each collection and comparison | all in order |
| Every quote stands in its source | the content tool's `quotes` with the copies kept outside the repository | 736 of 736 |
| Every dated statement has a quote with its year | each ruler, dynasty, period and event was written from a quoted sentence of the list or page that gives the year | done when written; the statements without a quote are relations without a year (membership, offices, places of events) |
| Persian beside English | a script compares every Persian label and summary with the English: presence, and the numbers of both | all present; the only differences are ordinals and counts written out in words in Persian (11th – یازدهم), which is the rule |
| One record, one thing | every id that two collections share was read in both | two corrected (below) |
| Names | the glossary rules for the five new histories were written and the glossary regenerated from the collections | see [the glossary](../content-glossary-fa.md) |
| The library tests | `Vafadar.Dastan.Data.Tests` and the library tests of `Vafadar.Dastan.Web.Tests` after each batch | pass |

## The collections

| Collection | Entities | Relations | Quotes | Statements without a quote | Sources |
|---|---|---|---|---|---|
| ancient-egypt | 261 (183 rulers, 33 houses, 11 periods, 15 events, 8 places, 5 works) | 415 | 460 | 210 | Britannica (lists of the pharaohs and dynasties, Roman and Byzantine Egypt, Alexandria, the Rosetta Stone, Karnak, the pyramids), Pleiades |
| ancient-mesopotamia | 68 | 96 | 109 | 44 | Livius, Pleiades |
| ancient-anatolia | 42 | 60 | 69 | 24 | Livius, Pleiades |
| ancient-greece | 57 | 66 | 56 | 50 | Livius, Pleiades |
| ancient-india | 32 | 38 | 42 | 19 | Livius, Britannica (the Guptas, the Kushans, the Indus civilization, Harsha), Pleiades |

## Corrected in this review

* **Demetrius II.** Greece had written the Antigonid king of Macedonia under the id the Iran collection uses for the
  Seleucid Demetrius II Nicator. Two different men; the Greek one is now `person-demetrius-ii-of-macedon`.
* **The Hittite sack of Babylon.** Mesopotamia gave about 1595 BCE (the Livius page on the Babylonian empire),
  Anatolia 1587 (the Livius page on the Hittites, by the low middle chronology). The event is now the same in both:
  a window from about 1595 to about 1587, and the summary names both pages and both years.
* Earlier, while the batches were written: the labels of the Persian kings and of Cleopatra VII in Egypt were aligned
  with Iran and Rome (other forms kept as alternative names); the shared events of Egypt and Iran took the Iran ids;
  Cyrus II and the Parthian conquest of Babylonia were given the same Persian name in Iran and Mesopotamia.

## Differences the sources keep, not corrected

* **The founding of Alexandria**: 332 BCE in Egypt (Britannica on Alexandria), 330 BCE in Rome (the source cited
  there). Both stand; an editor can decide whether to note the other year in each.
* **The pyramids of Giza**: Britannica's article on the pyramids dates the 4th dynasty c. 2575–c. 2465 BCE, its list
  of the pharaohs c. 2543–c. 2436. The collection follows the list; the pyramid summaries carry no year.
* **Sennacherib's sack of Babylon**: the Livius page on Babylon says 689, the page on Assyria 703; the event is a
  window over both, as the summary says.
* **Croesus and the fall of Sardis**: the traditional 547 BCE rests on an uncertain reading of a Babylonian
  chronicle; written as about 547 with that doubt in the summary, as in the Iran collection.

## The comparisons

`near-east` (Iran, Egypt, Mesopotamia, Anatolia), `mediterranean` (Egypt, Greece, Rome) and `asia` (Iran, India,
China), beside the existing `iran-china-rome`, following the recommendation of decision F3. Each is built from its
collections by the same rule as `iran-china-rome`: every period and event with years, with the sources of the
collection. An event two collections share – the fall of Nineveh, the conquest of Egypt, the battles of the Persian
wars – stands in both lanes; that is the meeting the comparison is for.

## The new stories of the Shahnameh (G5)

Four story collections were added on the recommendation of decision F6, each made like Forud from the poem's
sections on Ganjoor: `rostam-and-esfandiar` (24 scenes), `bizhan-and-manizheh` (15), `akvan-the-demon` (9) and
`great-war-of-kay-khosrow` (23, drawn from groups of the 47 sections). Every scene carries a verse of its section as
its quote – 75 in all, every one found in the text – and the summaries are the editors' own paraphrase, not a
translation. Checked in this review: every record has its Persian, every role in every scene has its Persian, no
character has a year, every place, object and character takes part in a scene, and every name shared with the
earlier stories is written the same way. Corrected: Lohrasp, written لهراسب after the poem's spelling in one place,
now لهراسپ as in the other collections; Iran, named in the story of Bizhan without a scene, now stands in the scene
at Kay Khosrow's court.

## The look in a browser (2026-11-28, on the owner's word)

The site was run on the library (`dastan-library`, port 5220) and looked at in the built-in browser:

* All 36 pages of the new content answered 200 with their title, in English and Persian: the five histories, the
  three comparisons, the four stories and a sample of their entity pages.
* At phone width in Persian and dark: Egypt (its page, timeline and map), the Near East comparison, the story of
  Rostam and Esfandiar and the page of Ramses II – the text runs right to left, numbers in Persian digits, nothing cut.
* At phone width in English and light: the battle of Salamis and the Mediterranean comparison, whose lanes stack
  one under another with their events in order.
* At desktop width in Persian and dark: the Near East comparison as a chart with its four lanes; dense, as an
  ancient-world chart of 150 records is, but readable. The console showed no errors.

Nothing had to be corrected. One observation for the owner, not a defect of the new content: the head of every story
page – the older ones as much as the new – shows "940 CE to 1025 CE", the span of the only dated record of a story,
the poet. A reader may take it for the time of the story; whether the page should say "the poet's life" or show no
span is a question of the site, not of the collections.

## Open

* **Gaps no free source read closes**, listed in the progress table of the plan: Sumer and Akkad, the Kassites and
  the Assyrian kings between those named; the Neo-Hittite states and Urartu's kings; the Minoans, Mycenaeans and the
  Dark Age of Greece; the Vedic period, the Shungas, the Kushan kings and India after Harsha. The Britannica history of
  Mesopotamia now asks for a subscription, and the bot check of UCL's Digital Egypt was not passed – neither was
  worked around.
* **The decisions F1–F6** of the plan. Part N followed the recommendations of F2–F5 and G5 that of F6 as working
  defaults, which the owner can change; F1 (hypothetical placements) is a change to the format and is the owner's to
  approve before G7 can be done.

