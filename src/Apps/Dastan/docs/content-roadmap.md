# Content roadmap – the passes after the first

The first pass of real content is in `library/` (backlog B1 to B8 and B10, 2026-11-23/24): three histories (Iran,
China, Rome to about 100 CE), their comparison, the Shahnameh as a work, the stories of Rostam and Sohrab and of
Siavash, and a small collection of places people were at. This page plans what comes after it, in passes that are
done one after another, each finished – checked, committed, merged – before the next begins. It was written on
2026-11-24 on the owner's word; the owner decides the order and may strike or add a pass at any time.

Every pass follows [the work order](content-work-order.md) and [the guide](content-guide.md): a source read for
every statement, never more exact than the source, own words, a story with an order and no dates, nothing reviewed
or released by the writer. The sizes are there to say how deep a pass goes, not a target.

## What the first pass taught

* **Sources that can be read.** Encyclopaedia Iranica, Britannica and the Metropolitan Museum refuse automated
  reading; Iranica can be read in a browser, page by page. Livius, the Pleiades gazetteer, GeoNames, ROR, Ganjoor
  (and its API), university pages and open-access books (Routledge, Indiana University) can be read and their text
  compared with a quote. China lacks a Livius; its histories rest on university pages and two open books.
* **Checking every quote against its page** before writing found what a summary of a page would have hidden: 3 of
  447 passages for Iran and 12 of 92 passages of PDF books for China did not match and were left out.
* **What the formats cannot say yet** – see "Development the content needs" below.
* **Where sources are thin**, a collection is smaller rather than filled: China has 8 events, the Roman kings have no
  years, the Shahnameh has no three parts, the London manuscript of 1276 is missing.

## Pass 0 – closing the first pass (the owner)

| # | What | Who | Size |
|---|---|---|---|
| 0.1 | Looking at the eight collections on the site, in English and Persian, light and dark, on a phone (backlog C1, C3) | the owner, with the writer | one sitting per collection |
| 0.2 | Reading the statements against their sources and recording the review, one collection at a time – the Shahnameh and its two stories first, then the histories (D1) | the owner or an editor | about a week per collection |
| 0.3 | Reading the Persian drafts and correcting names and wording; then `review: reviewed` | the owner | an evening per collection |
| 0.4 | The owner's family tree (B9) and a second, made-up account (C2) | the owner | one sitting |
| 0.5 | The owner's own institutions added to the places | the owner names them, the writer adds them | an hour |

Nothing of the later passes waits for this one, but a review found here changes how the next pass is written: read
the first review before starting pass 2.

## Pass 1 – the stories of the Shahnameh, in the order of the poem

The collection of the work already names twenty stories and fifty reigns. Each story becomes a collection of its
own, written like Rostam and Sohrab: every section of its Ganjoor text read, 20 to 35 steps, reading stages, its
characters with the identifiers the work and the other stories give them.

| # | Story | Ganjoor | Why here |
|---|---|---|---|
| 1.1 | Jamshid, Zahhak and Kaveh, Fereydun | jamshid, zahak, fereydoon | the opening of the poem; the first great change of kings |
| 1.2 | Fereydun's sons: Salm, Tur and Iraj | fereydoon | the beginning of the war of Iran and Turan |
| 1.3 | Zal and Rudabeh, the birth of Rostam | manoochehr | the house of Sam, before Rostam |
| 1.4 | The seven labours of Rostam | kkavoos/sh5–sh11 | Rostam's journey; the story the work names first |
| 1.5 | Forud | forood | follows Siavash; Forud is already in two collections |
| 1.6 | Bizhan and Manizheh | bizhan | a story of its own between Iran and Turan |
| 1.7 | The twelve combats and the great war of Kay Khosrow | 12rokh, mahmood | the end of the war of Iran and Turan |
| 1.8 | Rostam and Esfandiar | esfandyar | with 1.9, the end of Rostam |
| 1.9 | Rostam and Shaghad | shqad | |
| 1.10 | Bahram Gur; Khosrow and Shirin; Bozorgmehr and chess | bahgoor, parviz, anooshirvan | the historical part, where it meets pass 2 |

Done: 1.1 on 2026-11-27 (`library/samples/jamshid-zahhak-fereydun.json`, 35 steps in six reading stages, from Jamshid taking the throne to the crowning of Fereydun, with its Persian; not yet looked at in a browser).

Alongside each: `spoilerPolicy.correspondences` between stories where a reader of one has certainly read a stage
of another (Siavash before Forud; Rostam and Sohrab before Rostam and Esfandiar), and the same Persian review as
pass 0.

About 10 collections, each 30–50 records and 20–35 steps. One collection a session.

## Pass 2 – the histories into late antiquity

Each history goes on to about 650–700 CE, the span the first comparison was drawn for, and meets the historical part
of the Shahnameh.

