const path = require('node:path');

function buildOutputRelPath(platformCfg, productName) {
  if (!platformCfg.exeSuffix) {
    return platformCfg.buildDir;
  }
  return path.join(platformCfg.buildDir, `${productName}${platformCfg.exeSuffix}`);
}

function buildUnityArgs({ repoRoot, buildProfileRelPath, outputAbsPath, logFileAbsPath }) {
  return [
    '-batchmode',
    '-quit',
    '-nographics',
    '-projectPath', repoRoot,
    '-activeBuildProfile', buildProfileRelPath,
    '-build', outputAbsPath,
    '-logFile', logFileAbsPath,
  ];
}

module.exports = { buildOutputRelPath, buildUnityArgs };
