#!/usr/bin/env node
const fs = require('node:fs');
const path = require('node:path');
const https = require('node:https');
const { spawnSync } = require('node:child_process');
const { resolveButlerExePath } = require('./lib/find-butler');

const BROTH_BASE_URL = 'https://broth.itch.zone/butler';
const REPO_ROOT = path.resolve(__dirname, '..');
const TOOLS_DIR = path.join(REPO_ROOT, 'tools', 'butler');
const ZIP_PATH = path.join(REPO_ROOT, 'tools', 'butler.zip');

function resolveButlerChannel({ platform = process.platform, arch = process.arch } = {}) {
  if (platform === 'win32') {
    return 'windows-amd64';
  }

  const brothArch = arch === 'x64' ? 'amd64' : arch;
  if ((platform === 'darwin' || platform === 'linux') && (brothArch === 'amd64' || brothArch === 'arm64')) {
    return `${platform}-${brothArch}`;
  }

  throw new Error(`Unsupported butler platform "${platform}" with architecture "${arch}".`);
}

function resolveButlerDownloadUrl(options = {}) {
  return `${BROTH_BASE_URL}/${resolveButlerChannel(options)}/LATEST/archive/default`;
}

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

function extractButler({ zipPath = ZIP_PATH, toolsDir = TOOLS_DIR, platform = process.platform } = {}) {
  if (platform === 'win32') {
    return spawnSync(
      'powershell.exe',
      ['-NoProfile', '-Command', `Expand-Archive -Path "${zipPath}" -DestinationPath "${toolsDir}" -Force`],
      { stdio: 'inherit' }
    );
  }

  return spawnSync('unzip', ['-o', zipPath, '-d', toolsDir], { stdio: 'inherit' });
}

async function main({ platform = process.platform, arch = process.arch } = {}) {
  fs.mkdirSync(TOOLS_DIR, { recursive: true });

  const downloadUrl = resolveButlerDownloadUrl({ platform, arch });
  console.log(`Downloading butler from ${downloadUrl} ...`);
  await download(downloadUrl, ZIP_PATH);

  console.log('Extracting butler...');
  const extract = extractButler({ platform });
  if (extract.status !== 0) {
    console.error('Extraction failed.');
    process.exit(1);
    return;
  }
  fs.unlinkSync(ZIP_PATH);

  const butlerExe = resolveButlerExePath({ repoRoot: REPO_ROOT, platform });
  if (platform !== 'win32') {
    fs.chmodSync(butlerExe, 0o755);
  }

  console.log('Logging in to itch.io (a browser window will open to authorize)...');
  const login = spawnSync(butlerExe, ['login'], { stdio: 'inherit' });
  process.exit(login.status === null ? 1 : login.status);
}

if (require.main === module) {
  main();
}

module.exports = {
  main,
  resolveButlerChannel,
  resolveButlerDownloadUrl,
  BROTH_BASE_URL,
};