| # | What | Meets |
|---|---|---|
| 2.1 | Iran: the Sasanians, 224–651 – every king, Ctesiphon, the wars with Rome, Mazdak, the end under Yazdegerd III | the Sasanian kings of the poem: a `portrayalOf` relation from each character to the king of history, only where a source says so |
| 2.2 | Iran in the first Islamic centuries to the Ghaznavids – the Samanids and the time of Ferdowsi | Abu Mansur, Daqiqi, Mahmud of Ghazni of the work collection |
| 2.3 | Rome: the Antonines to Diocletian and Constantine; the end of the West (476); the eastern empire to Heraclius (641) | Shapur I and Valerian, Khosrow II and Heraclius on the Iranian side |
| 2.4 | China: the Three Kingdoms, the Jin, the Northern and Southern dynasties, the Sui, the Tang to about 700 | the Silk Road; envoys between the Tang and the last Sasanians |
| 2.5 | The comparison brought to 700 CE, with the events where the lanes meet | |

About 4 collections of 100–150 records each, and the comparison.

## Pass 3 – the neighbours

| # | Collection | Why |
|---|---|---|
| 3.1 | Greece to Alexander – the city states, the Persian wars from their side, Athens and Sparta | Marathon, Thermopylae, Salamis and Plataea are already events of Iran: the same identifiers |
| 3.2 | Egypt from the Late Period to the Ptolemies and Cleopatra | Cambyses, Alexander, Cleopatra are already in two collections |
| 3.3 | India: the Mauryas, the Kushans, the Guptas | the Shahnameh's chess and Kalila and Dimna come from India |
| 3.4 | Central Asia and the Silk Road: the Sogdians, the Xiongnu, the Kushans | ties the China and Iran lanes together |
| 3.5 | Armenia between Rome and Iran | Tiridates and the settlement of 63 CE are in two collections already |

About 5 collections of 60–120 records.

## Pass 4 – other works

The format of a work and its stories serves any literature. Candidates, one work collection each and then a story
or two of it:

* Nezami's Khamsa – Khosrow and Shirin (the same pair as in the Shahnameh, told again), Layla and Majnun.
* Attar's Conference of the Birds.
* Kalila and Dimna – a book the Shahnameh tells the arrival of.
* The Thousand and One Nights.
* Further away, if the owner wants them: the Iliad and the Odyssey; the Ramayana.

Each needs its own base text that can be read and cited (Ganjoor for the Persian works).

## Pass 5 – pictures

* **References** to the paintings of the Shahnameh manuscripts that their holders publish – the Shah Tahmasp
  folios at the Aga Khan Museum and the Metropolitan Museum, the Baysonghori pages through Cambridge – each at the
  stage of what it shows (work order, part 6). The rights stay the owner's (D2).
* **The project's own pictures**: maps of the reigns of the poem and of the empires, family trees of the houses,
  timelines – made as diagrams of today, like the pilot's two items.

## Pass 6 – comparisons

* The kings of the Shahnameh's historical part against the Sasanian kings of history (after 2.1).
* Iran and Rome at war, 53 BCE to 628 CE.
* Thinkers of the same centuries: Confucius, Laozi, the Greek philosophers, as far as sources date them.
* The Silk Road: who reached whom, from Zhang Qian to Gan Ying and beyond.

## Development the content needs

These are changes of the software or of a format; the writer of content does not make them (work order, part 1).
Each is a row for the development backlog when the owner wants it.

| What | Seen in |
|---|---|
| A reign known only by its length ("thirty-seven years"), without years | the Roman kings; the reigns of the Shahnameh ("Kayumars thirty years") |
| A picture of a history: a relief, a coin, a bronze as a media reference (`depictedContext` knows only stories and diagrams) – built on 2026-11-26: five kinds of a picture of a history ([format](collection-format.md), "What kind of picture an item is") | the Behistun relief, the Cyrus Cylinder, the terracotta army |
| A check that reads every source of a collection and compares a stored quote with it, kept with the collection – built on 2026-11-26: the command `quotes` ([content guide](content-guide.md)); the quotes and the copies of the sources are kept by the writer, outside the repository | done by hand for this pass; worth a command of the content tool |
| Quantities of a work as dated statements in the generator of the writer (verses, volumes) – the format has it; written by hand on 2026-11-26 | the length of the Shahnameh (50,000 or 60,000 couplets) |
| A relation of a person to the land they come from, in a story ("Sudabeh of Hamavaran") – built on 2026-11-26: the type `nativeOf` | Siavash |

## How long

One collection of a history or a story is a long working session: reading and checking the sources takes most of
it, writing and checking the file the rest. Pass 1 is about ten sessions, pass 2 four or five, pass 3 five, pass 4
and 6 as many as the owner chooses. Pass 0 is the owner's and sets the pace of everything after it: content that is
written faster than it is reviewed waits unreleased.
