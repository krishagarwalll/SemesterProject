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

  const lockFilePath = path.join(REPO_ROOT, 'Temp', 'UnityLockfile');
  if (fs.existsSync(lockFilePath)) {
    try {
      fs.closeSync(fs.openSync(lockFilePath, 'r+'));
    } catch (err) {
      console.error(
        `This project is already open in another Unity Editor instance (${lockFilePath} is locked).\n` +
        `Close it before running a headless build, or this will hang waiting on an invisible dialog.`
      );
      process.exit(1);
      return;
    }
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

  const args = buildUnityArgs({
    repoRoot: REPO_ROOT,
    buildProfileRelPath: platformCfg.buildProfile,
    outputAbsPath,
    logFileAbsPath,
  });

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
