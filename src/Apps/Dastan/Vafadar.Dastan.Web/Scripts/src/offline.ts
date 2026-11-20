// Keeping pages on a device is more than the site needs to work, so nothing is kept without a yes
// (§ 25 TDDDG): the page says whether the visitor gave it. Without it there is no worker, and a worker and what
// it kept from a time when there was a yes are removed (ADR 0007).

/** The cache of the pages a visitor has read. Its name is the service worker's. */
export const READ = 'dastan-read-1';

/** Registers the service worker where the visitor agreed, and removes it and what it kept where not. */
export async function keepOrForget(): Promise<void> {
  if (!('serviceWorker' in navigator)) {
    return;
  }

  if (document.documentElement.dataset['offline'] === 'on') {
    try {
      await navigator.serviceWorker.register('/service-worker.js', { scope: '/' });
    } catch {
      // Without the worker the browser shows its own page when there is no connection.
    }

    return;
  }

  try {
    for (const worker of await navigator.serviceWorker.getRegistrations()) {
      await worker.unregister();
    }

    if ('caches' in window) {
      for (const name of await caches.keys()) {
        if (name.startsWith('dastan-')) {
          await caches.delete(name);
        }
      }
    }
  } catch {
    // A browser that does not let a page ask keeps nothing the page could remove.
  }
}

/**
 * What this device keeps for a time without a connection (ADR 0007, step 18.3.2): the page of the list of the
 * visitor says how many pages are kept and lets them be forgotten. Without this script nothing is said there;
 * the browser has its own control for the data of a site.
 */
export async function sayWhatIsKept(): Promise<void> {
  const place = document.querySelector<HTMLElement>('[data-kept-pages]');
  if (!place || !('caches' in window)) {
    return;
  }

  // A page has no ending in its address; a file of a page – a style, a font, an image – has one or lies under /media/.
  const kept = (await (await caches.open(READ)).keys())
    .map(request => new URL(request.url).pathname)
    .filter(path => !path.startsWith('/media/') && !path.slice(path.lastIndexOf('/')).includes('.'));
  if (kept.length === 0) {
    return;
  }

  const count = place.querySelector<HTMLElement>('[data-kept-count]');
  if (count) {
    count.textContent = kept.length.toLocaleString(document.documentElement.lang);
  }

  place.hidden = false;
  place.querySelector<HTMLElement>('[data-kept-forget]')?.addEventListener('click', async event => {
    await caches.delete(READ);
    if (event.target instanceof HTMLElement) {
      event.target.hidden = true;
    }

    const forgotten = place.querySelector<HTMLElement>('[data-kept-forgotten]');
    if (forgotten) {
      forgotten.hidden = false;
    }
  });
}
