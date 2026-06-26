const fs = require('node:fs');
const os = require('node:os');
const path = require('node:path');

function unityHubEditorPath({ editorVersion, env, platform, homeDir }) {
  if (env.UNITY_EXE) {
    return env.UNITY_EXE;
  }

  if (platform === 'win32') {
    const programFiles = env.ProgramFiles || 'C:\\Program Files';
    return path.win32.join(programFiles, 'Unity', 'Hub', 'Editor', editorVersion, 'Editor', 'Unity.exe');
  }

  if (platform === 'darwin') {
    return path.posix.join(
      '/Applications',
      'Unity',
      'Hub',
      'Editor',
      editorVersion,
      'Unity.app',
      'Contents',
      'MacOS',
      'Unity'
    );
  }

  if (platform === 'linux') {
    return path.posix.join(homeDir, 'Unity', 'Hub', 'Editor', editorVersion, 'Editor', 'Unity');
  }

  throw new Error(`Unsupported platform "${platform}". Set UNITY_EXE to the Unity executable path.`);
}

function resolveUnityExePath({
  editorVersion,
  env = process.env,
  platform = process.platform,
  homeDir = os.homedir(),
  existsSync = fs.existsSync,
}) {
  const computedPath = unityHubEditorPath({ editorVersion, env, platform, homeDir });
  if (!existsSync(computedPath)) {
    if (env.UNITY_EXE) {
      throw new Error(`UNITY_EXE is set to "${env.UNITY_EXE}" but that file does not exist.`);
    }

    throw new Error(
      `Could not find Unity Editor ${editorVersion} at "${computedPath}". ` +
      `Install it via Unity Hub, or set the UNITY_EXE environment variable to the correct Unity executable path.`
    );
  }
  return computedPath;
}

module.exports = { resolveUnityExePath };
