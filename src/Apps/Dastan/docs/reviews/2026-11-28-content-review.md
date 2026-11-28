# Content review, 2026-11-28

The writer's own review of the histories of Iran, Rome and China, the comparison and the stories of the Shahnameh,
after the forward, backward and detail passes of `content-plan-histories.md` (phases A–C). It records what was
checked, what was corrected, what no source yet carries, and what only the owner can decide. Nothing is marked
reviewed: that remains the owner's (work order, part 1; ADR 0009). The collections are still not looked at in a
browser (decision E5).

## What the collections now hold

| Collection | Reaches | Entities | Relations |
|---|---|---|---|
| Iran (`ancient-iran`) | the first settlement at Susa, 5000 BCE – the Ghaznavids, 1030 CE | 340 | 668 |
| Rome (`ancient-rome`) | the Villanovan culture, 10th–9th century BCE – the death of Basil II, 1025 CE | 336 | 630 |
| China (`ancient-china`) | the first pottery, 10th millennium BCE – the Northern Song to 1025 CE | 244 | 358 |
| The comparison (`iran-china-rome`) | 62 periods, 238 events in three lanes | | |

(Counts as written by the content tool on the day; the stories of the Shahnameh are unchanged apart from the
corrections below.)

## D1 – every dated statement against its source

Each year of each dated statement – an event, a period, a birth or death, a reign held – was compared with a
passage of its source. The passage was read: Livius, Asia for Educators, Hawaii, the Smithsonian, UCLA and de
Crespigny's chronology by fetching the page; Britannica and the Encyclopaedia Iranica page by page in a browser, as
they refuse automated reading. The passage is kept as a quote outside the repository (`.local/quotes`), with a copy
of the text it stands in (`.local/sources`); the content tool finds every quote in its source.

| Collection | Quotes found | Dated statements still without a passage for every year |
|---|---|---|
| Iran | 416 | 14 |
| Rome | 524 | 7 |
| China | 303 | 40 |

Corrected on the way: a source the year actually stands in, but which the statement did not name, was added to
the statement – Bardiya's reign, the Arsacids over Parthia, the death of Mahmud of Ghazni.

**What no passage carries yet**, and why:

* **Years derived from what the source says**, not written in it: the Buyids' rise "in the 320s/930s" of the
  Hijra written as 932–941; Ya'qub the Coppersmith's death "three years later" than 876, written 879, and ʿAmr's
  reign from it; Ferdowsi's birth from "thirty-eight years old in 977"; the suppression of Mazdakism "in the 530s"
  written with its last year; the Seleucid period from the death of Darius III (330 BCE) and the Seleucid rule of
  Iran from the dynasty's own years; the first pottery of China "from the 10th to the 7th millennium" written as
  windows; the Villanovan culture by centuries.
* **King lists whose names stand on another line** than the years (Livius's Sasanian list: Hormizd IV's end in
  590, Hormizd V about 593) – the year is there, the quote cannot tie it to the name.
* **Sources not read as text**: two books – Major and Cook's *Ancient China* (Taylor & Francis) and Eno's teaching
  pages (Indiana) – carry almost all the Western Han and Qin years: Liu Bang, Empress Lü, the emperors from Huidi
  to Pingdi, Zhang Qian, Sima Qian, the Xin of Wang Mang, the end of Qin. They are cited faithfully but could not
  be quoted in this pass.
* **Not found in the source named**: the return of the Roman standards in 20 BCE (Iran), the birth of Camillus
  (365 BCE), the death of Julia (54 BCE), the birth of Mark Antony (82 BCE), Gaius Gracchus's tribunate (122 BCE),
  the Roman envoys of 166 in Rome's own collection (their source is de Crespigny's chronology for China), the
  Warring States beginning in 480 BCE (the sources say 475), the Zhou kings Cheng and You and Hao as capital
  from 1050 BCE.
* **Undated statements** – parents, offices without years, places of events – were not quoted in this pass:
  Iran 521, Rome 474, China 292 statements have no quote. They are as the batches wrote them, each with its
  source; quoting them is the next pass of D1.

