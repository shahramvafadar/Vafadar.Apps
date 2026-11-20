# 0016. Accounts on ASP.NET Core Identity, with a confirmed mail address

* Status: Accepted (D-077); takes the place of the accounts of [0014](0014-visitor-accounts.md) and of the editors' accounts of [0011](0011-editors-store.md)
* Date: 2026-11-19
* Step: 19.4 of the [delivery plan](../delivery-plan.md)

## Context

An account of a visitor was a name and a password with recovery codes, and nothing else: no mail address,
because no service to send mail had been chosen. That was a mistake of reasoning – which service sends mail
is a setting, whether an account has an address is part of what an account is. Without an address nobody can
be told anything, a lost password is a lost account, and an invitation is a link somebody has to carry. The
visitors and the editors also each had their own code for passwords, stamps and lock-outs.

## Decision

* **ASP.NET Core Identity** keeps passwords, tokens, lock-outs and sign-ins for both kinds of account, in the
  schema `identity`. Visitors and editors are **two kinds of user in two sets of tables with two cookies**; an
  editor is made by the operator and is never made, merged or given a role because a name or an address is
  the same as a visitor's.
* **A mail address is required, unique and confirmed.** An account that has not confirmed can sign in only to
  confirm or to have the mail sent again; nothing else of the site treats it as an account. The address is
  never shown to anybody else.
* **The name stays** what others see: chosen, unique, in any script. The password has at least 12 characters;
  five wrong ones pause sign-in for 15 minutes; another password signs out everywhere.
* **The journeys**: register, confirm, send again, sign in, forgotten password, another address (confirmed at
  the new one, told to the old one), recovery codes as a second way, close and think it over for 30 days,
  closed by the operator with the reasons – on the page after proof of who asks, and by mail.
* **Google and Microsoft** are asked for who somebody is and for the address they confirmed, nothing else.
  A first sign-in makes an account with that address. An address that an account already has does not sign
  anybody in: the visitor signs in to that account and links the provider there. An account that has only a
  provider proves itself through it – also to read why the operator closed it.
* **Mail leaves through an outbox**: a row in `delivery` written in the same transaction as what it tells of,
  sent by a service of the site with retries that wait longer each time, never twice (a key per message),
  with what failed kept for the operator. The sender is `IMailSender`; the one of the product speaks SMTP
  (MailKit) to whatever service the settings name – choosing a service is the owner's and changes no code.
  On a developer's machine and in the tests a mail catcher of the Compose project receives everything.
* **Texts of mail** are written in both languages, as plain text and as simple HTML with the direction of the
  language, and say only what the receiver needs; a link in one is used once and for a short time.
* **Invitations** go by mail and still as a link. What the trial asked for – a site by invitation only – stays
  a setting.
* **Nothing is carried over** (D-080). The legal texts and the records of processing are written again for
  accounts with an address (step 19.10).

## What is built

**The mail (2026-11-19, first part of step 19.4).** The outbox is the table `delivery."Mail"`
(`MailOutbox` in `Vafadar.Dastan.Data`): a message has a key and is kept once; it can be written inside the
transaction of what it tells of; what is due is taken with a lock of its row, so several senders never take
the same message; a failure waits 1, 5, 30 minutes, 2, 6, 12, 24 hours and is given up at the eighth try; the
text of a sent message is emptied and its record forgotten after 30 days. `SmtpMailSender` speaks SMTP with
MailKit 4.18.2 (MIT) – plain text and HTML side by side, the language in the head, the same Message-ID on
every try. The site sends every five seconds (`MailPump`) where `Dastan:Mail` names a server, and does not
start with half of the settings. The operator lists, retries and forgets with the command `mail`. Mailpit
1.33.0 (MIT), pinned by its digest, catches the mail of a development machine and of the tests, in Compose and
in CI. "Never twice" holds for every hand-over and for every sender at the same moment; a site that stops
between a server taking a message and that being written down sends it once more under the same identifier –
said in the operations guide.

**The accounts of visitors, in the store (2026-11-19, second part).** The tables of Identity lie in the
schema `identity` (`VisitorAccounts`, `VisitorTokens`, `VisitorLogins`, `VisitorClaims`) and are part of the
context of the visitors' store – so an account, what belongs to it in `personal`, the invitation it uses up
and the mail that tells of it are one transaction. A name and a mail address each have a unique index.
`store.Accounts` registers (name, address, password, the language of the page), confirms by the link, sends
it again, and signs in by name or address. Identity's `UserManager` makes the account, checks the name, the
address and the password and derives what is kept of it; its stamp is the stamp of the session.

**Built otherwise than "Identity keeps tokens and lock-outs" may be read**, and said here: the token of a
link and the pause after wrong passwords are *kept in Identity's tables* but *counted by the store* with its
own clock – a token is 32 random bytes, kept as its SHA-256 with the moment it ends, used once; five wrong
passwords set `LockoutEnd`. Identity's own token providers and lock-out read the clock of the machine and
cannot be moved in a test, and their tokens depend on the keys of the forms; these depend on nothing but the
database.

