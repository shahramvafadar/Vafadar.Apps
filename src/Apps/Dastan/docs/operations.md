# Running Dastan outside a developer's machine

How the site is built into an image, started, checked from outside, updated and taken back. It is written for
whoever hosts the site, independent of which host that is ([ADR 0010](adr/0010-running-the-site.md)). Nothing
here makes the site reachable: a host, an address and a certificate are the owner's decisions, and no host has
been used yet.

The tool of the editors is a different program and has a page of its own:
[operations of the tool](operations-studio.md).

## What runs

One container: the program of the site, listening on port 8080 without encryption. It holds no collection and no
secret. Without accounts it has no database, writes nothing to its file system and keeps nothing between two
starts except, when asked to, the keys of its forms; with accounts it keeps the store of the visitors, their
photographs and the keys on a volume ("A closed trial in a container", below).

| | |
|---|---|
| Image | Built from [`Dockerfile`](../Dockerfile) with the root of the repository as its context; what is sent to the build is named in `Dockerfile.dockerignore` – no content, no other app, no test |
| User | Not the administrator (`$APP_UID`, 1654) |
| File system | Can be read-only; the program needs `/tmp` (a `tmpfs` is enough) |
| Memory | Without accounts the trial on this machine ran with a limit of 512 MB; with accounts and photographs with 1 GB (`compose.trial.yaml`) |
| Port | 8080, HTTP. A proxy in front of it holds the certificate |

```powershell
docker build -f src/Apps/Dastan/Dockerfile -t dastan-site .
```

## What it is told

Settings are environment variables (two underscores for a level).

