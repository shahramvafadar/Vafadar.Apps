// The only script of the pages of Dastan (ADR 0007). It registers the service worker that shows a page for
// "no connection", and it moves a chart that is drawn around a name so that the name is in sight (ADR 0008).
// No page needs it to show its content or to be used.
if ('serviceWorker' in navigator) {
  window.addEventListener('load', () => {
    navigator.serviceWorker.register('/service-worker.js', { scope: '/' }).catch(() => {
      // Without the worker the browser shows its own page when there is no connection.
    });
  });
}

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
