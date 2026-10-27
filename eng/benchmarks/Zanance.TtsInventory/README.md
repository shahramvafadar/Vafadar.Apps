# Installed speech-engine metadata review (D-586)

This independent API36/ARM64 package is not referenced by the product. It has no
Activity, service, requested permission, Internet, microphone or accessibility
component. Only a receiver protected by Android's privileged DUMP permission
accepts one canonical request per process. It reuses the actual current-candidate
device/public-APK admission policy before connecting and checks the same installed
generation afterward. Never use it to inspect Main databases, keys or preferences.

Build a complete signed offline package through the normal build script with a
separate `ArtifactsPath`:

```powershell
./eng/scripts/Build-AndroidApk.ps1 -Project eng/benchmarks/Zanance.TtsInventory/Vafadar.Zanance.TtsInventory.csproj -Offline -Output artifacts/speech-inventory-package
```

The client reads `Voices`, `DefaultVoice`, `DefaultEngine` and Persian
`IsLanguageAvailable` metadata only. It never calls language/voice selection,
synthesis, sample playback, voice-data installation or a settings Activity.
Preserve network/missing-data flags; an advertised voice is not proof of installed
data or intelligibility. Initialization has an eight-second wait and the workload
has cooperative cancellation; native Binder calls themselves are not a guaranteed
wall-clock deadline. An observation timeout must retain the same request/process,
never trigger a restart or duplicate query. Shutdown/disposal and pending-result
completion release only this client's resources.

Actual public-client metadata can differ from the installed reader's engine.
D-586 demonstrates that difference: the ordinary client is denied private-engine
binding and uses Google, while the actual Samsung reader binds private Samsung
SMT. Do not diagnose the reader from another engine's voice catalogue. The owner
separately authorizes read-only system speech-language inspection, not voice
selection/download. See
`src/Apps/Zanance/docs/quality/installed-speech-engine-metadata.md` for actual
results, settings restoration, cleanup limits and remaining Persian acceptance.
