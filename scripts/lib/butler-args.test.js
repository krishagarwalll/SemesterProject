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
