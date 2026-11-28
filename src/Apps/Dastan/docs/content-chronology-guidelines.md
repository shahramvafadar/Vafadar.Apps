# Mythological and historical chronology – guidelines

Given by the owner on 2026-11-28. Part I is the owner's text as given; part II records how it applies to the
present collections and format, what the sources read so far say, and what the format cannot yet express.

# Part I – the owner's guidelines

## 1. Objective

When generating content for Dastan, establish an approximate chronological placement for mythological, legendary,
and semi-historical characters and events whenever reasonable evidence or comparative analysis allows it.

The objective is to integrate mythology, literature, genealogy, civilizations, historical figures, and historical
events into a unified timeline while clearly distinguishing documented history from hypothetical reconstructions.

## 2. Reference case: Rostam Dastan

### Identity

- Persian name: رستم دستان
- English name: Rostam Dastan
- Alternative names: Rostam, Rustam, Rostam-e Zal, Tahamtan
- Father: Zal (زال)
- Mother: Rudaba (رودابه)
- Grandfather: Sam (سام)
- Cultural origin: Iranian mythology
- Associated region: Zabulistan and Sistan
- Primary literary source: Ferdowsi's Shahnameh
- Historical status: Legendary / Mythological
- Associated dynasties: Pishdadian and Kayanian

### Proposed chronological placement

For the purposes of the Dastan timeline, use the following working hypothesis:

- Estimated birth: approximately 1300 BCE
- Hypothetical birth range: 1500–1100 BCE
- Associated historical period: Late Bronze Age
- Chronological basis: Mythological reconstruction
- Confidence level: Very low
- Historically verified: No

The proposed date is an editorial working hypothesis, not an established scholarly estimate of Rostam's birth.

Do not present the birth range as a statistically or historically validated confidence interval.

### Chronological reasoning

According to the Shahnameh, Rostam's life spans the reigns of the following legendary kings:

1. Manuchehr (منوچهر) — birth of Rostam
2. Nowzar (نوذر)
3. Zav Tahmasp (زو طهماسب)
4. Garshasp (گرشاسپ)
5. Kay Qobad (کیقباد)
6. Kay Kavus (کیکاووس)
7. Kay Khosrow (کیخسرو)
8. Lohrasp (لهراسپ)
9. Goshtasp (گشتاسپ) — death of Rostam

One of the most important chronological anchors is the relationship between Goshtasp, Esfandiyar, and Zoroaster.

According to Iranian religious and epic traditions:

- Goshtasp was a supporter of Zoroaster.
- Esfandiyar was the son of Goshtasp.
- Rostam encountered Esfandiyar toward the end of his legendary life.
- Consequently, Rostam and Zoroaster belong to overlapping periods within the traditional narrative.

Scholarly estimates of Zoroaster's lifetime vary considerably, ranging from the second millennium BCE to the sixth
century BCE.

The proposed placement of Rostam around 1300 BCE adopts an early chronological interpretation of Zoroaster's
lifetime.

Alternative chronological interpretations must remain possible.

## 3. Historical context for comparative timelines

Under the proposed chronological hypothesis, Rostam may be displayed alongside the following historical
civilizations, figures, and events:

| Historical entity | Approximate period |
|---|---|
| Late Bronze Age | 1600–1200 BCE |
| Ramesses II | 1279–1213 BCE |
| Hittite Empire | 1650–1180 BCE |
| Mycenaean civilization | 1600–1100 BCE |
| Traditional setting of the Trojan War | Around 1200 BCE |
| Middle Elamite period | Approximately 1500–1100 BCE |
| Shang dynasty of China | 1600–1046 BCE |
| Late Bronze Age collapse | Around 1200 BCE |

These associations represent approximate chronological overlap under the selected hypothesis.

They must not imply documented encounters, direct relationships, or confirmed contemporaneity.

## 4. Alternative historical origins

Distinguish the proposed setting of Rostam's adventures from the possible historical origins of his legend.

Consider the following scholarly hypotheses:

- Eastern Iranian heroic traditions
- Sistan and Zabulistan regional traditions
- Saka and Scythian cultural influences
- Parthian heroic traditions
- Possible connections with the House of Suren
- Possible similarities to Surena, the Parthian commander associated with the Battle of Carrhae in 53 BCE

Do not identify Rostam as Surena or any other historical person without sufficient evidence.

A legendary character may incorporate traditions originating from multiple historical periods.

## 5. General content generation rules

Apply the following methodology to all mythological and legendary entities, including Iranian, Greek, Roman,
Egyptian, Mesopotamian, Indian, Chinese, Norse, and other traditions.

### 5.1. Separate different chronological dimensions

Where applicable, distinguish:

