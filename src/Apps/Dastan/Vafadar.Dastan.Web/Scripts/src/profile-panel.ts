// The panel of a profile (ADR 0017, ADR 0019): where a list shows a node as a card, the card offers a look at
// the profile without leaving the list. The panel is filled from the address of the profile – the page of the
// node and "/profile.json" – which answers with what the page of the same visitor shows, in words; nothing is
// interpreted here and nothing is hidden here. Without this script the offer is not shown and the name of the
// card leads to the page, as before.

interface Words {
  text: string;
  language?: string;
  address?: string;
  unknown?: boolean;
}

interface Profile {
  name: string;
  nameLanguage: string | null;
  kind: string | null;
  address: string;
  summary: Words | null;
  picture: { source: string; sizes: string | null; words: string } | null;
  noPicture: string;
  facts: { label: string; values: Words[] }[];
  note: string | null;
}

// An address of the site as a page writes it, from the language of the page on.
function here(address: string): string {
  return new URL(address, document.baseURI).href;
}

// Words in their own language are said to be in it, as a page says it.
function inLanguage(element: HTMLElement, language: string | null | undefined): void {
  if (language) {
    element.lang = language;
    element.dir = 'auto';
  }
}

function written(words: Words): HTMLElement {
  const holder = document.createElement('bdi');
  inLanguage(holder, words.language);
  if (words.address) {
    const link = document.createElement('a');
    link.href = here(words.address);
    link.textContent = words.text;
    holder.append(link);
  } else {
    holder.textContent = words.text;
  }

  return holder;
}

function drawn(profile: Profile): HTMLElement[] {
  const parts: HTMLElement[] = [];
  if (profile.picture) {
    const frame = document.createElement('p');
    frame.className = 'profile-picture';
    const image = document.createElement('img');
    image.src = profile.picture.source;
    if (profile.picture.sizes) {
      image.srcset = profile.picture.sizes;
      image.sizes = '14rem';
    }

    image.alt = profile.picture.words;
    frame.append(image);
    parts.push(frame);
  } else {
    const none = document.createElement('p');
    none.className = 'meta';
    none.textContent = profile.noPicture;
    parts.push(none);
  }

  if (profile.summary) {
    const summary = document.createElement('p');
    summary.className = 'person-about';
    summary.append(written(profile.summary));
    parts.push(summary);
  }

  const facts = document.createElement('dl');
  facts.className = 'facts profile-facts';
  for (const fact of profile.facts) {
    const label = document.createElement('dt');
    label.textContent = fact.label;
    facts.append(label);
    for (const value of fact.values) {
      const stated = document.createElement('dd');
      if (value.unknown) {
        stated.className = 'unknown';
      }

      stated.append(written(value));
      facts.append(stated);
    }
  }

  parts.push(facts);
  if (profile.note) {
    const note = document.createElement('p');
    note.className = 'note';
    note.textContent = profile.note;
    parts.push(note);
  }

  return parts;
}

/** Shows the offers of the cards and opens the panel when one is taken. */
export function offerProfiles(): void {
  const panel = document.querySelector<HTMLDialogElement>('#profile-panel');
  const offers = document.querySelectorAll<HTMLElement>('[data-profile-peek]');
  if (!panel || typeof panel.showModal !== 'function' || offers.length === 0) {
    return;
  }

  const name = panel.querySelector<HTMLElement>('#profile-panel-name');
  const kind = panel.querySelector<HTMLElement>('[data-panel-kind]');
  const body = panel.querySelector<HTMLElement>('[data-panel-body]');
  const wait = panel.querySelector<HTMLElement>('[data-panel-wait]');
  const failed = panel.querySelector<HTMLElement>('[data-panel-failed]');
  const open = panel.querySelector<HTMLAnchorElement>('[data-panel-open]');
  if (!name || !kind || !body || !wait || !failed || !open) {
    return;
  }

  // Said aloud when shown: the panel was opened by the reader, who waits for it.
  wait.setAttribute('role', 'status');
  failed.setAttribute('role', 'alert');

  let asked: AbortController | null = null;
  panel.addEventListener('close', () => asked?.abort());

  // A press beside the panel closes it, like the key for it does.
  panel.addEventListener('click', event => {
    if (event.target === panel) {
      panel.close();
    }
  });

  const show = async (button: HTMLButtonElement): Promise<void> => {
    const address = button.dataset['profile'];
    if (!address) {
      return;
    }

    asked?.abort();
    asked = new AbortController();
    name.textContent = button.dataset['name'] ?? '';
    name.removeAttribute('lang');
    kind.textContent = '';
    body.replaceChildren();
    wait.hidden = false;
    failed.hidden = true;
    open.href = here(address.slice(0, address.lastIndexOf('/')));
    panel.showModal();
    try {
      const answer = await fetch(here(address), { headers: { Accept: 'application/json' }, cache: 'no-store', signal: asked.signal });
      if (!answer.ok) {
        throw new Error(String(answer.status));
      }

      const profile = await answer.json() as Profile;
      name.textContent = profile.name;
      inLanguage(name, profile.nameLanguage);
      kind.textContent = profile.kind ?? '';
      open.href = here(profile.address);
      body.replaceChildren(...drawn(profile));
      wait.hidden = true;
    } catch (problem) {
      if (!(problem instanceof DOMException && problem.name === 'AbortError')) {
        wait.hidden = true;
        failed.hidden = false;
      }
    }
  };

  for (const offer of offers) {
    const button = offer.querySelector<HTMLButtonElement>('button[data-profile]');
    if (button) {
      offer.hidden = false;
      button.addEventListener('click', () => void show(button));
    }
  }
}