**Whether an address has an account is told to nobody but who has the address.** Registering with an address
that is taken answers as every registration does – "look at your mail" – and the mail goes to who has the
address, once an hour at most, saying what was tried. For the same reason the recovery codes are shown when
the address is confirmed, not when the form is sent. A name is public, so "that name is taken" is said.

**The schema `delivery` has an owner of its own** (`DeliveryContext` in `Vafadar.Dastan.Data`, with its
history of changes): the site depends on it for mail and must not depend on the store of the editors for a
table. `database migrate` makes the three parts.

**Every journey of an account, in the store (2026-11-19, parts three and four).** Another password (proved by
the one it has), a forgotten one (a mailed link, once, one hour), another address (the link goes to the new
address, the old one is told, nothing changes before the link is opened), signing out everywhere, recovery
codes as the second way in, closing and opening again. Whatever changes how an account is entered signs it
out everywhere and is told to its address. Google and Microsoft are Identity's logins: a first sign-in makes
an account with the address the provider confirmed; an address that has an account makes no second one and
signs nobody in – its visitor signs in and links the provider there.

**The site is on these accounts (2026-11-19).** The code for accounts with a name and a password alone is
gone – from the store, its tables and its tests; the site never had two kinds of account. The forms:

* *Making an account* asks for a name, a mail address and a password and answers "look at your mail"; nobody
  is signed in. Signing in with the right password before the address is confirmed says so and offers the
  link once more.
* *A mailed link* leads to a page of its own (`/account/confirm`, `/account/reset`, `/account/address`).
  **Opening a link changes nothing**: the page shows a button or a form, and that does it – a program that
  follows the links of a mail (a scanner of a mail provider) uses none up. The page tells no other site
  where its visitor came from (`referrer: no-referrer`), since its address holds the token.
* *Confirming* signs the visitor in, on the device the link was opened on, and shows the recovery codes
  there. That is decided, not an accident: the codes open the account anyway, so whoever is shown them may
  as well be signed in. What the device holds – the list, the reading position – is brought into the account
  at that moment.
* *Asking for a link* ("forgot the password", "no mail with the link") answers in the same words whether
  there is such an account or not, and one sender gets ten such mails an hour at most.
* *The page of the account* shows its address to its visitor and nobody else, changes it, and signs out
  everywhere else.
* *The providers* are asked for the address they confirmed: Google with the scope `email`, taken only when
  Google says it verified it; Microsoft's `mail` or, without a mailbox, the name the account signs in with
  when that is an address of its own. A provider that names none makes no account. Everything else a provider
  sends is dropped as before.
* *A mail is written in the language of the page* the form was sent from; later mail in the language of the
  account.

**Written again in step 19.10 (2026-11-20):** the privacy notice and the terms, in English, German and Persian,
say what an account is now – a name, a confirmed mail address, a password –, which mail the site sends and for
how long it keeps it, what it asks of Google and Microsoft, and that a statement of reasons is mailed; the
company that sends the mail is named from a setting (`Dastan__Operator__Mailer`), and the record of processing
has the mail as an activity of its own. What follows was true until then.

