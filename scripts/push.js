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
