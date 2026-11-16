# Shared libraries backlog

Known gaps and planned work of the shared libraries (`src/Libraries/Vafadar.*`). App backlogs live with their app;
this list holds only what belongs to a library that several apps use.

Rules for this list:

* An item names the observed gap, who is affected and what a change would touch.
* A library change must not alter the behaviour an existing app relies on. Where it would, the item stays open until
  that app's owner decision is recorded, and the library offers the new behaviour **next to** the existing one.
* Status: **Open**, **In progress**, **Done** (with the date and where it was delivered).

## Vafadar.Core – `Text`

Reviewed on 2026-11-15, when the second app (Dastan, a public discovery site with name search in Persian and Latin
script) started to use the library.

`SearchText.Normalize` makes text typed on different keyboards equal: digit scripts, the Arabic forms of Yeh and Kaf,
and the Persian half-space. Zanance uses it for the transaction search, categorization rules and the "name is already
used" checks of accounts, categories and profiles. It deliberately does nothing else, and Zanance's stored rule
patterns and uniqueness checks rely on exactly that (see `AGENTS.md`, section 57). It is therefore unchanged.

| Id | Gap | Affects | Status |
|---|---|---|---|
| LIB-TEXT-01 | No search key that ignores case, accents and the spelling variants of Arabic-script words. `Normalize` keeps `Café` ≠ `cafe`, `آب` ≠ `اب`, `سؤال` ≠ `سوال`, `رئیس` ≠ `رییس`, `خانۀ` ≠ `خانه`, `فاطمة` ≠ `فاطمه`; vowel marks (harakat) and the stretching character (tatweel, `ـ`) make a word different; composed and decomposed Unicode forms of the same letter differ; presentation forms (`ﻻ`, `ﬁ`, full-width letters) are not recognised; invisible direction marks, soft hyphens and repeated or non-breaking spaces count as characters. | Dastan search; any app that searches names | **Done** 2026-11-15: `SearchText.Fold` (additive, `Normalize` untouched) |
| LIB-TEXT-02 | Zanance's transaction search and categorization rules still compare with `Normalize` plus a case-insensitive comparison, so the differences of LIB-TEXT-01 keep an entry from being found (for example a payee stored as `Café Müller` is not found by `cafe muller`). Moving them to `Fold` changes which rule matches a payee and therefore needs a Zanance decision and its own tests. | Zanance | Open – Zanance decision |
| LIB-TEXT-03 | The "name is already used" checks (accounts, categories, profiles) use `Normalize`: two names that look identical on screen can both be saved when they differ only in an invisible direction mark, a non-breaking or repeated space, or the composed/decomposed form of a letter. `Fold` is too broad for this purpose (`Müller` and `Muller` are different names). Needed: a third, identity-level form – Unicode composition, invisible characters removed, whitespace collapsed, no accent or spelling folding. | Zanance; later any app with unique names | Open |
| LIB-TEXT-04 | The half-space becomes a space in both forms, so a word typed without any separator (`میخواهم`) does not match the same word with a half-space (`می‌خواهم`). Solving it needs matching that ignores separators inside a word, not a different normalization. | All apps with Persian search | Open |
| LIB-TEXT-05 | `Digits.ToAscii` converts Persian and Arabic-Indic digits only; other decimal digit scripts and full-width digits are not parsed as numbers. (`SearchText.Fold` converts every decimal digit for searching.) Changing `ToAscii` changes what Zanance's amount fields accept. | Zanance input parsing | Open – Zanance decision |
| LIB-TEXT-06 | `Digits.ToPersian` is the only display conversion; an Arabic interface needs Arabic-Indic digits (`٠–٩`). | Apps that add Arabic | Open – with the first Arabic interface |
| LIB-TEXT-07 | No word splitting, prefix matching or ranking of results. Each app implements its own matching on top of the normalized text. Decided for Dastan on 2026-11-16: app code – `Projection.Search` ranks an exact name, a name that begins with the text, a word that begins with it, a name that contains it. A shared helper is written when a second app needs the same ranking. | Dastan | Decided – app code until a second user exists |
| LIB-TEXT-08 | Not covered by `Fold` on purpose: transliteration between scripts (`Rostam` ↔ `رستم`), language-specific spellings (German `ue` for `ü`), and punctuation variants (`’` and `'`). Apps provide alternative names as data where they need them. | – | Not planned |

### What `SearchText.Fold` does

`Fold` returns a key for comparing with `StringComparison.Ordinal`; it is never stored as the text and never shown.

1. Digits of every script become ASCII; the Arabic decimal and thousands separators become `.` and `,`.
2. Compatibility decomposition (Unicode NFKD): presentation forms, ligatures and full-width characters become their
   base letters, and accents are separated from their letter.
