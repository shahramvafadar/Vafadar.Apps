# 0024. The frame of the site and the parts every page is made of

* Status: Accepted for the structure; the palette stays **provisional** as in [0004](0004-design-tokens-and-fonts.md)
* Date: 2026-11-21
* Step: 20.5 of the [delivery plan](../delivery-plan.md)

## Context

Until phase 20 each page was laid out when it was built. The head of a page showed the name, "Discover", a
list of the visitor when it was not empty, the account, the two languages and the three choices of the
appearance – nine things in one row, of which six are settings. The same choice of an audience was written
twice, in two pages. A place of a page that had nothing to show said so in a sentence of its own each time.

The owner's sketches of November 2026 (Discover, the explorer, the profile of a node; a wide window and a
phone each) show one frame and a small number of parts that return on every page: a light, warm ground,
readable text, one accent colour, room for pictures, and details only when they are asked for. They are a
direction, not a design to copy: their pictures are made up, and the wordmark stays the plain name. (It did
until 2026-11-21: the owner then chose a logo – [ADR 0026](0026-logo.md).)

## Decision

**The frame.** Every page has the same head:

* the name of the site, which leads to the start;
* the **main ways** as one list: Discover, My World (the private space of an account – only where the site
  has accounts), Saved, Search. The way a page lies on is marked (`aria-current`) by its ground and colour.
  Saved has its place whether or not something is in it: an empty place says how it is filled;
* the account: the name of who signed in, or "Sign in";
* **the language and the appearance together**, behind one control (`details`) that needs no script. They
  are settings, not ways, and stand nowhere else on the page.

The list of the ways is one element of the page. In a wide window it stands beside the name; on a phone (up
to 40 rem) it is a bar at the foot of the window, where a thumb reaches it, and the page ends above it.
Whatever is as high as the window – the stage of a chart – is as high as what is left above the bar
(`--bar-space`). How many requests wait is shown on the way that leads to them, as a number for the eye and
as words for a screen reader.

**Tokens.** The tokens of ADR 0004 stay and keep their values. Added, each with one meaning:

| Token | Meaning |
|---|---|
| `--surface-sunken` | A ground that lies lower than the page: a band, the place of a picture that is not there, a state |
| `--accent-soft` | The ground of what is chosen: the way, the view, a filter that is set |
| `--danger`, `--ok` | What went wrong; what is in order |
| `--shadow-1`, `--shadow-2` | A card that lies on the page; a panel that lies above it |
| `--radius-s`, `--radius-l`, `--radius-full` | Corners of small things, of large ones, of a chip |
| `--step--2` | The smallest text: the words under a drawing in the bar |
| `--measure-wide` | The width of a page that shows things side by side (72 rem) |
| `--target` | The smallest height of what a finger presses (44 px) |
| `--bar`, `--bar-space` | The height of the bar of the ways, and the room it takes: none in a wide window |

A visitor who asks the device for more contrast gets lines and secondary text as strong as the text
(`prefers-contrast: more`). Dark values stand twice, as before.

**Parts, each made once** (`Components/Shared`):

| Part | What it is |
|---|---|
| `IdentityHead` | The picture or the drawing of the kind, the kind, the name as the title, one line, the facts of a card, and what can be done |
| `NodeCard` (and `Tall`) | A card of a node; `Tall` puts the picture above the words for a grid that lives from pictures |
| `ProfileFactList` | The rows of facts – stated, or said to be unknown |
| `ViewBar` | Where the visitor is, and the views of the same thing (connections, timeline, pictures) as links |
| `Chip` | A filter as a small link; set or not is said by `aria-current` and a mark, not by colour alone |
| `AudienceChoice` | Nobody else, a circle, everybody – the one choice of an audience |
| `RelationshipRequest` | A request that waits: who asks, what for, the two answers |
| `StateNote` | Something goes on, nothing is here, something went wrong – in words, with what to do next |
| `Icon` | A small drawing beside words |
| `IdentityMark` | What kind of somebody or something a profile is about: of a story, of the world, an account, a record in a tree (step 20.9, ADR 0017) |
| the panel of a profile | The inspector (ADR 0019), unchanged |

**Drawings.** The icons are Dastan's own: lines on a grid of 24, drawn for this site in `Icon.razor`. They
were not taken from a set of somebody else, so no licence of a third party applies and nothing has to be
attributed; like the rest of the repository they are the owner's. They stand in the page (`svg`), take the
colour of the words beside them, and are decoration: the words say what a control does.

**Where no picture is,** the drawing of the kind stands in its place on the lower ground
(`ProfilePicture`, ADR 0017). It is never a photograph or an illustration that pretends to show the thing.

**A page of the parts** (`/parts`) shows every part side by side, in the language and the appearance of the
visitor. It exists on a machine of development only; a site that runs for anybody answers "not found".
Pages carry no style of their own – the policy of the site forbids it – so each token has a class there.

## Consequences

* A page built from here on is put together from these parts; a page that needs something new adds a part.
  The pages that exist are brought to them in steps 20.7 to 20.11.
