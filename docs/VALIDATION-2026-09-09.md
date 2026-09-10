# Local validation — 2026-09-09

## Environment

- Windows 11; Unity Editor 6000.3.22f1 with matching Web Build Support.
- Unity Web/IL2CPP output; Docker Desktop with Linux containers; Nginx image pinned by digest in Dockerfile.
- Local-only origin: `http://localhost:8088`, published on `127.0.0.1:8088`.
- Interactive checks in the connected Chromium-based in-app browser. Viewports: 1280×900 and 390×844. Small viewport checks do not establish physical Android/iOS compatibility.
- Source is a local working copy; no Git commit, public repository, tagged release or public deployment is asserted.

## Results

| Check | Observed result |
|---|---|
| Unity Web build | `STAR_CATCHER_WEB_BUILD_OK bytes=20818319`; process exited successfully |
| Scene/material/module validation | `STAR_CATCHER_VALIDATION_OK` |
| Editor color parsing after repair | Zero `Parsing PrefColor failed` entries in new interactive log and validation log |
| C# compilation | Zero `error CS` entries in new interactive log |
| Docker service | Running, healthy, loopback port 8088 |
| HTTP files | HTML, WASM, data and framework JS returned 200 |
| WASM MIME | `application/wasm` |
| Browser loading | Unity loaded and Start button became available |
| Scoring | Stars collected; one observed round reached 70 points |
| Pause/resume | Clock remained at 54 while paused, continued after resume |
| Keyboard | Right-arrow input dispatched; Space opened pause state |
| Timer ending | Round ended with 0 seconds and displayed 70 points |
| Damage ending | Another round reached 0 lives with 31 seconds remaining |
| Restart | Score 0, time 60 and three lives restored |
| Pointer | Drag gesture moved the ship in the narrow viewport |
| Sound switch | UI toggled to enabled; auditory quality was not independently assessed |
| Narrow layout | Content width and viewport width both 390; no horizontal overflow |
| Browser errors | No captured error-level console messages during observed checks |

## Payload sizes

| File | Bytes |
|---|---:|
| `Web.wasm` | 13,095,824 |
| `Web.data` | 3,432,059 |
| `Web.framework.js` | 392,714 |
| `Web.loader.js` | 26,983 |

The build summary includes more than these runtime files. Compression is currently disabled; optimize and measure in a later release.

## PrefColor issue and repair

Unity's Mono environment used `/` as the fa-IR decimal separator when serializing color preferences; examples were `0/8`. Unity's reader expects invariant numeric formatting. The repair backed up and normalized 27 affected color preference entries, preserving their numeric values. No global Windows locale setting was changed and no unrelated preferences were reset.

`EditorNumberFormat.cs` normalizes numeric formatting only within this Editor process. Original machine-specific preference backups remain outside the repository. Reference: [Unity PrefColor source](https://github.com/Unity-Technologies/UnityCsReference/blob/master/Editor/Mono/Settings.cs).

## Known warnings and pending work

- Legacy Input Manager deprecation: migrate to Input System in v0.2.0.
- Unity was opened with Administrator privileges in this environment. Prefer a normal user launch for subsequent editing; no Windows privilege policy was changed.
- Physical mobile devices, browser versions outside the observed environment, automated C# gameplay tests, measured load capacity, restart recovery and public access are not yet verified.
- The supplementary Python browser test script is provided but has not been run. Its presence is not evidence of an automated passing suite.
