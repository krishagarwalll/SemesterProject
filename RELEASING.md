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

## Tooling notes

The npm scripts are thin wrappers around official tools:

- Unity builds use the Unity Editor command-line interface with `-batchmode`, `-projectPath`, `-activeBuildProfile`, `-build`, and `-logFile`.
- itch.io uploads use the official `butler` CLI.

There is no separate official `unity build` npm-style CLI. The supported Unity automation entry point is the Unity Editor executable for the installed editor version:

- macOS: `/Applications/Unity/Hub/Editor/<version>/Unity.app/Contents/MacOS/Unity`
- Windows: `C:\Program Files\Unity\Hub\Editor\<version>\Editor\Unity.exe`
- Linux: `/home/<user>/Unity/Hub/Editor/<version>/Editor/Unity`

The wrappers keep those commands consistent across machines, read the project Unity version from `ProjectSettings/ProjectVersion.txt`, and fail early when a build profile or executable is missing. Set `UNITY_EXE` if Unity Hub is installed somewhere non-standard.
