# Dastan and the law – what the site does and why

State: 2026-11-19 (row 18.8 of the [delivery plan](../delivery-plan.md)). The site is run by one private
person in Germany, is free of charge and shows no advertising. It answers to the law of the European Union
and of Germany. This folder is the working record of how it does so. It is **not legal advice**: the owner has
the texts and this record read by a lawyer before the site is public ([before-publication.md](before-publication.md)).

| File | What it holds |
| --- | --- |
| [research-eu-2026-11.md](research-eu-2026-11.md) | What was read about the law of the Union, with sources and what could not be verified |
| [research-de-2026-11.md](research-de-2026-11.md) | The same for German law |
| this file | What follows from it for the site: duties, records of processing, measures, procedures, the assessment of risk |
| [before-publication.md](before-publication.md) | What the owner still has to say, decide and sign, and what the lawyer is asked to look at |

The texts a visitor reads are in `Vafadar.Dastan.Web/Legal/` – `terms`, `privacy`, `cookies`, `imprint`, each
in `en`, `fa` and `de`. The German version is the authoritative one. What only the operator knows comes from
the settings `Dastan:Operator` ([operations.md](../operations.md)); a text with a value missing shows the gap
and says that it is not complete.

## 1. What applies, and how the site answers

| Law | What it asks | How the site answers |
| --- | --- | --- |
| § 5 DDG, § 18 MStV | Name, serviceable postal address and a fast electronic contact, easy to find from every page; a responsible person for editorial content | Page `imprint`, linked in the foot of every page. Address and telephone from `Dastan:Operator`. § 5 DDG may not apply to a site that is not offered for payment; § 18 (1) MStV applies in any case, so the full set is given |
| GDPR Art. 12–14 | Who processes what, why, on which ground, how long, who receives it, which rights | Page `privacy`, with a section for people who are named or shown by others (Art. 14) |
| GDPR Art. 6, 7, 8 | A ground for each processing; consent that can be taken back; 16 years | Grounds per activity in section 2 below; a tree is public only with the consent of every living person in it, a photograph with tagged people only with theirs, both taken back at once; registration asks for the age |
| GDPR Art. 15–22 | Access, rectification, erasure, restriction, portability, objection | In the account: a copy of the data (JSON), changing and removing everything, closing the account. For third persons: the notice form and the command `people person remove` |
| GDPR Art. 25, 32 | Data protection by design and default; security | Private by default; a mail address that nobody else is shown and no other contact data; metadata of photographs removed; section 3 below |
| GDPR Art. 28 | A contract with whoever processes for the site | Host, network provider and the company that sends the mail – to be signed by the owner ([before-publication.md](before-publication.md)) |
| GDPR Art. 30 | Records of processing | Section 2 below |
| GDPR Art. 33, 34 | Report a breach within 72 hours; tell the people concerned | Procedure 4.5; the setting `Dastan:Announcement` tells everybody; the accounts concerned can be written to at their addresses |
| GDPR Art. 35 | Assess the risk where it is likely to be high | Section 5 below |
| § 25 TDDDG (Art. 5 (3) of Directive 2002/58/EC) | Consent before storing on a device, unless strictly necessary for what was asked | Only necessary cookies, each written when its function is used (the language only when the visitor changes it); a note said once, no banner; page `cookies` lists all; keeping pages for reading without a connection only after a yes (`dastan.offline`) |
| DSA Art. 11, 12 | Contact points for authorities and for users | Page `imprint` |
| DSA Art. 14 | Terms that say what is restricted and how moderation works | Page `terms`, sections 6–11 |
| DSA Art. 16 | A way for anybody to give notice of illegal content, with a confirmation and a decision | Page `notice` with a form (`Dastan:Notices`), a reference at once; the operator reads with `notices`, decides, answers by mail |
| DSA Art. 17 | A statement of reasons to the user concerned | `people restrict …` records the facts and the ground; the user reads it on the account page – the owner of a closed account after giving name and password – and it is mailed to the address of the account (step 19.4) |
| DSA Art. 18 | Report suspicion of crimes against life or safety | Procedure 4.6 |
| DSA Art. 24 (3) | Tell the coordinator the number of active recipients on request | Procedure 4.7 |
| §§ 305 ff. BGB | Terms are standard terms: no surprising clauses, no exclusion of liability for intent, gross negligence, life and health; reasons and a hearing before content or an account is restricted | `terms` sections 10–14; registration notes the version read (`TermsVersion`); changed terms are agreed to by an act on the account page, never by silence |
| § 22 KUG, general right of personality | Pictures of people only with their consent | `terms` section 7; tags need confirmation; objection in the account; notice form |

