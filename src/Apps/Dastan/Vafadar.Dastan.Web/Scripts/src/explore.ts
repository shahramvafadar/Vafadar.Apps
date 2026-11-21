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
}
