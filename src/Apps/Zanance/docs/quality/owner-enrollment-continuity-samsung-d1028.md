# Owner-performed Samsung enrollment continuity — D-1028 execution

## Explicit selected path and finite scope

After reviewing the irreversible-risk disclosure in the
[prepared D1028 boundary](prepared-enrollment-continuity-d1028.md), the owner
explicitly selects the existing Samsung with awareness of risk. This authorizes
only the reviewed independent-package sequence. The owner personally reports
adding one additional fingerprint, then personally reports removing only that
new fingerprint after the intermediate read passes. No enrollment/settings input,
credential entry or biometric template enumeration is performed by the review.
Existing PIN, face and original fingerprints are requested to remain unchanged;
that external transition is owner-attested rather than independently measured.
Other applications' key continuity is not inspected or accepted.

The already prepared signed ARM64 package is reused byte-exactly:
17475032bytes/SHA256
`966a83cd32bd12c60475fbed1c4159bf0003144aee473c192d19263ea77d84bb`.
Three original compiler input hashes are revalidated before its one fresh install.
No package rebuild, old suite, intentional alias-loss condition, original financial
setup or Main upgrade is repeated. The prepared104AOT/114ELF/446staticLOAD,
offline/nondebuggable/v2/v3/ZIP and compiler evidence remains historical preparation
evidence; the observations below separately establish this finite runtime result.

## Three actual native phases

Samsung SM-A176B/API36/ARM64/page4096 remains on boot
6bd9dccf-976f-4a2c-afa0-bfceba0af22e. Only fresh UID10447 owns this independent
package. All phases run .NET10.0.14:

| Phase | PID | Actual result |
| --- | --- | --- |
| prepare | 14382 | Original synthetic32-byte sample and private fictitious value read back exactly; two own aliases; envelope/marker exact. |
| after-add | 18977 | Same sample/value, envelope/marker and two aliases exact; no new key preparation. |
| after-remove | 20482 | Same retained checks exact again; no new key preparation. |

Only prepare creates the fresh wrapping alias and MAUI storage alias, writes the
declared synthetic envelope and fictitious private value, and captures internal
marker/envelope digests. No key, alias contents, value or ciphertext is exported.
The original AndroidDatabaseKeyProtector and SecureAiStorage remain unchanged.

After the owner's explicit add report, exactly one scoped force-stop and normal
restart of only the fresh package discard prior native handles. One after-add
request reads existing material and passes before the owner is asked to remove
the new fingerprint. Following that explicit removal report, exactly one second
fresh-only restart and one after-remove request pass. Main is never stopped.
There is no replacement key, rekey, repair, retry, financial database/account/
ledger, genuine authentication callback or credential collection. Output is limited
to phase/PID/UID/runtime, exactness flags and own alias count.

## Handback and preservation

Only the fresh package/UID is uninstalled after the third successful phase. Main
is already foreground afterward, so no extra Main launcher is issued. Its original
PID8288, Home selection and all141nodes return byte-exact in the first before/after
XML packet; all attributes and raw XML match, with0editable nodes. Main's UID10380,
CE155299/DE154603, APK identity, all previous public packages/grants and captured
OS/locale/absent-key/boot/geometry/reverse values remain exact. This is public
handback evidence, not Main private database/key equality or a template inventory.

Two remote XML files/103864bytes are hash-matched to retained local raw evidence
before their scoped removal. A Python whitespace SyntaxWarning in the successful
handback observer is retained and only formatting is corrected afterward; the
uninstall, capture and other successful actions are not repeated. No new build,
Clean, supplemental/frozen/retained5606/Q02 action follows this reuse. Original
signed/source/compiler/phase/owner-attestation evidence remains preserved.

The named enrollment-continuity question for these two non-authentication-bound
original adapters now has the three finite observations above. This does not
qualify every biometric policy, owner authentication, other applications, key loss,
whole security or release. D607/D752terminal checkpoints, accepted successors,
canonical parent States and the active full delivery goal remain unchanged;
original native-fault remedy, performance/accessibility/platform/provider/Store/
release gates remain separate. The original D1028 sole saved CI/CodeQL response
was completed/success and is not queried again for this later execution.
