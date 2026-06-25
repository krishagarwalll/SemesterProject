# Releasing to itch.io

This project pushes builds to [niclas-rogulski/how-to-get-to-heaven](https://niclas-rogulski.itch.io/how-to-get-to-heaven)
via [butler](https://itch.io/docs/butler/pushing.html). Versions come straight from `PlayerSettings.bundleVersion`
(Project Settings > Player). No zip files are created — butler pushes the raw `Build/<Platform>/` folder directly.

## One-time setup

1. **Create the three Build Profiles** (Unity 6 feature, `File > Build Profiles`):
   - `Assets/Settings/Build Profiles/Windows.asset` — Standalone, Windows x64
   - `Assets/Settings/Build Profiles/MacOS.asset` — Standalone, macOS
   - `Assets/Settings/Build Profiles/WebGL.asset` — WebGL

   Set each profile's scene list to match the scenes you want in the build. Unity has no scripting API to create a
   profile with a target platform pre-assigned, so this step has to be done by hand, once, in the Editor.

2. **Install platform build support modules** via Unity Hub if you haven't: Mac Build Support, WebGL Build Support.

3. **Install and authorize butler**: `npm run setup:butler` (run this yourself, in your own terminal — it opens a
   browser for you to log in to itch.io).

4. **After your first WebGL push**, go to the game's Edit page on itch.io and tag the `web` channel as
   "HTML5 / Playable in browser". This is a one-time dashboard setting; itch can't auto-detect it from the channel
   name the way it does for `windows`/`mac`.

## Everyday usage

| Command | What it does |
|---|---|
| `npm run build:windows` / `build:mac` / `build:webgl` | Headless Unity build via the matching Build Profile, output to `Build/<Platform>/`. |
| `npm run push:windows` / `push:mac` / `push:webgl` | `butler push`es the existing `Build/<Platform>/` folder to the matching itch.io channel, tagged with the current `bundleVersion`. |
| `npm run release:windows` / `release:mac` / `release:webgl` | Runs the matching `build:*` then `push:*` in sequence. |

`build:*` and `push:*` can be run independently — e.g. build once, then push the same build folder again later if you
just want to re-tag a version.

## Troubleshooting

- `Build profile not found: ...` — go do step 1 above.
- `Could not find Unity Editor ... ` — install that Editor version via Unity Hub, or set the `UNITY_EXE` environment
  variable to your `Unity.exe` path.
- `butler.exe not found ...` — run `npm run setup:butler`.
- `Build folder missing or empty: ...` — run the matching `build:*` script first.
