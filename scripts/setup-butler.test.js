const test = require('node:test');
const assert = require('node:assert/strict');
const {
  resolveButlerChannel,
  resolveButlerDownloadUrl,
} = require('./setup-butler');

test('resolves stable butler channel for macOS Apple Silicon', () => {
  assert.equal(resolveButlerChannel({ platform: 'darwin', arch: 'arm64' }), 'darwin-arm64');
});

test('resolves stable butler channel for macOS Intel', () => {
  assert.equal(resolveButlerChannel({ platform: 'darwin', arch: 'x64' }), 'darwin-amd64');
});

test('resolves stable butler channel for Windows', () => {
  assert.equal(resolveButlerChannel({ platform: 'win32', arch: 'x64' }), 'windows-amd64');
});

test('resolves stable butler channel for Linux', () => {
  assert.equal(resolveButlerChannel({ platform: 'linux', arch: 'x64' }), 'linux-amd64');
});

test('builds the official broth download URL', () => {
  assert.equal(
    resolveButlerDownloadUrl({ platform: 'darwin', arch: 'arm64' }),
    'https://broth.itch.zone/butler/darwin-arm64/LATEST/archive/default'
  );
});

test('throws for unsupported butler platforms', () => {
  assert.throws(
    () => resolveButlerChannel({ platform: 'freebsd', arch: 'x64' }),
    /Unsupported butler platform/
  );
});
