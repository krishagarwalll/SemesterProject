# Butler / itch.io Build & Push Automation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add npm scripts that build each platform (Windows/MacOS/WebGL) headlessly via Unity 6 Build Profiles and push the result straight to `niclas-rogulski/how-to-get-to-heaven` on itch.io via butler, using `PlayerSettings.bundleVersion` as the version source — no zip files involved.

**Architecture:** Small CommonJS Node scripts under `scripts/`, split into pure/testable logic in `scripts/lib/*.js` (path resolution, arg-building, project metadata parsing) and thin CLI entrypoints (`scripts/build.js`, `scripts/push.js`, `scripts/setup-butler.js`) that wire the pure pieces together and `spawnSync` the real executables (`Unity.exe`, `butler.exe`). npm scripts in a new root `package.json` are one-line wrappers around these entrypoints.

**Tech Stack:** Node.js (v22, already installed, no new dependencies — uses only `node:fs`, `node:path`, `node:child_process`, `node:https`, `node:test`/`node:assert` for tests). PowerShell's `Expand-Archive` is shelled out to for unzipping butler (Windows-only project, avoids adding an npm unzip dependency).

## Global Constraints

- No zip files are created or pushed for itch.io builds (confirmed butler diffs raw folders directly — see spec).
- Version number for every push comes from `PlayerSettings.bundleVersion`, read from `ProjectSettings/ProjectSettings.asset` — never hand-entered.
- Folder conventions are exactly `Build/Windows/`, `Build/MacOS/`, `Build/WebGL/` (already matches the existing `Build/Windows/` folder in this repo).
- itch.io target is `niclas-rogulski/how-to-get-to-heaven`; channels are `windows`, `mac`, `web`.
- `build:*` and `push:*` must remain independently runnable (not merged into one inseparable command); `release:*` chains them for convenience.
- Unity Build Profile assets (`Assets/Settings/Build Profiles/{Windows,MacOS,WebGL}.asset`) are created manually in the Editor — there is no public scripting API to create one from scratch with a target platform assigned (confirmed against Unity 6.3 docs/forum). Scripts must fail with a clear, actionable error if a profile asset is missing, not try to create it.
- All new scripts are CommonJS (`require`/`module.exports`), matching plain Node with no `"type": "module"` in `package.json`.

---

### Task 1: Project scaffolding — package.json and .gitignore

**Files:**
- Create: `package.json`
- Modify: `.gitignore`

**Interfaces:**
- Produces: an npm project at the repo root with a `test` script (`node --test scripts/`) that later tasks' test files are picked up by automatically (any `*.test.js` under `scripts/`).

- [ ] **Step 1: Create `package.json`**

```json
{
  "name": "how-to-get-to-heaven-tooling",
  "version": "1.0.0",
  "private": true,
  "description": "Build and itch.io push automation for How to Get to Heaven.",
  "scripts": {
    "test": "node --test scripts/"
  }
}
```

- [ ] **Step 2: Add ignores to `.gitignore`**

Append to the end of `.gitignore`:

```gitignore

# Node tooling (butler/itch.io automation scripts)
node_modules/
/tools/butler/
/tools/butler.zip
build-*.log
```

- [ ] **Step 3: Verify**

Run: `npm test`
Expected: exits 0, Node reports no test files found yet (e.g. `# tests 0`). This confirms the test runner is wired up before any test files exist.

- [ ] **Step 4: Commit**

```bash
git add package.json .gitignore
git commit -m "Add npm project scaffolding for build/push automation"
```

---

### Task 2: `scripts/lib/project-info.js` — read version/product info from Unity project files

**Files:**
- Create: `scripts/lib/project-info.js`
- Test: `scripts/lib/project-info.test.js`

**Interfaces:**
- Produces:
  - `getBundleVersion(repoRoot: string): string` — reads `bundleVersion` from `ProjectSettings/ProjectSettings.asset`.
  - `getProductName(repoRoot: string): string` — reads `productName` from the same file.
  - `getEditorVersion(repoRoot: string): string` — reads `m_EditorVersion` from `ProjectSettings/ProjectVersion.txt`.
- All three throw `Error` with a message naming the file and field if the field can't be found.

- [ ] **Step 1: Write the failing test**

Create `scripts/lib/project-info.test.js`:

```js
const test = require('node:test');
const assert = require('node:assert/strict');
const path = require('node:path');
const { getBundleVersion, getProductName, getEditorVersion } = require('./project-info');

const REPO_ROOT = path.resolve(__dirname, '..', '..');

test('getBundleVersion reads bundleVersion from ProjectSettings.asset', () => {
  assert.equal(getBundleVersion(REPO_ROOT), '1.0');
});

test('getProductName reads productName from ProjectSettings.asset', () => {
  assert.equal(getProductName(REPO_ROOT), 'How to Get to Heaven');
});

test('getEditorVersion reads m_EditorVersion from ProjectVersion.txt', () => {
  assert.equal(getEditorVersion(REPO_ROOT), '6000.3.8f1');
});

test('getBundleVersion throws a clear error when the file is missing', () => {
  assert.throws(
    () => getBundleVersion(path.join(REPO_ROOT, 'does-not-exist')),
    /ProjectSettings\.asset/
  );
});
```

