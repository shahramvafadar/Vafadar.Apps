# Running Dastan outside a developer's machine

How the site is built into an image, started, checked from outside, updated and taken back. It is written for
whoever hosts the site, independent of which host that is ([ADR 0010](adr/0010-running-the-site.md)). Nothing
here makes the site reachable: a host, an address and a certificate are the owner's decisions, and no host has
been used yet.

The tool of the editors is a different program and has a page of its own:
[operations of the tool](operations-studio.md).

## What runs

One container: the program of the site, listening on port 8080 without encryption. It holds no collection, no
database and no secret, writes nothing to its file system and keeps nothing between two starts except, when asked
to, the keys of its forms.

| | |
|---|---|
| Image | Built from [`Dockerfile`](../Dockerfile) with the root of the repository as its context; what is sent to the build is named in `Dockerfile.dockerignore` – no content, no other app, no test |
| User | Not the administrator (`$APP_UID`, 1654) |
| File system | Can be read-only; the program needs `/tmp` (a `tmpfs` is enough) |
| Memory | The trial on this machine ran with a limit of 512 MB |
| Port | 8080, HTTP. A proxy in front of it holds the certificate |

```powershell
docker build -f src/Apps/Dastan/Dockerfile -t dastan-site .
```

## What it is told

Settings are environment variables (two underscores for a level).

| Variable | Meaning |
|---|---|
| `Dastan__Content` | The folder of the collection inside the container. Default `content`, that is `/app/content` – mount the content there, read-only |
| `Dastan__Sample` | The file inside `samples/` of that folder of the collection a visitor enters first. Default `shahnameh-pilot.json`. Every other collection in `samples/` is offered as well (step 14.10) |
| `Dastan__Audience` | `Public` (the default) or `LocalDemonstration`. A host never sets it: a demonstration shows what is not published |
| `Dastan__TrustedProxies__0`, `__1`, … | The proxies the site believes when they say how a visitor came (`X-Forwarded-Proto`, `X-Forwarded-For`): an address or a network such as `10.0.0.0/8`. Not set, the site believes nobody – then it does not know that a visitor came by https, sets no `Secure` cookie and sends no HSTS. Name the proxy of the host and nothing wider |
| `Dastan__Reports` | A folder for the reports of visitors, on a volume the user of the container may write to (step 14.8, [ADR 0012](adr/0012-visitor-reports.md)). The editors read it with their tool (`Studio__Reports`): on one machine the same folder, else it is carried over. Each report is a small file with the statement, the words and the day – nothing about the visitor | not set: the site offers no form for reports and takes none |
| `Dastan__Keys` | A folder for the keys of the forms, on a volume that outlives the container and that the user of the container may write to. Without it a form that a visitor opened before the site was started again is refused afterwards ("nothing was changed", and the visitor tries again) |
| `ASPNETCORE_HTTP_PORTS` | The port, when it is not 8080 |

## What it shows

The content folder of a **published revision** ([ADR 0009](adr/0009-published-revision.md)): `samples/` with the
file of the revision and its reviewed translations, `media/` with the files of its media, `comparisons/` if any.
The editors make the file of the revision with `store publish` out of their store, or with the `revision`
command from a working file ([guide](content-guide.md)); it is a new file in a new folder and is never changed
afterwards. The store keeps every revision it published and writes one again with `store revision`. A revision needs nothing beside its content folder – the
command leaves out the pointers to the working documents of reviewers.

The working files of the repository (`content/`) are not such a folder. Mounted, the site either shows nothing
(an editorial sample is not for the public) or does not start, because the working file points to documents
that are not in the container. That is intended: what is not published is not deployed.

The site reads the collection once, when it starts, and stops with the list of what is wrong when it cannot use
it – before it answers anything.

## Is it alive

`GET /health` answers `200` with the text `ok` when the process answers and the collection was read. It says
nothing else, is not cached and not indexed. The image asks it by itself (`HEALTHCHECK`: the program started
with `--probe`), so `docker ps` and an orchestrator see "healthy"; a proxy or a monitor asks the address.

## Checking it from outside

```powershell
./src/Apps/Dastan/eng/Check-Site.ps1 -Address https://<the address>
```

The script asks a running site what a host has to be sure of and ends with 1 when a check fails: alive; the
protecting headers on a page and on the answer about being alive (content security policy, no sniffing, no
framing, referrer, permissions, opener); a page is not kept by a cache and loads nothing from another site;
cookies are HttpOnly and SameSite; a public site is not marked "noindex" and does not call itself a
demonstration; the files of the collection, the settings and the program are not served; a page that does not
exist answers 404 with a page of the site; told by a trusted proxy that the visitor came by https, the site
sends HSTS and its cookies are `Secure`, and not told so it does not claim https.

Run it against the address visitors use, after every update. Run against the container directly (as on a
development machine) the last group passes only when the machine that asks is one of the trusted proxies.

## Trying it on a development machine

```powershell
$env:DASTAN_CONTENT = "<the content folder of a published revision>"
docker compose -f src/Apps/Dastan/compose.site.yaml up -d --build     # http://127.0.0.1:53380
./src/Apps/Dastan/eng/Check-Site.ps1 -Address http://127.0.0.1:53380
docker compose -f src/Apps/Dastan/compose.site.yaml down
```

The service belongs to the Compose project `test` like the browser of the tests, is reachable from the machine
only, and trusts the networks Docker gives its containers so that the proxy checks can be tried.

## Updating

There are two kinds of update, and they do not depend on each other.

**A new revision of the content** (the program stays):

1. The editors make revision *n* with the `revision` command, into a folder of its own (`…/revisions/<n>/content`),
   and check it where it lies (`check`). The folders of earlier revisions stay as they are.
2. Start a second container of the same image with the new folder mounted. If the collection cannot be used the
   container stops at once and says why in its log; the running site is not touched.
3. When the new container is healthy and `Check-Site.ps1` passes against it, point the proxy to it and stop the
   old one.

**A new version of the program** (the content stays): build the image from the commit to be deployed – only one
that passed the tests of the repository –, give it a tag that names the commit, and exchange the container in the
same way: start beside, check, switch, stop.

With one container and no proxy that can switch, stopping the old and starting the new one is a pause of a few
seconds. A form opened before the pause is accepted afterwards only with `Dastan__Keys`.

## Taking a revision back

A revision that turns out to be wrong is corrected by publishing the next one. Until that exists, the site can
show the revision before: start a container with the folder of revision *n − 1* mounted – it was never changed –
and switch to it as in an update. The page "what changed" then shows the history up to *n − 1*; the correction is
published as revision *n + 1*, which says in its history what it changed against *n*. The number *n* is not used
again.

Taking the program back is the same with the image: the tag of the commit before.

## What the site keeps about visitors

Nothing on its own: no database, no file, no account. Its log goes to the output of the container and holds what
the framework says at the level "Warning" and above and what the site says when it starts – no address of a
visitor and no request. A proxy and a host keep their own logs; what they hold and for how long is the host's
answer and belongs into the [privacy matrix](../../../../docs/privacy/privacy-matrix.md) when a host is chosen.

## What is not decided or not tried

* **A host, an address, a certificate, a proxy, monitoring, backups of the revisions.** The owner's decisions.
* **More than one container at a time.** With several, the keys of the forms have to be shared (`Dastan__Keys`
  on a volume all of them use); it was tried with one container.
* **The image on another processor or operating system** than this machine's (linux/amd64 under Docker Desktop).
* **Load.** No measurement of how many visitors a container serves.
* **An image registry.** The image was built and run locally; nothing was pushed anywhere.