| Variable | Meaning |
|---|---|
| `Dastan__Content` | The folder of the collection inside the container. Default `content`, that is `/app/content` – mount the content there, read-only |
| `Dastan__Sample` | The file inside `samples/` of that folder of the collection a visitor enters first. Default `shahnameh-pilot.json`. Every other collection in `samples/` is offered as well (step 14.10). Empty: the site begins with the first collection of the folder in the order of the names, and does not start with a folder that holds none (step 25.1) |
| `Dastan__Audience` | `Public` (the default) or `LocalDemonstration`. A host never sets it: a demonstration shows what is not published |
| `Dastan__TrustedProxies__0`, `__1`, … | The proxies the site believes when they say how a visitor came (`X-Forwarded-Proto`, `X-Forwarded-For`): an address or a network such as `10.0.0.0/8`. Not set, the site believes nobody – then it does not know that a visitor came by https, sets no `Secure` cookie and sends no HSTS. Name the proxy of the host and nothing wider |
| `Dastan__Reports` | `true`: the site takes the reports of visitors (step 14.8, [ADR 0012](adr/0012-visitor-reports.md)) and keeps them in its database (`Dastan__Database`), in the table `delivery."Reports"`. The editors read them with their tool (`Studio__Reports`), which is told the same database. A report is the statement, the words and the day – nothing about the visitor. The same setting lets who signed in propose a place to the editors (D-085): a place they were at that no collection has an entry for, kept in the table `delivery."Proposals"` as the place in their words and the day – not who proposed it. That table came on 2026-11-23: run `database migrate` before a site of that day or later starts. A site told to take reports without a database, or on a database without the tables, does not start | not set: the site offers no form for reports and takes none |
| `Dastan__Published` | `true`: the site reads its collections from the published revisions of its database – for each collection the revision that was **released** (`store release`, see "Taking a revision back") – and not from the files in `samples/`. It needs `Dastan__Database`. The files of media still lie in the folder of content. A site without a released revision does not start and says so. Default: the files |
| `Dastan__First` | For a site that reads from the database: the identifier of the collection a visitor enters first. Empty, the first by its identifier |
| `Dastan__Database` | The connection of the PostgreSQL database of the site (step 19.3, [ADR 0015](adr/0015-postgresql.md)), as Npgsql reads it – `Host=…;Database=…;Username=…;Password=…`. With it the site has accounts of visitors and what belongs to them. The password is a secret: it is set where the site runs and never written into a file of the repository. The database is made with `--encoding=UTF8 --locale=C`, and its tables are made and brought up by the command `database migrate` – the site changes no table and does not start on a database that is behind | not set: no accounts, nothing kept about a visitor |
| `Dastan__Photos` | A folder for the photographs of the visitors (step 16.5), on a volume that outlives the container, that the user of the container may write to and that **no web server shows**: a photograph is given only through the site, which asks who may see it. Needs `Dastan__Database`. It is backed up together with the store of the visitors – one without the other is of no use | not set: the site has no photographs |
| `DASTAN_LEGAL` (of `compose.trial.yaml`) | A folder with legal texts of the owner's own inside the container of the trial, on its volume – for example `/data/legal`; it becomes `Dastan__Legal` | not set: the texts of the program |
| `Dastan__Operator__Street`, `__Town`, `__Country` | The postal address of who runs the site (row 18.8, [legal/before-publication.md](legal/before-publication.md)): shown in the imprint, the privacy notice and the terms. Said to the running site, never written into the repository | not set: the legal pages show a gap and say that they are not complete |
| `Dastan__Operator__Name`, `__Site`, `__Mail` | The name of the operator, the address of the site and the mail address the legal pages name. **The address of the site is also what every link in a mail about an account begins with** (step 19.4): it must be the address visitors reach the site at, with `https://` and without a slash at its end – a wrong one mails links that lead nowhere | `Shahram Vafadar`, `https://dastan.online`, `info@dastan.online`; the town is `Niedersachsen` and the country `Deutschland` until the owner says the full address (D-075), and the authority is the one of Niedersachsen |
| `Dastan__Operator__Phone` | A telephone number for the imprint; optional | not set: the line is left out |
| `Dastan__Operator__Host`, `__Network`, `__Authority` | Who hosts the site, who carries its traffic when that is somebody else, and the data protection authority that is competent, each with its address: named in the privacy notice | not set: the lines are left out |
| `Dastan__Notices` | `true`: the site takes notices about content (Article 16 of the Digital Services Act) with a form and keeps them in its database (`Dastan__Database`), in the table `delivery."Notices"`. Read with the command `notices` | not set: the page `notice` names the mail address and has no form |
| `Dastan__Mail__Host`, `__Port`, `__Security`, `__User`, `__Password`, `__From`, `__FromName` | The SMTP server the mail of the site leaves by ([ADR 0016](adr/0016-identity-and-mail.md)): its name; its port (587); how the connection is protected – `StartTls` (the default, and a server that does not offer it is refused), `Tls` from the start, or `None` for a catcher on the same machine only; the name and the password the site signs in with, when the server asks; the address the mail comes from and the name beside it. **The password is a secret**: it is given by the environment of the server and is never written into a file of the repository, a command or a log. A message waits in the database (`delivery."Mail"`) and is sent by the site every few seconds; one that fails is tried again later and later and given up after eight tries. A site told a server without an address to send from, without a database or without the tables does not start | not set: the site sends no mail |
| `Dastan__Announcement` | A text the operator has to tell everybody – changed terms, the end of a function, a breach of security – shown on every page as plain text until it is taken out again. The site knows no mail address of its visitors | not set |
| `Dastan__SignIn__MadeUp` | For a developer's machine only (step 18.2.2): a made-up provider that asks nobody and says the visitor is whoever this value names, so that the forms after a sign-in through a provider can be looked at without a client of Google or Microsoft. It exists in the environment `Development` only; anywhere else it does nothing | not set |
| `Dastan__SignIn__MadeUpProves` | With the made-up provider only: `false` makes it name an address without proving it, as Microsoft does, to try the way of an address that still has to be confirmed by mail (step 20.3). Not set, it proves its address, as Google does. |
| `Dastan__Legal` | A folder with legal texts of the owner's own (step 18.3.4, row 18.8), read-only: a plain text file for a page and a language – `terms.en.txt`, `privacy.de.txt`, `cookies.fa.txt`, `imprint.en.txt`, … – stands in the place of the text of the program for that page and language. A line that begins with `#` is a heading, `##` a smaller one, `- ` an item of a list, `\| ` a line that keeps its end (an address); lines that follow each other are one paragraph; `{{operator.name}}`, `{{operator.street}}`, `{{operator.town}}`, `{{operator.country}}`, `{{operator.mail}}`, `{{operator.phone}}`, `{{site}}`, `{{host}}`, `{{network}}` and `{{authority}}` are filled in from the settings, and a line that begins with `?` is left out when its value is not said. Nothing in a file is taken as markup. An empty file shows "this text is not written yet" | not set: the texts of the program, in English, Persian and German |
| `Dastan__InvitationOnly` | `true`: a new account needs an invitation of somebody who has an account (D-068) – the site of a closed trial. Make the first account before setting it | not set: anybody may make an account |
| `Dastan__SignIn__Google__ClientId`, `Dastan__SignIn__Google__ClientSecret` | The client of the site at Google (step 16.8), for signing in through Google. The owner registers the site there with the address `https://<the site>/signin-google` to come back to and the scopes `openid` and `email` – the site asks for who somebody is and for the address Google confirmed, which becomes the address of the account (step 19.4) – and enters both values into the environment of the container – the secret never into a file of the repository, a command line or a ticket. Needs `Dastan__Database`. **Not tried against Google yet** | not set: no sign-in through Google is offered |
| `Dastan__SignIn__Microsoft__ClientId`, `Dastan__SignIn__Microsoft__ClientSecret` | The same for Microsoft; the address to come back to is `https://<the site>/signin-microsoft`. The site takes the mailbox of the account or the name it signs in with as its address; an account at Microsoft that names neither makes no account here. **Not tried against Microsoft yet** | not set: no sign-in through Microsoft is offered |
| `Dastan__Syncfusion__Key` | The key of the licence of the control that draws the graphs (ADR 0020). The owner enters it; it is written into the pages with a graph and so readable by everybody, as the maker of the control intends. Without it the graphs work and the control puts a notice about its licence over the page |
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