- [ ] **Step 2: Run test to verify it fails**

Run: `npm test`
Expected: FAIL — `Cannot find module './project-info'`

- [ ] **Step 3: Write the implementation**

Create `scripts/lib/project-info.js`:

```js
const fs = require('node:fs');
const path = require('node:path');

function readProjectSettingsField(repoRoot, fieldName) {
  const filePath = path.join(repoRoot, 'ProjectSettings', 'ProjectSettings.asset');
  let contents;
  try {
    contents = fs.readFileSync(filePath, 'utf8');
  } catch (err) {
    throw new Error(`Could not read ${filePath}: ${err.message}`);
  }
  const match = contents.match(new RegExp(`^\\s*${fieldName}:\\s*(.+)\\s*$`, 'm'));
  if (!match) {
    throw new Error(`Could not find field "${fieldName}" in ${filePath}`);
  }
  return match[1].trim();
}

function getBundleVersion(repoRoot) {
  return readProjectSettingsField(repoRoot, 'bundleVersion');
}

function getProductName(repoRoot) {
  return readProjectSettingsField(repoRoot, 'productName');
}

function getEditorVersion(repoRoot) {
  const filePath = path.join(repoRoot, 'ProjectSettings', 'ProjectVersion.txt');
  let contents;
  try {
    contents = fs.readFileSync(filePath, 'utf8');
  } catch (err) {
    throw new Error(`Could not read ${filePath}: ${err.message}`);
  }
  const match = contents.match(/^m_EditorVersion:\s*(.+)\s*$/m);
  if (!match) {
    throw new Error(`Could not find field "m_EditorVersion" in ${filePath}`);
  }
  return match[1].trim();
}

module.exports = { getBundleVersion, getProductName, getEditorVersion };
```

- [ ] **Step 4: Run test to verify it passes**

Run: `npm test`
Expected: PASS — `# tests 4`, `# pass 4`

- [ ] **Step 5: Commit**

```bash
git add scripts/lib/project-info.js scripts/lib/project-info.test.js
git commit -m "Add project-info.js to read version/product name from Unity project files"
```

---

### Task 3: `scripts/lib/itch-config.js` — static itch.io/platform configuration

**Files:**
- Create: `scripts/lib/itch-config.js`
- Test: `scripts/lib/itch-config.test.js`

**Interfaces:**
- Produces:
  - `ITCH_USER: string` = `'niclas-rogulski'`
  - `ITCH_GAME: string` = `'how-to-get-to-heaven'`
  - `PLATFORMS: { [key: string]: { channel: string, buildProfile: string, buildDir: string, exeSuffix: string|null } }` with keys `windows`, `mac`, `webgl`.
    - `windows`: `{ channel: 'windows', buildProfile: 'Assets/Settings/Build Profiles/Windows.asset', buildDir: 'Build/Windows', exeSuffix: '.exe' }`
    - `mac`: `{ channel: 'mac', buildProfile: 'Assets/Settings/Build Profiles/MacOS.asset', buildDir: 'Build/MacOS', exeSuffix: '.app' }`
    - `webgl`: `{ channel: 'web', buildProfile: 'Assets/Settings/Build Profiles/WebGL.asset', buildDir: 'Build/WebGL', exeSuffix: null }`
  - Later tasks (`build-args.js`, `build.js`, `push.js`) consume `PLATFORMS[<key>]` and `ITCH_USER`/`ITCH_GAME` exactly as shaped above.

- [ ] **Step 1: Write the failing test**

Create `scripts/lib/itch-config.test.js`:

```js
const test = require('node:test');
const assert = require('node:assert/strict');
const { ITCH_USER, ITCH_GAME, PLATFORMS } = require('./itch-config');

test('itch user/game slugs are correct', () => {
  assert.equal(ITCH_USER, 'niclas-rogulski');
  assert.equal(ITCH_GAME, 'how-to-get-to-heaven');
});

test('PLATFORMS has exactly windows, mac, webgl with the right shape', () => {
  assert.deepEqual(Object.keys(PLATFORMS).sort(), ['mac', 'webgl', 'windows']);
  assert.deepEqual(PLATFORMS.windows, {
    channel: 'windows',
    buildProfile: 'Assets/Settings/Build Profiles/Windows.asset',
    buildDir: 'Build/Windows',
    exeSuffix: '.exe',
  });
  assert.deepEqual(PLATFORMS.mac, {
    channel: 'mac',
    buildProfile: 'Assets/Settings/Build Profiles/MacOS.asset',
    buildDir: 'Build/MacOS',
    exeSuffix: '.app',
  });
  assert.deepEqual(PLATFORMS.webgl, {
    channel: 'web',
    buildProfile: 'Assets/Settings/Build Profiles/WebGL.asset',
    buildDir: 'Build/WebGL',
    exeSuffix: null,
  });
});
```

