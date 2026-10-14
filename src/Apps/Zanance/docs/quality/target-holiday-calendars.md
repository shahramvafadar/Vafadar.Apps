# D-273 target holiday calendar delivery

## Scope and maintenance

The owner approved Germany and other Western Europe countries, USA, Canada and Australia.
The initial explicit Western Europe boundary is DE/AT/BE/FR/LI/LU/MC/NL/CH/GB/IE; a wider
Nordic/Southern-Europe interpretation remains an unanswered optional scope question. Additional
countries are registry additions, subject to official source review. Iran variant work is deferred;
existing IR dates and stored rules are preserved. TargetMarkets links these 14 countries to the
already-supported EUR/CHF/GBP/USD/CAD/AUD, each with two minor digits, using the official SIX
currency-maintenance source: https://www.six-group.com/en/products-services/financial-information/data-standards.html.
There is no implicit language/currency change or stored money conversion.

The reviewed Gregorian window is 2026-2027; source snapshot date is 2026-10-14. New countries
outside it return no guessed dates and expose Unverified state in Settings/onboarding/Plan.
This is regular public-date coverage, not a promise about bank opening, payroll, local employment
substitutes or newly announced one-off closures. DE and IR legacy calculations remain unchanged.
IR lunar dates continue their existing approximate warning. A country must extend both provenance
and dated coverage before being called reviewed for another year. No runtime source fetch occurs.

## Official sources

| Country | Scope | Official authority/source |
|---|---|---|
| DE | Nationwide | [Deutsche Bundesbank](https://www.bundesbank.de/de/startseite/hinweise-zur-abwicklung-des-unbaren-zahlungsverkehrs-bei-der-deutschen-bundesbank-im-zusammenhang-mit-bundesweiten-und-regionalen-feiertagen-671010) |
| AT | Nationwide | [RIS](https://www.ris.bka.gv.at/NormDokument.wxe?Abfrage=Bundesnormen&Gesetzesnummer=10008541&Paragraf=7) |
| BE | Nationwide | [SPF Emploi](https://emploi.belgique.be/fr/themes/jours-feries-et-conges/jours-feries) |
| FR | Nationwide | [Service Public](https://www.service-public.gouv.fr/particuliers/vosdroits/F2405) |
| LI | Nationwide | [Liechtensteinische Landesverwaltung](https://www.gesetze.li/konso/1967.006) |
| LU | Nationwide | [ITM Luxembourg](https://itm.public.lu/fr/conditions-travail/conges/jours-feries.html) |
| MC | Nationwide | [Legimonaco](https://legimonaco.mc/tnc/loi/1966/02-18-798/) |
| NL | Nationwide | [Government.nl](https://www.government.nl/themes/work/working-hours/question-and-answer/work/public-holidays-in-the-netherlands) |
| CH | Federal | [ch.ch](https://www.ch.ch/de/arbeit/arbeitszeit/ferien--feiertage-und-arbeitsabwesenheiten/) |
| GB | Common | [GOV.UK](https://www.gov.uk/bank-holidays.json) |
| IE | Nationwide | [Workplace Relations Commission](https://www.workplacerelations.ie/en/what_you_should_know/public-holidays/) |
| US | FederalObserved | [OPM](https://www.opm.gov/policy-data-oversight/pay-leave/federal-holidays/) |
| CA | Federal | [Government of Canada](https://www.canada.ca/en/services/jobs/workplace/federal-labour-standards/vacations-holidays.html) |
| AU | Common | [Fair Work Ombudsman](https://www.fairwork.gov.au/employment-conditions/public-holidays) |

Austria excludes Good Friday. France excludes Alsace/Moselle and overseas additions. Belgium
excludes community days and negotiated substitutes. Liechtenstein includes Saint Joseph and
Nativity of Mary, excluding bridge/school days. Luxembourg includes Europe Day. Monaco preserves
Sunday holidays plus the legally applicable following Monday for the reviewed dates. Netherlands
includes statutory public dates such as Easter/Whit Sundays and annual Liberation Day, without
asserting paid leave. Switzerland is August 1 only. Ireland excludes Good Friday and does not
invent a next-Monday holiday for every weekend benefit.

UK coverage is the intersection of England/Wales, Scotland and Northern Ireland lists: six dates
in each reviewed year; Easter Monday, regional summer dates and Scotland's June 15 2026 exceptional
date are excluded. Australia uses the common full-day dates across all eight Fair Work state/
territory lists, seven dates in each year. December 26 is not common (Tasmania differs), and
April 25/26 2027 and January 4 2027 are not invented common substitutes. US observed New Year
includes December 31 2027 for January 1 2028; state/local and special inauguration closures are
excluded. Canada is ten federal statutory nominal dates; workplace/provincial substitutes are
explicitly outside this scope.

## Verification and negative evidence

The 43 new AT-273 cases cover exact counts/dates, excluded regional or guessed substitutes,
US cross-year shifting, target ISO currency digits, saved Plan country/weekend preservation and
actual six-language translated provenance/uncertainty text. The final complete app filter passes
3,569 tests with zero failures/skips (3m12.541s). A missing pure-helper compile link in App.Tests
was corrected before that final run. Backend PostgreSQL tests are separate and unchanged.

Windows production-identity builds have zero warnings/errors; PackageName metadata is
pro.vafadar.zanance after independent review. Debug-only review uses the original regional view,
memory preferences, de-DE formatting and Thursday week start. Eighteen focused own-window captures
pass en/fa/de light/dark at 360/412/1280 px, scale 2, complete captions and 44 px picker height;
financial row fingerprints remain unchanged. Eighteen real native selections pass in three English
light contexts. Earlier wide popup visibility was corrected with native scrolling. Hidden RTL
popup item arrangement and closed peer selection fail (COM 0x80040201); seeded layout fixtures
are not claimed to validate those native selections. Earlier top-only captures were replaced by
focused field captures, not counted as complete field evidence.

Release emulator is the independently owned Zanance_Storage_Review, adb port 5038,
emulator-5570 only. Existing main app receives the complete signed APK. Actual country selection
CA/US/AU, scope text and retained EUR formatting independence are checked through native XML;
Persian/English/German translation and dark/light app choices are checked separately from the
Windows layout matrix. Original null holiday, English/Gregorian, automatic digits/week start are
restored. Theme ends at Use device setting; its original selected state was not captured before
review, so exact theme restoration is not asserted. Home returns 0.00 EUR with no ledger write.
Initial guessed activity failed; the actual activity was resolved and started. A transient null
UI hierarchy was resolved by reading the same live process, not restarting it; stale XML was not
counted as acceptance. FLAG_SECURE remains enabled and no OS/device setting was changed.

Complete signed Release APK: artifacts/android-target-holidays-final/pro.vafadar.zanance-Signed.apk,
88,676,848 bytes; SHA-256 9b65c545c1ccc8a7152a77bc10590e5afa2b9b9fd94161d0554802d1b33a6d0b.
Package pro.vafadar.zanance, target SDK 36, arm64-v8a/x86_64 payload; v2/v3 signature verified.
This is local engineering evidence, not physical-phone, spoken screen-reader, iOS, service,
commercial-enforcement, store-signing or publication acceptance. Hosted CI is pending a single
post-push follow-up. The full development goal remains active.