3. Lower case (invariant).
4. Combining marks are removed: Latin accents, Arabic vowel marks, and the hamza and madda above or below a letter –
   so `آ أ إ` become `ا`, `ؤ` becomes `و` and `ئ` becomes `ی`.
5. Letters without a decomposition are mapped: `ي ى` → `ی`, `ك` → `ک`, `ة` and `ۀ` → `ه`, `ٱ` → `ا`, `ß` → `ss`,
   `æ` → `ae`, `œ` → `oe`, `ø` → `o`, `đ` → `d`, `ł` → `l`, `ı` → `i`, `ς` → `σ`.
6. The tatweel, format characters (direction marks, joiners, soft hyphen, byte order mark) and control characters are
   removed; the half-space counts as a space.
7. Whitespace of any kind is collapsed to single spaces and trimmed.

Letters that are separate sounds in Kurdish (`ێ ۆ ڕ ڵ ە`) are kept as they are.

## Vafadar.Localization

Reviewed on 2026-11-15 for use in a web app with several visitors at the same time.

| Id | Gap | Affects | Status |
|---|---|---|---|
| LIB-LOC-01 | One list of languages for the whole repository: every resource file had to exist in all six languages, so an app with fewer languages could not have its own texts. | Dastan | **Done** 2026-11-15: each app declares `VafadarLanguages` ([ADR 0011](adr/0011-languages-per-app.md)) |
| LIB-LOC-02 | The library holds two things: the language machinery (languages, direction, calendars, date formatting) and Zanance's shared texts in six languages. An app that references it for the machinery ships those texts. Separating them moves files Zanance builds on. | Apps with their own texts | Open – only when another app needs the calendars or date formatting; Dastan does not reference the library |
| LIB-LOC-03 | `LocalizationService` and `Translator.Instance` hold one language for the whole process and set the process cultures; `NativeDigits` reads process-wide switches. Correct for an app on a device, wrong for a server where every visitor has a language. Web apps use the request culture of ASP.NET Core instead. | Web apps | Open – no change planned; documented so the service is not registered in a web host |
| LIB-LOC-04 | `ResourceUsageTests` checks that every used key exists for Zanance only, and only in `.cs` and `.xaml` files. | Dastan (`.razor`) | Open – Dastan gets the same check for its own resources with its web project |
| LIB-LOC-05 | `AppLanguage.IsRightToLeft` is the only direction help; the helpers for layout direction are in `Vafadar.Maui`. A web page needs `lang` and `dir` from the language. | Web apps | Open – delivered in `Vafadar.Web` |

## Vafadar.Temporal

Created on 2026-11-16 ([ADR 0012](adr/0012-temporal-model.md)). What it deliberately does not do yet:

| Id | Gap | Affects | Status |
|---|---|---|---|
| LIB-TEMP-01 | No conversion between calendars. Dates of different eras (common era, Solar Hijri, lunar Hijri) are not compared; dates in different or unnamed calendars of one era are compared by year count with a margin. `Vafadar.Core.Dates.LunarHijri` and the .NET Persian calendar could supply conversions for the years they cover. | Timelines that mix calendars | Open – with the first content that needs it |
| LIB-TEMP-02 | Ages and dates have no common scale, although an age in calendar years before a stated year can be placed among dates. | Timelines from prehistory into history | Open |
| LIB-TEMP-03 | Calendar precision is year, month or day. Decades, centuries and millennia ("the fifth century BCE") cannot be stated as a precision; an occurrence window is the nearest form and means something else. | Historical content | Open |
| LIB-TEMP-04 | No time of day and no time zone. A photograph's capture time to the minute is an operational `DateTimeOffset` in the app, not a position. | Media times | Open |
| LIB-TEMP-05 | No formatting for display. The formatter comes with its first screen; the words come from the app. | Dastan, phase 4 or 5 | Open – planned |
| LIB-TEMP-06 | `EventTime` is a reference only; resolving it to the time of the event is left to the app. | Dastan media | Open – in `Vafadar.Dastan.Core` |
| LIB-TEMP-07 | Day numbers are checked exactly for the Gregorian and Julian calendars only; for the Hijri calendars and unnamed calendars the upper limit of the month is used. | Input validation | Open – with LIB-TEMP-01 |
| LIB-TEMP-08 | No reading or writing of EDTF, the Library of Congress date format. The JSON format is the library's own. | Imports | Not planned until an import needs it |
| LIB-TEMP-09 | A partial order ("A before B", nothing else known) is not a position. It is a relation between events and belongs to the app's model. | – | Not planned here |
