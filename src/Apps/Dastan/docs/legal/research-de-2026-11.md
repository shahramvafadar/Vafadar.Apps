# German law for Dastan – research, state 2026-11-19

A record of what was read about German law before the legal pages of Dastan were written (row 18.8 of the delivery plan). It is research and not legal advice; a lawyer reads the texts before the site is published. The companion record on the law of the European Union is `research-eu-2026-11.md`.

Method: statute texts were read on gesetze-im-internet.de / gesetze-bayern.de today. The DSK guidance (OH Digitale Dienste v1.2), the HmbBfDI Russmedia paper and the Stiftung Datenschutz Mastodon guide were read in full text from the PDFs. Case law is mostly confirmed through secondary reports only. Everything I could not confirm is tagged **[NOT VERIFIED]** and collected at the end. This is research, not legal advice.

## A. Most important obligations first

1. **Impressum with name and serviceable postal address is required.** It follows at least from § 18 Abs. 1 MStV, whatever the answer under § 5 DDG. Prudent course: give the full § 5 DDG set and a "Verantwortlicher" under § 18 Abs. 2 MStV.
2. **GDPR applies in full; there is no household exemption for a public site.** After EuGH *Russmedia* (C-492/23, 2 Dec 2025) the hosting privilege does not shield the operator from GDPR duties for personal data in user content. Family trees and tagged photos of third persons are the largest legal risk of the project.
3. **No cookie banner is needed** if only § 25 Abs. 2 Nr. 2 TDDDG storage is used. The DSK reads the exemption granularly, though: preferences, saved list, reading position and offline cache should be written only on a user action and without unique IDs.
4. **DSA basics should be implemented:** contact points, terms with moderation rules, notice-and-action, statement of reasons, crime reporting (Art. 11, 12, 14, 16, 17, 18). No registration with the BNetzA. Transparency reports and the platform section do not apply to micro/small providers.
5. **Not applicable:** BFSG, UrhDaG, NetzDG, VSBG § 36, §§ 327 ff. BGB, § 38 BDSG (DPO), Jugendschutzbeauftragter, and UWG warning letters from competitors. The operator is not an "Unternehmer" and has no profit intent.

## 1. Impressum

**§ 5 DDG**
- In force since 14 May 2024 (replaced § 5 TMG). https://www.gesetze-im-internet.de/ddg/__5.html
- It applies to "geschäftsmäßige, in der Regel gegen Entgelt angebotene digitale Dienste". A "digitaler Dienst" is an information society service under Art. 1(1)(b) Directive 2015/1535 (§ 1 Abs. 4 Nr. 1 DDG). https://www.gesetze-im-internet.de/ddg/__1.html
- Items for a natural person: name and Anschrift (Nr. 1); details for fast electronic contact and direct communication including an e-mail address (Nr. 2); VAT ID only if held (Nr. 6). Nr. 3–5, 7, 8 do not apply.
- The information must be "leicht erkennbar, unmittelbar erreichbar, ständig verfügbar". Practice: a link named "Impressum" in the footer of every page; IHK Düsseldorf (Jan 2026) reports the BGH accepted two clicks. https://www.ihk.de/duesseldorf/recht-und-steuern/recht/internetrecht/impressum-5109750
- Hobby sites: no 2025–26 court decision on non-commercial UGC sites was found. Secondary sources mostly say a purely private site with no ads or income is outside § 5 DDG, but stress that courts read "geschäftsmäßig" (sustained activity, no profit intent needed) broadly.
- The BMJ-funded Stiftung Datenschutz Mastodon guide (v1.1, 23 Sep 2024) says for volunteer-run instances that the exceptions apply "nur theoretisch" and that "praktisch müssen alle Instanzen die Impressumspflicht erfüllen". https://stiftungdatenschutz.org/fileadmin/Redaktion/Dokumente/Mastodon-Leitfaden/Mastodon-Leitfaden_Web-v1_1.pdf

