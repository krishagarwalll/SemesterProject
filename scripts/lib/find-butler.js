const path = require('node:path');

function resolveButlerExePath({ repoRoot, env = process.env, platform = process.platform }) {
  if (env.BUTLER_EXE) {
    return env.BUTLER_EXE;
  }

  const executableName = platform === 'win32' ? 'butler.exe' : 'butler';
  return path.join(repoRoot, 'tools', 'butler', executableName);
}

module.exports = { resolveButlerExePath };
