// The state of exploring in the browser (step 20.6, ADR 0025). The server reads the state from the address of
// a page and writes it into the links of the views. A drawing that moves changes a part of it without asking
// the server – what is chosen, the stretch of years –; then the address of the page and the links of the views
// are brought in step here, so that a change of the view, the Back button and an address that is passed on
// all carry what the visitor sees. Only names the address has on every page are kept in step this way.
//
// A page without the bar of exploring – a tree of a visitor, a comparison – explores no public thing: nothing
// of it is written into an address by this.

/** Whether the page shows the bar of exploring: only then a drawing writes what is chosen into the address. */
export function explores(): boolean {
  return document.querySelector('nav.view-bar') !== null;
}

/** What the address says under a name, or nothing. */
export function said(name: string): string | null {
  return new URLSearchParams(location.search).get(name);
}

/** Writes a part of the state into the address of the page – or takes it out – and into the links of the views. */
export function carry(name: string, value: string | null): void {
  if (!explores()) {
    return;
  }

  const address = new URL(location.href);
  if (value === null) {
    address.searchParams.delete(name);
  } else {
    address.searchParams.set(name, value);
  }

  history.replaceState(history.state, '', address);
  inStep([name]);
}

// The name under which the address says that the visitor chose the still drawing of a page. The tools of a still
// drawing – across or down, closer or wider, earlier or later – are links that ask the server for the page again;
// without this name the script would draw the moving drawing again over the choice the visitor made.
const stillName = 'still';

/** Whether the address says that the still drawing of the page was chosen. */
export function stillChosen(): boolean {
  return said(stillName) === '1';
}

/**
 * Keeps the choice between the still and the moving drawing: in the address of the page, and in the links and
 * forms of the still drawing that ask for the same page again, so that its tools keep the still drawing.
 */
export function keepStill(still: HTMLElement, chosen: boolean): void {
  const address = new URL(location.href);
  if (chosen) {
    address.searchParams.set(stillName, '1');
  } else {
    address.searchParams.delete(stillName);
  }

  history.replaceState(history.state, '', address);
  for (const link of still.querySelectorAll<HTMLAnchorElement>('a[href]')) {
    const target = new URL(link.href);
    if (target.origin !== location.origin || target.pathname !== location.pathname) {
      continue;
    }

    if (chosen) {
      target.searchParams.set(stillName, '1');
    } else {
      target.searchParams.delete(stillName);
    }

    link.href = target.href;
  }

  for (const form of still.querySelectorAll<HTMLFormElement>('form')) {
    if (form.method.toLowerCase() !== 'get' || new URL(form.action).pathname !== location.pathname) {
      continue;
    }

    let field = form.querySelector<HTMLInputElement>(`input[type="hidden"][name="${stillName}"]`);
    if (!chosen) {
      field?.remove();
    } else if (!field) {
      field = document.createElement('input');
      field.type = 'hidden';
      field.name = stillName;
      field.value = '1';
      form.prepend(field);
    }
  }
}

/** Brings the links of the views in step with what the address of the page says under some names. */
export function inStep(names: string[]): void {
  const now = new URLSearchParams(location.search);
  for (const link of document.querySelectorAll<HTMLAnchorElement>('a[data-explore]')) {
    const address = new URL(link.href);
    for (const name of names) {
      const value = now.get(name);
      if (value === null) {
        address.searchParams.delete(name);
      } else {
        address.searchParams.set(name, value);
      }
    }

    link.href = address.href;
  }

  // A form of the bar – the years – sends the rest of the state along as fields nobody sees; they are kept in
  // step like the links. What the form asks for itself is never one of them.
  for (const form of document.querySelectorAll<HTMLFormElement>('form[data-explore]')) {
    for (const name of names) {
      if (form.querySelector(`input[name="${name}"]:not([type="hidden"])`)) {
        continue;
      }

      const value = now.get(name);
      let field = form.querySelector<HTMLInputElement>(`input[type="hidden"][name="${name}"]`);
      if (value === null) {
        field?.remove();
      } else {
        if (!field) {
          field = document.createElement('input');
          field.type = 'hidden';
          field.name = name;
          form.prepend(field);
        }

        field.value = value;
      }
    }
  }
}
