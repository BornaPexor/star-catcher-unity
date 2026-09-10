# Validation and release gate

## Current evidence

- Unity 6000.3.22f1 opened this project and generated the saved scene/materials.
- `STAR_CATCHER_SETUP_OK` in the Editor log is the setup completion marker.
- The matching Web support module is installed. The Web build succeeded and the local Docker container is healthy.
- Editor reference/material/color validation and browser gameplay checks passed as recorded in [the dated evidence](VALIDATION-2026-09-09.md).
- No automated C# gameplay test suite exists yet. Tests from any earlier HTML prototype are not evidence for this Unity implementation.
- `tests/browser_smoke.py` is a supplementary automation script for future use; it was not executed in this session. Actual browser verification used the connected browser. Do not label the script itself as passing.

## Required before v0.1.0

1. Editor: compile without errors, enter Play, start, collect a star (+10), hit a meteor (-1), verify the damage cooldown.
2. Editor: test both movement directions, screen boundaries, pause/resume, zero-life ending, timer ending and a fresh restart.
3. Build: install the exact Web module, run `Star Catcher → Build Web`, require `STAR_CATCHER_WEB_BUILD_OK`.
4. Server: run `docker compose config --quiet`, build/start with `--wait`, require a healthy container and HTTP 200 at `/health` and `/`.
5. Browser: confirm the loader completes, JS/WASM/data requests succeed, start/pause/restart and HUD synchronization work without runtime errors.
6. Input: test keyboard and real touch input, pointer release outside the canvas, tab switches and browser resizing/orientation changes.
7. Devices: record actual browser/version/device results for desktop and mobile. Do not infer compatibility from responsive CSS alone.
8. Operations: restart Docker and verify recovery; verify any public URL from a separate network.

Record pass/fail, date, exact commit, environment, observed build size and limitations. Physical-device and restart-recovery checks remain outstanding. A public tagged release must disclose these gaps or finish them first. A future CI workflow should add C# model tests and licensed Unity build validation; an empty workflow badge is not a substitute.
