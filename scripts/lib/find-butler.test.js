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
