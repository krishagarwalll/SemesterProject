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
