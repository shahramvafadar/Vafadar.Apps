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

## Sixth addendum (step 16.4, third part): the tree is drawn

The page of a tree draws it above the list, with the layout and the drawing of the family of a collection
([ADR 0008](0008-charts.md), ninth addendum): parents above their children, partners side by side, a name
leading to its record. "This is me" is offered only on a record somebody else can confirm – not on a record
its viewer made in a tree they own, where it could only be refused.

## Seventh addendum (step 16.5): photographs of visitors

* **The files lie in a folder of their own** (`Dastan:Photos`), on a volume no web server shows; the store
  keeps what a photograph is and the name of its file. Without the folder the site has no photographs – no
  page, no form, no address.
* **Bringing one in** goes through the intake of the editors' files (`MediaIntake`, step 13.7): what the file
  is is read from its content, not from its name or from what the browser says; only PNG, JPEG and WebP images
  are kept, of at most 10 MB; an SVG is not. **What an image says about where and with which device it was
  made is taken out before it is kept.** The name of the file is made by the site from the identifier of the
  photograph. A file that is not kept leaves nothing behind. An account holds 200 photographs.
* **Every way to a photograph asks its audience** through the rule of step 15.2 (`SharedMedia`): the page of
  its owner, the life of somebody it shows, the list of recent shares – and **the address of the file itself**
  (`/photo/{id}`), which answers "not found", without a page, for a photograph that does not exist and for one
  the viewer may not see alike. A change of the audience holds at once, for all of them. The audience is the
  owner alone (as it begins), a circle of the owner, or everybody.
* **A tag** says that a photograph shows somebody: the owner, or an account the owner shows a relationship
  with – a tag is no way to reach a stranger. The person is asked on their own page and approves or says that
  it is not them; only that account answers, and only for a photograph it may see. **An approved tag shows the
  photograph to nobody who could not see it** (PRIV-013): it then stands among the photographs of that person
  for those the *owner* lets see it. A tag the person took away stays taken away – the owner cannot put it on
  again, the person can change their mind.
* **The life of a person** on their page is their own photographs and those of others they approved to be
  shown in, each asked for this viewer. **Recent shares** are what the people a visitor shows a relationship
  with brought in during the last thirty days and let them see – by the day it was brought in, whenever it was
  taken; a stranger's public photograph is not "shared with" anybody.
* **Removing** deletes the file. A closed account shows no photograph and is shown in none; when it is deleted
  its photographs go with it, and a file no photograph names any more is deleted by the first request of a day
  (a file that only just arrived is left alone). The copy of one's own data names the photographs with their
  captions, audiences and tags; the pictures themselves are fetched at their addresses.

Not built: videos of visitors; smaller renditions – a page loads the picture as it was brought in; albums; a
gallery by the time a picture was taken (the day is kept and shown); telling somebody outside the site that
they were tagged.

Checked: `PhotographsTests` (the store: 15 cases), `PhotoPagesTests` (the site: 24 cases – among them that
the kept file no longer says who made it, that the address of a file refuses a friend who is not in the
audience, and that a tag does not open a photograph to a friend of the tagged person). Looked at in a browser
with two made-up pictures, English and Persian at 375 px, dark appearance. Not tried: a real photograph of a
camera or a phone, a large number of photographs, a container, a screen reader.

## Eighth addendum (step 16.6): making a family tree public

* **Making public is an act of its own** (PRIV-014), of the owner of the tree, in two steps: asking, and –
  when nobody stands in the way – making it public. Sharing a tree with accounts never makes it public.
* **The check before it** is the rule of step 15.2 (`PublicationCheck`): the store tells it, for every record,
  whether that person may be named in public – somebody who has died, or somebody whose account agreed – and
  shows the owner who stands in the way and why.
* **Everybody living the tree names agrees with their own account** (D-053). A record is somebody's account
  only after that account said so and the maker of the record confirmed it (D-052), so nobody agrees for
  anybody else. **Somebody living who is no account cannot agree, and a tree that names such a person cannot
  be public** – the owner is told so, and can only leave that person out or wait until they join. A record is
  taken to be of a living person unless it says that the person has died.
* **What everybody then sees** (`/trees/{id}`): the name of the tree, the drawing, the people with their years
  and how they belong together. Not the owner, not who it is shared with, not its history, not which account
  a record is, no form. The page asks search engines to stay away.
* **Whether the tree is shown is asked with every request**: it is shown while the owner made it public *and*
  nobody stands in the way. So it is hidden at once (D-055) when somebody takes their agreement back, stops
  being a record of the tree, loses access or closes their account, when a living person without agreement is
  added, or when a record somebody agreed to is changed – that person is asked again – and it is shown again
  when that is settled. The owner sees on the page of the tree that it is hidden and who it waits for.
