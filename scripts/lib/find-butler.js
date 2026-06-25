const path = require('node:path');

function resolveButlerExePath({ repoRoot, env = process.env }) {
  if (env.BUTLER_EXE) {
    return env.BUTLER_EXE;
  }
  return path.join(repoRoot, 'tools', 'butler', 'butler.exe');
}

module.exports = { resolveButlerExePath };
