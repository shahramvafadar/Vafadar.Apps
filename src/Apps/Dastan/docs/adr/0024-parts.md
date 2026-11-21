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
direction, not a design to copy: their pictures are made up, and the wordmark stays the plain name.

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
