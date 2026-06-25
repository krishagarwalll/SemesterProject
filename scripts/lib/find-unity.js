const fs = require('node:fs');
const path = require('node:path');

function resolveUnityExePath({ editorVersion, env = process.env, platform = process.platform }) {
  if (platform !== 'win32') {
    throw new Error(`resolveUnityExePath only supports Windows (got platform "${platform}").`);
  }

  if (env.UNITY_EXE) {
    if (!fs.existsSync(env.UNITY_EXE)) {
      throw new Error(`UNITY_EXE is set to "${env.UNITY_EXE}" but that file does not exist.`);
    }
    return env.UNITY_EXE;
  }

  const programFiles = env.ProgramFiles || 'C:\\Program Files';
  const computedPath = path.join(programFiles, 'Unity', 'Hub', 'Editor', editorVersion, 'Editor', 'Unity.exe');
  if (!fs.existsSync(computedPath)) {
    throw new Error(
      `Could not find Unity Editor ${editorVersion} at "${computedPath}". ` +
      `Install it via Unity Hub, or set the UNITY_EXE environment variable to the correct Unity.exe path.`
    );
  }
  return computedPath;
}

module.exports = { resolveUnityExePath };
