# 0010. Running the site: one container, a mounted revision, a proxy it is told to believe

* Status: Accepted
* Date: 2026-11-17
* Step: 10.5 of the [delivery plan](../delivery-plan.md)

## Context

Until now the site ran on a developer's machine only. Whichever host the owner chooses, the site has to be a thing
that can be started there, asked whether it is alive, checked from outside, updated and taken back. None of that
depends on the host; all of it can be built and tried before one exists.

## Decision

1. **One container image, without content.** The image is built from the repository and holds the program. The
   collection is a folder mounted read-only when the container starts. Content and program are published
   separately, a new revision needs no new image, and the working files of the editors cannot get into an image:
   the build is not sent them.
2. **What is mounted is a published revision, and a revision is its content folder.** The `revision` command
   leaves out the pointers of media items to the working documents of reviewers, which lie outside the content
   folder; the readers never showed them, and a site must not need them to start. Found by starting the image:
   with the pointers the container stopped at once.
3. **The site says that it is alive at `/health`**, and the program can ask its own site (`--probe`), because the
   image has no tool to ask with. The answer is `ok` and nothing about the collection.
4. **The site listens without encryption and believes only the proxies it is told to believe.** A proxy holds the
   certificate and passes on how the visitor came. The site takes that from the addresses and networks named in
   `Dastan:TrustedProxies` and from nobody else; told nothing, it believes nobody. A general switch that
   believes every sender is not used: with it, any visitor could claim to be the proxy.
5. **Every cookie follows how the visitor came**, the cookie of the forms too: `Secure` when the visitor came by
   https.
6. **The keys of the forms can outlive a container** (`Dastan:Keys`, a folder on a volume). Without it they live
   as long as the process, which is right for a development machine and a pause for visitors at every update.
7. **The checks a host needs are a script that asks from outside** (`eng/Check-Site.ps1`), not a test inside the
   process: what a proxy and a container do to an answer is exactly what a test inside cannot see.
8. **Update and taking back are an exchange of containers**: start the new one beside the old, check it, switch.
   A collection that cannot be used stops the new container before it answers; the old one keeps running.
   Revisions are folders that are never changed, so taking one back is mounting the one before.

How it is done is written in [operations](../operations.md).

## Considered and not chosen

* **Content in the image.** One thing to deploy instead of two, but every correction of a text would be a build of
  the program, and an image would have to be kept from containing working files by care instead of by construction.
* **`ASPNETCORE_FORWARDEDHEADERS_ENABLED`.** Tried first. The framework no longer believes unknown senders with
  it, and where it did, it believed everybody.
* **Checking the headers in the tests of the repository only.** They are checked there too; but the first run
  from outside found two things the tests inside could not: the proxy was not believed, and the cookie of the
  forms was not `Secure`.

## Consequences

* The repository has a `Dockerfile` for Dastan, a Compose file to try the image on a development machine (in the
  Compose project `test`), a script of checks and a page of operations.
* A host has four things to give the container: the content folder, the proxies to believe, optionally a volume
  for the keys, and a proxy with a certificate.
* The privacy matrix records what the site keeps about visitors when it runs this way: nothing, and that the
  logs of a proxy and a host are theirs.

## Not decided

A host, an address, a certificate, monitoring, backups, a registry for the image, several containers at once,
load. See "What is not decided or not tried" in [operations](../operations.md).

## Verification

2026-11-17, Docker Desktop on Windows, linux/amd64, .NET 10 images: the image was built from the repository
(345 MB); a revision of the first collection, made with the `revision` command from a copy reviewed for the test,
was mounted; the container ran as user 1654 on a read-only file system with 512 MB and reported "healthy";
`Check-Site.ps1` passed all its checks against it, after it had failed on the proxy and on the cookie of the
forms and both were corrected; a form opened before a restart was refused after it without `Dastan:Keys` and
accepted with it. `SiteHealthTests` (Web) and `ContentRevisionTests` (Data: a revision of the collection starts
a site that has the content folder and no document of the editors). The revision of the trial is test data;
nothing is published and nothing was deployed.