- [ ] **Step 2: Run test to verify it fails**

Run: `npm test`
Expected: FAIL — `Cannot find module './itch-config'`

- [ ] **Step 3: Write the implementation**

Create `scripts/lib/itch-config.js`:

```js
const ITCH_USER = 'niclas-rogulski';
const ITCH_GAME = 'how-to-get-to-heaven';

const PLATFORMS = {
  windows: {
    channel: 'windows',
    buildProfile: 'Assets/Settings/Build Profiles/Windows.asset',
    buildDir: 'Build/Windows',
    exeSuffix: '.exe',
  },
  mac: {
    channel: 'mac',
    buildProfile: 'Assets/Settings/Build Profiles/MacOS.asset',
    buildDir: 'Build/MacOS',
    exeSuffix: '.app',
  },
  webgl: {
    channel: 'web',
    buildProfile: 'Assets/Settings/Build Profiles/WebGL.asset',
    buildDir: 'Build/WebGL',
    exeSuffix: null,
  },
};

module.exports = { ITCH_USER, ITCH_GAME, PLATFORMS };
```

- [ ] **Step 4: Run test to verify it passes**

Run: `npm test`
Expected: PASS — `# tests 6`, `# pass 6`

- [ ] **Step 5: Commit**

```bash
git add scripts/lib/itch-config.js scripts/lib/itch-config.test.js
git commit -m "Add itch.io/platform static configuration"
```

---

### Task 4: `scripts/lib/find-unity.js` — resolve the Unity Editor executable path

**Files:**
- Create: `scripts/lib/find-unity.js`
- Test: `scripts/lib/find-unity.test.js`

