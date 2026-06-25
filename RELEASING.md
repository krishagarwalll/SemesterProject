# Releasing to itch.io

Pushes to [niclas-rogulski/how-to-get-to-heaven](https://niclas-rogulski.itch.io/how-to-get-to-heaven) via butler. Version = `PlayerSettings.bundleVersion`. No zips.

## One-time setup

1. `File > Build Profiles` — create `Windows.asset`, `MacOS.asset`, `WebGL.asset` under `Assets/Settings/Build Profiles/`.
2. Install Mac/WebGL Build Support modules via Unity Hub.
3. `npm run setup:butler`
4. After first WebGL push: tag the `web` channel "Playable in browser" on itch's Edit Game page.

## Usage

| Command | Does |
|---|---|
| `build:windows` / `build:mac` / `build:webgl` | Headless build to `Build/<Platform>/`. Close the Editor first — it'll hang otherwise. |
| `push:windows` / `push:mac` / `push:webgl` | `butler push` the build folder. |
| `release:windows` / `release:mac` / `release:webgl` | build then push. |
| `setup:butler` | one-time install + login |

`UNITY_EXE` / `BUTLER_EXE` env vars override auto-detected paths.