**§ 18 MStV** (text "gilt ab 01.12.2025") https://www.gesetze-bayern.de/Content/Document/MStV-18
- Abs. 1: providers of telemedia "die nicht ausschließlich persönlichen oder familiären Zwecken dienen" must keep name and address available, with the same three accessibility criteria. There is no commerciality requirement. A public site with open registration is not exclusively personal, so this duty applies to Dastan.
- Abs. 2: "journalistisch-redaktionell gestaltete Angebote" must, in addition to §§ 5, 6 DDG, name a Verantwortlicher with name and address. That person needs permanent residence in Germany, full legal capacity, no loss of capacity for public office, and must be fully prosecutable.
- Whether the edited, sourced epic pages count as journalistic-editorial is a judgement call; I found no decision on comparable cultural sites **[NOT VERIFIED]**. Low-cost safe course: add "Verantwortlich i. S. d. § 18 Abs. 2 MStV: Shahram Vafadar, [address]". Do not cite "§ 55 RStV" (IHK Düsseldorf).

**Address**
- It must be ladungsfähig, i.e. formal service must actually work. A Postfach is insufficient.
- c/o is acceptable only if service really reaches the person (OLG Hamm, 7 May 2015, 27 W 51/15, on a GmbH; via e-recht24, Stand 27 Feb 2025). https://www.e-recht24.de/impressum/8369-impressum-c-o-adresse.html
- A virtual address where service failed was insufficient (OLG München, 19 Oct 2017, 29 U 8/17), but virtual addresses are not excluded per se. https://www.it-recht-kanzlei.de/impressum-virtuelle-adresse-ausreichend.html
- No 2024–2026 appellate ruling on Impressum-service addresses for private persons was found, so that route stays legally uncertain. The Stiftung Datenschutz guide names one alternative: an authorised person at an office address.

**Second contact channel:** EuGH C-298/07 (16 Oct 2008) holds a phone number is not mandatory; another fast, direct channel next to e-mail suffices, e.g. an enquiry form answered promptly (confirmed via vzbv/wbs summaries). https://www.vzbv.de/urteile/europaeischer-gerichtshof-internetanbieter-muss-keine-telefonnummer-nennen
Although the site sends no e-mail, the operator needs a monitored mailbox.

**Language:** no statutory rule and no decision found **[NOT VERIFIED]**. Secondary view: follow the languages of the audience. Suggestion: English and Persian with the German word "Impressum" as heading. German text is also useful for Art. 11(3) DSA (see section 4).

**Sanctions**
- § 33 Abs. 2 Nr. 1 with Abs. 6 Nr. 3 DDG: fine up to €50,000. https://www.gesetze-im-internet.de/ddg/__33.html
- The MStV fine provision for § 18 was not checked **[NOT VERIFIED]**.
- Warning letters: § 13 Abs. 4 UWG excludes cost reimbursement to competitors for breaches of information duties in digital services and for GDPR/BDSG breaches of firms under 250 staff. https://www.gesetze-im-internet.de/uwg_2004/__13.html
- For a non-commercial private operator the UWG's basic requirement of a "geschäftliche Handlung" is missing anyway (my assessment, not tested in a decision I found).

## 2. § 25 TDDDG

- Text: Abs. 1 requires consent for storing or reading information in terminal equipment. Abs. 2 Nr. 2 exempts what is "unbedingt erforderlich … damit der Anbieter eines digitalen Dienstes einen vom Nutzer ausdrücklich gewünschten digitalen Dienst zur Verfügung stellen kann". https://www.gesetze-im-internet.de/ttdsg/__25.html
- In force since 1 Dec 2021 as TTDSG **[date from memory]**, renamed TDDDG in May 2024.
- Fine: § 28 Abs. 1 Nr. 13, Abs. 2 – up to €300,000. https://www.gesetze-im-internet.de/ttdsg/__28.html

**Newest DSK guidance: "OH Digitale Dienste", Version 1.2, November 2024** https://datenschutzkonferenz-online.de/media/oh/OH_Digitale_Dienste.pdf
- § 25 is technology-neutral and covers every storage or read access, with or without personal data. Local storage and a service-worker cache are therefore in scope.
- Rn. 74–76: beyond the "Basisdienst", functions such as "Spracheinstellungen … Log-in Bereiche inkl. Authentifizierung … Merklisten oder Favoritenlisten" are "Zusatzfunktionen". They count as wanted only once the user actually uses them (picks a language, logs in, creates a list).
- Rn. 78–80: "unbedingt erforderlich" means technically necessary. Storage may start only when the function is used. A session is the default lifetime; regular use can be assumed only for services that require login.
- Rn. 81: language or background colour needs no unique ID; store a non-identifying value such as "language: de".
- Rn. 83: user-oriented security cookies that protect the login system are exempt. Antiforgery tokens fit this.
- Rn. 86: a multi-purpose cookie is exempt only if every purpose is.
- Rn. 116: consent banners "sollten … nur eingesetzt werden, wenn tatsächlich eine Einwilligung notwendig ist"; otherwise they mislead.

