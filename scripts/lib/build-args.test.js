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