* Two tests said that the list of a visitor is not offered while it is empty; they now say the opposite.
* Without script the control of language and appearance stays open until it is pressed again. With the
  script of the site it closes when the visitor presses elsewhere or the Escape key (`frame.ts`).

## Not decided here

* The palette, the Latin font and a logo: the owner's, as ADR 0004 says.
* A page of its own for the private space ("My World"): step 20.11. Until then the way leads to the people
  of the account, and to the page of the account for who has not signed in.

## What is an account's own (step 20.11)

* **My World** (`/account/world`) is where the way "My World" of the head leads who signed in. It decides
  nothing and has no form: what waits for an answer – a request for a relationship, a tag on a photograph –
  is named first and leads to the page where it is answered; then the spaces stand as cards with how much each
  holds, or that it holds nothing yet, said as that (`StateNote`). It carries the mark of an account
  (`IdentityMark`).
* **`AccountBar`** is the one list of the spaces – My World, people, where one was, family trees, photographs,
  the account – on each of their pages, the one shown marked. A site that keeps no photographs offers no way to
  them: nothing is offered for what does not exist, and the site has no posts and no videos of visitors to offer.
* **The page of a family tree is three tasks**: seeing (the drawing, the people, every change), changing (adding
  somebody, the links, the name of the tree and its removal) and sharing (who may see or change it, making it
  public, the agreement that is asked, records somebody says are them, looking as somebody else). Each is a
  part of the page under a heading of its own, with a way to it at the top; a part somebody may not use is not
  in their page, and neither is the way to it. What waits for an answer of this visitor is said first.
* **Five answers stay five.** Accepting a relationship, being found by a place, showing its dates, agreeing to a
  tag and making a tree public are asked and answered apart, each where it belongs; none is on My World, and
  none follows from another. An invitation makes an account and nothing else. These rules are those of
  ADR 0014 and are shown by the tests named there (`PeoplePagesTests`, `AffiliationPagesTests`,
  `PhotoPagesTests`, `FamilyTreePagesTests`, the tests of invitations); this step moved no rule.

**Added on 2026-11-21** (section 23 of the delivery plan): My World names what a family tree asks of the
account too – an agreement to be named in public that is asked and not given, and a claim on a record that
this account is the one to confirm or refuse –, each leading to the part of the tree that answers it
(`PeopleStore.TreesWaiting`). It says nothing of a tree the account may not see, and nothing to who made a claim
or to the owner about an agreement that is somebody else's. The note at the top of a tree no longer says
"something waits" to somebody who has agreed already.

**Not built** (*decided by the owner and built on 2026-11-23 – see "The head of an account and of a record in
a tree" below*)**:** the head of a page of an account and of a record in a tree is the mark above the name, not
`IdentityHead` – that part is fed by the profile of a node of a collection (ADR 0017), which neither has; a
profile of that shape for people of accounts is not made up here. The pages of the people, of where one was and
of the photographs keep their bodies as they were and gained the bar; they were not put together anew.

## The head of an account and of a record in a tree (2026-11-23, D-086)

The owner chose a head for both, and a whole one: "what we build is built whole".

1. **One part for both: `PersonHead`.** It is the head of an identity as a name of a collection has it – the
   picture or a drawing, the mark that says what kind of somebody this is, the name as the title of the page,
   where it belongs, a line about it, a few facts, what can be done – and it is fed by what the store of the
   visitors lets this viewer see, not by the profile of a node. `IdentityHead` stays the head of a name of a
   collection; the two share their classes and so their look.
2. **An account says about itself what it chooses** (`PeopleStore.DescribeAccount`): a few words – at most
   300 characters, a lead under a name and not a life – with an audience of their own (nobody else, a circle,
   everybody), and a picture. Only the account writes them, on its own page.
3. **The picture has no audience of its own.** It is one of the account's own photographs and is shown to who
   may see that photograph. So a photograph cannot be seen as a picture by somebody who may not see it as a
   photograph, and there is one place where its audience is chosen. A photograph that is removed is the
   picture of nobody; that holds for the picture of a record of a tree too, which kept a name of nothing before.
4. **Nothing is hinted at** (PRIV-012). The words are read where their scope allows, asked before anything is
   made of them. The head of an account that says nothing and of one that says nothing to this viewer are the
   same: the drawing of a person and the name. Looking at one's own page as somebody else draws the head by
   the same scopes. A circle that is deleted takes the words back to the account alone. A copy of one's data
   holds the words, their audience and the picture.
5. **The head of an account names the first three places** the viewer may see, with the way to the rest; it
   counts nothing.
6. **The head of a record of a tree** shows the picture this viewer may see or the drawing, the name, the tree,
   when the person was born and died in two lines (`PersonLife`), and – where the record is an account – the
   way to it. The profile below is the facts and does not show the picture a second time. The public page of
   a person of a public tree has the same head.

The table of the visitors gained four columns by a change of form written by hand
(`20261123130000_AccountDescription`); `AccountDescriptionTests` asks the database whether the model has a
change no change of form carries.

Not built: a date of birth, a place of living or any other fact of an account – none is asked for, and each
would be a decision about what the site keeps of a person.

Tests: `AccountDescriptionTests`, `AccountProfilePagesTests`, `TreePersonPagesTests`.