**Applied to Dastan (my assessment)**

| Storage | Assessment |
|---|---|
| Session/auth cookie after login | Exempt |
| Antiforgery token | Exempt |
| Language, theme | Exempt once the user chooses; plain value, no ID |
| Saved list | Exempt once the user creates it |
| Reading position | Safest as an explicit "continue reading" feature or session-only |
| Service-worker offline cache | Not addressed by the DSK **[NOT VERIFIED]** – see below |

For the offline cache, either treat it as an opt-in ("make available offline" or app install), or document why it is technically necessary for the delivery the user asked for.

- Information still required: Art. 13 GDPR privacy notice describing each storage item, purpose and lifetime.
- The Stiftung Datenschutz guide reaches the same result for Mastodon: only necessary cookies, so "in der Regel" no banner.
- § 19 TDDDG: pseudonymous use must be enabled where reasonable (display name without e-mail fits), and state-of-the-art security including encryption is required. https://www.gesetze-im-internet.de/ttdsg/__19.html

**EinwV:** in force 1 Apr 2025, based on § 26 TDDDG. It governs recognised consent-management services; the BfDI recognises them, and use by site operators is voluntary. One service ("Consenter", 17 Oct 2025) is reported as recognised by a single secondary source **[NOT VERIFIED at the BfDI register]**. It is irrelevant when no consent is needed. https://www.haufe.de/recht/weitere-rechtsgebiete/wirtschaftsrecht/bundesverordnung-zur-cookie-einwilligung-einwv_210_635192.html

## 3. GDPR / BDSG

**Scope**
- Publication to an unlimited audience is outside Art. 2(2)(c) (EuGH *Lindqvist*, C-101/01).
- The BGH referred new questions on the household exemption to the EuGH on 17 Sep 2026 (I ZR 256/25, I ZR 289/25). They concern private chats and home video, not public websites. https://www.otto-schmidt.de/news/wirtschaftsrecht/dsgvo-eugh-vorlage-zur-haushaltsausnahme-2026-09-17.html

**EuGH C-492/23 *Russmedia* (2 Dec 2025)** https://www.lto.de/recht/hintergruende/h/eugh-c49223-russmedia-kein-provider-privileg-verarbeitung-personenbezogene-daten
- A platform can be a (joint) controller for personal data in user posts.
- For sensitive data it must take preventive measures, including checking whether the poster is the data subject.
- It must act against copying of such content.
- It cannot rely on the hosting privilege.

**HmbBfDI "Praxisfolgen des Russmedia-Urteils"** (file dated 12 May 2026) https://datenschutz-hamburg.de/fileadmin/user_upload/HmbBfDI/Datenschutz/Informationen/260512_Information_Praxisfolgen_Russmedia.pdf
- Controllership arises "insbesondere" where the platform uses the content in its own, typically commercial, interest; broad licence grants in the terms count towards this.
- There is no general monitoring duty and no general duty to identify private users.
- After a notice the platform must remove the content and prevent identical re-publication ("Notice-and-Sweep").
- Preventive duties scale with risk, e.g. where sensitive data are typically published or anonymous posting is allowed.
- The same reasoning is assumed to apply to Art. 6 and 8 DSA.

Consequences for Dastan (my assessment):
- Take no broad licence in the terms.
- Keep trees and profiles non-public by default; do not let search engines index data of living persons.
- Offer no Art. 9 fields (religion, health, ethnicity) for living third persons.
- Record the uploader's confirmation of consent per living person.
- Give non-members an easy objection and removal channel, with stay-down.
- Tags of other users only after their confirmation.

