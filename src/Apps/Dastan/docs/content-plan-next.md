# Content plan – the next goal: the gaps of the three histories, and the other ancient civilizations

Written on 2026-11-28 on the owner's word, after [the plan for the three histories](content-plan-histories.md) was
carried out to its review ([the content review](reviews/2026-11-28-content-review.md)). It has two parts: **G**,
closing what that plan left open in Iran, Rome, China and the Shahnameh; and **N**, new collections for the other
ancient civilizations, each carried as the three histories were. Both follow
[the chronology guidelines](content-chronology-guidelines.md): myth and history are kept apart in time, and no
hypothetical date is written as history.

## Decisions for the owner, before the goal starts

| # | Question | Options | Recommendation |
|---|---|---|---|
| F1 | **Hypothetical placements** (Rostam about 1300 BCE and the like) | (a) approve the proposal of the chronology guidelines – a placement statement, a lane or style of its own in the comparison, a heading on the page – as an ADR and a development task before the content; (b) keep the narrative chronology in words only | (a): without it the timeline cannot show a legend at all, and the guideline's core principle asks for marked placements, not empty positions |
| F2 | **Which civilizations, in which order** | Egypt, Mesopotamia, Anatolia (the Hittites), Greece, India first; later the Levant, the steppe, Mesoamerica and the Andes | Egypt, Mesopotamia, Anatolia, Greece, India – the five whose lanes meet Iran, Rome and China most often |
| F3 | **One comparison or several** | (a) one comparison of the ancient world with all lanes; (b) regional comparisons (the Near East: Iran, Egypt, Mesopotamia, Anatolia; the Mediterranean: Egypt, Greece, Rome; Asia: Iran, India, China) beside the present one | (b) and keep `iran-china-rome`: more than four lanes side by side is hard to read on a phone |
| F4 | **How far each new history goes** | (a) to the common horizon of 1025 CE; (b) to the end of the civilization as an independent tradition | (b): Egypt to the Arab conquest (641), Mesopotamia to Cyrus (539 BCE) with the Seleucids as epilogue, the Hittites to their fall (about 1180 BCE), Greece to the Roman conquest (146 BCE) with the Hellenistic kingdoms to Actium, India to 1025 |
| F5 | **Myths of the new civilizations** | (a) story collections of their own (Gilgamesh, the Iliad, the Osiris myth), as the Shahnameh; (b) only historical collections now | (b) now, (a) as the next goal after F1 is built: the stories need the placement statements to stand on the timeline |
| F6 | **More of the Shahnameh** | which stories next | Rostam and Esfandiar, Bizhan and Manizheh, the great war of Kay Khosrow, Akvan the demon – the stories where Rostam's narrative chronology and Goshtasp's court meet |

## How every batch is done

As in [the plan for the three histories](content-plan-histories.md), "How every batch is done": sources read in
full (Livius and the university and museum sites by fetching, Britannica and the Encyclopaedia Iranica page by page
in a browser), every dated statement with a quote kept outside the repository, the content tool's `check` and
`quotes` clean, the library tests after each batch and the whole suite at the end of each phase, one commit merged
per batch, the changelog and this plan's progress table written in the same commit. A legend placed in a history
collection is labelled "(tradition)"; a character of a story gets no year.

## Part G – closing the gaps of the three histories

| Batch | What | Sources to try first |
|---|---|---|
| G1 | **Iran**: the Tahirids; the Samanid, Ziyarid and Buyid rulers beyond the founders; Mazdak and Tansar; the Sasanian offices (spahbed, mobad) with holders where dated; Jiroft and Ganj Dareh | Iranica (other article addresses, the search page), Britannica |
| G2 | **Rome**: Libius Severus; Tacitus, Polybius, Cicero, Ovid, Horace and their works; the Colosseum; Bronze Age Italy if an article can be read | Britannica, Livius |
| G3 | **China**: the Tang emperors Muzong, Jingzong, Xuanzong II, Zhaozong and Aidi; the rulers of the Five Dynasties; the Jin, Northern Wei and southern emperors between the founders; the Shang kings between Tang and Pangeng; Kaifeng | Britannica (biographies), de Crespigny, Asia for Educators |
| G4 | **Quotes for what has none**: the undated statements (parents, offices, places of events – about 1,300) and the Western Han years from Major and Cook and Eno, if a readable copy is at hand | the sources already cited |
| G5 | **The Shahnameh**: the stories chosen in F6, each a collection like Forud; the other Sasanian kings of the poem linked to history (`portrayalOf`) where a source says so; correspondences between stories | Ganjoor, Iranica |
| G6 | **The Persian**: every Persian summary read against its English; the glossary extended | the collections |
| G7 | **Placements** (only after F1 is built): Rostam's hypothetical placement as the guideline states it, the medieval synchronisms of the Kayanian kings as competing placements, Zoroaster's date range from Iranica | Iranica, the chronology guidelines |