Whether the Digital Services Act applies at all to a service that is not provided for remuneration is open
(see the research). The site keeps to its rules for hosting services anyway: they are what a careful operator
does, and § 7 DDG gives the liability privilege of Art. 6 DSA to free services too.

**Does not apply** (and why): the platform duties of DSA Art. 20–28 except 24 (3) – micro enterprise
(Art. 19); DSA Art. 15 reports – Art. 15 (2); NetzDG – no intent to make profit; UrhDaG – not a service whose
main purpose is large amounts of protected uploads for profit; BFSG – no consumer contracts with a trader;
§ 36 VSBG and §§ 327 ff. BGB – the operator is no trader; a data protection officer – § 38 BDSG, Art. 37
GDPR; a youth protection officer – § 7 JMStV; a consent banner – nothing needs consent but the offline
function, which has its own switch. All of this changes if the site ever takes money, donations or
advertising: then § 5 DDG, competition law and consumer law apply in full.

## 2. Records of processing (Art. 30 GDPR)

Controller: the operator named in `Dastan:Operator`. No data protection officer. No joint controller by
contract; for data of third persons in trees and photographs the operator may be jointly responsible with the
user (CJEU C-492/23 *Russmedia*) and answers the people concerned directly.

| # | Activity | Whose data, which | Purpose | Ground | Recipients | Kept |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | Delivering pages | Every visitor: IP address, time, address asked for, referring page, browser | Answer the request; security | Art. 6 (1) (f) | Host, network provider (processors) | Not stored by the program. Counts per address for the limits per hour in memory, one hour. Logs of the host: see its contract |
| 2 | Choices on the device | Visitors: language, colours, reading stage, saved list, the note read, offline switch | The function the visitor used | § 25 (2) no. 2 TDDDG; offline: consent, § 25 (1) | – (on the device) | One year after last written |
| 3 | Account | Users: chosen name, mail address and whether it is confirmed, hash of the password, hashes of recovery codes, hashes of the secrets of mailed links with the moment they end, language, creation, failed attempts, lock, closing, version and time of the terms read | Account and its safety; proof of the agreement | Art. 6 (1) (b), (f) | Host | Life of the account; 30 days after closing; then deleted |
| 4 | Sign-in through Google or Microsoft | Users who choose it: provider, hash of its identifier, and the mail address the provider confirmed (it becomes the address of the account) | Sign-in | Art. 6 (1) (b) | Google Ireland, Microsoft Ireland as controllers of their own; transfer to the USA under the Data Privacy Framework | Until unlinked or the account is deleted |
| 5 | What belongs to an account | Users: reading positions, saved pages, invitations, relations, circles, albums; what the page of the account says about it – a few words with their audience, and which photograph is its picture (D-086) | The functions | Art. 6 (1) (b) | The accounts the user chose | Until removed or the account is deleted |
| 6 | Family trees | Users; third persons: name, years of birth and death, deceased or not, kinship; history of changes | Family history of the user | User: household exemption or own ground; operator: Art. 6 (1) (f); public tree: consent of every living person, Art. 6 (1) (a) | Accounts the owner shares with; everybody for a public tree | Until removed or the account is deleted; a person's record on objection |
| 7 | Photographs | Users; people shown: picture (metadata removed), caption, date, audience, tags and their state, objections | Memories of the user | As 6; shown to everybody only with the consent of every tagged account | The audience the owner chose | As 6 |
| 8 | Reports of mistakes on editorial pages | Visitors: day, page and statement, language, text – no name, no address | Correct content | Art. 6 (1) (f) | Editors | 90 days after an editor looked at it |
| 8a | Places proposed to the editors (D-085) | Users who propose one: day, language, the name of the place, its kind, where it is, a public page, a note – no name, no address, no account | Complete content | Art. 6 (1) (f) | Editors | 90 days after an editor decided it (`reports forget`) |
| 9 | Notices about content | Whoever gives one: time, address of the content, kind, explanation, name and mail if given | Examine, decide, answer, show how it was handled | Art. 6 (1) (c) with Art. 16 DSA, else (f) | Operator | One year after the decision (`notices forget`) |
| 10 | Restrictions and statements of reasons | Users: what, facts, ground, after a notice or not, dates | Enforce law and terms; tell the reasons | Art. 6 (1) (c) with Art. 17 DSA, else (f) | The user concerned | With the account |
| 11 | Mail to the operator | Writers: address, name, content | Answer; rights of data subjects | Art. 6 (1) (f), (c) | Mail provider of the operator | Until settled, at most three years after the end of that year |
| 13 | Mail of the site (ADR 0016) | Users: the address a message goes to, its kind, its text until it is handed over, when it was sent or why not | Confirm an address; a new password; tell what changed in the account, what another user asks (can be turned off), and a decision with its reasons | Art. 6 (1) (b); what others ask: (f); statement of reasons: (c) with Art. 17 DSA | The company that sends the mail (processor), the mail provider of the receiver | The text is emptied when the message is handed over; the record 30 days; a message that cannot be handed over is given up after about two days |
| 12 | Backups | Everything of 3–10 and 13 | Restore after a failure | Art. 6 (1) (f), Art. 32 | Host | 30 days at most |