**What was true until step 19.10:** the legal texts of the program (privacy notice,
terms) still said that an account needs no mail address. They are written again in step 19.10, together with
the record of processing and the mail provider (the owner's decision). A site with these accounts is not
shown to the public before that.

**The mail about what others do, and invitations by mail (2026-11-19, part five).** The store writes a
message in the step of what it tells of (`NoticeMail`):

* *What somebody else asks of an account* – to show a relationship, a tag on a photograph, to agree to a
  photograph or a family tree being shown to everybody (R-PER-03) – is told to the account in its language.
  The message says who asked and where to answer, and carries nothing of what it is about: not the kind of
  the relationship, no photograph, no name of a tree or of a person in it. An account turns these messages
  off and on (PRIV-007, the page of the account; the choice is part of the copy of its data). A tag is told
  once for a photograph, a request to agree once a day at most.
* *What who runs the site decides* – a photograph hidden, a tree taken back, an account closed – is mailed
  with the full statement of reasons, in the words of the page of the account, and so is the end of a
  decision. These are always sent: an account that was closed cannot read its page. The commands
  `people restrict` and `people lift` are told the address of the site (`--site`), which the link begins
  with, and refuse an address that is none.
* *An invitation* can be mailed by the site to an address its account names: one message, in the language
  of the page it was asked on, with the name of who invites. The address is in that message and nowhere else
  – the invitation does not know it – and the message is emptied when it was sent, like every other. It
  counts against the open invitations of the account (ten), and a sender gets ten mails an hour.

**The editors on Identity (2026-11-19, part six).** An editor is an `EditorAccount` – Identity's user – in
the table `identity."EditorAccounts"`, made by Identity's `UserManager` with the command of the operator as
before; the store's own code for passwords is gone. The name is what an act is kept with; an editor has no
mail address, no token and no login of a provider, so the account has one table. Roles, closing and the list
of the last sign-ins stay Dastan's. The pause after wrong passwords is counted by the clock of the store in
Identity's columns, as for visitors. The tool keeps its own cookie. **Two kinds, never mixed, by the database
too:** `database grant` gives the role of the site the tables of the visitors and the role of the tool the
table of the editors – table by table in the schema they share – and a test asks each for the other's and is
refused. Nothing an editor sees or types has changed.

**What is kept of a password**, for both kinds: Identity's third form – PBKDF2 with HMAC-SHA-512, a salt per
password, **210,000 rounds** (Identity's own default is 100,000; the number follows what OWASP names for this
function and is written into the value, so it can be raised without asking anybody for a new password).

**The journeys in a real browser (2026-11-19, part seven).** `AccountJourneyBrowserTests` run the site with a
database of its own and with mail that really leaves by SMTP to the catcher; the browser fills in the forms
as a person does, the test reads the mail from the catcher and opens the link exactly as it was written:
making an account in English and Persian and without scripts, a link that is used up, a forgotten password,
another address, and that the page of a link hands its address to no other page. The pages of an account
are checked against the rules of accessibility a tool can decide, in both languages and both appearances.
In Chromium, Firefox and WebKit. The check found a list of problems that was itself announced as an alert –
which takes the list away from a screen reader – on every page with a form; the alert is the box around the
list now.

**Step 19.4 is built.** Not tried, and said in the delivery plan: a real client of Google or Microsoft; the
look of a message in a mail program; a real mailbox and a sender with a domain (the owner's part). The
browser tests run where a browser is named – on a developer's machine; in CI with step 19.11.

## Consequences

The site sends mail, so it needs a sender with a domain that receivers trust (SPF, DKIM, DMARC) before it is
public – the owner's part. The store of the visitors loses its own code for passwords. The rules of Dastan –
invitations, claims, consent, circles, publication – stay Dastan's and are not Identity's.

## Added in step 20.3 (2026-11-20): who somebody is at a provider is not that an address is theirs

A review of the source found that an account made through Microsoft took the field `mail` of the answer or,
without one, a name to sign in with that looks like an address, and counted it as confirmed. Neither says
that there is such a mailbox or that it is the visitor's. Signing in through a provider did not ask whether
the address of the account was confirmed, and a visitor who came with an address that has an account was told
so. Reproduced by a test that failed (`UnprovenAddressTests`), then changed:

* **Two things.** Who somebody is at a provider – the provider and its identifier of the person – finds the
  account of a sign-in. An address never finds an account and never joins two.
* **Proven or named.** A provider carries a claim that the address is proven only when it says it verified
  it: Google, with `email_verified`. Microsoft never does; what it names is put into the form as a proposal.
  Where a provider names nothing, the visitor writes the address.
* **An address that is not proven is confirmed by a mail of the site**, like the address of an account with
  a password: the account is made, the link goes through the outbox, and the answer carries no sign-in and no
  codes. The codes come with the confirmation.
* **No sign-in before that – by no way.** Signing in through the provider answers "this account waits";
  a cookie of the account does not hold (`PeopleStore.Holds` asks for the confirmed address on every request
  that checks a sign-in); the password, where there is one, was refused already.
* **Who waits** can have the mail once more or correct the address. Only who signs in at the provider comes
  to that form, so nobody else moves the address of an account that is not confirmed. A new link takes the
  place of the one before: a link that went to a mistyped address opens nothing afterwards.
* **Nothing is said about an address.** An address that has an account makes no second one, whether it is
  written at the first form or as a correction; its holder is told that somebody tried, and who wrote it reads
  the words of a mail that was sent. Only with an address a provider verified is the visitor told that it has
  an account – they have that address.
* **How an address was confirmed is written down** (`AddressProof`): `mail`, or `provider:` and its name.
  A password set with a recovery code no longer marks the address as confirmed – the codes are given with the
  confirmation and say nothing about a mailbox.
* **What was there before.** When the store is brought up, an account that never had a provider is written
  down as confirmed by mail – nothing else was possible. An account with a provider stays without a record;
  nobody can say any more which way it went. `people addresses` lists these, and `people addresses ask
  microsoft` asks those of them that can be signed in through Microsoft to confirm anew: the link is mailed,
  every sign-in ends, nothing of the account is removed. Nobody is asked twice, and no evidence is invented.
  No account was ever made through Microsoft – no client exists – so the list of a real store is empty.

**Shown by tests:** a name to sign in with, no address, and an address the provider did not verify
(`UnprovenAddressPagesTests`, both cases of the form); coming back twice before confirming; correcting and
sending again; an address that has an account, at the form and as a correction, with the same words and the
mail to its holder; a link that is old or was replaced; a page of an account and an early sign-in; an account
that was confirmed before; the list and the asking. The mail of the journey is really sent to the catcher and
opened from there. **Not shown:** a real provider and a real mailbox – no client exists; that is the owner's.
