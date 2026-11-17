# Running the tool of the editors on a server

The tool of Dastan's editors (Dastan Studio, [ADR 0011](adr/0011-editors-store.md)) can run on one editor's
machine without accounts, or on a server for editors who sign in (D-046). This page is about the server. The
public site is a different program with a different page: [operations](operations.md).

The tool shows and changes **what is not published**. Everything here follows from that: it answers only who
signed in, only over an encrypted connection, and its store is one file that somebody has to keep copies of.

## What runs

One container of the image `src/Apps/Dastan/Dockerfile.studio`, built from the root of the repository:

```powershell
docker build -f src/Apps/Dastan/Dockerfile.studio -t dastan-studio .
```

* The image holds the tool and, beside it, the command of the store (`/command`) – accounts are made with it
  where the store lies. It holds nothing of a store and nothing of a collection.
* The container runs as a user without rights, with a file system that cannot be written to – except `/data`,
  the folder of the store.
* **`/data` is a volume**: the store (`/data/editors.db`) and the keys of the sign-in (`/data/keys`). It is all
  the tool keeps. A container without that volume starts with an empty store and forgets it when it is removed.
* The tool listens on port 8080 without encryption. **A proxy in front of it holds the certificate**; the tool
  is never reachable from outside but through that proxy.
* One container. The store is one file with one program writing to it; two containers on the same volume are
  not supported.

## What it is told

| Setting (environment variable) | Meaning | In the image |
|---|---|---|
| `Studio__Accounts` | The tool has accounts: nothing is shown to who has not signed in | `true` |
| `Studio__Store` | The file of the store | `/data/editors.db` |
| `Studio__Keys` | The folder of the keys of the sign-in; without it everybody signs in again after a restart | `/data/keys` |
| `AllowedHosts` | The names the tool is reached by, separated by `;`. A request by another name is refused | has to be given |
| `Studio__TrustedProxies__0`, `__1`, … | The address or network of the proxy that is believed about how an editor came | has to be given |
| `Studio__Files` | The folder of the files of the collections (`content/samples`). With it the tool checks that the file of a media item exists, and editors can bring files in: they are kept in the folder `media` beside it, which the tool must be able to write to. On a server that is a folder of the volume, such as `/data/content/samples` – copy the folder `media` of the collections there first, or every change is refused for the files that are missing | not set: files are not looked for, and no file can be brought in |

The tool answers a request from another machine only when it came by https. It learns that from the proxy
(`X-Forwarded-Proto`) and believes it only from the proxies it was told. **A proxy that is not named is not
believed, and the tool then refuses every request** ("The tool of the editors is reached over https.") – that
is the sign of a missing `Studio__TrustedProxies`.

## The accounts

An account is made by a command in the container – by whoever may run a command on the server. The tool has no
page for it. The password is asked for and typed twice; it is never part of the command and is kept nowhere but
as a value from which it cannot be read back.

```powershell
docker exec -it <container> dotnet /command/Vafadar.Dastan.Content.dll store editor add "<name>" --store /data/editors.db
docker exec -it <container> dotnet /command/Vafadar.Dastan.Content.dll store editor password "<name>" --store /data/editors.db
docker exec -it <container> dotnet /command/Vafadar.Dastan.Content.dll store editor close "<name>" --store /data/editors.db
docker exec    <container> dotnet /command/Vafadar.Dastan.Content.dll store editors --store /data/editors.db
```

* `-it` is needed where a password is typed: without a terminal the command makes no account and says so.
* Another password signs the editor out everywhere; so does closing the account. What a closed account did
  stays in the history under its name.
* After five wrong passwords in a row an account takes none for fifteen minutes. `store editors` shows it;
  `store editor password` ends it.
* Every editor may do every act, also publish. There are no roles.

## The first collection

A store starts empty. A collection comes in from its file with `store import` – on a machine that has the
files, into a store that is then put on the server (below, "Bringing a copy back"), or in the container when
the files are mounted there. After that the editors work in the tool.

## Is it alive

`/health` answers `ok` when the process answers and the store was opened; it needs no sign-in and says nothing
of the store. The container asks itself (`--probe`, the health check of the image).

## Checking it from outside

```powershell
./src/Apps/Dastan/eng/Check-Studio.ps1 -Address http://127.0.0.1:53381
```

The script needs no account. It asks as the proxy would and checks that a connection that is not encrypted is
refused, that the tool is alive, that every page leads to the sign-in for somebody who has not signed in, that a
form without a sign-in is refused, that the cookies are HttpOnly, SameSite and Secure, that the files of the
tool and of the store are not served, and that the tool refuses another name than its own. Run it after every
change of the proxy or of the settings.

