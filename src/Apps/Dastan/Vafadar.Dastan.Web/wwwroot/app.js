// The only script of the pages of Dastan (ADR 0007): it registers the service worker that shows a page for
// "no connection". No page needs it to show its content or to be used.
if ('serviceWorker' in navigator) {
  window.addEventListener('load', () => {
    navigator.serviceWorker.register('/service-worker.js', { scope: '/' }).catch(() => {
      // Without the worker the browser shows its own page when there is no connection.
    });
  });
}