**Other points**
- **Genealogy:** no German DPA paper found **[NOT VERIFIED]**. The GDPR does not apply to deceased persons (Recital 27/160), but data on the dead can reveal data on living relatives. For living third persons Art. 14 information duties arise; §§ 32–37 BDSG modify information and access duties, but I did not re-read them today **[NOT VERIFIED]**.
- **Age:** Art. 8 GDPR sets 16; Germany has made no derogation (confirmed by secondary sources; nothing in the BDSG). A 16-year threshold is therefore consistent. The contractual capacity of 16–17-year-olds (§§ 106 ff. BGB) is a separate question **[NOT VERIFIED]**.
- **DPO:** § 38 Abs. 1 BDSG requires 20 persons constantly engaged in automated processing, so not applicable. A repeal of Abs. 1 is politically agreed (Dec 2025/July 2026) but no enactment was found. https://www.gesetze-im-internet.de/bdsg_2018/__38.html
- **Supervisory authority:** the Landesdatenschutzbehörde of the operator's Land. List: https://www.bfdi.bund.de/DE/Service/Anschriften/Laender/Laender-node.html (in Bavaria the BayLDA handles the private sector **[from memory]**). The privacy notice must state the right to complain (Art. 13(2)(d)); naming the authority is good practice.
- **Photos, KUG vs GDPR:** the BGH applies §§ 22, 23 KUG via Art. 85(2) GDPR in the journalistic field (VI ZR 246/19, 7 Jul 2020; VI ZR 337/22, 27 May 2025). For non-journalistic publication there is no BGH ruling, so the GDPR governs (consent, or Art. 6(1)(f) balancing using KUG criteria); consent is revocable. Secondary sources only. https://www.srd-rechtsanwaelte.de/blog/kunsturhebergesetz-neben-dsgvo-anwendbar-auswirkungen-fuer-unternehmen
- **Social login:** no DSK position found **[NOT VERIFIED]**. OH Rn. 101 applies generally: embedding third-party content discloses data and needs a legal basis. So load no Google/Microsoft script before the click, redirect only on click, and store only the subject identifier.
- **Log files:** no statutory period, and I found no primary DPA text fixing "7 days" **[NOT VERIFIED]**. Secondary sources call 3–7 days for security purposes defensible under Art. 6(1)(f). The Stiftung Datenschutz guide recommends disabling IP logging where possible.
- **Hosting:** an Art. 28 contract (AVV) is needed with the VPS host and with Cloudflare, which sees traffic in clear text as tunnel/proxy. No German DPA statement on Cloudflare was found **[NOT VERIFIED]**; one secondary report mentions an unnamed DPA prohibiting US CDNs in a Shopify case.
- **US transfers:** the EU–US Data Privacy Framework decision of 10 Jul 2023 is valid. The EuG dismissed *Latombe* (T-553/23) on 3 Sep 2025; the appeal C-703/25 P was pending as of the latest report found (June 2026). Cloudflare's DPF certification was not checked **[NOT VERIFIED]**. https://www.wilmerhale.com/insights/blogs/wilmerhale-privacy-and-cybersecurity-law/20251201-european-court-of-justice-to-review-challenge-to-eu-us-data-privacy-framework

## 4. DSA / DDG

- The DDG has been in force since 14 May 2024; the Bundesnetzagentur is the Digital Services Coordinator. https://www.bundesnetzagentur.de/DE/Fachthemen/DSC/1_Themen/PflichtenVermittlunggsdienste/artikel.html
- Applicability to a truly non-commercial private service is unsettled, because the DSA builds on "information society service" (normally for remuneration). No BNetzA statement was found **[NOT VERIFIED]**. The Stiftung Datenschutz guide assumes volunteer instances are covered.
- Dastan would be a hosting service and an "online platform" (it stores and disseminates user content to the public; not a minor ancillary feature).

**Duties to implement**
- Art. 11/12: electronic contact points for authorities and users. Art. 11(3) language requirement **[NOT VERIFIED today]**: I believe it must include an official language of the Member State, i.e. German.
- Art. 14: terms in plain language describing restrictions and moderation.
- Art. 16: notice mechanism (reason, exact location, name and e-mail of notifier, good-faith statement; confirm receipt; inform of the decision). The BNetzA FAQ notes missing details alone do not justify rejecting a notice.
- Art. 17: statement of reasons to the affected user.
- Art. 18: report suspected offences threatening life or safety.

