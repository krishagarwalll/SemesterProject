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
