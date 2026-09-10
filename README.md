# Star Catcher — Unity Web prototype

A small single-player arcade game built with Unity and C#, designed for a browser release served by Nginx in Docker. Move a ship, collect stars, avoid meteors, and score points within 60 seconds. The browser interface is Persian; the Unity Editor play interface is English.

**Status: locally verified v0.1.0 prototype.** The Unity Web build and local Docker deployment succeeded on 2026-09-09. Browser checks covered loading, scoring, damage, pause/resume, both ending conditions, restart, pointer input and responsive layouts. Physical mobile-device testing and public publication remain pending. No public demo URL or GitHub release has been created.

Local game: **http://localhost:8088** while the Docker service is running. The service binds to loopback only, as requested.

## Requirements

- Unity Editor **6000.3.22f1** and its matching Web Build Support module.
- An activated Unity license appropriate for your use.
- Docker Engine/Desktop with Compose to serve the exported build.
- A modern browser supporting WebGL 2 and WebAssembly.

Players need a compatible browser; Unity and Docker are required on development/hosting machines only. The included analytics service records anonymous game rounds locally; it is not an account system or multiplayer server.

## Run in Unity

1. Add this directory as a project in Unity Hub and open it with the pinned Editor version.
2. Open `Assets/Scenes/StarCatcher.unity`. On a clean source checkout without the scene, use **Star Catcher → Prepare scene**.
3. Enter Play mode and click **START GAME**. Move with Left/Right or A/D; Space pauses/resumes.

## Build and serve

1. Install **Web Build Support** for Editor 6000.3.22f1 through Unity Hub. An Android module does not provide Web builds.
2. In the Editor choose **Star Catcher → Build Web**. Output is written to `Web/`.
3. From this project directory run:

```sh
docker compose up -d --build --wait
```

4. Open `http://localhost:8088`. Stop the server with `docker compose down`.

Open `http://localhost:8090/admin` on the host computer to see the local analytics dashboard. It lists up to 200 recent rounds, device category/browser, a masked IP address, duration and score. The dashboard port is bound to `localhost` and is not published through the public game link.

The Docker image packages an **already built** `Web/` directory. It does not install Unity or compile C# in Docker. Rebuild in Unity before rebuilding the image when gameplay changes. The matching GitHub Pages copy lives in `docs/` and must be refreshed from `Web/` before publishing a new build.

## Documentation

- [Architecture and browser bridge](docs/ARCHITECTURE.md)
- [Deployment without a static IP](docs/DEPLOYMENT.md)
- [Validation and release checklist](docs/TESTING.md)
- [Roadmap](docs/ROADMAP.md)
- [Persian quick start](docs/QUICKSTART.fa.md)
- [Change log](CHANGELOG.md)
- [Recorded validation results and known warnings](docs/VALIDATION-2026-09-09.md)

## Repository practice

Commit `Assets/` including `.meta` files, `Packages/`, `ProjectSettings/`, documentation, and deployment configuration. Do not commit `Library/`, generated IDE files, local secrets, Unity license files, or built Web payloads. Use focused commits and tagged releases only after validation. Select a source-code license before public distribution; no license choice has been made for the project yet. Unity and third-party components retain their own license terms.

Development assistance: this prototype was prepared with AI coding assistance; inspect and understand the implementation, validate releases, and describe your own contributions accurately in your portfolio.
