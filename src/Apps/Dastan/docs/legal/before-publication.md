# Before Dastan is public – what is still to say, to decide and to sign

State: 2026-11-19. The texts and the functions are built ([README.md](README.md)). Nothing here can be done
by the program: it is the owner's part, and the lawyer's.

## 1. What the owner tells the site

These are settings of the running site ([operations.md](../operations.md)), not files of the repository: a
private address does not belong into a public repository. Until they are said, the legal pages show a gap
and say that they are not complete.

| Setting | What | Needed for |
| --- | --- | --- |
| `Dastan__Operator__Street`, `__Town`, `__Country` | A postal address at which a letter and a court document reach the owner. A post office box is not enough. An address "c/o" or of a service works only if delivery there really reaches the owner; this is not settled by the courts | Imprint (§ 5 DDG, § 18 MStV), privacy notice (Art. 13 GDPR), terms |
| `Dastan__Operator__Phone` | Optional. Without it the imprint has e-mail and the form, which the Court of Justice accepted (C-298/07) as long as mail is answered fast | Imprint |
| `Dastan__Operator__Host` | Name and address of the company whose machines the site runs on | Privacy notice, section 11 |
| `Dastan__Operator__Network` | Name and address of the company that carries the traffic, if there is one besides the host (for example Cloudflare, Inc., 101 Townsend St, San Francisco, CA 94107, USA) | Privacy notice, cookies page |
| `Dastan__Operator__Mailer` | Name and address of the company that sends the mail of the site on its behalf (the service `Dastan__Mail__Host` belongs to). The privacy notice names it as a processor; a contract according to Article 28 GDPR is needed with it |  |
| `Dastan__Operator__Authority` | The data protection authority of the Land the owner lives in, with its address (list: https://www.bfdi.bund.de/DE/Service/Anschriften/Laender/Laender-node.html) | Privacy notice, section 16 |
| `Dastan__Notices` | `true`: notices about content are taken with a form and kept in the database | The form of the page `notice` |

Already in the program: the name (Shahram Vafadar), the site (https://dastan.online), the mail address
(info@dastan.online) and, since the owner's word of 2026-11-19, the Land – Niedersachsen, Germany – with the
authority that is competent there (Die Landesbeauftragte für den Datenschutz Niedersachsen, Hannover). The
Land stands in the place of the town until the owner says the full address: the street is still a gap, so
the pages keep saying that they are not complete. `Dastan__Operator__Town` then takes the postal code and the
town.

Confirmed by the owner on 2026-11-19 (D-075): the network provider of the trial stays for now and is decided
again with the host; the age stays 16; info@dastan.online is the mailbox that is read; the site stays
non-commercial.

## 2. What the owner does

1. **A mailbox that is read.** info@dastan.online must exist and be read at least every working day:
   notices, requests of data subjects (answer within one month) and authorities arrive there.
2. **Contracts under Art. 28 GDPR** with the host and, if used, the network provider. Both offer them as a
   standard document to accept in the customer account. Keep a copy.
3. **Where the data lie.** The privacy notice says the data are stored in the European Union. Choose the
   location of the server accordingly, or change the sentence.
4. **Backups for 30 days at most.** The privacy notice says so; set the backup of the volume to keep copies
   no longer.
5. **Logs of the host and the proxy.** Find out how long they keep addresses of visitors; if it is longer
   than a few days, shorten it or say the period in the privacy notice.
6. **Registration at Google and Microsoft** only when the sign-in through them is wanted; their consoles ask
   for the addresses of the privacy notice and the terms – they exist now.
7. **Look at the waiting notices** every working day (`notices <folder>`), and once a month `notices forget`.
8. **Liability.** A private person is liable with everything they own. Ask the insurer whether the private
   liability insurance covers running a website with content of others; consider an association or a company
   if the site grows.
9. **Keep it non-commercial**, or come back to the texts first: advertising, donations, paid functions or
   affiliate links change which laws apply (§ 5 DDG in full, competition law, consumer law, VAT).

## 3. Decisions the owner may want to take with the lawyer

| Question | What the site does now | Alternative |
| --- | --- | --- |
| Which version is authoritative | German | English – German courts and authorities read German; a German text avoids disputes about translation |
| Does the DSA apply to a free private service | The site keeps to it either way | Say in the terms that it does not apply – gains nothing |
| Is the editorial part "journalistic-editorial" (§ 18 (2) MStV) | A responsible person is named | Leave it out – saves a line, risks a fine |
| Age | 16 (Art. 8 GDPR), confirmed by a tick | 18 – avoids questions of contractual capacity of minors (§§ 106 ff. BGB); or verification, which no law asks for here |
| Internal complaints (Art. 20 DSA does not bind a micro enterprise) | Offered voluntarily, by mail, six months | Not offered |
| Trees and photographs with living third persons | Private by default; public only with consent of everybody; objection; removal by the operator | Forbid living third persons entirely – safest, and takes the purpose from a family tree |

## 4. What the lawyer is asked to look at

1. The imprint: is the address serviceable, are § 5 DDG and § 18 MStV both satisfied, is the responsible
   person rightly named?
2. The privacy notice: the grounds of section 9 (third persons; Art. 6 (1) (f) and the household exemption
   after *Russmedia*, C-492/23), joint responsibility (Art. 26), and whether the information under Art. 14
   reaches the people concerned well enough.
3. The terms as standard terms (§§ 305–310 BGB): the clause on liability (section 12) for a free service, the
   clause on changes (section 14), the clauses on restricting and closing (sections 10, 11) against BGH
   III ZR 179/20 and III ZR 192/20, the licence of section 5.
4. The cookies page against the guidance of the German authorities (OH Digitale Dienste 1.2): is every item
   strictly necessary, in particular the reading stage and the saved list?
5. The notice-and-action procedure and the statement of reasons against Art. 16 and 17 DSA.
6. The assessment of risk in [README.md](README.md), section 5: is a full assessment under Art. 35 needed?
7. The German translations of legal terms the translator marked as uncertain (list below).
8. Whether minors of 16 and 17 can agree to the terms without their parents (§ 110 BGB, a free service).

Terms the translation into German was unsure about: "real name" (bürgerlicher Name / Klarname); "notice"
(Meldung) against "report of a mistake" (Fehlerhinweis); "reference" of a notice (Aktenzeichen /
Vorgangsnummer); "symbols of banned organisations" (§ 86a StGB speaks of verfassungswidrige und
terroristische Organisationen); "what we fraudulently concealed" (arglistig verschwiegen); "breach of
security" (Art. 34 speaks of Verletzung des Schutzes personenbezogener Daten).

## 5. What nobody can promise

No text and no function makes a site free of every legal risk: courts decide single cases, the guidance of
the authorities changes, and several questions that matter here are open (the reach of *Russmedia* for
private platforms, the application of the DSA and of § 5 DDG to free private services, addresses of service
in an imprint). The research files say for every point what was verified and what was not. The greatest
remaining risk is not a text but content: a person who is named or shown against their will. The site is
built so that this is rare, noticed and quickly undone.
