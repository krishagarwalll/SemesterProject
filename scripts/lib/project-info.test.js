const test = require('node:test');
const assert = require('node:assert/strict');
const path = require('node:path');
const { getBundleVersion, getProductName, getEditorVersion } = require('./project-info');

const REPO_ROOT = path.resolve(__dirname, '..', '..');

test('getBundleVersion reads bundleVersion from ProjectSettings.asset', () => {
  assert.equal(getBundleVersion(REPO_ROOT), '1.0');
});

test('getProductName reads productName from ProjectSettings.asset', () => {
  assert.equal(getProductName(REPO_ROOT), 'How to Get to Heaven');
});

test('getEditorVersion reads m_EditorVersion from ProjectVersion.txt', () => {
  assert.equal(getEditorVersion(REPO_ROOT), '6000.3.8f1');
});

test('getBundleVersion throws a clear error when the file is missing', () => {
  assert.throws(
    () => getBundleVersion(path.join(REPO_ROOT, 'does-not-exist')),
    /ProjectSettings\.asset/
  );
});
