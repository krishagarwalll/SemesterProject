const test = require('node:test');
const assert = require('node:assert/strict');
const { resolveUnityExePath } = require('./find-unity');

test('UNITY_EXE env override is used when it points at an existing file', () => {
  const result = resolveUnityExePath({
    editorVersion: '6000.3.8f1',
    env: { UNITY_EXE: '/custom/Unity' },
    existsSync: filePath => filePath === '/custom/Unity',
  });
  assert.equal(result, '/custom/Unity');
});

test('throws a clear error when UNITY_EXE override does not exist', () => {
  assert.throws(
    () => resolveUnityExePath({
      editorVersion: '6000.3.8f1',
      env: { UNITY_EXE: 'Z:\\nonexistent\\Unity.exe' },
      platform: 'win32',
      existsSync: () => false,
    }),
    /UNITY_EXE/
  );
});

test('resolves the Unity Hub editor path on Windows', () => {
  const expected = 'C:\\Program Files\\Unity\\Hub\\Editor\\6000.3.8f1\\Editor\\Unity.exe';
  const result = resolveUnityExePath({
    editorVersion: '6000.3.8f1',
    env: {},
    platform: 'win32',
    existsSync: filePath => filePath === expected,
  });
  assert.equal(result, expected);
});

test('resolves the Unity Hub editor path on macOS', () => {
  const expected = '/Applications/Unity/Hub/Editor/6000.3.8f1/Unity.app/Contents/MacOS/Unity';
  const result = resolveUnityExePath({
    editorVersion: '6000.3.8f1',
    env: {},
    platform: 'darwin',
    existsSync: filePath => filePath === expected,
  });
  assert.equal(result, expected);
});

test('resolves the Unity Hub editor path on Linux', () => {
  const expected = '/home/niclas/Unity/Hub/Editor/6000.3.8f1/Editor/Unity';
  const result = resolveUnityExePath({
    editorVersion: '6000.3.8f1',
    env: {},
    platform: 'linux',
    homeDir: '/home/niclas',
    existsSync: filePath => filePath === expected,
  });
  assert.equal(result, expected);
});

test('throws a clear, actionable error for an editor version that is not installed', () => {
  assert.throws(
    () => resolveUnityExePath({
      editorVersion: '9999.9.9f9',
      env: {},
      platform: 'darwin',
      existsSync: () => false,
    }),
    /9999\.9\.9f9/
  );
});

test('throws a clear error for unsupported platforms', () => {
  assert.throws(
    () => resolveUnityExePath({
      editorVersion: '6000.3.8f1',
      env: {},
      platform: 'freebsd',
    }),
    /Unsupported platform "freebsd"/
  );
});
