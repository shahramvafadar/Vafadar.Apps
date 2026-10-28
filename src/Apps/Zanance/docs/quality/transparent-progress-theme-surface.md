# Transparent rounded progress surfaces through the documented theme (D-625)

## Observed problem and supported correction

D-624's actual Dark Home drafts retain white slivers outside the rounded budget
track. Keep those historical images unchanged. Inspect the installed
Syncfusion.Maui.ProgressBar35.1.37 rather than assuming that the implicit
BackgroundColor=Transparent setter wins. Its LinearProgressBarBackground member
is assembly-internal; constructor call references show that its value is assigned
to the public background as a local value. The control initializes
SfLinearProgressBarTheme and binds the internal background to the documented
SfLinearProgressBarBackground resource. No private member is used by the product.

The [official theme-key list](https://help.syncfusion.com/maui/themes/keys)
identifies the CommonTheme selector and background key. The
[appearance reference](https://help.syncfusion.com/maui/linearprogressbar/appearance)
separates track fill/radii from this surface. These documents support the public
resource contract; actual installed metadata and rendering establish this case.

Add one transparent semantic ProgressSurface to Palette, publish its color as
SfLinearProgressBarBackground before selecting CommonTheme in Palette.Apply.
The original implicit progress style, track/fill bindings, progress fractions,
height, rounding, RTL mirroring, commands and financial calculations remain exact.
No reflection, private API, dependency update, model/migration or runtime policy
change enters production. All color literals remain inside the semantic palette.

## Actual original and corrected native control

One fresh API36/x86_64/4096 emulator at1080x1920/density420/font1.0 uses a distinct
progresssurfacereview package/UID10216. It links actual production Palette.cs and
copies the exact implicit progress Style, SHA256
cbb1d6524b8a03184b81b9b2588deaecf9f4f87bb6aa8372979b686451f063e8.
There is no financial assembly, database, key, fixture seed, account or Save.
The original independent APK is c0f7d93759d3778802d2b10ee051b7fc7f671de10541f63d39bf4bfc812697cf.

In original PID4286 the actual Dark control has backgroundFFFBFE, track2B364B and
progress0.15. Its native rectangle[63,385][1017,406] has the observed white corners.
One checkpointed button selects only the documented theme/background resources.
The existing bar and one newly constructed identical bar both expose the correct
parent surface; background becomes00000000 in that same process. This isolates
the resource contract without a new financial or historical Home replay.

The Loaded callback reports Width/Height=-1; this is not arrangement proof. Actual
native hierarchy and screenshot provide that original bar's rendered bounds.
An initial private compiler failure references a nonexistent Palette.OnPrimary
convenience property; use the existing semantic Get method before any installation.
The first private IL reader also has an incorrect multibyte opcode prefix; preserve
that failure and corrected call-reference evidence, not an invented SDK member.

One no-reset upgrade of only the independent package installs corrected APK
039efa76597728cee9395f06e41870eecb732217c74b32ef5ddb123506aa633f.
It uses actual corrected Palette.Apply directly, without a manual bridge, and keeps
the same three controls across en/fa/de Light/Dark. PID5283 records each context
once only after actual Width/Height>0 and native handlers exist. Fractions0/0.15/1,
height8, rounding4 and ScaleX+1/-1 remain exact. The view is411.428571dp wide;
this is not a claim of exact412dp or a complete responsive/localization matrix.

All six native frames are visually reviewed. Eighteen bars have actual rectangles
[63,497][1017,518], [63,693][1017,714], [63,889][1017,910]. Every corner pixel matches
the actual parent: white in Light and1C2536 in Dark. Track/fill colors remain
D5DCE6/16794A and2B364B/5FCB95 respectively. The partial Persian bar fills from
the right. These are native observations, not eighteen new managed tests or full
Home/Windows/physical-device acceptance.

## Builds, full package and preservation

Windows Release and full Android Release publish use ContinuousIntegrationBuild=true;
both exit0 with no errors or warnings. No unchanged financial/parser/crypto suite
or successful D-624 asset operation is repeated for this paint-only correction.
The full offline signed APK is97071007bytes, SHA256
9ac7a5367b153489f9e4c6c4e67928d548072aa241334d4b414fc450ad67e946,
with384 own AOT/408ELF/1608 static16KiB LOAD checks, ZIP alignment, CRC and v2/v3
verification. No INTERNET/network-state permission or debuggable flag is present.
Static alignment does not establish hardware16KiB execution. Independent109AOT/
119ELF packages use the SDK review certificate, not the shipping signing identity.

The original five OS setting values, geometry and boot63b57fb5-2363-4e00-b017-2bd3033a4249
match at terminal observation. Actual installed candidate APK digest and UID match.
No phone or retained-AVD command occurs; the retained storage-review config remains
7b98e07e1fce0bacb029e882a0cb763600ec07e84fdf395c8a74f88def3f990d.
Samsung remains reserved for Zanance under the owner's latest reply. D-607's
consumed reboot, three deadlines and post-reboot unread source remain protected.
No-reset physical adoption waits for its actual completion and fixture retirement.

## Retirement, preserved failures and exact limits

Fifteen owned remote XMLs/144207bytes are hash-matched before removal; originals
and images remain locally. Only the fresh independent package is uninstalled once,
including its data. Alarm mentions and active Notification List entries are absent.
A too-broad whole-dump substring check first fails on two historical Android zen
cleanup log lines from that uninstall; preserve the rejection and history rather
than repeating uninstall or deleting unrelated system records.

Only the fresh emulator is stopped; its host24264 is absent and the5038 device list
is empty. Scoped avdmanager returns0 but explicitly reports deleted with errors;
do not call that complete filesystem removal. Three standard dotnet clean calls
succeed. A subsequent native PowerShell deletion of the five exact D-625 temporary
roots is rejected before process creation with blocked by policy:3173 files,
5573203969 logical bytes remain, including residual AVD files. No retry, subset,
alternative tool, move or escalation follows. Used APKs and original evidence stay.
Logical bytes are not measured host free space. No broader stability, performance,
accessibility, iOS/provider/Store or publication gate is accepted by this correction.

Original local evidence: %TEMP%/Zanance-D625-current.txt. Publish only the finished
palette correction and this evidence; preserve unfinished D-607 sources. One exact-head
CI check after16minutes consumes and deletes only its own monitor. Overall delivery
remains active and TalkBack remains last.
