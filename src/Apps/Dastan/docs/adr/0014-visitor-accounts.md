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

## First addendum (step 16.2): what follows a visitor, and invitations

* **Signed in, the reading position and the list are the account's**, kept in the store and the same on every
  device. The pages ask one place for both (`ReadingPreference.Read`, `SavedList.Read`), which asks the
  account of the request (`AccountState`) first and the cookies of the device otherwise. The projection and
  every rule about what a reading position opens are untouched: an account changes where the position is kept,
  never what it allows.
* **A device keeps its own.** While somebody is signed in, the cookies of the device are neither read nor
  changed; signed out, the device shows what it holds itself again. On a machine several people use, signing
  out leaves nothing of the account behind.
* **A new account begins with what its device holds** – the form says so. **Signing in takes nothing from a
  device**: who signs in on somebody else's machine does not carry that machine's list away. The page of the
  account offers to bring it in; then pages the list lacks are added and a reading position is taken only for a
  collection the account has none for. What the account says is never moved by a device – a position is a
  promise about what was read, and the cautious answer is the account's own.
* The list of an account holds 200 pages (a cookie holds 30). A page is kept only if its visitor can open it,
  and a page that cannot be opened any more is neither shown nor counted – as before.
* **An invitation** (D-049) is a code of twenty letters made by the store; its address is the page of the
  account with that code. The sender passes it on; the site sends nothing. It can be used for fourteen days,
  once, and taken back while nobody used it; an account has at most ten that can be used. Whether it can be
  used is decided by the rule of step 15.2 (`Invitation`), which the store brings to where it stands.
  **It makes an account and nothing else** (PRIV-006): no relationship, no claim; the sender sees that it was
  used, not by whom, and the new account sees nothing of the sender. Since anybody may make an account, an
  invitation that cannot be used stops nobody – the page says so and offers the form all the same.
* What belongs to an account – positions, list, invitations – is deleted with it. That an invitation was used
  stays when the account that used it is gone.

Checked: `BelongingsAndInvitationsTests` (the store: 16 cases), `AccountFollowsTests` (the site: 19 cases –
two devices of one account, a later stage opened on another device only through the account, signing out, a new
account, bringing in, the list, invitations made, used, taken back, limited). Looked at in a browser signed in,
in English and Persian at 375 px, dark appearance. Not tried: two real devices; a container.

## Second addendum (step 16.3): relationships two accounts show together, and circles

* **The rules are those of step 15.2** ([ADR 0013](0013-private-space-rules.md)). The store keeps where a
  relationship stands and brings a `SocialRelationship` to that place before every act and every question of
  who may see it; no rule is written a second time in the store or in a page.
* **Asking** names the other account by its name and one of six kinds (friend, close friend, family, partner,
  colleague, classmate – a proposal; the owner may change the list). Two accounts have one request or
  relationship at a time. The site sends nothing: who is asked finds "1 waiting" in the header of every page.
  That a name is no account is said – a name is public already, the form to make an account says when it is
  taken – but an address asks only thirty times an hour.
* **Only the side that was asked answers** (PRIV-005). Declined, the request is kept for thirty days so that
  the same question is not asked again at once, then forgotten. Taken back or ended, nothing of it is kept.
* **Who sees it**: each side chooses for its own side – nobody else, one of its circles, everybody – and
  begins with nobody else. A relationship is shown to who **both** allow (PRIV-008); until then to the two
  only. The page about an account (`/people/{id}`, the identifier the store made, which says nothing) shows
  the name and what this viewer may see; what they may not see is neither counted nor hinted at (PRIV-012) –
  the page of somebody who shows nothing and of somebody who shows nothing to this viewer are the same. It is
  not for search engines.
* **A circle** is a name its owner gives to some of the people they have an accepted relationship with, at
  most twenty of them. Nobody is told they are in one and only the owner sees its name. Somebody the owner
  has no relationship with does not come into a circle – a circle is no way to probe for accounts. Removing a
  circle narrows what was shown to it to the two sides; ending a relationship takes each side out of the
  circles of the other. A circle that is gone never widens anything: the scope falls back to the owner alone.
* A closed account shows nothing and is shown nowhere at once; deleted, its relationships and circles go
  with it.

Not built: blocking an account; telling somebody outside the site; relationships in a chart; the kinds as
content the editors or the owner can change without a release.

Checked: `RelationshipsAndCirclesTests` (the store: 23 cases), `PeoplePagesTests` (the site: 25 cases,
with up to four accounts – both sides' audiences, a circle, what a third account and a visitor without one
see, every act tried by the wrong account). Looked at in a browser with two accounts, English and Persian at
375 px, dark appearance. Not tried: a screen reader, the light appearance, a container.

## Third addendum (step 16.4, first part): family trees

