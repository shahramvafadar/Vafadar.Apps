# Current MAUI Shell disposal-source admission (D1140)

The new [D1139 managed OOM](entry-footer-large-text-interruption-d1139.md) warrants
a specific dependency premise check, not a speculative framework/runtime patch.
The current build resolves Microsoft.Maui.Controls.Core10.0.120 for Android/x64.
Its cached MIT package metadata identifies repository commit
`6aca5366139ed0e1cd05cca3e32b6a887d5a8dd0`.

Two public Android files are fetched at that immutable commit and checked against
the actual package's portable PDB document hashes. The first LF comparison fails;
the retained failure is corrected only by LF-to-CRLF conversion of these public
source files. Complete CRLF bytes then match both compiler document hashes:

| Public compiler input | Exact SHA256 |
| --- | --- |
| ShellContentFragment.cs | `0CECA2D35798C3BDCF7BDB171F6515A178F892AC0843816C9CD1E5DB5BE0BFF9` |
| ShellItemRendererBase.cs | `D5D151F3BD1CBB6F15F7A59AC3C6C05901F62A675E02DECA45E95EB0E1592599` |

This public-source newline admission does not normalize any financial source or
read application IL, private configuration, memory, registers or keys. Actual
build assets also identify the10.0.120 Android runtime assembly path. Package/PDB
source binding is not identity of every linked/AOT product byte or proof that a
particular native branch executed during the OOM.

[PR33426](https://github.com/dotnet/maui/pull/33426) addresses Shell replacement
navigation cleanup. Both complete added blocks already exist: DisposePage's
cleanup/disconnection method and its renderer-side invocation. The pinned
[fragment source](https://github.com/dotnet/maui/blob/6aca5366139ed0e1cd05cca3e32b6a887d5a8dd0/src/Controls/src/Core/Compatibility/Handlers/Shell/Android/ShellContentFragment.cs)
and [renderer source](https://github.com/dotnet/maui/blob/6aca5366139ed0e1cd05cca3e32b6a887d5a8dd0/src/Controls/src/Core/Compatibility/Handlers/Shell/Android/ShellItemRendererBase.cs)
are the exact admitted files.

[PR37113](https://github.com/dotnet/maui/pull/37113), linked to the
[hidden-fragment issue36852](https://github.com/dotnet/maui/issues/36852), changes
absolute-route removal. The current method contains its lazy transaction and
parent/current-fragment guards, with the old stack-count early return absent.
Later fragment bookkeeping differs, so whole-method identity is not claimed.
The premise that either of these specific changes is simply missing from the
current dependency is unsupported. No dependency update, singleton-page workaround
or copied framework patch follows.

Application-side review also finds an explicit await-using Home operation loop;
HomeReadSession clears its context/validator and awaits disposal. This rules out
only a literal absence of that disposal code, not every live lease, retention
path or allocation origin in the failed execution. Source inspection cannot
replace the missing OOM stack or actual lifetime measurements.

Raw primary responses, pinned public sources, compiler checks, build-assets
admission and finite block checks remain under
`artifacts/d1140-maui-shell-disposal-source`. No build/test/device/financial,
runtime/GC, frozen-scope or publication action occurs in this source-only step.
The [separate actual Home-root question](home-root-undo-native-admission-d1141.md)
is independently versioned; the20remaining matrix contexts, original OOM cause
and broader delivery gates remain open.
