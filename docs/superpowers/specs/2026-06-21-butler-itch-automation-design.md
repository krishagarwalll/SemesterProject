# Butler / itch.io build & push automation

Status: approved
Date: 2026-06-21

## Goal

Standardize Unity builds into `Build/Windows/`, `Build/MacOS/`, `Build/WebGL/`, and add npm scripts that build each platform headlessly and push it to `niclas-rogulski/how-to-get-to-heaven` on itch.io via butler, using `PlayerSettings.bundleVersion` as the single source of truth for version numbers. No zip files are produced — butler diffs raw build folders directly (confirmed against itch's own docs), which is faster on repeat pushes and is butler's documented/recommended workflow.

## Manual one-time setup (cannot be scripted)

Unity 6's `UnityEditor.Build.Profile.BuildProfile` class has no public scripting API to create a brand-new profile and assign it a target platform — Unity's own docs and forum guidance say to do this via `File > Build Profiles` in the Editor UI. So, once:

1. Create three Build Profile assets via `File > Build Profiles`:
   - `Assets/Settings/Build Profiles/Windows.asset` → Standalone, Windows x64
   - `Assets/Settings/Build Profiles/MacOS.asset` → Standalone, macOS
   - `Assets/Settings/Build Profiles/WebGL.asset` → WebGL
   - Each profile's scene list should match the current "Scenes in Build" list (or be customized later per-profile if needed).
2. Install the Mac Build Support and WebGL Build Support modules via Unity Hub if not already installed.
3. Run `npm run setup:butler` once — downloads `butler.exe` from itch's official broth URL (`https://broth.itch.zone/butler/windows-amd64/LATEST/archive/default`) into `tools/butler/` (gitignored), then runs `butler login` (opens a browser to authorize; stores the API key in butler's own config dir, not in the repo).

## Build step (per platform)

Unity 6 has native CLI flags for building from a profile (confirmed against the Unity 6.3 manual's Editor command-line arguments page) — no custom Editor C# script is needed:

```
Unity.exe -batchmode -quit -nographics -projectPath <repoRoot> \
  -activeBuildProfile "Assets/Settings/Build Profiles/Windows.asset" \
  -build "Build/Windows/How to Get to Heaven.exe" \
  -logFile <repoRoot>/build-windows.log
```

- `npm run build:windows` / `build:mac` / `build:webgl` each invoke `scripts/build.js <platform>`.
- `scripts/build.js`:
  1. Resolves the Unity Editor executable: env var `UNITY_EXE` if set, else computed from `ProjectSettings/ProjectVersion.txt`'s `m_EditorVersion` against the standard Hub install path (`%ProgramFiles%\Unity\Hub\Editor\<version>\Editor\Unity.exe`). Errors clearly if not found.
  2. Resolves the output path per platform: `Build/Windows/<productName>.exe`, `Build/MacOS/<productName>.app`, `Build/WebGL` (a directory — WebGL builds are a folder of files, not a single executable). `<productName>` is read from `ProjectSettings/ProjectSettings.asset`.
  3. Spawns Unity synchronously with the flags above, streaming stdout/stderr live, and exits with Unity's own exit code (non-zero on build failure) so the command fails loudly.

## Push step (per platform)

- `npm run push:windows` / `push:mac` / `push:webgl` each invoke `scripts/push.js <platform>`.
- `scripts/push.js`:
  1. Reads `bundleVersion` from `ProjectSettings/ProjectSettings.asset` (currently `1.0`).
  2. Verifies `Build/<Platform>/` exists and is non-empty (fails clearly if a build hasn't been produced yet).
  3. Resolves `butler.exe`: env var `BUTLER_EXE` if set, else `tools/butler/butler.exe`.
  4. Runs:
     ```
     butler push Build/<Platform> niclas-rogulski/how-to-get-to-heaven:<channel> \
       --userversion <bundleVersion> \
       --ignore "*_BurstDebugInformation_DoNotShip/**" \
       --ignore "*.pdb"
     ```
  5. Channel names: `windows`, `mac`, `web` — chosen because itch.io auto-tags platform from the substrings `win`/`windows` and `mac`/`osx` in the channel name (confirmed from butler docs); `web` has no auto-tag substring, so after the very first WebGL push you tag that channel "HTML5 / Playable in browser" once on the itch.io Edit Game page (a one-time dashboard setting, not part of the push itself).

## Convenience chains

- `npm run release:windows` = `npm run build:windows && npm run push:windows` (same for `mac`/`webgl`). `build:*` and `push:*` remain independently runnable.

## File layout (new)

```
package.json                          # npm scripts (private:true, no published package)
scripts/
  build.js                            # build:<platform> entrypoint
  push.js                             # push:<platform> entrypoint
  setup-butler.js                     # setup:butler entrypoint
  lib/
    find-unity.js                     # resolves Unity.exe path
    find-butler.js                    # resolves butler.exe path
    project-info.js                   # reads bundleVersion/productName/editor version from ProjectSettings/*
    itch-config.js                    # itch user/game slug, channel names, build profile paths, output dirs
tools/butler/                         # downloaded butler.exe + DLLs (gitignored)
Assets/Settings/Build Profiles/       # Windows.asset, MacOS.asset, WebGL.asset (created manually in-Editor)
```

`.gitignore` additions: `node_modules/`, `/tools/butler/`.

## Error handling

- Missing Unity Editor at the expected Hub path → clear error naming the expected path and the `UNITY_EXE` override.
- Missing build profile asset for a platform → clear error naming the expected `Assets/Settings/Build Profiles/<X>.asset` path.
- `build:*` failing (non-zero Unity exit code) → script exits non-zero, log file path is printed for inspection.
- `push:*` with no `Build/<Platform>` folder or an empty one → refuses to push, clear error.
- Missing `tools/butler/butler.exe` → clear error pointing at `npm run setup:butler`.

## Out of scope

- No CI/CD (GitHub Actions etc.) — local/manual invocation only, per current request.
- No zip artifacts of any kind.
- No automated creation of the Build Profile assets themselves (Unity API limitation, documented above).