**Not applicable to micro/small providers**
- Art. 15 transparency report (Art. 15(2)).
- All of Section 3 – complaints system, Art. 24(2) user numbers, Art. 28 minors – "with the exception of Article 24(3)", i.e. user numbers on request (Art. 19(1)). https://www.eu-digital-services-act.com/Digital_Services_Act_Article_19.html
- Art. 19 speaks of "enterprises"; the guide argues it covers private operators equally but notes "Restunsicherheit". Its statement that Art. 24(2) still applies contradicts the Art. 19 text.

**Registration, fines**
- There is no registration or notification duty with the BNetzA; a legal representative is only for non-EU providers. The guide suggests naming the BNetzA complaint channel in the legal notice.
- Fines under § 33 Abs. 5–6 DDG run up to €300,000 / €100,000 / €50,000 by tier (which DSA breach falls in which tier not checked). Art. 18 DSA is not among the fined provisions.

**Liability**
- § 7 Abs. 1 DDG: Art. 4–8 DSA apply to all providers "unabhängig davon, ob für die Nutzung ein Entgelt erhoben wird". The hosting privilege (Art. 6 DSA) therefore covers Dastan. https://www.gesetze-im-internet.de/ddg/__7.html
- Injunctive Störerhaftung after notice remains. Per HmbBfDI: the BGH so far triggers duties only on a concrete notice (VI ZR 476/18, 23 May 2023) and has ordered stay-down for trademarks and copyright (I ZR 140/15, YouTube). *Künast v. Meta* is pending at the BGH, stayed for Russmedia.
- No 2025–26 BGH ruling on forums under Art. 6 DSA was found.

**NetzDG:** only §§ 1, 4, 4a, 5, 6 remain (last amended by Art. 29 G v. 6 May 2024); §§ 2–3f and 5a are "weggefallen". It covers only platforms run "mit Gewinnerzielungsabsicht", so it does not apply. https://www.gesetze-im-internet.de/netzdg/BJNR335210017.html

## 5. Youth protection

- The JMStV as amended by the Sechster Medienänderungsstaatsvertrag has been in force since 1 Dec 2025. Main new content: technical youth protection and operating-system duties. https://www.verkuendung-bayern.de/gvbl/2025-712/
- § 7 JMStV: a Jugendschutzbeauftragter is required only from "geschäftsmäßige" providers of publicly accessible telemedia with developmentally harmful or youth-endangering content. Providers under 50 staff or under 10 million monthly hits may instead join a recognised self-regulation body. Abs. 3 exempts DSA intermediary services from the appointment duty. Result: none needed. https://www.gesetze-bayern.de/Content/Document/JMStV-7
- § 5 JMStV: if own content could impair under-16s or under-18s, access barriers, time limits or a machine-readable age label are required. Age steps are 0/6/12/16/18. The assessment may consider contact and communication functions. For literary epic content this is normally not triggered (assessment). https://www.gesetze-bayern.de/Content/Document/JMStV-5
- § 24a JuSchG in its current text no longer shows the old 1-million-user threshold. It makes the BzKJ the authority for Art. 28(1) DSA and lists example measures. Art. 28 sits in the DSA section that does not apply to micro/small providers. https://www.gesetze-im-internet.de/juschg/__24a.html
- No German minimum-age law for social media was found. The federal expert commission delivered recommendations on 24 June 2026 (under-13 ban as one of two options, plus age verification). An EU proposal was expected in autumn 2026 **[NOT VERIFIED]**.
- No statute requires age verification for a general-content service admitting users from 16.

## 6. Contract and consumer law

