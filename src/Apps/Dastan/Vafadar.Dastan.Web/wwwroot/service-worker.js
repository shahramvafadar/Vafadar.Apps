// The service worker of Dastan (ADR 0007). It does one thing: when a page cannot be fetched, it shows the page
// that says so. It keeps the few files that page is made of and nothing else – no page of the site, no file of a
// collection, no answer that depends on a visitor. Everything else goes to the network exactly as without it.

// Changing this name makes browsers drop what an earlier worker kept.
const SHELL = 'dastan-shell-1';

// The page for "no connection" and what it needs to look like the site. Nothing of a collection.
const FILES = [
  '/offline.html',
  '/app.css',
  '/_content/Vafadar.Web/vafadar.css',
  '/fonts/vazirmatn-nl-wght.woff2',
  '/icon.svg',
];

self.addEventListener('install', event => {
  // A new worker does not wait for open pages: no page depends on what an old one kept.
  event.waitUntil(caches.open(SHELL).then(cache => cache.addAll(FILES)).then(() => self.skipWaiting()));
});

self.addEventListener('activate', event => {
  event.waitUntil(
    caches.keys()
      .then(names => Promise.all(names.filter(name => name !== SHELL).map(name => caches.delete(name))))
      .then(() => self.clients.claim()));
});

self.addEventListener('fetch', event => {
  const request = event.request;

  // A page: always from the network. Only when the network fails the visitor gets the page that says so.
  if (request.mode === 'navigate') {
    event.respondWith(fetch(request).catch(() => caches.match('/offline.html')));
    return;
  }

  // The files of that page: from the network while there is one, so that they are never older than the site.
  const address = new URL(request.url);
  if (request.method === 'GET' && address.origin === self.location.origin && FILES.includes(address.pathname)) {
    event.respondWith(fetch(request).catch(() => caches.match(address.pathname)));
  }

  // Anything else – the files of a collection above all – is not touched.
});
