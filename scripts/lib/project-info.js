const fs = require('node:fs');
const path = require('node:path');

function readProjectSettingsField(repoRoot, fieldName) {
  const filePath = path.join(repoRoot, 'ProjectSettings', 'ProjectSettings.asset');
  let contents;
  try {
    contents = fs.readFileSync(filePath, 'utf8');
  } catch (err) {
    throw new Error(`Could not read ${filePath}: ${err.message}`);
  }
  const match = contents.match(new RegExp(`^\\s*${fieldName}:\\s*(.+)\\s*$`, 'm'));
  if (!match) {
    throw new Error(`Could not find field "${fieldName}" in ${filePath}`);
  }
  return match[1].trim();
}

function getBundleVersion(repoRoot) {
  return readProjectSettingsField(repoRoot, 'bundleVersion');
}

function getProductName(repoRoot) {
  return readProjectSettingsField(repoRoot, 'productName');
}

function getEditorVersion(repoRoot) {
  const filePath = path.join(repoRoot, 'ProjectSettings', 'ProjectVersion.txt');
  let contents;
  try {
    contents = fs.readFileSync(filePath, 'utf8');
  } catch (err) {
    throw new Error(`Could not read ${filePath}: ${err.message}`);
  }
  const match = contents.match(/^m_EditorVersion:\s*(.+)\s*$/m);
  if (!match) {
    throw new Error(`Could not find field "m_EditorVersion" in ${filePath}`);
  }
  return match[1].trim();
}

module.exports = { getBundleVersion, getProductName, getEditorVersion };