## Trying it on a development machine

```powershell
docker compose -f src/Apps/Dastan/compose.studio.yaml up -d --build     # http://127.0.0.1:53381, this machine only
docker compose -f src/Apps/Dastan/compose.studio.yaml down              # the store stays; "down -v" throws it away
```

A browser on the same machine is "another machine" for the container and comes without encryption, so the tool
refuses it. To work in the tool on one machine, run it without a container and without accounts (README), or
put a proxy with a certificate in front of the container.

## Copies of the store

The store is the only place where the work of the editors lies before it is published. **Nothing makes copies
of it by itself.**

```powershell
docker exec <container> dotnet /command/Vafadar.Dastan.Content.dll store copy --store /data/editors.db --out /data/copies/<day>.db
docker cp <container>:/data/copies/<day>.db <a place that is not this server>
```

* `store copy` writes the whole store – collections, history, revisions, accounts – as it is at that moment.
  The tool can run meanwhile. It never writes over a file.
* A copy holds what is not published and the derived passwords of the editors: keep it where the store itself
  could be kept, and nowhere public.
* A copy that stays on the same disk as the store is lost with it. Where the copies go, how often and for how
  long is not decided.

### Bringing a copy back

Stop the tool, put the copy in the place of the file of the store with the user of the tool as its owner, start
the tool. Everything done after the copy was made is gone.

```powershell
docker compose stop studio
docker run --rm --user root -v <the volume>:/data -v <folder of the copy>:/in:ro --entrypoint sh dastan-studio -c 'cp /in/<copy>.db /data/editors.db && chown $APP_UID /data/editors.db'
docker compose start studio
```

## Updating

Build the image of the new commit, start the container from it with the same volume. The tool brings the store
to the form the new program knows when it starts – **make a copy of the store before**: an older program does
not read a store a newer one has changed. Taking the program back is the image of the commit before, with the
copy made before the update.

## What the tool keeps about editors

In the store: for each editor the name, a value derived from the password, when the account was made and
closed, the count of wrong passwords; with every change the name of the editor, the time and the reason. In the
browser of a signed-in editor: one cookie that ends after twelve hours without use, with another password or
with the account being closed. Its log goes to the output of the container and holds what the framework says at
the level "Warning" and above – no name and no address. What the proxy and the server log is theirs
([privacy matrix](../../../../docs/privacy/privacy-matrix.md)).

## What is not decided or not tried

* **A server, an address, a certificate, a proxy.** The owner's decisions. The tool was run in its container on
  a development machine and asked as a proxy would ask; no proxy with a certificate was put in front of it, and
  no browser has signed in to the container.
* **Making an account in the container with a terminal.** The command was run there without one – it refuses,
  as it has to – and with typed passwords in its tests; nobody typed a password into `docker exec -it`.
* **Where copies of the store go, how often, for how long,** and a test of bringing one back on a real server.
  Putting a store into the volume as described above was done once, on the development machine.
* **Roles**, a page for an editor to change their own password, a record of who signed in when.
* **Load**, and two editors changing the same record at the same moment (the later act replaces the record; the
  history shows both).
* **An image registry.** The image was built and run locally; nothing was pushed anywhere.

## Comparisons

A comparison is kept in the same store as the collections. On the machine of the store:

```powershell
dotnet run --project $content -- store comparison import <file> --store <database> --by <name> --reason "<why>"
dotnet run --project $content -- store comparison list --store <database>
dotnet run --project $content -- store comparison export <id> --store <database> --out <a new file>
```

What `export` writes – the file and its translations beside it – is what a site is given in its folder
`comparisons`. An update of the tool that brings new tables (here: the documents) applies them when the store
is opened; make a copy of the store first (`store copy`).

## What an account may do

```powershell
dotnet run --project $content -- store editor add <name> --store <database> --roles writer,reviewer
dotnet run --project $content -- store editor roles <name> publisher --store <database>
dotnet run --project $content -- store editors --store <database>
```

The roles are `writer`, `reviewer` and `publisher`, with commas between them, or `all`; an empty text makes
an account that reads only. A change holds from the next page the editor opens. An editor changes the own
password on the page `account` of the tool; a forgotten one is set anew with `store editor password`.

## Files that editors bring in

With `Studio__Files` set, the files editors bring in lie in the folder `media` beside it, on the volume. They
are part of what has to be copied when the store is copied (`store copy` copies the store, not the files),
and part of what a site is given: beside a published revision, the folder `media` goes into the content
folder of the site. A file that an item no longer names stays in that folder; nothing deletes it.
