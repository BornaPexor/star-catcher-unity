# Roadmap

This is a proposed sequence, not a list of completed features. Keep each release independently playable and documented.

| Version | Scope | Exit condition |
|---|---|---|
| v0.1.0 | Single-player Web prototype, touch/keyboard, Docker and docs | Editor, Web and Docker release checks pass |
| v0.2.0 | Migrate to Input System; separate round model/input/presentation; configurable balance; C# tests | Tests cover timing, collisions, pause and reset; same gameplay preserved |
| v0.3.0 | Levels and improved art/audio; measured performance | Playtesting and recorded device results; asset provenance documented |
| v0.4.0 | Optional player progress and leaderboard API | Defined data schema, persistence, validation and abuse handling |
| Later | Multiplayer evaluation | A measured transport/latency prototype and explicit authoritative-server design |

For each upgrade: create an issue with acceptance criteria, implement one coherent change, test it, update the change log, and tag the validated release. Keep short development notes explaining decisions and measured results for portfolio discussions.
