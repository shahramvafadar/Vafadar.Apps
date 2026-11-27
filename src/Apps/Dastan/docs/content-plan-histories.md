# Content plan – the three histories, whole

Written on 2026-11-27 on the owner's word: first carry the histories of Iran, Rome and China **forward** until
they are complete to one common horizon, then, where sources allow, **back** before their present beginning,
then add **detail** throughout, and finally **review everything written so far**, separately and completely. The
plan is laid out so that it can be carried out as one long goal, batch after batch, without asking again – the
decisions it needs are listed first and are the owner's.

It continues [the content roadmap](content-roadmap.md) (passes 2, 5 and 6 for the histories) and follows
[the work order](content-work-order.md) and [the guide](content-guide.md) in everything: a source read for every
statement, never more exact than the source, own words, nothing marked reviewed or released by the writer.
Other civilizations (Greece, Egypt, India, Central Asia, Armenia – pass 3 of the roadmap) come after this plan,
as the owner said; so do the remaining stories of the Shahnameh (1.6 to 1.10), unless the owner puts them in.

## Where the histories stand (2026-11-27)

| Collection | From | To | People | Events | What is missing inside the span |
|---|---|---|---|---|---|
| `ancient-iran` | Awan and Šimaški (no year), Middle Elamite c. 1500 BCE | Yazdgard III, 651 | 127 | 56 | Elamite rulers before 1500 BCE; Sasanian administration, Tansar and the Avesta, the Lakhmids, the Hephthalite wars in detail; parents of Valkaš and Kavad I (Livius contradicts itself); 30 people without any year |
| `ancient-rome` | Founding, 753 BCE (tradition) | Heraclius, 641 | 118 | 63 | Libius Severus (461–465), Zeno (474–491), Basiliscus; Odoacer, Theodoric, Attila, Belisarius only named; the provinces; 10 people without any year |
| `ancient-china` | Xia (tradition), Shang c. 1600 BCE | Wuhou's abdication, 705 | 70 | 32 | The emperors between the founders of each house (Jin after Wudi, Northern Wei but Xiaowendi, the southern dynasties but Wudi of Liang, Sui Gongdi); the Tang after 705; 20 people without any year |
| `iran-china-rome` | 2100 BCE shown | 705 | – | 150 | everything after 705; few events where the lanes meet |

## Decisions for the owner, before the goal starts

Each with the options and a recommendation. The goal begins only when these are answered; a later answer
changes only the batches after it.

| # | Decision | Options | Recommended |
|---|---|---|---|
| E1 | **How far forward** – the common horizon | (a) about 1025 CE, the time of Ferdowsi: Iran to Mahmud of Ghazni, Byzantium to Basil II, China to the early Northern Song; (b) 907, the end of the Tang; (c) 1453, the fall of Constantinople | (a): it meets the Shahnameh as a work, closes roadmap 2.2, and the three lanes end together |
| E2 | **How far back** | (a) to the Neolithic, about 10,000 BCE, as periods in deep time; (b) to the beginning of writing, about 3200 BCE; (c) not at all | (a) for periods and a few sites; people only from the beginning of writing |
| E3 | **Rome after 641** | (a) the same collection goes on as the East-Roman (Byzantine) empire; (b) a new collection `byzantium` | (a): Livius and Britannica treat it as the same state; one lane in the comparison |
| E4 | **Iran after 651** | (a) the same collection, its label becoming "Iran" rather than "Ancient Iran"; (b) a new collection for Islamic Iran to the Ghaznavids | (a): one lane; the label changes, the identifier `subject-ancient-iran` stays |
| E5 | **Looking at the site** during the goal | (a) allowed after each phase, on the owner's word given once in the goal; (b) never, the owner looks | (a): the chart with hundreds of events must be seen; the work order allows it only when the owner says so |
| E6 | **A crowded chart** | (a) content only – events stay as they are; (b) a development step to let the comparison show fewer events at once (by importance or zoom) | (b) as a separate development item after phase A, not part of the content goal |
| E7 | **The Shahnameh stories 1.6–1.10** | (a) outside this goal; (b) one story after each phase | (a): the owner asked for the civilizations first |
| E8 | **Pictures** (pass 5) | (a) only references to museum pages whose terms allow showing; (b) none in this goal | (b): the rights of pictures are the owner's (backlog D2) |

**As taken (2026-11-28).** The owner set the goal to carry out this plan to the end without answering E1–E8
one by one; the recommendations are taken: E1 (a) 1025 CE, E2 (a), E3 (a), E4 (a), E6 (b) outside the goal,
E7 (a), E8 (b). For E5 the goal gives no word to start the site, and the work order allows it only on the
owner's word, so E5 is taken as (b): the site is not started and batch D5 is left to the owner.