## D2 – the collections agree with each other

85 identifiers stand in more than one collection; 20 differed. Corrected:

* **Doubles merged**: Bahram the champion is `character-bahram-pahlavan` and Houman `character-houman` in every
  story (they were `character-bahram` and `character-human` in Forud, Rostam and Sohrab and Siavash).
* **Names and Persian forms**: Forud (Farud in Siavash), Kay Kavus کی‌کاووس, Tahmuras تهمورث, the seven labours
  هفت‌خوان رستم, Sistan with Nimruz as another name, Khotan a region everywhere, *The Shahnameh* with the years of
  its writing in each story.
* **Years one collection knew and another did not**, carried over with their sources: Crassus's birth, Mahmud of
  Ghazni's birth and death, Daqiqi's death, Ferdowsi's birth and death (now 940 everywhere, as the Shahnameh
  collection has it; Iranica's "thirty-eight in 977" would allow 939).
* **One place, two meanings**: the poem's Hari in the story of Siavash is now `place-hari`, no longer the Herat
  of history.

The comparison was checked against the collections: its periods and events say what the collections say.

## D3 – the Persian

`docs/content-glossary-fa.md` gathers the rules by which the translations write names and every name the
collections use. Corrected to it: Zhou is ژو in every house that bears the name (the Later Zhou, the Northern
Zhou), Justinian II ژوستینین like Justinian I, Claudius II کلودیوس, Zhengzhou as its own entry writes it. The
translations stay drafts. Not done: reading every Persian summary against its English – only names were compared.

## D4 – the stories of the Shahnameh

Each step of the seven stories was compared with the sections of the poem on Ganjoor it cites, by looking for the
name of each character the step associates in the verses of those sections; and the reading stages were checked
for a name given before its stage.

| Story | Steps | Characters not named in the cited section | Named before their stage |
|---|---|---|---|
| Jamshid, Zahhak and Fereydun | 35 | 4 | 6 |
| Salm, Tur and Iraj | 31 | 20 | 0 |
| Zal and Rudabeh | 34 | 16 | 3 |
| The seven labours | 27 | 0 | 1 |
| Rostam and Sohrab | 30 | 8 | 2 |
| Siavash | 35 | 2 | 5 |
| Forud | 32 | 11 | 0 |

A character "not named" is not necessarily wrong: the poem often calls a person by an epithet or a title (Zal as
Dastan, the three sons of Fereydun before he names them, Tus as the commander), or the step joins two sections.
Each case is to be read against the verses; none was changed here. The names given before their stage are of two
kinds: the descriptions of the work and of a story, which name its people by its title (left as they are), and a
few passing mentions – Kabul and Sistan in the second step of Zal and Rudabeh, Rakhsh one stage early in the seven
labours, Siavash and Afrasiab in the first descriptions of Siavash. Lowering their stage is refused by the content
tool, because their sources belong to the later stage; the wording is the owner's to choose.

Not done: correspondences between stories where a reader has certainly read a stage of another.

## D5 – looking at it

Not done: the site is not started without the owner's word (decision E5).

## For the owner

1. **Two identifiers for two things**: `dynasty-sasanian` is the Sasanian house of history in the history of Iran
   and the Sasanians of the poem in the Shahnameh; `place-baghdad` is the city of history in Iran and the
   Baghdad of the poem in Jamshid, Zahhak and Fereydun. Should the poem's entities get their own identifiers, as
   Hari now has?
2. **509 or 510**: the Roman monarchy ends in 509 BCE in the collection, while the Republic begins in 510 by
   Varro's dating, which the sources call unreliable. One year for both, or both kept as the sources give them?
3. **Ferdowsi's birth**: 940 everywhere now; Iranica allows 939 or 940. A window, or 940?
4. **Early mentions in the stories** (D4 above): keep the passing names, or rewrite those summaries?
5. **The books**: Major and Cook and Eno could be quoted if a readable copy is at hand.
6. **The look**: D5, when the site may be started.