**A site that reads from the database** (`Dastan__Published`) needs no folder of a revision. Publishing makes
a revision and shows it to nobody; the site shows the revision that was released, and reads it when it starts:

```powershell
dotnet Vafadar.Dastan.Content.dll store revisions <collection> --store env                       # which exist, which is shown
dotnet Vafadar.Dastan.Content.dll store release <collection> <number> --store env --by "<name>"  # the newest, or an earlier one again
dotnet Vafadar.Dastan.Content.dll store withdraw <collection> --store env --by "<name>"          # the site shows none of it
```

Then start the site again. Every word is kept with who said it and when, and nothing that was published is
removed. A revision is kept with the hash of its text; a site that finds a revision changed does not show it
and does not start. The comparisons are still read from the folder `comparisons`.

Taking the program back is the same with the image: the tag of the commit before.

## A closed trial in a container

`compose.trial.yaml` runs the site with accounts as the image of the Dockerfile: the content mounted
read-only, and **one volume at `/data`** for everything the site keeps – the store of the visitors
(`/data/people.db`), their photographs (`/data/photos`) and the keys of the forms (`/data/keys`). The folder
`/data` of the image belongs to the user of the container, so a new volume does too.

1. `$env:DASTAN_CONTENT = "<the content folder>"`, then
   `docker compose -f src/Apps/Dastan/compose.trial.yaml up -d --build`. The site answers at
   `http://127.0.0.1:53382` – for this machine only.
2. **The site takes a password over https only.** A container sees this machine as another one, so the forms
   of an account are refused over plain http; the page of the account says so. A proxy or a tunnel in front
   of the site holds the certificate and says how the visitor came (`X-Forwarded-Proto: https`), and the
   site believes it when it is among `Dastan__TrustedProxies__…`.
3. Make the first account. Then start again with `$env:DASTAN_INVITATION_ONLY = "true"`: a new account
   needs an invitation of somebody who has one (D-068).
4. From outside (D-067): `--profile tunnel` starts a tunnel of Cloudflare beside the site. The owner makes
   the tunnel in their own account, points it at `http://trial:8080` and enters its token into
   `CLOUDFLARE_TUNNEL_TOKEN` of the environment – into no file. **Not tried**: it needs that account.