- Terms of use are AGB (§§ 305 ff. BGB) even when the user of the terms is a private person.
- § 309 Nr. 7 BGB: no exclusion or limitation of liability for injury to life, body or health, nor for gross negligence or intent. https://www.gesetze-im-internet.de/bgb/__309.html
- BGH III ZR 179/20 and 192/20 (29 Jul 2021): removal and blocking clauses are invalid unless the user is informed, given the reason and can respond; for account blocks, in advance (secondary). This matches Art. 14/17 DSA. https://www.lto.de/recht/hintergruende/h/bgh-iiizr17920-facebook-meinungsfreiheit-beitraege-loeschen-sperre-nutzer-information
- Unilateral change clauses by fiction of consent: BGH case law **[NOT VERIFIED]**.
- The Stiftung Datenschutz guide notes the terms decide whether the service is a mere "Gefälligkeit" or a binding contract, judged objectively.
- §§ 327 ff. BGB apply only to "Verbraucherverträge" where an "Unternehmer" supplies digital products. Even then § 327 Abs. 3 with § 312 Abs. 1a Satz 2 excludes cases where personal data are processed "ausschließlich … um seine Leistungspflicht oder an ihn gestellte rechtliche Anforderungen zu erfüllen". Not applicable to a private operator. https://www.gesetze-im-internet.de/bgb/__327.html and https://www.gesetze-im-internet.de/bgb/__312.html
- § 36 VSBG addresses only "Ein Unternehmer, der eine Webseite unterhält oder AGB verwendet". Not applicable. https://www.gesetze-im-internet.de/vsbg/__36.html
- The EU ODR platform was shut down on 20 Jul 2025; do not link to it (IHK Düsseldorf).
- "Unternehmer" (§ 14 BGB) needs a commercial or self-employed professional activity, which is absent here. This changes if ads, donations or paid features are added; § 5 DDG, UWG, VSBG and BFSG then become live.
- BGH, 27 Mar 2025 (I ZR 186/17; I ZR 222/19, 223/19): competitors and consumer associations may pursue GDPR breaches under the UWG. This is relevant only for market participants. https://www.heuking.de/de/news-events/newsletter-fachbeitraege/artikel/bgh-bestaetigt-datenschutzverstoesse-koennen-durch-wettbewerber-und-verbraucherverbaende-verfolgt-werden.html

## 7. BFSG

- It applies to services "die für Verbraucher nach dem 28. Juni 2025 erbracht werden", including "Dienstleistungen im elektronischen Geschäftsverkehr" (§ 1 Abs. 3 Nr. 5). https://www.gesetze-im-internet.de/bfsg/__1.html
- § 2 Nr. 26 defines these as digital services offered via websites or apps with a view to concluding a Verbrauchervertrag. The obliged party is a "Dienstleistungserbringer" / "Wirtschaftsakteur". https://www.gesetze-im-internet.de/bfsg/__2.html
- A free private site with no consumer contract with a trader is outside the scope.
- Micro-enterprises providing services (under 10 staff and at most €2 million turnover) are exempt as well; the § 3 Abs. 3 wording is from secondary sources.

## 8. Copyright and personality rights

- § 2 Abs. 1 UrhDaG requires, among other things, a main purpose of storing "eine große Menge" of third-party protected uploads and promoting them for profit. Dastan is not such a "Diensteanbieter". Thresholds: start-up up to €10 million turnover and under 3 years; small up to €1 million. https://www.gesetze-im-internet.de/urhdag/__2.html
- General UrhG liability therefore applies, with Art. 6 DSA plus stay-down after notice (BGH I ZR 140/15, following EuGH C-682/18 *YouTube/Cyando*, as cited by HmbBfDI).
- Old family photos: the photographer's rights are a separate issue **[NOT VERIFIED]**.
- § 22 KUG: "Bildnisse dürfen nur mit Einwilligung des Abgebildeten verbreitet oder öffentlich zur Schau gestellt werden"; for 10 years after death the consent of relatives is needed (spouse or partner and children; failing those, parents). https://www.gesetze-im-internet.de/kunsturhg/__22.html
- Postmortem personality right (secondary): non-economic protection has no fixed term but fades and covers gross distortion; commercial components end 10 years after death (BGH I ZR 277/03).
- Names and dates of living third persons are GDPR data (see section 3).

## 9. Further points a careful lawyer would raise