* **A tree has an owner** (D-054): the account that made it. The owner names accounts that may see it and
  accounts that may change it – among the people the owner shows an accepted relationship with, for the same
  reason as with circles – and only the owner names others, renames the tree or removes it. Who may change it
  adds, changes and removes records and links. **A tree that is not shared with somebody does not exist for
  them**: its page answers "not found" whether they are signed in or not. Nothing of a tree is public; making
  something public is step 16.6.
* **A record of a person needs no account** (PRIV-002) and says little on purpose (PRIV-003): a name and,
  when they are known, the year of birth and of death, and whether the person has died. No day, no place, no
  note, no picture. A tree holds 500 records; an account owns 20 trees.
* **Links** are "a parent of" and "the partner of". The same link is not made twice, a record is not linked
  to itself, and nobody becomes their own ancestor.
* **Every change is kept with who made it** (D-054): what was done, what it was done to as it was named then,
  the account and the time. The history is shown to everybody who may see the tree. When an account is
  deleted, the changes stay and say "an account that is deleted" – the name goes with the account (D-055).
* When the relationship between an owner and somebody they named ends, that account sees the tree no more.
  A tree goes with its owner: closed, nobody sees it; deleted, it is deleted. **A tree with several people
  who work on it still belongs to one account** – handing a tree on to another owner is not built.

Not built yet: the claim of an account on a record (D-052); the drawing of a tree (the family chart of
[ADR 0008](0008-charts.md) draws entities of a collection; a tree is shown as a list of its people with their
parents, partners and children); children and guardians (PRIV-003 needs a rule of the owner's); handing a
tree on.

Checked: `FamilyTreesTests` (the store: 19 cases), `FamilyTreePagesTests` (the site: 28 cases, with up to
four accounts). Looked at in a browser in English and Persian at 375 px, dark appearance; two things were
found there and corrected – a row with a box to tick that looked like an input, and names of another script
that turned the words of the history around. Not tried: a tree of several hundred people in a browser, a
screen reader, the light appearance, a container.

## Fourth addendum (step 16.4, second part): "this record is me"

* An account that may see a tree can say of one record that it is this account (D-052, PRIV-004). An account
  is at most one record of a tree and has at most one claim waiting there.
* **Who answers**: the account that made the record, as long as it is open and may still see the tree;
  otherwise the owner of the tree. **Never the claimant** – that is the rule of step 15.2
  (`IdentityClaim`), which the store asks. So a record somebody made themselves in their own tree cannot be
  claimed by them: there is nobody else to confirm it, and the page says so.
* A claim is shown to who made it and to who is to answer it – not to the others who see the tree. The page
  of who answers says that the same name is not proof.
* **Confirmed, the record names the account, and that is all**: nothing is merged, no role changes, nobody
  sees more than before. Refused, nothing is kept but the line in the history.
* **Only the account takes it back** – its waiting claim, or that a record is it – at any time; not the
  owner and not who made the record. An account that may not see the tree any more, was closed or deleted, is
  no record of it and claims none.
* A claim, its answer and its taking back are in the history of the tree.

Checked: `IdentityClaimsTests` (the store: 10 cases), three more cases of `FamilyTreePagesTests` (the site).
**Not looked at in a browser.**

## Fifth addendum (step 16.7): taking back, closing, and a copy of one's own data

* **Taking back holds at once** (D-055), and each thing is taken back by the account it belongs to: a
  relationship is ended by either side; who may see it is narrowed by a side for its own side; a claim, or
  that a record is the account, by that account only; a place in a tree by the owner or by ending the
  relationship. None of these waits for anybody, and none leaves the thing visible meanwhile.
* **A closed account** signs nobody in, shows nothing and is shown nowhere from that moment – its page, its
  relationships, its trees, its records in the trees of others – and is deleted for good thirty days later
  with everything that belongs to it. What others made stays: a record of a tree names nobody then, a change
  of a tree says "an account that is deleted". Until then the name and the password open the account again
  with what it had.
* **A copy of one's own data**: the page of the account hands over one file (`dastan-account.json`, JSON,
  written to be read) for the password – it is everything at once. It holds the name and when the account was
  made, how many recovery codes are left, the reading positions, the list, the invitations, the relationships
  with who they are shown to, the circles, and the trees the account owns with their people, links, who they
  are shared with and their history. A tree of somebody else is named with its owner and the role, and with
  the record that is this account – it is not copied, it is theirs. **Not in it**: the password or anything
  derived from it, the recovery codes, any identifier the store made, and anything about an account the
  visitor has nothing to do with. Other accounts stand in it by their names.
* The form of the file has a version (`PeopleStore.ExportVersion`); bringing such a file back in is not built.

Checked: `OwnDataTests` (the store: 5 cases, among them that no secret and no identifier is in the copy),
two more cases of `VisitorAccountTests` (the site). The form was not looked at in a browser.
