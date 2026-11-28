# The library – the collections written to be published

This folder holds the real collections of Dastan, as they are written and before they are reviewed:

```
samples/        one file for each collection, and its translations beside it
comparisons/    comparisons of lands over time, and their translations
media/          files of pictures whose rights are established – nothing else
kinds.json      the list of kinds records may name (ADR 0029), and kinds.fa.json its Persian
```

It has the same shape as [`content/`](../content), and the site reads it the same way when it is started with
`--Dastan:Content=<this folder>`. The difference is what each is for: `content/` is the small pilot the tests
of the software are written against and is not changed by content work; everything that is written to be
read by visitors goes here.

* What is to be written, and how: [the work order for content](../docs/content-work-order.md) and
  [the guide for writers](../docs/content-guide.md).
* Nothing here is reviewed or released by who writes it. A public site shows a statement only after the
  editors reviewed it and published a revision (ADR 0009).
* These are working files of the editors. They are not served by the site and are in no image of it
  (`Dockerfile.dockerignore`, `Dockerfile.studio.dockerignore`).
* Check the whole folder as the site reads it:

  ```powershell
  dotnet run --project src/Apps/Dastan/Vafadar.Dastan.Content -- check src/Apps/Dastan/library
  ```