- Narrative chronology: when the character supposedly lived within the story.
- Historical chronology: independently documented historical dates.
- Hypothetical chronology: reconstructed dates based on comparative analysis.
- Literary chronology: when the story was written or recorded.
- Cultural origin: when the underlying oral tradition may have developed.
- Historical inspiration: when possible real-world inspirations lived.

Never merge these dimensions into a single historical assertion.

### 5.2. Classification

Distinguish between:

- Historically documented
- Historically estimated
- Historically disputed
- Semi-legendary
- Legendary
- Mythological
- Fictional
- Unknown

Historical existence and chronological confidence are separate concepts.

### 5.3. Date representation

Support the following concepts wherever the existing data model permits:

- Exact date
- Approximate date
- Date range
- Earliest possible date
- Latest possible date
- Century
- Millennium
- Historical period
- Unknown date
- Multiple competing chronological hypotheses

Conceptual example for Rostam:

```json
{
  "historicalStatus": "Legendary",
  "birthChronology": {
    "estimatedYear": -1300,
    "rangeStart": -1500,
    "rangeEnd": -1100,
    "precision": "Approximate",
    "basis": "MythologicalChronology",
    "confidence": "VeryLow",
    "isHistoricallyVerified": false,
    "isEditorialHypothesis": true,
    "historicalPeriod": "Late Bronze Age"
  }
}
```

This example uses negative numbers to illustrate BCE years. Follow the existing Dastan calendar and date
conventions instead of introducing an incompatible representation.

### 5.4. Chronological consistency

When generating related characters and events:

- Preserve narrative relationships and event ordering.
- Check chronological consistency between parents, children, ancestors, descendants, rulers, and associated events.
- Do not silently invent dates to fill missing information.
- Do not interpret supernatural lifespans as verified biological lifespans.
- Preserve extraordinary lifespans separately as attributes of the original narrative.
- Do not assign exact birth or death dates when the evidence only supports a broad period.
- Allow competing chronological interpretations to coexist.
- Avoid propagating one speculative date through an entire genealogy as though it were verified.

### 5.5. Timeline presentation

When generating content intended for timeline visualization:

- Clearly identify hypothetical dates.
- Visually distinguish historical and mythological entities.
- Preserve uncertainty and alternative date ranges.
- Allow chronological comparison without implying historical relationships.
- Include concise explanations of why an estimated date was selected.

Recommended public-facing wording:

**English:** Approximately 1300 BCE — hypothetical placement based on Iranian mythological chronology. No
historically verified birth date exists.

**Persian:** حدود ۱۳۰۰ پیش از میلاد — جایگاه زمانی فرضی بر اساس گاه‌شماری اسطوره‌ای ایران. هیچ تاریخ تولد مستند
تاریخی برای رستم وجود ندارد.

**German:** Etwa 1300 v. Chr. — hypothetische zeitliche Einordnung anhand der iranischen mythologischen Chronologie.
Ein historisch belegtes Geburtsdatum existiert nicht.

## 6. Research and source requirements

Prioritize reliable sources: Encyclopaedia Iranica, Encyclopaedia Britannica, academic publications, archaeological
research, university publications, critical editions of ancient texts, primary literary and historical sources,
established scholarly reference works.

For Iranian mythology, consult Ferdowsi's Shahnameh, the Avesta, Middle Persian religious and historical texts,
and the Encyclopaedia Iranica articles on traditional Iranian history, Rostam, the Kayanian dynasty, Goshtasp,
Zoroaster, and Parthian heroic traditions. Starting references: https://www.iranicaonline.org/,
https://www.britannica.com/, https://ganjoor.net/ferdousi/shahname.

Verify individual claims and source references before publishing content. Explicitly distinguish statements found
in primary sources, scholarly interpretations, and editorial hypotheses introduced by Dastan.

## 7. Implementation instructions

Before generating or updating content:

1. Inspect the existing Dastan data model and content generation pipeline.
2. Identify existing mechanisms for uncertain dates, historical periods, source attribution, and entity
   classification.
3. Reuse existing capabilities wherever possible.
4. Apply these guidelines to Rostam and related entities.
5. Use this methodology for future mythological content generation.
6. Review existing content for misleading historical certainty and chronological inconsistencies.
7. Report any data-model limitations that prevent accurate representation.

Do not introduce duplicate fields, incompatible date conventions, unnecessary schema changes, or architectural
changes without reviewing the existing implementation.

The primary purpose of this instruction is to improve content quality and chronological consistency. If structural
changes are required, propose them separately before implementation.

## 8. Core principle

Dastan should make mythology and history explorable within a unified knowledge graph and timeline, without
sacrificing historical accuracy.