**Interfaces:**
- Produces: `resolveUnityExePath({ editorVersion: string, env?: object, platform?: string }): string`
  - If `env.UNITY_EXE` is set, returns it after verifying it exists (throws if not).
  - Else computes `` `${env.ProgramFiles}\Unity\Hub\Editor\${editorVersion}\Editor\Unity.exe` `` (falling back to `C:\Program Files` if `env.ProgramFiles` is unset), verifies it exists, throws a clear error naming the computed path and mentioning the `UNITY_EXE` override if not.
  - Throws immediately if `platform !== 'win32'` (out of scope — this project's tooling is Windows-only).
- Consumed by `scripts/build.js` (Task 8) as `resolveUnityExePath({ editorVersion: getEditorVersion(REPO_ROOT) })`.

- [ ] **Step 1: Write the failing test**

Create `scripts/lib/find-unity.test.js`:

```js
const test = require('node:test');
const assert = require('node:assert/strict');
const { resolveUnityExePath } = require('./find-unity');

test('throws on non-Windows platform', () => {
  assert.throws(
    () => resolveUnityExePath({ editorVersion: '6000.3.8f1', platform: 'darwin' }),
    /Windows/
  );
});

test('UNITY_EXE env override is used when it points at an existing file', () => {
  // __filename always exists - stands in for a real Unity.exe for this test
  const result = resolveUnityExePath({
    editorVersion: '6000.3.8f1',
    env: { UNITY_EXE: __filename },
    platform: 'win32',
  });
  assert.equal(result, __filename);
});

test('throws a clear error when UNITY_EXE override does not exist', () => {
  assert.throws(
    () => resolveUnityExePath({
      editorVersion: '6000.3.8f1',
      env: { UNITY_EXE: 'Z:\\nonexistent\\Unity.exe' },
      platform: 'win32',
    }),
    /UNITY_EXE/
  );
});

test('resolves the real installed Unity Editor for the project editor version', () => {
  const result = resolveUnityExePath({ editorVersion: '6000.3.8f1', env: process.env, platform: 'win32' });
  assert.match(result, /Unity\.exe$/);
  assert.match(result, /6000\.3\.8f1/);
});

test('throws a clear, actionable error for an editor version that is not installed', () => {
  assert.throws(
    () => resolveUnityExePath({ editorVersion: '9999.9.9f9', env: {}, platform: 'win32' }),
    /9999\.9\.9f9/
  );
});
```

- [ ] **Step 2: Run test to verify it fails**

Run: `npm test`
Expected: FAIL — `Cannot find module './find-unity'`

- [ ] **Step 3: Write the implementation**

Create `scripts/lib/find-unity.js`:

```js
const fs = require('node:fs');
const path = require('node:path');

function resolveUnityExePath({ editorVersion, env = process.env, platform = process.platform }) {
  if (platform !== 'win32') {
    throw new Error(`resolveUnityExePath only supports Windows (got platform "${platform}").`);
  }

  if (env.UNITY_EXE) {
    if (!fs.existsSync(env.UNITY_EXE)) {
      throw new Error(`UNITY_EXE is set to "${env.UNITY_EXE}" but that file does not exist.`);
    }
    return env.UNITY_EXE;
  }

  const programFiles = env.ProgramFiles || 'C:\\Program Files';
  const computedPath = path.join(programFiles, 'Unity', 'Hub', 'Editor', editorVersion, 'Editor', 'Unity.exe');
  if (!fs.existsSync(computedPath)) {
    throw new Error(
      `Could not find Unity Editor ${editorVersion} at "${computedPath}". ` +
      `Install it via Unity Hub, or set the UNITY_EXE environment variable to the correct Unity.exe path.`
    );
  }
  return computedPath;
}

module.exports = { resolveUnityExePath };
```

- [ ] **Step 4: Run test to verify it passes**

Run: `npm test`
Expected: PASS — `# tests 11`, `# pass 11`

- [ ] **Step 5: Commit**

```bash
git add scripts/lib/find-unity.js scripts/lib/find-unity.test.js
git commit -m "Add find-unity.js to resolve the installed Unity Editor executable"
```

---

### Task 5: `scripts/lib/find-butler.js` — resolve the butler executable path

**Files:**
- Create: `scripts/lib/find-butler.js`
- Test: `scripts/lib/find-butler.test.js`

**Interfaces:**
- Produces: `resolveButlerExePath({ repoRoot: string, env?: object }): string`
  - If `env.BUTLER_EXE` is set, returns it as-is (no existence check here — callers decide what to do; kept symmetrical with how it's used in Task 9).
  - Else returns `path.join(repoRoot, 'tools', 'butler', 'butler.exe')`.
  - Does **not** check existence (unlike `find-unity.js`) — `scripts/push.js` (Task 9) does that check itself so it can print the "run `npm run setup:butler`" hint alongside it.

- [ ] **Step 1: Write the failing test**

Create `scripts/lib/find-butler.test.js`:

```js
const test = require('node:test');
const assert = require('node:assert/strict');
const path = require('node:path');
const { resolveButlerExePath } = require('./find-butler');

test('BUTLER_EXE env override is returned as-is', () => {
  const result = resolveButlerExePath({ repoRoot: 'C:\\repo', env: { BUTLER_EXE: 'D:\\tools\\butler.exe' } });
  assert.equal(result, 'D:\\tools\\butler.exe');
});

test('defaults to tools/butler/butler.exe under repoRoot', () => {
  const result = resolveButlerExePath({ repoRoot: 'C:\\repo', env: {} });
  assert.equal(result, path.join('C:\\repo', 'tools', 'butler', 'butler.exe'));
});
```

- [ ] **Step 2: Run test to verify it fails**

Run: `npm test`
Expected: FAIL — `Cannot find module './find-butler'`

- [ ] **Step 3: Write the implementation**

Create `scripts/lib/find-butler.js`:

```js
const path = require('node:path');

function resolveButlerExePath({ repoRoot, env = process.env }) {
  if (env.BUTLER_EXE) {
    return env.BUTLER_EXE;
  }
  return path.join(repoRoot, 'tools', 'butler', 'butler.exe');
}

module.exports = { resolveButlerExePath };
```

- [ ] **Step 4: Run test to verify it passes**

Run: `npm test`
Expected: PASS — `# tests 13`, `# pass 13`

- [ ] **Step 5: Commit**

```bash
git add scripts/lib/find-butler.js scripts/lib/find-butler.test.js
git commit -m "Add find-butler.js to resolve the butler executable path"
```

---

### Task 6: `scripts/lib/build-args.js` — pure Unity build path/arg builders

**Files:**
- Create: `scripts/lib/build-args.js`
- Test: `scripts/lib/build-args.test.js`

**Interfaces:**
- Produces:
  - `buildOutputRelPath(platformCfg: PLATFORMS[key], productName: string): string` — e.g. for `windows` + `"How to Get to Heaven"` returns `'Build/Windows/How to Get to Heaven.exe'`; for `webgl` (where `exeSuffix` is `null`) returns just `platformCfg.buildDir` (`'Build/WebGL'`).
  - `buildUnityArgs({ repoRoot: string, buildProfileAbsPath: string, outputAbsPath: string, logFileAbsPath: string }): string[]` — returns the argv array `['-batchmode', '-quit', '-nographics', '-projectPath', repoRoot, '-activeBuildProfile', buildProfileAbsPath, '-build', outputAbsPath, '-logFile', logFileAbsPath]`.
- Consumed by `scripts/build.js` (Task 8), which computes the absolute paths (via `path.join(REPO_ROOT, ...)`) before calling `buildUnityArgs`.

- [ ] **Step 1: Write the failing test**

Create `scripts/lib/build-args.test.js`:

```js
const test = require('node:test');
const assert = require('node:assert/strict');
const path = require('node:path');
const { buildOutputRelPath, buildUnityArgs } = require('./build-args');
const { PLATFORMS } = require('./itch-config');

test('buildOutputRelPath appends the exe suffix for windows', () => {
  assert.equal(
    buildOutputRelPath(PLATFORMS.windows, 'How to Get to Heaven'),
    path.join('Build/Windows', 'How to Get to Heaven.exe')
  );
});

test('buildOutputRelPath appends the .app suffix for mac', () => {
  assert.equal(
    buildOutputRelPath(PLATFORMS.mac, 'How to Get to Heaven'),
    path.join('Build/MacOS', 'How to Get to Heaven.app')
  );
});

test('buildOutputRelPath returns just the build dir for webgl (no suffix)', () => {
  assert.equal(buildOutputRelPath(PLATFORMS.webgl, 'How to Get to Heaven'), 'Build/WebGL');
});

test('buildUnityArgs produces the expected argv array', () => {
  const args = buildUnityArgs({
    repoRoot: 'C:\\repo',
    buildProfileAbsPath: 'C:\\repo\\Assets\\Settings\\Build Profiles\\Windows.asset',
    outputAbsPath: 'C:\\repo\\Build\\Windows\\Game.exe',
    logFileAbsPath: 'C:\\repo\\build-windows.log',
  });
  assert.deepEqual(args, [
    '-batchmode',
    '-quit',
    '-nographics',
    '-projectPath', 'C:\\repo',
    '-activeBuildProfile', 'C:\\repo\\Assets\\Settings\\Build Profiles\\Windows.asset',
    '-build', 'C:\\repo\\Build\\Windows\\Game.exe',
    '-logFile', 'C:\\repo\\build-windows.log',
  ]);
});
```

- [ ] **Step 2: Run test to verify it fails**

Run: `npm test`
Expected: FAIL — `Cannot find module './build-args'`

- [ ] **Step 3: Write the implementation**

Create `scripts/lib/build-args.js`:

```js
const path = require('node:path');

function buildOutputRelPath(platformCfg, productName) {
  if (!platformCfg.exeSuffix) {
    return platformCfg.buildDir;
  }
  return path.join(platformCfg.buildDir, `${productName}${platformCfg.exeSuffix}`);
}

function buildUnityArgs({ repoRoot, buildProfileAbsPath, outputAbsPath, logFileAbsPath }) {
  return [
    '-batchmode',
    '-quit',
    '-nographics',
    '-projectPath', repoRoot,
    '-activeBuildProfile', buildProfileAbsPath,
    '-build', outputAbsPath,
    '-logFile', logFileAbsPath,
  ];
}

module.exports = { buildOutputRelPath, buildUnityArgs };
```

- [ ] **Step 4: Run test to verify it passes**

Run: `npm test`
Expected: PASS — `# tests 17`, `# pass 17`

- [ ] **Step 5: Commit**

```bash
git add scripts/lib/build-args.js scripts/lib/build-args.test.js
git commit -m "Add build-args.js for Unity build output path and CLI arg construction"
```

---

### Task 7: `scripts/lib/butler-args.js` — pure butler push arg builder

**Files:**
- Create: `scripts/lib/butler-args.js`
- Test: `scripts/lib/butler-args.test.js`

**Interfaces:**
- Produces: `buildButlerArgs({ buildDirAbsPath: string, itchUser: string, itchGame: string, channel: string, version: string }): string[]` — returns `['push', buildDirAbsPath, `${itchUser}/${itchGame}:${channel}`, '--userversion', version, '--ignore', '*_BurstDebugInformation_DoNotShip/**', '--ignore', '*.pdb']`.
- Consumed by `scripts/push.js` (Task 9).

- [ ] **Step 1: Write the failing test**

Create `scripts/lib/butler-args.test.js`:

```js
const test = require('node:test');
const assert = require('node:assert/strict');
const { buildButlerArgs } = require('./butler-args');

test('buildButlerArgs produces the expected argv array', () => {
  const args = buildButlerArgs({
    buildDirAbsPath: 'C:\\repo\\Build\\Windows',
    itchUser: 'niclas-rogulski',
    itchGame: 'how-to-get-to-heaven',
    channel: 'windows',
    version: '1.0',
  });
  assert.deepEqual(args, [
    'push',
    'C:\\repo\\Build\\Windows',
    'niclas-rogulski/how-to-get-to-heaven:windows',
    '--userversion', '1.0',
    '--ignore', '*_BurstDebugInformation_DoNotShip/**',
    '--ignore', '*.pdb',
  ]);
});
```

- [ ] **Step 2: Run test to verify it fails**

Run: `npm test`
Expected: FAIL — `Cannot find module './butler-args'`

- [ ] **Step 3: Write the implementation**

Create `scripts/lib/butler-args.js`:

```js
function buildButlerArgs({ buildDirAbsPath, itchUser, itchGame, channel, version }) {
  return [
    'push',
    buildDirAbsPath,
    `${itchUser}/${itchGame}:${channel}`,
    '--userversion', version,
    '--ignore', '*_BurstDebugInformation_DoNotShip/**',
    '--ignore', '*.pdb',
  ];
}

module.exports = { buildButlerArgs };
```

- [ ] **Step 4: Run test to verify it passes**

Run: `npm test`
Expected: PASS — `# tests 18`, `# pass 18`

- [ ] **Step 5: Commit**

```bash
git add scripts/lib/butler-args.js scripts/lib/butler-args.test.js
git commit -m "Add butler-args.js for butler push CLI arg construction"
```

---

### Task 8: `scripts/build.js` — build CLI entrypoint + `build:*` npm scripts

**Files:**
- Create: `scripts/build.js`
- Modify: `package.json` (add `build:windows`, `build:mac`, `build:webgl` scripts)

**Interfaces:**
- Produces: `module.exports = { main }` where `main(argv: string[]): void` reads `argv[2]` as the platform key, runs the full build flow, and calls `process.exit(code)`. Also runnable as `node scripts/build.js <platform>`.
- Consumes: `PLATFORMS` (Task 3), `getEditorVersion`/`getProductName` (Task 2), `resolveUnityExePath` (Task 4), `buildOutputRelPath`/`buildUnityArgs` (Task 6).

- [ ] **Step 1: Write the implementation**

Create `scripts/build.js`:

```js
#!/usr/bin/env node
const fs = require('node:fs');
const path = require('node:path');
const { spawnSync } = require('node:child_process');
const { getEditorVersion, getProductName } = require('./lib/project-info');
const { resolveUnityExePath } = require('./lib/find-unity');
const { PLATFORMS } = require('./lib/itch-config');
const { buildOutputRelPath, buildUnityArgs } = require('./lib/build-args');

const REPO_ROOT = path.resolve(__dirname, '..');

function main(argv) {
  const platformKey = argv[2];
  const platformCfg = PLATFORMS[platformKey];
  if (!platformCfg) {
    console.error(`Unknown platform "${platformKey}". Expected one of: ${Object.keys(PLATFORMS).join(', ')}`);
    process.exit(1);
    return;
  }

  const buildProfileAbsPath = path.join(REPO_ROOT, platformCfg.buildProfile);
  if (!fs.existsSync(buildProfileAbsPath)) {
    console.error(
      `Build profile not found: ${platformCfg.buildProfile}\n` +
      `Create it once via File > Build Profiles in the Unity Editor (see RELEASING.md).`
    );
    process.exit(1);
    return;
  }

  let unityExe;
  try {
    unityExe = resolveUnityExePath({ editorVersion: getEditorVersion(REPO_ROOT) });
  } catch (err) {
    console.error(err.message);
    process.exit(1);
    return;
  }

  const productName = getProductName(REPO_ROOT);
  const outputAbsPath = path.join(REPO_ROOT, buildOutputRelPath(platformCfg, productName));
  const logFileAbsPath = path.join(REPO_ROOT, `build-${platformKey}.log`);

  const args = buildUnityArgs({ repoRoot: REPO_ROOT, buildProfileAbsPath, outputAbsPath, logFileAbsPath });

  console.log(`Building "${platformKey}" -> ${outputAbsPath}`);
  console.log(`Unity:   ${unityExe}`);
  console.log(`Log:     ${logFileAbsPath}`);

  const result = spawnSync(unityExe, args, { stdio: 'inherit' });
  if (result.error) {
    console.error(`Failed to launch Unity: ${result.error.message}`);
    process.exit(1);
    return;
  }
  process.exit(result.status === null ? 1 : result.status);
}

if (require.main === module) {
  main(process.argv);
}

module.exports = { main };
```

- [ ] **Step 2: Add npm scripts**

In `package.json`, add to `"scripts"`:

```json
    "build:windows": "node scripts/build.js windows",
    "build:mac": "node scripts/build.js mac",
    "build:webgl": "node scripts/build.js webgl",
```

- [ ] **Step 3: Verify the unknown-platform error path**

Run: `node scripts/build.js bogus`
Expected: prints `Unknown platform "bogus". Expected one of: windows, mac, webgl` to stderr, exits with code 1.

- [ ] **Step 4: Verify the missing-build-profile error path (real, since no profile assets exist yet)**

Run: `npm run build:windows`
Expected: prints `Build profile not found: Assets/Settings/Build Profiles/Windows.asset` and the `RELEASING.md` hint to stderr, exits with code 1. (This is expected and correct at this point in the plan — the Build Profile assets are created manually per Task 11/RELEASING.md, not by this plan.)

- [ ] **Step 5: Commit**

```bash
git add scripts/build.js package.json
git commit -m "Add build.js CLI and build:* npm scripts for headless Unity builds"
```

---

### Task 9: `scripts/push.js` — push CLI entrypoint + `push:*`/`release:*` npm scripts

**Files:**
- Create: `scripts/push.js`
- Modify: `package.json` (add `push:windows`, `push:mac`, `push:webgl`, `release:windows`, `release:mac`, `release:webgl` scripts)

**Interfaces:**
- Produces: `module.exports = { main }` where `main(argv: string[]): void` reads `argv[2]` as the platform key and runs the full push flow. Also runnable as `node scripts/push.js <platform>`.
- Consumes: `PLATFORMS`/`ITCH_USER`/`ITCH_GAME` (Task 3), `getBundleVersion` (Task 2), `resolveButlerExePath` (Task 5), `buildButlerArgs` (Task 7).

- [ ] **Step 1: Write the implementation**

Create `scripts/push.js`:

```js
#!/usr/bin/env node
const fs = require('node:fs');
const path = require('node:path');
const { spawnSync } = require('node:child_process');
const { getBundleVersion } = require('./lib/project-info');
const { resolveButlerExePath } = require('./lib/find-butler');
const { PLATFORMS, ITCH_USER, ITCH_GAME } = require('./lib/itch-config');
const { buildButlerArgs } = require('./lib/butler-args');

const REPO_ROOT = path.resolve(__dirname, '..');

function main(argv) {
  const platformKey = argv[2];
  const platformCfg = PLATFORMS[platformKey];
  if (!platformCfg) {
    console.error(`Unknown platform "${platformKey}". Expected one of: ${Object.keys(PLATFORMS).join(', ')}`);
    process.exit(1);
    return;
  }

  const buildDirAbsPath = path.join(REPO_ROOT, platformCfg.buildDir);
  if (!fs.existsSync(buildDirAbsPath) || fs.readdirSync(buildDirAbsPath).length === 0) {
    console.error(
      `Build folder missing or empty: ${platformCfg.buildDir}\n` +
      `Run "npm run build:${platformKey}" first.`
    );
    process.exit(1);
    return;
  }

  const butlerExe = resolveButlerExePath({ repoRoot: REPO_ROOT });
  if (!fs.existsSync(butlerExe)) {
    console.error(`butler.exe not found at "${butlerExe}". Run "npm run setup:butler" first.`);
    process.exit(1);
    return;
  }

  const version = getBundleVersion(REPO_ROOT);
  const args = buildButlerArgs({
    buildDirAbsPath,
    itchUser: ITCH_USER,
    itchGame: ITCH_GAME,
    channel: platformCfg.channel,
    version,
  });

  console.log(`Pushing "${platformKey}" (v${version}) -> ${ITCH_USER}/${ITCH_GAME}:${platformCfg.channel}`);

  const result = spawnSync(butlerExe, args, { stdio: 'inherit' });
  if (result.error) {
    console.error(`Failed to launch butler: ${result.error.message}`);
    process.exit(1);
    return;
  }
  process.exit(result.status === null ? 1 : result.status);
}

if (require.main === module) {
  main(process.argv);
}

module.exports = { main };
```

- [ ] **Step 2: Add npm scripts**

In `package.json`, add to `"scripts"`:

```json
    "push:windows": "node scripts/push.js windows",
    "push:mac": "node scripts/push.js mac",
    "push:webgl": "node scripts/push.js webgl",
    "release:windows": "npm run build:windows && npm run push:windows",
    "release:mac": "npm run build:mac && npm run push:mac",
    "release:webgl": "npm run build:webgl && npm run push:webgl",
```

- [ ] **Step 3: Verify the unknown-platform error path**

Run: `node scripts/push.js bogus`
Expected: prints `Unknown platform "bogus". Expected one of: windows, mac, webgl` to stderr, exits with code 1.

- [ ] **Step 4: Verify the missing-build-folder error path (real — Build/MacOS and Build/WebGL don't exist yet)**

Run: `npm run push:mac`
Expected: prints `Build folder missing or empty: Build/MacOS` and the `npm run build:mac` hint, exits with code 1.

Run: `npm run push:webgl`
Expected: same shape of error for `Build/WebGL`.

- [ ] **Step 5: Verify the missing-butler error path (real — `Build/Windows` already exists and is non-empty in this repo, so this exercises the next guard)**

Run: `npm run push:windows`
Expected: passes the build-folder check, then prints `butler.exe not found at "...tools\butler\butler.exe". Run "npm run setup:butler" first.`, exits with code 1.

- [ ] **Step 6: Commit**

```bash
git add scripts/push.js package.json
git commit -m "Add push.js CLI and push:*/release:* npm scripts for butler pushes"
```

---

### Task 10: `scripts/setup-butler.js` — one-time butler install + login

**Files:**
- Create: `scripts/setup-butler.js`
- Modify: `package.json` (add `setup:butler` script)

**Interfaces:**
- Produces: `module.exports = { main, BUTLER_DOWNLOAD_URL }`. `main(): Promise<void>` downloads butler, extracts it, and runs `butler login` interactively.
- This task's "run for real" step requires the user's own browser/itch.io credentials for the `butler login` OAuth step — do not run `npm run setup:butler` unattended as the implementing agent. Verify the script's logic by reading it and running the download+extract portion only if you want to confirm it end-to-end; leave the interactive login to the user.

- [ ] **Step 1: Write the implementation**

Create `scripts/setup-butler.js`:

```js
#!/usr/bin/env node
const fs = require('node:fs');
const path = require('node:path');
const https = require('node:https');
const { spawnSync } = require('node:child_process');

const BUTLER_DOWNLOAD_URL = 'https://broth.itch.zone/butler/windows-amd64/LATEST/archive/default';
const REPO_ROOT = path.resolve(__dirname, '..');
const TOOLS_DIR = path.join(REPO_ROOT, 'tools', 'butler');
const ZIP_PATH = path.join(REPO_ROOT, 'tools', 'butler.zip');

function download(url, destPath) {
  return new Promise((resolve, reject) => {
    const file = fs.createWriteStream(destPath);
    https.get(url, (response) => {
      if (response.statusCode >= 300 && response.statusCode < 400 && response.headers.location) {
        file.close();
        fs.unlink(destPath, () => {
          download(response.headers.location, destPath).then(resolve, reject);
        });
        return;
      }
      if (response.statusCode !== 200) {
        reject(new Error(`Download failed: HTTP ${response.statusCode} from ${url}`));
        return;
      }
      response.pipe(file);
      file.on('finish', () => file.close(() => resolve()));
    }).on('error', reject);
  });
}

async function main() {
  fs.mkdirSync(TOOLS_DIR, { recursive: true });

  console.log(`Downloading butler from ${BUTLER_DOWNLOAD_URL} ...`);
  await download(BUTLER_DOWNLOAD_URL, ZIP_PATH);

  console.log('Extracting butler...');
  const extract = spawnSync(
    'powershell.exe',
    ['-NoProfile', '-Command', `Expand-Archive -Path "${ZIP_PATH}" -DestinationPath "${TOOLS_DIR}" -Force`],
    { stdio: 'inherit' }
  );
  if (extract.status !== 0) {
    console.error('Extraction failed.');
    process.exit(1);
    return;
  }
  fs.unlinkSync(ZIP_PATH);

  console.log('Logging in to itch.io (a browser window will open to authorize)...');
  const login = spawnSync(path.join(TOOLS_DIR, 'butler.exe'), ['login'], { stdio: 'inherit' });
  process.exit(login.status === null ? 1 : login.status);
}

if (require.main === module) {
  main();
}

module.exports = { main, BUTLER_DOWNLOAD_URL };
```

- [ ] **Step 2: Add npm script**

In `package.json`, add to `"scripts"`:

```json
    "setup:butler": "node scripts/setup-butler.js",
```

- [ ] **Step 3: Hand off the real run to the user**

Tell the user to run `npm run setup:butler` themselves once, in their own terminal, so they can complete the `butler login` browser authorization with their own itch.io account.

- [ ] **Step 4: Commit**

```bash
git add scripts/setup-butler.js package.json
git commit -m "Add setup-butler.js for one-time butler install and login"
```

---

### Task 11: `RELEASING.md` — manual setup + everyday usage docs

**Files:**
- Create: `RELEASING.md`

- [ ] **Step 1: Write the doc**

Create `RELEASING.md`:

```markdown
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
```

- [ ] **Step 2: Verify**

Read through `RELEASING.md` and confirm every command and path it references matches the actual scripts/`package.json` (`npm run build:windows`, `push:windows`, `release:windows`, `setup:butler`, the three Build Profile asset paths, the `UNITY_EXE`/`BUTLER_EXE` env vars).

- [ ] **Step 3: Commit**

```bash
git add RELEASING.md
git commit -m "Add RELEASING.md documenting one-time setup and everyday release commands"
```

---

## Post-plan manual steps (for the user, not the implementing agent)

1. Create the three Build Profile assets in the Unity Editor (RELEASING.md step 1).
2. Run `npm run setup:butler` yourself to authorize butler with your itch.io account.
3. Run a real `npm run build:windows` and `npm run push:windows` to confirm the end-to-end flow against the live Unity Editor and itch.io project once the profiles exist.
