// The service worker of Dastan (ADR 0007). It does two things when a page cannot be fetched: it shows a public
// page the visitor has read on this device before (D-064), or else the page that says there is no connection.
// It keeps the few files that page is made of, the public pages the site allows it to keep, and what such a
// page is drawn with. It keeps nothing of an account and nothing of a private space, and with a connection
// everything goes to the network exactly as without it.

// Changing a name makes browsers drop what an earlier worker kept.
const SHELL = 'dastan-shell-1';
const READ = 'dastan-read-1';

// How many pages and files of pages are kept; beyond that the ones kept longest ago go.
const MOST = 200;

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
      .then(names => Promise.all(names.filter(name => name !== SHELL && name !== READ).map(name => caches.delete(name))))
      .then(() => self.clients.claim()));
});

// A page that is shown from what was kept says so at its top (step 18.3.2): it is the page as it was when the
// visitor read it, and the site may have changed since. The worker reads the language from the address.
const KEPT = {
  fa: '<p class="note kept-copy" role="status" lang="fa" dir="rtl">اتصال برقرار نیست. این صفحه همان است که پیش‌تر روی این دستگاه خوانده‌اید؛ ممکن است سایت از آن زمان تغییر کرده باشد.</p>',
  en: '<p class="note kept-copy" role="status" lang="en" dir="ltr">There is no connection. This is the page as you read it on this device before; the site may have changed since.</p>',
};

async function marked(kept, path) {
  const page = await kept.text();
  const main = page.indexOf('<main');
  const end = main < 0 ? -1 : page.indexOf('>', main);
  const note = path.startsWith('/fa/') ? KEPT.fa : KEPT.en;
  return new Response(end < 0 ? page : page.slice(0, end + 1) + note + page.slice(end + 1), { headers: { 'Content-Type': 'text/html; charset=utf-8' } });
}

// Keeps an answer of the network for later, and lets go of what was kept longest ago.
async function keep(address, answer) {
  const read = await caches.open(READ);
  await read.put(address, answer);
  const kept = await read.keys();
  await Promise.all(kept.slice(0, Math.max(0, kept.length - MOST)).map(old => read.delete(old)));
}

self.addEventListener('fetch', event => {
  const request = event.request;
  const address = new URL(request.url);
  if (address.origin !== self.location.origin) {
    return;
  }

  // A form of the preferences changes what the visitor sees – the reading position, the language, who is
  // signed in. What was kept was kept for the visitor as they were before: it is let go.
  if (request.method !== 'GET') {
    if (address.pathname.startsWith('/preferences/')) {
      event.waitUntil(caches.delete(READ));
    }

    return;
  }

  // A page: always from the network. The site says which page may be kept – a public one, for a visitor who is
  // not signed in. Only when the network fails the visitor gets what was kept, or the page that says so.
  if (request.mode === 'navigate') {
    event.respondWith(
      fetch(request)
        .then(answer => {
          if (answer.ok && answer.headers.get('Dastan-Keep') === 'page') {
            event.waitUntil(keep(request.url, answer.clone()));
          }

          return answer;
        })
        .catch(() => caches.open(READ)
          .then(read => read.match(request.url))
          .then(kept => kept ? marked(kept, address.pathname) : caches.match('/offline.html'))));
    return;
  }

  // The files of the page for "no connection": from the network while there is one, so that they are never
  // older than the site.
  if (FILES.includes(address.pathname)) {
    event.respondWith(fetch(request).catch(() => caches.match(address.pathname)));
    return;
  }

  // What a kept page is drawn with – its style, its script, its font, the images of its collection: kept when
  // it was fetched, and given from what was kept only when the network fails. A photograph of a visitor is
  // never kept, and neither is a video.
  const drawn = ['style', 'script', 'font'].includes(request.destination)
    || (request.destination === 'image' && address.pathname.startsWith('/media/'));
  if (drawn) {
    event.respondWith(
      fetch(request)
        .then(answer => {
          if (answer.ok) {
            event.waitUntil(keep(request.url, answer.clone()));
          }

          return answer;
        })
        .catch(() => caches.open(READ).then(read => read.match(request.url)).then(kept => kept || Response.error())));
  }

  // Anything else is not touched.
});