No special categories are asked for; the terms forbid entering them about living people. No profiling, no
automated decision. Transfers to third countries only as in row 4 and where the host or the network provider
belongs to a group from the USA (Data Privacy Framework or standard contractual clauses).

## 3. Technical and organisational measures (Art. 32 GDPR)

* **Less data**: a mail address shown to nobody else and told to nobody (whether an address has an account is
  not answered); no real name, no date of birth; only years in trees; reports without
  name or address; the identifier of Google or Microsoft only as a hash; metadata of JPEG and PNG removed.
* **Private by default**: a photograph and a tree are seen by their owner only until the owner says otherwise;
  public only with consent of the living people concerned; pages of the private area and public trees are not
  indexed (`noindex`).
* **Access**: passwords as PBKDF2 hashes, at least 12 characters; recovery codes hashed; five wrong passwords
  pause sign-in for 15 minutes; limits per hour per address; a new password signs out everywhere; sessions of
  30 days in an encrypted cookie that scripts cannot read.
* **Transport and browser**: TLS; cookies `Secure`, `HttpOnly`, `SameSite=Lax`; antiforgery tokens on every
  form; a strict content security policy; nothing loaded from another domain; `Referrer-Policy`.
* **Files**: photographs are served only through the address that asks who may see them; uploads are checked
  by their content and limited in size and number.
* **Operation**: the container runs without root; the store, the photographs and the keys lie on one volume
  that is backed up as one; access to the server only for the operator; updates of the base image with every
  release.
* **No third parties in the page**: no analytics, no fonts or scripts of others, no embedded players.
* **Deleting**: closing deletes after 30 days with everything that belongs to the account; names of a deleted
  account are taken out of the histories of trees; reports and notices have their own periods.

## 4. Procedures of the operator

The commands are those of `Vafadar.Dastan.Content` ([operations.md](../operations.md)).

### 4.1 A notice about content arrives (Art. 16 DSA)

1. Look at the waiting notices at least every working day: `notices <folder>`, then `notices show <reference> <folder>`.
2. Look at the content. Is it illegal, or against the terms? Weigh the rights of everybody, also freedom of
   expression. Sexual depictions of minors and threats to life: act at once and go to 4.6.
3. Decide: leave it, restrict it (4.2), or ask the user to change it. Decide without undue delay; within 24
   hours where a person's privacy or safety is at stake.
4. Tell who gave the notice, by mail if they left an address: what was decided, in short why, and that they
   can ask for another look and go to court. Do not name the user's identity beyond what the page shows.
5. Note the decision: `notices settle <reference> <folder> --decision "<text>"`.
6. Once a month: `notices forget <folder>` removes what was settled more than a year ago.

### 4.2 Restricting (Art. 17 DSA)

`people restrict photo|tree|account <target> --people <store> --site <address of the site> --reason "<facts>" --ground "<law or section of the terms>" [--noticed]`

* The reason states the facts so that the user can understand and answer: what, where, why it is a problem.
  Never name who gave the notice.
* The ground names the law (for example "§ 22 KUG", "Art. 21 GDPR") or the section of the terms.
* The mildest measure first. Close an account only for a serious breach or after a restriction that said so;
  otherwise restrict the content, which gives the user the hearing the courts ask for (BGH III ZR 179/20).
* The user reads the statement on the account page; the owner of a closed account after name and password.
  The same statement is mailed to the address of the account, with the number of the decision; `--site` is
  what the link in that mail begins with.
* A complaint by mail within six months is looked at again by a person; if it is justified:
  `people lift <number> --people <store> --site <address of the site>`. The account is told by mail.

### 4.3 Somebody is named in a tree or shown on a photograph and objects (Art. 17, 21 GDPR)

1. Make sure, as far as reasonable, that the person is who they say (no more data than needed).
2. Ask the user through a restriction that says what to change, or act at once where waiting would harm:
   `people persons <tree> --people <store>`, `people person remove <tree> <person> --people <store>`; for a
   photograph `people restrict photo …`.
3. Answer the person within one month (Art. 12 (3) GDPR).
4. If the same data are entered again, restrict the tree or close the account ("stay down").

