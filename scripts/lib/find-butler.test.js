const test = require('node:test');
const assert = require('node:assert/strict');
const path = require('node:path');
const { resolveButlerExePath } = require('./find-butler');

test('BUTLER_EXE env override is returned as-is', () => {
  const result = resolveButlerExePath({ repoRoot: 'C:\\repo', env: { BUTLER_EXE: 'D:\\tools\\butler.exe' } });
  assert.equal(result, 'D:\\tools\\butler.exe');
});

test('defaults to tools/butler/butler.exe under repoRoot on Windows', () => {
  const result = resolveButlerExePath({ repoRoot: 'C:\\repo', env: {}, platform: 'win32' });
  assert.equal(result, path.join('C:\\repo', 'tools', 'butler', 'butler.exe'));
});

test('defaults to tools/butler/butler under repoRoot on macOS and Linux', () => {
  const macResult = resolveButlerExePath({ repoRoot: '/repo', env: {}, platform: 'darwin' });
  const linuxResult = resolveButlerExePath({ repoRoot: '/repo', env: {}, platform: 'linux' });

  assert.equal(macResult, path.join('/repo', 'tools', 'butler', 'butler'));
  assert.equal(linuxResult, path.join('/repo', 'tools', 'butler', 'butler'));
});