## How every batch is done

The goal runs as a sequence of **batches**. One batch is one civilization and one stretch of time – never two
civilizations at once, as the owner asked. A batch is finished only when all of these hold:

1. **Sources first.** Find the pages, read them, keep the text as `.local/sources/<collection>/<source id>.txt`
   and the passages each statement rests on in `.local/quotes/<collection>.quotes.json` (guide, "Comparing
   quotes with their sources"). Neither is committed.
2. **Write**: sources, then entities, then relations, then the Persian – with a generator script outside the
   repository, never by hand in a shell here-document.
3. **Check**: `check` says "in order" with no notice; `quotes` finds every quote; every record has its Persian;
   no source is listed that nothing cites.
4. **Comparison**: the comparison is rebuilt from the collections (every period and event with years, same
   years, same uncertainty, same sources).
5. **Tests**: after each batch the tests that read the library (`LibrarySiteTests`, `LibraryTests`); the whole
   suite of Dastan at the end of each phase. A browser test that fails only on a time budget is run once alone;
   if it passes alone it is recorded as unsteady, not as a failure of content.
6. **Record**: a line in the changelog, the batch marked done in this file with the day and what was left out
   for want of a source.
7. **Commit** on `content/dastan-<name>`, merged into `main`, pushed – as the owner, naming no tool.

**When a source cannot be read**: Iranica, Britannica and the Metropolitan Museum refuse automated reading;
read them page by page in a browser, one page at a time, as a reader would. If a permission is refused, do not
work around it: leave the statement out, name it under "left out", and go on.

**Never**: a year from memory, a parent from "son of" in a name, `reviewed` or `released`, a picture without
established rights, a change of the software inside a content batch.

## Phase A – forward to the common horizon

First the gaps inside the present spans, then onward. Order of the batches: Iran, Rome, China, then the
comparison – and again, so that no lane runs far ahead of the others.

| Batch | Collection | Stretch | What | Sources to try first |
|---|---|---|---|---|
| A1 | Rome | gaps to 641 | Libius Severus, Zeno, Basiliscus; Odoacer, Theodoric, Attila, Aetius, Belisarius, Stilicho as people; the Vandal and Ostrogothic kingdoms as governments where an event needs them | Livius; De Imperatoribus Romanis (`roman-emperors.org`); Britannica |
| A2 | China | gaps to 705 | every emperor of the Western and Eastern Jin, of the Northern Wei, of the Liu-Song, Qi, Liang and Chen, of the Northern Zhou; Sui Gongdi; the Sixteen Kingdoms as named states | Britannica; Asia for Educators; Smithsonian |
| A3 | Iran | gaps to 651 | the Sasanian administration and offices (spahbed, mobad), Tansar, Mazdak, Bozorgmehr only where history names him, the Lakhmids, the Hephthalites as a people, the Res Gestae of Shapur as a work, Peroz III's flight to China | Livius; Iranica (browser) |
| A4 | Iran | 651–1030 | the caliphate in Iran as periods; Tahirids, Saffarids, Samanids, Ziyarids, Buyids, Ghaznavids with their rulers; Abu Muslim, Ya'qub the Coppersmith, Isma'il the Samanid, Rudaki, Daqiqi, Ferdowsi (`person-ferdowsi`, the identifier of the Shahnameh collection), Avicenna, Biruni, Mahmud of Ghazni; events: Nehavand's aftermath, the Abbasid revolution (750), Talas (751), the founding of Baghdad (762), the completion of the Shahnameh | Iranica (browser); Britannica (browser) |
| A5 | Rome | 641–1025 | the East-Roman emperors from Constans II to Basil II with their houses (Heraclian, Isaurian, Amorian, Macedonian); the Arab sieges of Constantinople, iconoclasm, the loss of Egypt and Syria, the Bulgarian wars | Britannica (browser); De Imperatoribus Romanis |
| A6 | China | 705–1025 | the Tang emperors to 907, the An Lushan rebellion, Talas, the persecution of 845, Huang Chao; the Five Dynasties and Ten Kingdoms; the Northern Song to Zhenzong | Britannica (browser); Asia for Educators; Smithsonian (Song) |
| A7 | Comparison | to 1025 | rebuilt; the shown stretch to 1025; the events where lanes meet, entered in each lane they concern (Talas in Iran and China; the Sasanian envoys to the Tang; Heraclius and Khusrau II; Roman envoys in 166) | the collections |

**Phase A is done** when each history reaches the horizon of E1 with every ruler of every house that held the
throne it follows (or the gap named), each with the years of the reign as the source gives them.

## Phase B – back before the present beginning

Only as far as sources that can be read go; periods and sites before writing, people only after it.

| Batch | Collection | What | Sources to try first |
|---|---|---|---|
| B1 | China | the Neolithic cultures (Yangshao, Longshan, Hongshan, Liangzhu) and Erlitou as periods; the Shang kings the oracle bones confirm, beyond Wu Ding | Asia for Educators; Smithsonian (Neolithic); University of Washington, Visual Sourcebook |
| B2 | Iran | Ganj Dareh and the first villages, Susa I, the Proto-Elamite period, Shahr-e Sukhteh, Jiroft, Awan and Šimaški with the rulers the king lists name | Livius (Elam, Susa, king lists); Iranica (browser) |
| B3 | Rome | Bronze Age Italy, the Villanovan culture, the Etruscans, the Greek colonies of the south, the Latins and Alba Longa as tradition | Livius; Britannica (browser) |
| B4 | Comparison | the earlier periods in each lane; the shown stretch unchanged, a visitor moves back | the collections |

Times before the calendar are written as the source states them – "about 6000 BCE" as an approximate year;
a depth of time (`frame: age`) only where the source counts so.

## Phase C – detail

Over the whole span, one civilization per batch (C1 Iran, C2 Rome, C3 China, then again for the next stretch):

* **People beyond rulers**: writers, thinkers, generals, queens and wives, builders – where a source dates them.
* **Works and monuments**: inscriptions, histories, buildings, with their makers (`authorOf`, `commissionerOf`)
  and where they are kept.
* **Places**: every capital and battle site with a position from Pleiades or GeoNames; regions with `partOf`.
* **Offices and titles** beyond the throne, where a person held them with years.
* **Relations** the sources state: spouses, children, teachers, allies and enemies.
* **People without any year** (about 60 now): look for a source once more; where there is none, leave them
  as they are.
* **Links between collections**: `portrayalOf` from a king of the Shahnameh to the king of history, only where a
  source (Iranica) says the poem portrays him – the Sasanian part of the poem first.

## Phase D – review of everything written so far

Separate from phases A–C and done after them (or, if the owner says so, before them). The writer's own
review: it finds and corrects, and records what it did; it does **not** set `reviewed` – that is the owner's
(work order, part 1; ADR 0009).

| Batch | What | How | Result |
|---|---|---|---|
| D1 | Every statement against its source | for each collection: quotes for every relation, event and dated statement; `quotes` run until every quote is found; each year compared with the passage | corrections committed; a list of statements that no source supports, removed or rewritten |
| D2 | Consistency between collections | one script over all files: the same identifier has the same name and years everywhere; known doubles merged (`character-bahram` / `character-bahram-pahlavan`, `character-human` / `character-houman`, Farud / Forud); the comparison says the same as the collections | corrections committed |
| D3 | The Persian | one glossary of names for the three histories and the Shahnameh (`docs/content-glossary-fa.md`), the forms Persian books of history use; every translation brought to it; summaries checked against the English, never saying more | corrections committed; translations stay `draft` |
| D4 | The stories of the Shahnameh | each step against its section on Ganjoor; reading stages checked for a later stage named early; `correspondences` between stories where a reader certainly read a stage of another | corrections committed |
| D5 | Looking at it | if E5 allows: each collection and the comparison in English and Persian, light and dark, at phone width; what looks wrong noted and corrected | a review note in `docs/reviews/` |
| D6 | The report | one file `docs/reviews/<day>-content-review.md`: what was checked, what was corrected, what is still unsupported, what only the owner can decide | the owner's reading list for pass 0 |

## Size and order

About 20 batches: A1–A7 (seven), B1–B4 (four), C1–C3 at least once (three), D1–D6 (six). A batch of history is
a long working session; the review of one collection about as long. The order is A, then B, then C, then D,
unless the owner puts D first. After every phase: a short report to the owner in Persian, the batches done, what
was left out, and – if E5 allows – the chart looked at.

## The goal, as it can be given

> Carry out `src/Apps/Dastan/docs/content-plan-histories.md` with the decisions E1–E8 as answered: phase A, then
> B, then C, then D, one batch at a time, each finished as "How every batch is done" says, committed and merged
> before the next. Stop only when every batch is done or a decision only the owner can make is reached; report
> in Persian after each phase.

## Progress

| Batch | Done | Note |
|---|---|---|
| A1 | 2026-11-28 | Zeno, Basiliscus, the kingdom of Italy (Odoacer, Theodoric), Attila, Aetius, Stilicho, Belisarius, 2 events, 11 quotes found. Left out: Libius Severus (no page could be read: Britannica began a browser check, which is not to be passed; De Imperatoribus Romanis the same). |