5. Content that is not a published revision is shown only with `$env:DASTAN_AUDIENCE = "LocalDemonstration"`
   – for a trial among invited people, never on a site everybody can reach.

Tried on 2026-11-18 on the development machine, asked from outside as a proxy would: an account made, a
picture of 1200 by 630 pixels brought in and its smaller size made (800 by 420 – the library for images runs
in the image), `/preview.png` drawn, a stranger given nothing of the photograph; then with invitations only:
no form and no account without an invitation, an account with one, the invitation used up by it.

## Backing up what the site keeps, and bringing it back

Everything is on the one volume, and it is backed up as one – the photographs without the store are of no
use, and without the keys every form that was open is refused once. **Stop the site first**: the store is a
database file that is copied whole.

```
docker compose -f src/Apps/Dastan/compose.trial.yaml stop trial
docker compose -f src/Apps/Dastan/compose.trial.yaml run --rm --no-deps -v "<a folder for backups>:/backup" --entrypoint tar trial czf /backup/dastan-data.tgz -C /data .
docker compose -f src/Apps/Dastan/compose.trial.yaml start trial
```

Bringing it back, into a volume that is new (`docker volume rm test_dastan-trial-data` removes the old one –
only when the backup is known to be good):

```
docker compose -f src/Apps/Dastan/compose.trial.yaml down
docker compose -f src/Apps/Dastan/compose.trial.yaml run --rm --no-deps -v "<the folder of backups>:/backup" --entrypoint tar trial xzf /backup/dastan-data.tgz -C /data
docker compose -f src/Apps/Dastan/compose.trial.yaml up -d
```

Tried on 2026-11-18: backed up, the volume removed, brought back – both accounts signed in again, and the
photograph and its smaller size were given as before. The backup holds what the visitors entrusted to the
site: it is kept where the volume would be kept, and not in a folder anybody else reads.

## Smaller sizes of photographs that are there already

The site makes a smaller size of a photograph when it is brought in (step 17.3). For the photographs that were
there before, run once, with the site stopped or running:

```
dotnet Vafadar.Dastan.Content.dll photos sizes <Dastan__Photos>/media
```

It says how many sizes it made, changes no photograph, and finds nothing to do when it is run again.

## The database

The site keeps what belongs to accounts in PostgreSQL ([ADR 0015](adr/0015-postgresql.md)). The commands of
the operator read its connection from the variable `DASTAN_DATABASE`, named with the word `env`:

```
dotnet Vafadar.Dastan.Content.dll database migrate --database env
```

