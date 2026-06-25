const test = require('node:test');
const assert = require('node:assert/strict');
const { resolveUnityExePath } = require('./find-unity');

test('throws on non-Windows platform', () => {
  assert.throws(
    () => resolveUnityExePath({ editorVersion: '6000.3.8f1', platform: 'darwin' }),
    /Windows/
  );
});

test('UNITY_EXE env override is used when it points at an existing file', () => {
  // __filename always exists - stands in for a real Unity.exe for this test
  const result = resolveUnityExePath({
    editorVersion: '6000.3.8f1',
    env: { UNITY_EXE: __filename },
    platform: 'win32',
  });
  assert.equal(result, __filename);
});

test('throws a clear error when UNITY_EXE override does not exist', () => {
  assert.throws(
    () => resolveUnityExePath({
      editorVersion: '6000.3.8f1',
      env: { UNITY_EXE: 'Z:\\nonexistent\\Unity.exe' },
      platform: 'win32',
    }),
    /UNITY_EXE/
  );
});

test('resolves the real installed Unity Editor for the project editor version', () => {
  const result = resolveUnityExePath({ editorVersion: '6000.3.8f1', env: process.env, platform: 'win32' });
  assert.match(result, /Unity\.exe$/);
  assert.match(result, /6000\.3\.8f1/);
});

test('throws a clear, actionable error for an editor version that is not installed', () => {
  assert.throws(
    () => resolveUnityExePath({ editorVersion: '9999.9.9f9', env: {}, platform: 'win32' }),
    /9999\.9\.9f9/
  );
});