### 4.4 A request of somebody with an account

Access and portability: the copy in the account. Erasure: closing, which deletes after 30 days; on request at once
with `people forget <name> --people <store>`, after the account was closed by its visitor – which shows that
the request comes from the account. Answer within one month.

### 4.5 A breach of security (Art. 33, 34 GDPR)

1. Stop it: take the site offline if needed; change keys (`Dastan__Keys`) to end all sessions.
2. Write down what happened, when, which data, how many people, what was done (Art. 33 (5)).
3. Within 72 hours of knowing, report to the competent authority (its online form), unless a risk to people
   is unlikely.
4. If the risk is high: tell everybody with `Dastan:Announcement` in both languages and on the first page –
   and write to the accounts concerned at their addresses (the outbox, with a text of its own for the case) –
   what happened, what to do (a new password), whom to ask.

### 4.6 Suspicion of a serious crime (Art. 18 DSA)

Information that makes a crime against the life or safety of a person likely is reported at once to the
police of the Member State concerned (in Germany any police station or the online station of the Land; for
depictions of abuse of children also the Bundeskriminalamt). Keep the content away from everybody
(`people restrict …`), do not delete it before the police said so, and do not look at it more than needed.

### 4.7 Numbers for the coordinator (Art. 24 (3) DSA)

On request of the Bundesnetzagentur or the Commission: the average number of monthly active recipients in
the Union over the last six months. The site counts nobody; an estimate comes from the number of open
accounts in the store and from the request counts of the host.

### 4.8 Changing the terms or the privacy notice

1. Change the three language versions together; the tests compare their structure.
2. For a change that matters to users: set `AccountDesk.TermsVersion` to the new date. Every account is then
   asked on its page to read and confirm; nothing is agreed to by silence.
3. Announce it at least 30 days before with `Dastan:Announcement`.
4. A change to what is stored on devices: update `cookies.*.txt` and raise `DeviceStorage.NoticeVersion`, so
   that the note is shown again.

### 4.9 Ending the site or a part of it

Announce with `Dastan:Announcement` at least 30 days before; the copy of the data stays available until the
end; afterwards delete the store, the photographs, the backups and the notices.

## 5. Assessment of the risk (short DPIA, Art. 35 GDPR)

A full assessment is required where processing is likely to result in a high risk. The site processes data
of third persons in family trees and photographs, which the supervisory authorities and the Court of Justice
(*Russmedia*) treat as a matter that needs care; so the risks are written down here.

| Risk | Why it could happen | What stands against it | What remains |
| --- | --- | --- | --- |
| A living person is named or shown without knowing | A user enters relatives | Private by default; public only with the consent of every living person, which needs an account; terms section 7; objection and removal by the operator; no indexing | Among the people a tree is shared with, a person can be named without knowing – as in every family album; they are told when they ask |
| Sensitive data about a person | Free text in names, captions, notes | No fields for them; forbidden by the terms; short limits of length; notice and removal | Text nobody reports |
| A photograph of a child | Family pictures | Terms section 7; private by default; public only with the consent of tagged accounts, and children have none | A picture shared within a circle |
| Somebody gets into an account | Weak or reused password | 12 characters, lock after five tries, limits per hour, sign-out everywhere, optional sign-in through a provider | A password given away |
| A lost account | The password is lost | A link mailed to the confirmed address; eight recovery codes as the second way | An account whose address and codes are lost too cannot be opened by anybody – its data are deleted with it, none are exposed |
| An account is taken over | Somebody reads the mailbox of a user | A link works once and for one hour; opening it changes nothing before the button of the page; every change of the way in is told to the address and signs out everywhere | Whoever has the mailbox has the account – as with every account that can be reset by mail |
| An address is found out | Asking the site whether an address has an account | The same answer whether it has or not; the mail goes to who has the address; ten such mails an hour for a sender | None found |
| The store is taken | Attack on the server | Hashes of passwords, codes and links; no real names; the texts of sent mail emptied; access for one person, encrypted transport, backups for 30 days | The mail addresses of the accounts, the names in trees and the pictures themselves |
| A provider sees the traffic | Network provider in front of the site | Contract under Art. 28; Data Privacy Framework | To be confirmed by the owner |
| Misuse of the notice form | False notices to silence somebody | A person decides; the user is told the reasons and can answer; limits per hour | – |

Result: with these measures the remaining risk is that of a private family album kept online among invited
people. No consultation of the authority under Art. 36 is needed. Look at this again when the site opens to
everybody, when videos of visitors are allowed, or when anything is added that shows data of living people
to the public without their own act.