- **Fonts:** self-hosting avoids the Google Fonts problem. The BGH referred the dynamic-IP, damage and abuse questions to the EuGH (VI ZR 258/24, order of 27/28 Aug 2025; C-654/25). An Advocate General opinion of 5 Nov 2026 is reported by one source only **[NOT VERIFIED]**. https://www.it-recht-kanzlei.de/bgh-eugh-abmahnungen-google-fonts.html
- **Where the real exposure lies:** individual claims (Art. 82 GDPR damages, injunctions and costs for image and personality violations) and DPA or BNetzA proceedings, rather than competitor warnings. A private operator is liable with all personal assets. Whether private liability insurance covers website operation must be checked in the policy **[NOT VERIFIED]**; a legal entity (e.V. or UG) would limit liability.
- **Art. 30 GDPR record of processing** is likely required because the processing is not occasional and may include Art. 9 data; also breach notification (Art. 33) and a DPIA check for family-tree data **[from memory, NOT VERIFIED today]**.
- **Without e-mail addresses,** plan how to authenticate data-subject requests and how to notify users of moderation decisions (Art. 17 DSA), e.g. an in-app inbox.
- **Terrorist-content removal orders** (Regulation (EU) 2021/784, one hour) apply to hosting providers of any size **[NOT VERIFIED today]**.
- **Home hosting:** Art. 32 security, backups, and the ISP's terms.
- **EU "Digital Omnibus"** (cookie rules, GDPR changes): the Council fixed its position on 21 Oct 2026 and reportedly keeps cookies in the ePrivacy Directive; there is no Parliament position and nothing adopted. https://www.heuking.de/de/news-events/newsletter-fachbeitraege/artikel/cookie-einwilligung-nach-dem-digital-omnibus-geplante-aenderungen-und-technische-umsetzung.html

## Changed in 2025–2026

| Date | Change |
|---|---|
| 27 Mar 2025 | BGH: GDPR breaches actionable under the UWG |
| 1 Apr 2025 | EinwV in force |
| 27 May 2025 | BGH VI ZR 337/22: KUG via Art. 85(2) GDPR confirmed for journalism |
| 28 Jun 2025 | BFSG applies |
| 20 Jul 2025 | EU ODR platform closed |
| 27/28 Aug 2025 | BGH Google Fonts referral (C-654/25) |
| 3 Sep 2025 | EuG upholds the Data Privacy Framework; appeal C-703/25 P pending |
| 1 Dec 2025 | JMStV/MStV as amended by the 6th Medienänderungsstaatsvertrag |
| 2 Dec 2025 | EuGH *Russmedia* C-492/23 |
| 2026 | DDG amended three times, none changing § 5 in substance: 3 Feb 2026 (BGBl. 2026 I Nr. 29, a comma in § 5 Abs. 1 Nr. 8); 25 Mar 2026 (Nr. 81, content not checked); 12 May 2026 (Nr. 138, §§ 22a–22c, effective 1 Jul 2026) – all via secondary sources |
| 2026 | BDSG amended (BGBl. 2026 I Nr. 139 and Nr. 199 cited in Bundesrat papers; content not checked); Bundesrat bill 356/26 on supervisory structure; repeal of § 38 Abs. 1 announced only |
| 12 May 2026 | HmbBfDI paper on Russmedia |
| 24 Jun 2026 | Federal expert commission recommendations on minors and social media |
| 17 Sep 2026 | BGH referral on the household exemption |
| 21 Oct 2026 | Council position on the Digital Omnibus |

## Not verified (summary)

- No court decision on § 5 DDG or § 18 Abs. 2 MStV for non-commercial UGC or cultural sites.
- MStV fine provision for § 18; §§ 19, 20 MStV were not examined.
- Impressum language; case law since 2017 on Impressum-service addresses.
- DSK or any DPA position on social login, genealogy, Cloudflare, service-worker caches, or a fixed log-retention period.
- §§ 32–37 BDSG text; content of the 2026 BDSG amendments.
- BfDI register of EinwV services.
- Art. 11(3) DSA wording; BNetzA view on non-commercial services; tier assignment in § 33 Abs. 6 DDG.
- Cloudflare's DPF certification; current status of C-703/25 P and C-654/25 beyond the reports cited.
- BGH case law on change clauses; minors' contractual capacity; insurance coverage.
- TCO Regulation details; the reported EU minors proposal.
- The DSA text itself was read only on an unofficial mirror (the EUR-Lex fetch failed).
- BGH and EuGH decisions were generally confirmed through secondary reports, not full texts.