makes the tables or brings them to the form the program knows, and is run before a new version of the site is
started. On a developer's machine and for the tests, `docker compose -f src/Apps/Dastan/compose.yaml up -d
postgres` starts a PostgreSQL that keeps nothing (its data lies in memory) and answers this machine only; the
tests make a database of their own for every test in it and are skipped without it – in CI, where
`DASTAN_POSTGRES_REQUIRED` is set, they fail instead.

**The parts of the database.** `identity` (the accounts as ASP.NET Core Identity keeps them), `personal` (what belongs to the accounts), `editorial` (the work of the
editors), `published` (the revisions and comparisons that were released, and which of them the site shows) and
`delivery` (reports of visitors, notices about content). One `database migrate` makes all of them.

**Roles.** The role that runs `database migrate` owns the tables and may do everything; it is the role of the
operator and of nobody else. The site and the tool of the editors each connect as a role of their own:

```
psql … -c "create role dastan_site login" -c "\password dastan_site"     # the password is typed, never written
psql … -c "create role dastan_tool login" -c "\password dastan_tool"
dotnet Vafadar.Dastan.Content.dll database grant site dastan_site --database env
dotnet Vafadar.Dastan.Content.dll database grant tool dastan_tool --database env
```

`Dastan__Database` then names `dastan_site`, `Studio__Store` names `dastan_tool`, and `DASTAN_DATABASE` of the
commands stays the owner's. The site reads what was released and writes accounts, reports and notices; it
cannot read a table of the editors – their accounts lie in the schema `identity` beside those of the visitors,
and each role is given its own tables there and none of the other's – change a revision or make a table. The tool does the work of the editors,
adds revisions and releases, reads reports and notes them as looked at; it cannot reach what belongs to
visitors and cannot change a revision that was published. `grant` is run again after an update that adds
tables – it says the same for tables made later by the role that ran it, and asked twice it changes nothing.
The Compose files of this repository start with one role for everything, which is enough for a development
machine and a closed trial among invited people; a site for the public uses the two roles.

**Backup.** `pg_dump -Fc` of the database, together with the volume of the files – the photographs without
the database are of no use, and the database names files it does not hold:

```
docker compose -f src/Apps/Dastan/compose.trial.yaml exec -T database pg_dump -U dastan -d dastan -Fc > <a place that is not this server>/<day>.dump
```

Bringing it back, with the site stopped: `pg_restore --clean --if-exists -d dastan` fed with that file, then
the volume, then the site. Tried once on the development database (2026-11-19): a collection with its history
and accounts were copied, both schemas dropped and restored, and the collection written out afterwards was
the same file byte for byte. Not tried on a server, and not together with the volume of the files.

## Mail

What the site has to tell somebody leaves by SMTP through whatever service the settings name – which one is
the owner's choice and changes no code. Before the site is public its sender needs a domain that receivers
trust (SPF, DKIM, DMARC), set up where the domain is kept. The operator sees what waits and what failed,
never what a message says:

```
dotnet Vafadar.Dastan.Content.dll mail env                          # what waits, what was given up, and why
dotnet Vafadar.Dastan.Content.dll mail again <key> env              # try a message that was given up again
dotnet Vafadar.Dastan.Content.dll mail forget env [--kept-days 30]  # remove the record of what left long ago
```

A message is kept with a key and is sent once however often it is handed over; once sent, its text is
emptied – it may hold a link that opens an account – and the record that it left, and to which address, is
kept for 30 days unless another number is named. If the site stops between the moment a server took a
message and the moment that is written down, the message is sent once more, under the same identifier, so
that a server knows it as the same. On a developer's machine `docker compose -f src/Apps/Dastan/compose.yaml
up -d mail` starts a catcher that takes every message, sends none on and shows them at
http://127.0.0.1:53480 (SMTP on port 53425, `Security` = `None`); the tests send there.

## Notices about content, and restricting

What the law asks of a site that keeps what others wrote, and how the operator does it, is in
[legal/README.md](legal/README.md), section 4. The commands:

```
dotnet Vafadar.Dastan.Content.dll notices env
dotnet Vafadar.Dastan.Content.dll notices show <reference> env
dotnet Vafadar.Dastan.Content.dll notices settle <reference> env --decision "<what was decided>"
dotnet Vafadar.Dastan.Content.dll notices forget env [--kept-days 365]

