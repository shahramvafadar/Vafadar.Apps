# Fixed public receipt-corpus observation (D-501 / QA-05 remains partial)

## Source and finite selection

Use the first ten official test rows of CORD v2, without selecting by recognition outcome.
Source: [CORD project](https://github.com/clovaai/cord), Park et al.,2019;
[official v2 distribution](https://huggingface.co/datasets/naver-clova-ix/cord-v2).
The publisher provides Indonesian receipt photographs with blurred identifying regions and
annotated totals. These Latin-script samples do not establish all six product languages,
Persian image OCR, merchant extraction, a fresh camera acquisition or target-country coverage.

The original project declares [CC-BY-4.0](https://github.com/clovaai/cord/blob/master/LICENSE-CC-BY).
Retain its original licence and source/author attribution with the local input and independent
APK. No endorsement is claimed. Images/annotation responses remain outside the repository and
shipping app; no model is trained, remote code executed, account created or document uploaded.
A small public metadata response and ten original JPEGs total1,140,680image bytes. Original
metadata bytes and per-image/annotation digests are retained privately. All ten original images
are visually inspected; nine have an annotated final total, while row9 has only subtotal/payment/
change evidence. Public data is not an owner receipt or owner financial/account data.

## Original reader and parser, measured separately

An independent AndroidHome receipt-corpus phase uses the actual unchanged DocumentReader and
ReceiptParser. An opt-in local ReceiptCorpusRoot supplies only manifest, ten fixed test-NNN.jpg
files, licence and attribution. Native byte-size/name/SHA checks precede recognition; no expected
amount or dataset annotation is supplied to recognition or total parsing. Original bounded image
preparation and OCR line reconstruction remain, including the4096-pixel source's3200-pixel bound.
Captured injected TimeProvider supplies the observation day. A prequalification fixed-date
helper build is superseded before any install/run; qualify only the final dated-observation build.

A desktop original-Core pass over annotated source text produces eight Found, one Review and
one NotFound. It is a perfect-text parser control, not OCR or a new managed test cohort.
Actual Samsung SM-A176B/API36/ARM64/4KiB runf4ec3c3c12cd4dfdbaf332ef50d12132,
comparisonfa2810d6a81943848ee0af52ef9126bc/process30506 reads each image once and persists its
original recognized text/layout/semantic candidates. No retry, re-recognition, seed or financial
Save repairs an observation. Source annotation amounts are compared afterward, independently of
the reader. Found denotes the parser's semantic status, not a probabilistic OCR confidence score.

| Official test row | Annotated final amount | Native parser status | Suggested amount |
| --- | ---: | --- | ---: |
| 0 | 60,000 | Review | none |
| 1 | 91,000 | Found | 91,000 |
| 2 | 28,000 | Review | none |
| 3 | 11,000 | Found | 11,000 |
| 4 | 174,600 | NotFound | none |
| 5 | 31,000 | Found | 31,000 |
| 6 | 46,000 | Found | 46,000 |
| 7 | 111,000 | Review | none |
| 8 | 50,000 | Found | 50,000 |
| 9 | no final total | NotFound | none |

Five of nine labelled totals are suggested correctly; three require review and one is missed.
All five Found numbers match their annotation. The no-total negative remains NotFound; do not
infer its amount from payment-minus-change or a subtotal. No false Found amount is observed in
this small selection; this is not a broad accuracy/safety guarantee or acceptance threshold.
Currencies/units and user's original confirmation remain separate: unitless candidates do not
establish a receipt currency, foreign conversion or permission to save to a selected account.

Preserve original refusals: row0's damaged discount/total evidence, row2's count label recognized
as ITL1S alongside the purchase total, row7's damaged numeric text, and row4's low-contrast missed
total. Do not repair recognized digits or exclude a conflicting count merely to fit annotations.
No product change is justified from these safe abstentions alone. Remaining real six-language/
camera/scanned-PDF corpora and quality improvements stay open. Median154.94ms/max600.11ms cover
bounded preparation/recognition/reconstruction only; not app startup, picker/editor latency,
all receipt conditions or a speed objective. Complete quality is not accepted.

## Build, privacy and restoration

Final strict signed Offline ARM64 caller passes with zero warnings/errors,192AOT modules,
five unchanged original native engines, v2/v3 and no INTERNET/debuggable declaration. APK SHA256:
c7c3687af9b0b36e67312ae2a72dd6ee0fbd490ea0f8b9f0435fda8899bde83a (57320817bytes).
All ten image digests, original licence and manifest match their packaged assets. No SDK/model,
shipping app source, financial writer, permission, schema or public service changes. Existing
Windows/shipping builds and successful managed/native financial cohorts are not repeated.

Only this independent phase opens the public corpus assets and original on-device reader/parser;
no Main file/account/key/attachment/backup or financial service is opened by the phase. Restore
the old independent APK/data/stopped state and Main25043/Home979/all141 public tuples/APK/OS.
Two exact owned remote XMLs are hash-matched/removed, with local originals retained. No profile
or ledger entry is created, no new permission/OS setting is changed and no real authentication
occurs. Public UI equality is not private Main database/key equality. Corpus reports remain only
in the owned independent diagnostic source; original photos leave the installed APK on restoration.

D-494 remains24914/four same resumed pending native alarm/intent identities with no delivery yet;
no provider read, rearm or subject replacement. D-496's inactive future-day source remains.
Main shipping remains D-495; later integration of D-497/D-499/D-500 awaits timed-cohort completion.
QA-05 and security/stability/spoken/iOS/provider/Store/publication remain open.

The one delayed4de95cfb response reports success for CI40004246720, CodeQL40004246807,
service40004246802 and identity40004246884. Its consumed monitor is deleted; no second request.
New-head CI follows push. These ten observations are not ten unit tests or complete product QA.