A useful hypothetical date is preferable to an empty timeline position only when its assumptions, uncertainty, and
non-historical status are clearly communicated.

Never convert speculation into historical fact.

# Part II – how it applies today (2026-11-28)

## What the format already expresses

* **Historical chronology** – `Person`, `Dynasty`, `Period`, `Event` in the history collections carry times with
  their uncertainty: an exact year, `approximate` ("about"), a window (`occurrenceWindow`, `between`), `unknown`
  and `inapplicable`. Centuries and millennia are written as windows. Competing dates stand side by side as a
  window with the summary naming both readings, or in the `note` of a relation.
* **Narrative chronology** – a story has an order and no dates: its steps carry a `narrative_position`, its kings
  follow each other by `successorOf` without a time. A `Character` has no `born` or `died`; the profile of a
  character is its `role` and, optionally, its `origin` (the domain model, DOM-018).
* **Literary chronology** – the `Work` (the Shahnameh, written about 977–1010) carries `created`, and a story
  collection's statements with a date (`dated`) say when its work or a manuscript was made.
* **Classification** – the type separates a `Character` of a story from a `Person` of history; a legend told as
  history in a history collection says so in its label or summary ("The Xia (tradition)", "The founding of Alba
  Longa (tradition)").
* **Historical inspiration** – `portrayalOf` leads from a character of the poem to the person of history it portrays,
  only where a source says so (five Sasanian kings so far).
* **Whose account** – the `note` of a relation says whose account a statement is.

## What the sources read so far say

* **Rostam and Zal are absent from the Avesta**; scholars take the story of Rostam's combat with Esfandiar to be of
  Saka origin, and see in Sistan the place where Saka legends were joined with those of the Kayanians, in a saga
  where the house of Zal and Rostam predominates (Encyclopaedia Iranica, "Esfandīār (Kayanian prince)").
* **Zoroaster's date**: "if anything approaching a consensus exists, it is that he lived ca. 1000 BCE give or take
  a century or so", with proposals from about 1750 BCE to "258 years before Alexander"; serious proposals fall
  between the mid-second millennium and the early sixth century BCE (Iranica, "Zoroaster ii. General survey").
* **Medieval synchronisms**: the medieval historians fitted Iranian, Biblical and Classical history together –
  Moses in the reign of Manuchehr, Kay Qobad in the time of Solomon (Tabari, Balʿami, Ebn al-Balḵi); Biruni instead
  made Kay Khosrow the Koresh of the Bible and his successor Cyrus (Iranica, "Kayānīān xiii").
* **Surena**: neither Britannica's article on Surenas nor the Iranica articles read mention Rostam; the link with
  the House of Suren is not supported by a source read so far and is not written into the collections.

The owner's working hypothesis of about 1300 BCE for Rostam's birth agrees with the medieval synchronism that put
Moses in Manuchehr's reign, and with the early readings of Zoroaster's date; Biruni's synchronism would put the
late Kayanians in the sixth century BCE. Both are chronologies of tradition and of scholars, not dates of history.

## What was done (2026-11-28)

* The Shahnameh collection now says of Rostam, Esfandiar, Goshtasp and of the kings from Manuchehr to Lohrasp what
  the sources say about their place in time: Rostam's absence from the Avesta and the Saka origin of his legend;
  Esfandiar and Goshtasp as Avestan names and the patron of Zoroaster; the medieval synchronisms as the chroniclers'
  reckoning. None of them received a year.
* The collections were checked for characters with a date or a myth told as history: no `Character` carries a
  time; legends in the history collections are labelled as tradition.

## What the format cannot yet express – a proposal for the owner

A hypothetical placement (Rostam about 1300 BCE, within 1500–1100 BCE, very low confidence, an editorial
hypothesis) cannot be written today without either giving a `Character` a historical `born` – which would turn the
hypothesis into a statement of history – or putting the year into a summary, where it reads as a claim. It is
therefore **not written** into any collection yet.

Proposed, to be decided before any implementation (an ADR when approved):

1. **A placement statement** for story collections, beside `dated`: the entity it places, the time (in the existing
   time vocabulary – a year, `approximate`, a window), its `basis` (narrative, synchronism of a named source,
   editorial hypothesis), a `confidence` word, the sources and a short explanation; several placements of one
   entity stand side by side as competing hypotheses.
2. **The comparison** draws placements in a lane of their own or with a distinct style, never as events of
   history, with the wording of section 5.5.
3. **The page of a character** shows its placements under a heading that says they are hypotheses, and its
   narrative chronology (the reigns it spans) beside them.

Until then, the narrative chronology (the reigns of the poem) and the synchronisms of the sources stay in words.