dotnet Vafadar.Dastan.Content.dll people restrictions --people env
dotnet Vafadar.Dastan.Content.dll people restrict photo|tree|account <id or name> --people env --site https://<the site> --reason "<the facts>" --ground "<the law or the section of the terms>" [--noticed]
dotnet Vafadar.Dastan.Content.dll people lift <number> --people env --site https://<the site>
dotnet Vafadar.Dastan.Content.dll people persons <tree> --people env
dotnet Vafadar.Dastan.Content.dll people person remove <tree> <person> --people env
dotnet Vafadar.Dastan.Content.dll people forget <name> --people env
dotnet Vafadar.Dastan.Content.dll people repair --people env [--change]
dotnet Vafadar.Dastan.Content.dll people addresses --people env
dotnet Vafadar.Dastan.Content.dll people addresses ask <provider> --people env --site https://<the site>
```

`people addresses` lists the open accounts whose mail address counts as confirmed while nothing says how it
was confirmed (ADR 0016). Since step 20.3 every account records that – by a link the site mailed, or by a
provider that said it had verified the address. An account made before that through a provider has no such
record; one that never had a provider is written down as confirmed by mail when the store is brought up.
`people addresses ask microsoft` asks the accounts of the list that can be signed in through Microsoft to
confirm the address anew: the link is mailed to the address, every sign-in of the account ends, and the
account – with everything of it – is an account again when the link was opened. Nobody is asked twice. An
account without a record that is signed in through Google only is left as it is: Google's word was taken
only when it said it had verified the address.

`people repair` looks for what two requests at the same moment could leave behind before every act took its
locks (ADR 0022): somebody in a circle or a tree of an account they show no accepted relationship with, and
a tree in which somebody is their own ancestor. Without `--change` it only counts. With it, the places in
circles and trees are taken away, as the end of the last relationship would have; a tree with a loop is named
and left as it is – which link is wrong only who knows the family can say. It can be run as often as wanted.
Run it once after the version that brings it.

A restricted photograph is seen by its owner only; a restricted tree is not public and cannot be made public;
a closed account signs in nowhere and is deleted after 30 days like one its visitor closed. The person
concerned reads the reason and the ground on the page of the account – the owner of a closed account after
giving the name and the password on the sign-in page. `people forget` ends the 30 days of a closed account
for somebody who asks to be deleted at once. The commands write into the store the site writes into: run them
where the store lies, while few people are on the site.

## A family tree from a file

An account can ask who runs the site to write a family tree for it from a file, instead of typing it into the
pages. The file is of the format `dastan-tree-0.1` ([the work order for content](content-work-order.md), part
7): a name of the tree, people with a name and – where known – the years of birth and death, who is a parent of
whom and who are partners. It has no field for more, so of somebody who lives nothing but the name, the year and
the family is written (PRIV-003).

```powershell
dotnet Vafadar.Dastan.Content.dll people import-tree "<account name>" --from <file> --people env
```

* The account must be there and open; the command makes none.
* The tree is written whole or not at all, by the rules every tree keeps, as a new and private tree of the
  account. If the account has a tree of that name the command stops: nothing is added to a tree that is there.
* No record is said to be an account: that is a claim on the site, confirmed by somebody else.
* The command prints how many people and links it wrote and never a name. The file is read and kept nowhere;
  remove it when the tree is written, unless the account wants it kept.

## What the site keeps about visitors

Without `Dastan__Database` nothing on its own: no database, no file, no account. With it, the accounts of the
visitors: for each a name the visitor chose, a value derived from the password, the hashes of the recovery codes,
the mail address and whether it is confirmed, when the account was made, how often the password was wrong and
when it was closed – no address of a machine; for an account that is signed in through Google or Microsoft, which of them and a hash
of the identifier it gives the person, with the mail address it confirmed as the address of the account – no
name or picture of theirs, and no token; and what the visitor keeps with the account: reading positions, a list, invitations,
relationships and circles, family trees with records of people, and – with `Dastan__Photos` – photographs
(the [privacy matrix](../../../../docs/privacy/privacy-matrix.md) lists each). A closed account is deleted for good thirty days later, by the first request of a day
(the site must be visited for it; a site nobody opens deletes nothing). Its log goes to the output of the container and holds what
the framework says at the level "Warning" and above and what the site says when it starts – no address of a
visitor and no request. A proxy and a host keep their own logs; what they hold and for how long is the host's
answer and belongs into the [privacy matrix](../../../../docs/privacy/privacy-matrix.md) when a host is chosen.

## What is not decided or not tried

* **A host, an address, a certificate, a proxy, monitoring, backups of the revisions.** The owner's decisions.
* **The store of the visitors in a container**: the volume, its backup and bringing a backup back were tried
  on 2026-11-18 ("Backing up what the site keeps, and bringing it back"); not with more than a handful of
  accounts, and not over a real tunnel.
* **More than one container at a time.** With several, the keys of the forms have to be shared (`Dastan__Keys`
  on a volume all of them use); it was tried with one container.
* **The image on another processor or operating system** than this machine's (linux/amd64 under Docker Desktop).
* **Load.** No measurement of how many visitors a container serves. What one visitor's largest drawing costs – the database, the answer, the browser – was measured on the machine of development (step 20.12; the numbers and the machine are in [ADR 0020](adr/0020-graph.md), "The whole way, measured"). To write the times of a run down instead of judging them, set `DASTAN_BUDGETS=record` and name a file in `DASTAN_MEASURES`, as the workflow of CI does.
* **An image registry.** The image was built and run locally; nothing was pushed anywhere.
