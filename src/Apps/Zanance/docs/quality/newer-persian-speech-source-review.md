# Newer Persian speech candidate review (D-589)

Status: the owner-cancelled1.52.0 attempt remains closed. A genuine newer upstream
source candidate is identified and reviewed; no newer engine is built, installed,
selected or acoustically accepted. The build-tool proposal is now parked by the later owner priority decision D594;
no installation/selection approval is inferred.

## Actual Samsung catalogue, without a download

Under the existing read-only system speech-list authority, navigate from the normal
TTS settings page through Samsung settings to Install voice data. This is the
catalogue entry, not a download confirmation. The actual owned window is
`com.samsung.SMT/com.samsung.SMT.gui.DownloadList`, UID1000. Read the initial page
and six subsequent snapshots to a stable bottom:39 distinct language/region
captions, without Persian/Farsi. Installed-language absence from D586 alone did
not establish catalogue absence; this is a separate concrete inspection of the
catalogue exposed on this phone. Do not generalize to every Samsung region/version
or claim that other vendors have no Persian engine.

No row, download icon, voice, language, engine, store/account page or permission is
selected. An initial observation sees a transient null focus after the one normal
page launch; preserve that refusal and observe the same launch. A first list guard
finds both an outer ScrollView and inner RecyclerView, refuses before input, then
uses the exact actual inner list. Only vertical list scrolling follows. No old
successful language selection or reader gesture is replayed.

Close the three owned pages and return normally to Main. Opening the system page
again normalizes the originally absent default-synth key to Samsung; restore its
original absence, with four bounded stable samples and exact original14-value/
presence verification. Preserve the baseline rather than changing the expected
engine. Main22461/f748/UID10380/CE155299/DE154603/permission flags and original
caller metadata/process absence remain exact. Compare every one of141 original
public Home-node attribute dictionaries, including Home979/English/Light. This is
selected public-state preservation, not a private database/key comparison.

The final host summary initially fails cp1252 encoding on an existing app glyph
after restoration; finish from the already captured XML using UTF-8, with no new
navigation or app capture. Cleanup initially removes nine manifest-matched XMLs
before refusing the differently prefixed Main-return file. Preserve that refusal;
the continuation observes those nine absent without deletion replay and removes
only the remaining independently hash-matched owned file. Total10 remote XMLs/
310990 logical bytes are removed. Local source/provenance and receipts remain.

## Genuine newer official source

The official stable-release endpoint still resolves to1.52.0. Do not relabel,
re-sign or merely raise the target of the blocked old APK. Instead pin the official
upstream development source at:

`ba90c8e9f440ad544f674a790bb5f53878b6ffc5`

The commit metadata date is2026-09-22. The official source archive is22270943 bytes,
SHA256 `2fe0a7f9cd154741dd90ead23c572947600013612500bc3fabc49b2cdd7a20d5`.
Validate archive member scope and selected files against the separately retrieved
pinned raw sources. Android configuration declares1.53.0/code23, minimum21,
compile/target36, NDK29.0.14206865, CMake3.22.1, Gradle9.6.1 and plugin9.3.0.
The manifest requests no permission; the20 Android Java sources contain none of
the reviewed direct network API imports/calls. This is bounded static review,
not a complete security audit or proof of the future merged APK manifest.

The Persian language source is bundled. DownloadVoiceData delegates to
CheckVoiceData.extractVoiceData, which reads the packaged raw ZIP and checks the
canonical output directory. An initial direct ZipInputStream search in the
calling Activity returns false; retain it and inspect the delegated implementation
instead of concluding that data is absent. The engine remains a separate GPL
program, with original notices; it is not linked into or committed as Zanance code.

Official workflow source builds debug and unsigned release artifacts with five-day
retention. That source is not a verified current signed release APK. The official
Gradle checksum and plugin POM endpoints return200, but neither a completed build
nor Android installation acceptance follows from endpoint availability. No source
is executed, built, signed, installed or chosen on the phone in this review.

## Concrete next gate

Review official Android package metadata for platform36, build-tools36.0.0,
NDK29.0.14206865 and CMake3.22.1:974545892 compressed bytes in total. These packages
use android-sdk-license. The earlier compatibility approval names only three other
packages in a different scoped SDK. Present the exact licence and ask the owner
before extending scope into the separate PersianSpeechCandidate SDK directory.
No licence is accepted or new SDK package installed by this review. If approved,
prepare the complete independent package and review its actual digest/signature,
merged permissions/native compatibility before any fresh phone-install/selection
request. An increased target alone does not prove that Play Protect will accept it.

No shipping code/APK, managed-test count, financial state, reader recording or
voice choice changes. Persian speech and broader A11Y/stability/performance,
iOS/provider/Store/public-release gates and the overall goal remain open. The
separate D588 hosted repair check is not replaced with a local green claim.

## Primary sources

- [Official stable release](https://github.com/espeak-ng/espeak-ng/releases/tag/1.52.0)
- [Pinned newer Android configuration](https://github.com/espeak-ng/espeak-ng/blob/ba90c8e9f440ad544f674a790bb5f53878b6ffc5/android/build.gradle)
- [Pinned Android manifest](https://github.com/espeak-ng/espeak-ng/blob/ba90c8e9f440ad544f674a790bb5f53878b6ffc5/android/AndroidManifest.xml)
- [Pinned local data extraction](https://github.com/espeak-ng/espeak-ng/blob/ba90c8e9f440ad544f674a790bb5f53878b6ffc5/android/src/com/reecedunn/espeak/CheckVoiceData.java)
- [Samsung catalogue navigation and separate download step](https://www.samsung.com/us/support/answer/ANS10003701/)
- [Official SDK package metadata](https://dl.google.com/android/repository/repository2-3.xml)


## Later execution order (D-594)

The owner defers further TalkBack behind every other available task and gives
Persian speech no current priority. Park this proposal and its question; do not
install tools/voices, chase an answer or replace the cancelled legacy package.
Record a later answer if received without overriding that execution order. Existing
Persian UI/functional translations and the overall delivery goal remain unchanged.
