# 0014. Accounts of visitors: a store of their own, a name and a password

* Status: Accepted (D-048 to D-050, D-055)
* Date: 2026-11-18
* Step: 16.1 of the [delivery plan](../delivery-plan.md)

## Context

Until now the site kept nothing about a visitor: a language, an appearance, a reading position and a list, each
in a cookie of the visitor's own device. The private spaces of document 06 – relationships two people show
together, records of people and family trees, personal photographs – need somebody to belong to. On 2026-11-18
the owner decided (D-048 to D-055): a visitor signs in with a name and a password the site keeps itself, and
later also with Google or Microsoft; anybody may make an account; accounts and private spaces are kept in a store
of their own beside the site; a closed account is deleted for good after 30 days.

No mail service is decided. The site therefore cannot send anything to anybody: it cannot confirm an address and
cannot send a link for a lost password.

The public pages stay what they are: read from the files of a published revision, the same for everybody at the
same reading position, without a script ([ADR 0002](0002-one-projection.md), [0003](0003-server-rendered-pages.md),
[0009](0009-published-revision.md)).

## Decision

* **A third store.** The project `Vafadar.Dastan.People` holds a SQLite database of its own (`PeopleStore`,
  migrations like the store of the editors). It is neither the files of the collections nor the store of the
  editors ([ADR 0011](0011-editors-store.md)), and it references neither: the site reads the collections as
  before, and nothing a visitor writes can reach a collection. The file is named by `Dastan:People`; **a site
  that is told no file has no accounts** – no page, no form, no link, and it behaves exactly as before.
* **What an account is**: an identifier the store makes (it says nothing about the person and never changes), a
  name, what is kept of a password, when it was made, how often the password was wrong, and when it was closed.
  **No mail address, no real name, no date of birth, no address of a machine.** The name is free to choose
  (3 to 40 letters, digits, single spaces, dots, underscores, hyphens – of any script), belongs to one account
  whatever its case, and is what others will see.
* **The password** has at least 12 characters and no other rule. Only a value derived from it is kept
  (PBKDF2 with SHA-256, 600,000 rounds, a salt per password; `PasswordHash` in `Vafadar.Core`, now shared with
  the accounts of the editors). After five wrong passwords in a row an account takes none for fifteen minutes.
  A wrong name, a wrong password, a locked and a closed account are answered alike, and a wrong name takes as
  long as a wrong password.
* **A lost password**: with the account come eight **recovery codes**, shown once. Each sets a new password one
  time. Only a hash of a code is kept. New codes can be made with the password; the old ones are then worth
  nothing. Wrong codes count like wrong passwords. **Who loses the password and the codes has lost the
  account** – without mail there is no other way, and the page says so before the account is made.
* **A sign-in** is a cookie (`dastan.visitor.session`, HttpOnly, SameSite=Lax, Secure where the visitor came by
  https, thirty days, renewed while used) sealed with the keys of the site. With every request the store is
  asked whether it still holds: another password or a closed account ends every sign-in at once.
* **The forms** are plain forms sent to addresses of their own (`/preferences/account/…`) with the token every
  form of the site carries; each sends the visitor back to the page `/account`, which says what happened. A
  form leads back to that page only, whatever it names as its return. What an address says is turned into
  words of the page only when it is one of the answers the site gives. No script.
* **A password does not travel in the open**: the forms are refused unless the request came by https or from
  the machine itself, and the page then offers no form.
* **Against programs**: a field no person sees; at most five accounts an hour from one address and at most
  thirty tried passwords or codes an hour from one address over all accounts – counted in the memory of the
  process and forgotten with the hour, the address written nowhere. No picture puzzle and no third party.
* **Closing** needs the password, signs out everywhere at once and keeps the account for thirty days, in which
  its name and password open it again; then the first request of a day deletes it for good with everything
  that belongs to it (D-055). The name is free again afterwards.
* **Every page is still answered with "no-store"**, and the page of the account is not for search engines.

## Consequences

* The site, which kept nothing, now keeps a database on the server that has to be on a volume, backed up and
  protected like one. It is one file: **one container at a time** may use it.
* The header of a page shows the name of who signed in, so a page is no longer the same for everybody. The
  content of the public pages still depends on the reading position only.
* A sign-in survives a restart only where the keys of the site are kept (`Dastan:Keys`).
* Names of different scripts that look alike are different names; somebody can take a name that looks like
  another's. Nothing is built against that yet.
* Without mail nobody can be told about anything: an invitation will be a code or a link the sender passes on
  (step 16.2).

## Not built here

Signing in with Google or Microsoft (step 16.8: needs the site registered with each of them, which is the
owner's, and secrets the owner enters); what an account opens (steps 16.2 to 16.7); a copy of one's own data
(step 16.7); a second factor.

## Checked

`PasswordHashTests` (shared library), `VisitorAccountsTests` (the store: 29 cases), `VisitorAccountTests` (the
site: making an account, the codes shown once and unreadable in the cookie, signing in and out, both kinds of
wrong answered alike, another password ending other browsers, recovery, closing and opening again, what is
refused, the limits, where a form leads, forged sign-in). Looked at in a browser in English at 800 px and in
Persian at 375 px, dark appearance. **Not tried**: in a container, behind a proxy, with a password manager, with
a screen reader, in the light appearance, under load.