## Part N – the other ancient civilizations

Each new history is a collection of its own (`ancient-egypt`, `ancient-mesopotamia`, `hittites`, `ancient-greece`,
`ancient-india`), built in the order of the three histories: **framework** (periods, dynasties, every ruler of every
house with the years the source gives), **events**, **back before writing**, **detail** (people beyond rulers,
works, places with positions from Pleiades or GeoNames), and its **comparison lanes**. Every batch below is one
working session; a civilization is about four batches.

| Batch | Collection | What | Sources to try first |
|---|---|---|---|
| N1 | Egypt | Predynastic Egypt and the unification; the Old, Middle and New Kingdoms with their dynasties and pharaohs; the Intermediate Periods; the Late Period and the Persian dynasties (cross-lane with Iran: Cambyses, Darius) | Livius (Egypt, king lists), Britannica (browser), Digital Egypt for Universities (UCL) |
| N2 | Egypt | Alexander and the Ptolemies to Cleopatra VII (cross-lane with Rome: Actium), Roman and Byzantine Egypt to the Arab conquest of 641 (cross-lane with Iran and Rome: the Sasanian occupation of 619, Alexandria in 642); works and monuments (the pyramids of Giza, Karnak, the Rosetta Stone) | Livius, Britannica, Pleiades |
| N3 | Mesopotamia | Uruk and the beginnings of writing; the Sumerian city states; Akkad (Sargon); Ur III; the Old Babylonian period (Hammurabi); the Kassites | Livius (very full on Mesopotamia), Britannica |
| N4 | Mesopotamia | Assyria (Middle and Neo-Assyrian kings), the Neo-Babylonian empire to Cyrus in 539 BCE (cross-lane with Iran: the fall of Nineveh 612, Cyrus takes Babylon), the Seleucid epilogue; Elam's wars with Assyria (cross-lane with Iran) | Livius, Britannica |
| N5 | Anatolia | The Hittite Old and New Kingdoms with their kings; Kadesh (with Egypt: Ramesses II), the treaty of about 1259 BCE; the fall about 1180 BCE; the Neo-Hittite states, Phrygia and Lydia (cross-lane with Iran: Croesus) | Britannica, Livius |
| N6 | Greece | The Minoans and Mycenaeans as periods; the Dark Age; the Archaic and Classical poleis (Athens, Sparta) with their leaders; the Persian wars (cross-lane with Iran: Marathon, Salamis, Plataea); Alexander; the Hellenistic kingdoms to 146 BCE and Actium | Livius, Britannica, Perseus |
| N7 | India | The Indus civilization; the Vedic period as a period; the Mahajanapadas; the Mauryas (Ashoka; cross-lane with Alexander and the Seleucids); the Shungas, Kushans (cross-lane with Iran and China), the Guptas; Harsha; the dynasties to 1025 (cross-lane with Mahmud of Ghazni) | Britannica, Asia for Educators (Columbia), Livius |
| N8 | Comparisons | the comparisons of F3, with the events where lanes meet entered in each collection they concern | the collections |
| N9 | Review | the review of the new collections as D1–D4 of the earlier plan, and a report | the collections |

## The goal, as it can be given

> Carry out `src/Apps/Dastan/docs/content-plan-next.md` with the decisions F1–F6 as answered: part G, then part N,
> one batch at a time, each finished as "How every batch is done" says, committed and merged before the next.
> Follow `content-chronology-guidelines.md` throughout. Stop only when every batch is done or a decision only the
> owner can make is reached; report in Persian after each part.

## Progress

| Batch | Done | Note |
|---|---|---|
| G1 | 2026-11-28 | 15 people, the Tahirids as a house with their office, 20 quotes. Left out: Mansur I of the Samanids (no reign dates in the pages read); Mazdak and Tansar (Iranica has no article of their own; the article on the Khorramis is the next to read); Jiroft and Ganj Dareh. |