* The owner takes it back at any time; what was agreed is then forgotten, and asking again asks everybody
  again. Asking, agreeing, taking back and making public are in the history of the tree.

**Open, for the owner to decide** (the legal review is the owner's, D-053):

* A **photograph** its owner shows to everybody is not held back by the people in it. Somebody it is said to
  show can take their tag away, which takes it out of their own page – not out of the owner's. D-053 read
  strictly would ask their agreement before "everybody"; people in a picture who are not tagged cannot be
  known at all. A **relationship** is public only when both sides say so, which is the consent of both.
* Whether a public tree may be **found by search engines**. Until that is decided it is public for who has
  its address.
* **Children** (PRIV-003): no rule yet; a record of a child is a record of a living person without an
  account and so keeps a tree from being public.

Checked: `MakingPublicTests` (the store: 13 cases), five more cases of `FamilyTreePagesTests` (the site).
**Not looked at in a browser.**

## Ninth addendum (2026-11-18): signing in through Google or Microsoft (step 16.8)

D-048 names two ways into an account: a name with a password, and Google or Microsoft. The second is built so
that it adds nothing to what the site knows about a person.

* **What is asked of the provider**: who it is, and nothing else. Google is asked for the scope `openid` only.
  Microsoft tells who somebody is only together with the right to read their profile; of what it answers the
  identifier is taken and everything else is dropped before a claim is made of it. No name, no mail address and
  no picture is asked for, shown or kept, and no token of the provider is kept.
* **What is kept**: the provider and a SHA-256 hash of "provider, identifier" (`ExternalLogins`). The hash finds
  the account again; who reads the store cannot tell which account of the provider it is. The copy of one's own
  data names the providers, not the hash.
* **The name is the visitor's**: a sign-in no account belongs to leads to a form on which the visitor chooses
  the name of the account, under the rules of every name. What the provider said waits for that form in a
  cookie only the site can read, for ten minutes. Such an account has no password and gets recovery codes like
  every account.
* **A way in is kept**: an account adds a provider while signed in and removes it again; the only way in – one
  provider and no password – is not removed. An account without a password sets one without being asked for a
  present one; what asks for the password (new recovery codes, the copy of one's data, closing) waits until
  then, and the page says so.
* **One account per sign-in**: a sign-in belongs to one account and an account has one sign-in of a provider.
  The sign-in of a closed account makes no second account; it is free again when the account is deleted.
* **Offered only where set up**: a provider appears on the page when the owner entered its client
  (`Dastan:SignIn:Google:ClientId` and `ClientSecret`, the same for `Microsoft`). Without them the site is as
  before and has neither the addresses nor the forms. The secret comes from the environment of the container and
  is never written into the repository, a command or a log.
* **Limits**: an account made through a provider counts against the limit of new accounts like every other, and
  an invitation is used up by it too.

Considered and not done: taking the name or the mail address from the provider (it would bring what the site
promises not to keep); joining a sign-in to an account "with the same mail address" (the site has none);
signing in through a provider as a second step of a password.

Checked: `ExternalSignInTests` (the store: 11 cases), `ExternalSignInPagesTests` (the site: 11 cases, with a
made-up provider that answers at once, and for Google and Microsoft up to the address the visitor is sent to).
The page was looked at in a browser with made-up clients (fa, dark): the buttons signed out and signed in. The
form that chooses a name was not looked at in a browser. **Nothing was tried against Google or Microsoft**: that
needs the site registered with each of them (the addresses to give them are `/signin-google` and
`/signin-microsoft` of the site), which is the owner's, and it is the first thing to do before anybody relies
on it.

## Tenth addendum (2026-11-18): a photograph is shown to everybody with the agreement of who it names (step 16.9)

Step 16.5 let the owner of a photograph choose "everybody" alone; the people it names could take their tag
away, not the picture. D-053 says that something private about living people is public only with the agreement
of everybody it names. It is read as written, for photographs as for family trees.

* **Who is named**: an account the owner tagged, as long as the person did not take the tag away. The owner's
  own tag asks nobody. A tag that waits for its answer names the person already – the photograph is held back
  from the moment its owner says who is in it.
* **Held back**: the owner chose "everybody", and somebody the photograph names has not agreed. It is then seen
  by its owner and the people it names (they are asked, so they see what they are asked about) and by nobody
  else – every way to it, the address of the file too. The owner is told that it is held back and who is waited
  for.
* **Agreeing is not approving the tag**: "this is me" and "everybody may see it" are two answers. A tag can be
  approved and the photograph stay among the people its owner knows.
* **Taken back at once**: an agreement that is taken back hides the photograph with the next request. One that
  was given to a showing to everybody is not kept for the next: an owner who makes the photograph private and
  public again asks again. Nothing is agreed ahead.
* **Somebody named later**: a person who is tagged on a photograph everybody saw holds it back until they
  agree.
* **A closed account** that did not agree holds the photograph back until it is deleted; with it its tag goes.

What this does not do: a person a photograph shows and its owner did not name is asked by nothing – the site
does not know who is in a picture. That stays with the owner of the picture, and with the terms of a public
site (the legal review, which is the owner's).

Checked: `PhotoAgreementTests` (the store: 9 cases), one more case of `PhotoPagesTests` and the form among
those refused to who is not signed in. **Not looked at in a browser.**

## Eleventh addendum (2026-11-18): albums and a gallery by time (step 16.10)

R-MED-07 asks for curated albums and for galleries over the same items; the media rules say that the
visibility of an album does not override the permission of an item, and that taking an item out of an album is
not withdrawing it.

* **An album** is a name, a note and photographs of its owner in the order they were put there
  (`Albums`, `AlbumPhotos`). A photograph stands in as many albums as its owner likes; the file is kept once.
* **No audience of its own**: nothing is chosen for an album. Each photograph in it is seen by who its owner
  chose for that photograph – a photograph that is held back (step 16.9) is held back in an album too.
* **An album tells nothing by being there**: a viewer is given the albums in which they may see at least one
  photograph, each with those photographs only. An album of which they may see nothing is not named to them –
  a name such as "The wedding" says something. The owner alone sees empty albums, where albums are changed.
* **Taking out is not removing**: out of an album a photograph stays among its owner's and as visible as it
  was; removing an album removes no photograph. A photograph that is removed leaves its albums; an account
  that is deleted takes its albums with it.
* **A gallery by time**: above the photographs of an account and of the life of a person stand the years in
  which they were taken, each a way to the photographs of that year. The years are those of the photographs
  the viewer was given, so a year tells nothing about a photograph the viewer may not see. A photograph that
  does not say when it was taken is among all, not in a year.
* **The copy of one's own data** names the albums and the files in them.

Not built: an order the owner changes afterwards (a photograph is taken out and put in again), an album of
several owners, an album of the editors' media – none is named for visitors by a requirement that is decided.

Checked: `AlbumsTests` (the store: 13 cases), two more cases of `PhotoPagesTests` and the five forms among
those refused to who is not signed in. Looked at in a browser: the albums on the page of the photographs (fa)
and on the page of a person (en), 375 px, dark. The years were not seen there.

## Twelfth addendum (2026-11-18): the owner of a photograph says that the people in it agree (step 16.11)

The tenth addendum asks the people a photograph names. People it shows and does not name can be asked by
nothing: the site does not know who is in a picture. The owner was given four ways on 2026-11-18 (a statement
of the owner of the picture, a form to object, both, nothing but the terms) with the first recommended for
now, and had not answered when this was built. It is built because it takes nothing away that cannot be given
back: one condition in two forms.

* Whoever brings a photograph in for everybody, or changes one to everybody, ticks that the people in it
  agree. Without the tick nothing is kept or changed, and the page says why. For the owner alone or a circle
  nothing is asked.
* The statement is asked for by the form and **is not kept**: the store does not record that it was made. It
  is a promise, not a proof – a site with open registration needs the way to object as well, and with it
  somebody who looks at objections. That stays the owner's.

Checked: one more case of `PhotoPagesTests`. The refusal was looked at in a browser (fa, 375 px, dark).

## Thirteenth addendum (2026-11-18): who makes an account (step 17.1; D-061, D-063, D-068)

* **16 and older** (D-061): both forms that make an account – with a password, and after a sign-in through a
  provider – ask for the tick "I am 16 or older". Without it no account is made. The site asks and believes:
  it keeps no day of birth and checks no document. A record of a child may stand in a private family tree; it
  is a record of a living person without an account, and so keeps the tree from being public (eighth
  addendum). There are no accounts of children with a guardian.
* **By invitation only** (D-068): with `Dastan:InvitationOnly` a new account needs an invitation that can be
  used. The page shows the form to who came by one and says "by invitation only" to everybody else; a form
  that is sent all the same makes nothing. Who has an account signs in as ever and invites as ever. **The
  first account is made before the setting is set** – there is nobody to invite it.
* **Another kind** (D-063): the kinds of relationship are the fixed list with "other" added. A kind says
  nothing about who sees a relationship.

Checked: `ClosedTrialTests` (8 cases). Not looked at in a browser.
