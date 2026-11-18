// The only script of the pages of Dastan (ADR 0007). It registers the service worker that shows a page for
// "no connection", it says what the device keeps for that time and lets it be forgotten, and it moves a chart
// that is drawn around a name so that the name is in sight (ADR 0008).
// No page needs it to show its content or to be used.
if ('serviceWorker' in navigator) {
  window.addEventListener('load', () => {
    navigator.serviceWorker.register('/service-worker.js', { scope: '/' }).catch(() => {
      // Without the worker the browser shows its own page when there is no connection.
    });
  });
}

// What this device keeps for a time without a connection (ADR 0007, step 18.3.2): the page of the list of the
// visitor says how many pages are kept and lets them be forgotten. Without this script nothing is said there;
// the browser has its own control for the data of a site.
window.addEventListener('load', async () => {
  const place = document.querySelector('[data-kept-pages]');
  if (!place || !('caches' in window)) {
    return;
  }

  // A page has no ending in its address; a file of a page – a style, a font, an image – has one or lies under /media/.
  const kept = (await (await caches.open('dastan-read-1')).keys())
    .map(request => new URL(request.url).pathname)
    .filter(path => !path.startsWith('/media/') && !path.slice(path.lastIndexOf('/')).includes('.'));
  if (kept.length === 0) {
    return;
  }

  place.querySelector('[data-kept-count]').textContent = kept.length.toLocaleString(document.documentElement.lang);
  place.hidden = false;
  place.querySelector('[data-kept-forget]').addEventListener('click', async event => {
    await caches.delete('dastan-read-1');
    event.target.hidden = true;
    place.querySelector('[data-kept-forgotten]').hidden = false;
  });
});

// A chart of connections or of a family is drawn around one name. Its stage begins at the top of the drawing;
// when much is opened above the name or before it, the name lies outside what the stage shows first. The stage
// is moved once, when the page has arrived, and only as far as the name needs – a visitor who scrolls afterwards
// is not followed. Without this script the name is reached by scrolling the stage, as before.
// It waits until the page has arrived with its styles and fonts: before that a box is not where it will be.
window.addEventListener('load', () => {
  for (const frame of document.querySelectorAll('.stage-frame')) {
    // The box is asked for by its place in the drawing: an engine does not say where an element inside it is.
    const name = frame.querySelector('.graph-focus')?.closest('foreignObject');
    if (!name || frame.clientHeight === 0) {
      continue;
    }

    const shown = frame.getBoundingClientRect();
    const place = name.getBoundingClientRect();
    if (place.top < shown.top || place.bottom > shown.bottom) {
      frame.scrollTop += place.top + (place.height / 2) - (shown.top + (shown.height / 2));
    }

    if (place.left < shown.left || place.right > shown.right) {
      frame.scrollLeft += place.left + (place.width / 2) - (shown.left + (shown.width / 2));
    }
  }
});
